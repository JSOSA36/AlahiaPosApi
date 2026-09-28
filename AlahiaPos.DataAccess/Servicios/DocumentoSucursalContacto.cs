using System;
using System.Collections.Generic;
using System.Linq;
using AlahiaPos.Entities.Domain;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Contacto del emisor en documentos impresos: sucursal si existe, si no la empresa.
    /// El XML e-CF sigue usando solo datos de empresa.
    /// </summary>
    public static class DocumentoSucursalContacto
    {
        public static string? Nombre(Sucursal? sucursal)
        {
            var nombre = sucursal?.Nombre?.Trim();
            return string.IsNullOrWhiteSpace(nombre) ? null : nombre;
        }

        public static string Telefono(Sucursal? sucursal, Empresas? empresa)
            => PrimeroNoVacio(sucursal?.Telefono, empresa?.Telefono) ?? "";

        public static string FormatearDireccion(Sucursal? sucursal, Empresas? empresa)
        {
            var partes = new List<string>();
            var direccion = PrimeroNoVacio(sucursal?.Direccion, empresa?.Direccion);
            if (!string.IsNullOrWhiteSpace(direccion))
                partes.Add(direccion);

            var localidad = Localidad(sucursal) ?? Localidad(empresa);
            if (!string.IsNullOrWhiteSpace(localidad)
                && !partes.Any(p => p.Contains(localidad, StringComparison.OrdinalIgnoreCase)))
            {
                partes.Add(localidad);
            }

            return string.Join(", ", partes);
        }

        private static string? Localidad(Sucursal? sucursal)
            => UnirLocalidad(sucursal?.Municipio, sucursal?.Provincia);

        private static string? Localidad(Empresas? empresa)
            => UnirLocalidad(empresa?.Municipio, empresa?.Provincia);

        private static string? UnirLocalidad(string? municipio, string? provincia)
        {
            var texto = string.Join(", ",
                new[] { municipio, provincia }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim()));
            return string.IsNullOrWhiteSpace(texto) ? null : texto;
        }

        private static string? PrimeroNoVacio(params string?[] valores)
        {
            foreach (var valor in valores)
            {
                if (!string.IsNullOrWhiteSpace(valor))
                    return valor.Trim();
            }

            return null;
        }
    }
}
