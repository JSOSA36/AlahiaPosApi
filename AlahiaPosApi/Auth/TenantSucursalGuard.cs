using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AlahiaPosApi.Auth
{
    /// <summary>
    /// Si el cliente envía IdSucursal &gt; 0 distinto al de la sesión → Forbidden.
    /// Si envía 0/null → se sustituye por la sucursal autenticada.
    /// Si la sesión aún no tiene sucursal (compatibilidad), no bloquea.
    /// </summary>
    public static class TenantSucursalGuard
    {
        public static TenantBindResult Apply(
            IDictionary<string, object> arguments,
            int idSucursalSesion,
            bool omitir)
        {
            if (omitir || idSucursalSesion <= 0)
                return new TenantBindResult();

            var ajustes = 0;
            foreach (var key in arguments.Keys.ToList())
            {
                var value = arguments[key];
                var r = ApplyValue(key, value, idSucursalSesion, new HashSet<object>(ReferenceEqualityComparer.Instance));
                if (r.Forbidden)
                    return new TenantBindResult { Forbidden = true };
                if (r.Value != null && !ReferenceEquals(r.Value, value))
                    arguments[key] = r.Value;
                ajustes += r.Ajustes;
            }

            return new TenantBindResult { Ajustes = ajustes };
        }

        private readonly struct NodeResult
        {
            public bool Forbidden { get; init; }
            public int Ajustes { get; init; }
            public object? Value { get; init; }
        }

        private static NodeResult ApplyValue(
            string? name,
            object? value,
            int idSucursalSesion,
            HashSet<object> seen)
        {
            if (value == null)
            {
                if (IsSucursalIdName(name))
                    return new NodeResult { Ajustes = 1, Value = idSucursalSesion };
                return new NodeResult { Value = value };
            }

            if (IsSucursalIdName(name) && TryReadInt(value, out var idFromName))
            {
                var check = CheckId(idFromName, idSucursalSesion);
                if (check.Forbidden) return new NodeResult { Forbidden = true };
                if (check.Ajustes > 0 && value is int)
                    return new NodeResult { Ajustes = 1, Value = idSucursalSesion };
                return new NodeResult { Ajustes = check.Ajustes, Value = value };
            }

            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
                || type == typeof(DateTime) || type == typeof(Guid) || type == typeof(byte[]))
                return new NodeResult { Value = value };

            if (!seen.Add(value))
                return new NodeResult { Value = value };

            if (value is IDictionary dict)
            {
                var ajustes = 0;
                foreach (var k in dict.Keys)
                {
                    var inner = ApplyValue(k?.ToString(), dict[k], idSucursalSesion, seen);
                    if (inner.Forbidden) return inner;
                    ajustes += inner.Ajustes;
                }
                return new NodeResult { Ajustes = ajustes, Value = value };
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                var ajustes = 0;
                foreach (var item in enumerable)
                {
                    var inner = ApplyValue(null, item, idSucursalSesion, seen);
                    if (inner.Forbidden) return inner;
                    ajustes += inner.Ajustes;
                }
                return new NodeResult { Ajustes = ajustes, Value = value };
            }

            if (type.Namespace != null &&
                (type.Namespace.StartsWith("System", StringComparison.Ordinal)
                 || type.Namespace.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)))
                return new NodeResult { Value = value };

            var total = 0;
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!prop.CanRead) continue;
                object? current;
                try { current = prop.GetValue(value); }
                catch { continue; }

                if (IsSucursalIdName(prop.Name) && prop.CanWrite && TryReadInt(current, out var idProp))
                {
                    var check = CheckId(idProp, idSucursalSesion);
                    if (check.Forbidden) return new NodeResult { Forbidden = true };
                    if (check.Ajustes > 0)
                    {
                        try
                        {
                            if (prop.PropertyType == typeof(int?))
                                prop.SetValue(value, (int?)idSucursalSesion);
                            else if (prop.PropertyType == typeof(int))
                                prop.SetValue(value, idSucursalSesion);
                            total++;
                        }
                        catch { /* propiedad no writable en la práctica */ }
                    }
                    continue;
                }

                var nested = ApplyValue(prop.Name, current, idSucursalSesion, seen);
                if (nested.Forbidden) return nested;
                total += nested.Ajustes;
            }

            return new NodeResult { Ajustes = total, Value = value };
        }

        public static TenantBindResult CheckId(int incoming, int idSucursalSesion)
        {
            if (idSucursalSesion <= 0)
                return new TenantBindResult();

            if (incoming <= 0)
                return new TenantBindResult { Ajustes = 1 };

            if (incoming == idSucursalSesion)
                return new TenantBindResult();

            return new TenantBindResult { Forbidden = true };
        }

        public static bool IsSucursalIdName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var n = name.Replace("_", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
            return n.Equals("IdSucursal", StringComparison.OrdinalIgnoreCase)
                || n.Equals("SucursalId", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryReadInt(object? value, out int id)
        {
            id = 0;
            if (value == null) return false;
            if (value is int i) { id = i; return true; }
            if (value is long l && l <= int.MaxValue) { id = (int)l; return true; }
            return int.TryParse(value.ToString(), out id);
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new();
            bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);
            int IEqualityComparer<object>.GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
