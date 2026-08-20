using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AlahiaPos.DataAccess.Servicios.Rrhh
{
    public sealed class RrhhKioscoService : IRrhhKioscoService
    {
        public const string OrigenFacial = "KIOSCO_FACIAL";
        public const string OrigenPin = "KIOSCO_PIN";
        private const double DistanciaMaxima = 0.50;
        private const double MargenSegundo = 0.08;
        private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(90);
        private static readonly Regex PinRegex = new(@"^\d{4,8}$", RegexOptions.Compiled);

        private readonly AlahiaPosContext _db;
        private readonly IRrhhPonchadorService _ponchador;

        public RrhhKioscoService(AlahiaPosContext db, IRrhhPonchadorService ponchador)
        {
            _db = db;
            _ponchador = ponchador;
        }

        public async Task<IReadOnlyList<RrhhEmpleadoRostroEstadoDto>> ListarRostrosAsync(int idEmpresa)
        {
            var empleados = await _db.EmpleadosP.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa)
                .Select(e => new { e.IdEmpleados, e.Nombre, e.Estado })
                .ToListAsync();
            var rostros = await _db.RrhhEmpleadoRostro.AsNoTracking()
                .Where(r => r.IdEmpresa == idEmpresa)
                .ToListAsync();
            var byEmp = rostros.ToDictionary(r => r.IdEmpleados);

            return empleados
                .Where(e => e.Estado)
                .OrderBy(e => e.Nombre)
                .Select(e =>
                {
                    byEmp.TryGetValue(e.IdEmpleados, out var r);
                    return MapEstado(e.IdEmpleados, e.Nombre ?? "", r);
                })
                .ToList();
        }

        public async Task<RrhhEmpleadoRostroEstadoDto> EnrolarAsync(RrhhEnrolarRostroRequest req, int idUsuario)
        {
            if (req.IdEmpresa <= 0 || req.IdEmpleados <= 0)
                throw new ArgumentException("Indique empresa y empleado.");
            if (!req.Consentimiento)
                throw new ArgumentException("El colaborador debe aceptar el uso de su rostro para marcar asistencia.");
            if (req.Embeddings is null || req.Embeddings.Count < 2)
                throw new ArgumentException("Se necesitan al menos 2 tomas del rostro.");
            if (req.Embeddings.Count > 5)
                throw new ArgumentException("Máximo 5 tomas por enrolamiento.");
            var tomas = new List<float[]>();
            foreach (var emb in req.Embeddings)
            {
                var f = ToFloat(emb);
                if (f.Length != 128)
                    throw new ArgumentException("Cada toma debe ser un descriptor facial de 128 valores.");
                tomas.Add(f);
            }

            var emp = await _db.EmpleadosP.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == req.IdEmpresa && e.IdEmpleados == req.IdEmpleados)
                ?? throw new InvalidOperationException("El empleado no existe.");

            var promedio = PromedioNormalizado(tomas);
            var json = JsonSerializer.Serialize(promedio);

            string? pinHash = null;
            string? pinSalt = null;
            if (req.PermitirPinExcepcion)
            {
                if (!PinRegex.IsMatch(req.Pin ?? ""))
                    throw new ArgumentException("El PIN de excepción debe tener de 4 a 8 dígitos.");
                pinSalt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
                pinHash = HashPin(req.Pin!, pinSalt);
            }

            var row = await _db.RrhhEmpleadoRostro.AsTracking()
                .FirstOrDefaultAsync(r => r.IdEmpresa == req.IdEmpresa && r.IdEmpleados == req.IdEmpleados);
            if (row is null)
            {
                row = new RrhhEmpleadoRostro
                {
                    IdEmpresa = req.IdEmpresa,
                    IdEmpleados = req.IdEmpleados
                };
                _db.RrhhEmpleadoRostro.Add(row);
            }

            row.Embedding = json;
            row.Muestras = tomas.Count;
            row.Consentimiento = true;
            row.FechaEnrolamiento = DateTime.UtcNow;
            row.IdUsuarioEnrolamiento = idUsuario;
            row.Activo = true;
            row.PermitirPinExcepcion = req.PermitirPinExcepcion;
            if (req.PermitirPinExcepcion)
            {
                row.PinHash = pinHash;
                row.PinSalt = pinSalt;
                row.IntentosPinFallidos = 0;
                row.PinBloqueadoHasta = null;
            }
            else
            {
                row.PinHash = null;
                row.PinSalt = null;
            }

            await AuditarAsync(req.IdEmpresa, req.IdEmpleados, "ENROLAR", null, idUsuario, null, null,
                $"muestras={tomas.Count}; pin={(req.PermitirPinExcepcion ? "si" : "no")}");
            await _db.SaveChangesAsync();
            return MapEstado(emp.IdEmpleados, emp.Nombre ?? "", row);
        }

        public async Task<RrhhEmpleadoRostroEstadoDto> CambiarEstadoAsync(RrhhRostroEstadoRequest req, int idUsuario)
        {
            var row = await _db.RrhhEmpleadoRostro.AsTracking()
                .FirstOrDefaultAsync(r => r.IdEmpresa == req.IdEmpresa && r.IdEmpleados == req.IdEmpleados)
                ?? throw new InvalidOperationException("El colaborador no tiene rostro enrolado.");
            row.Activo = req.Activo;
            await AuditarAsync(req.IdEmpresa, req.IdEmpleados, req.Activo ? "ACTIVAR" : "DESACTIVAR",
                null, idUsuario, null, null, null);
            await _db.SaveChangesAsync();
            var nombre = await _db.EmpleadosP.AsNoTracking()
                .Where(e => e.IdEmpleados == req.IdEmpleados)
                .Select(e => e.Nombre)
                .FirstOrDefaultAsync();
            return MapEstado(req.IdEmpleados, nombre ?? "", row);
        }

        public async Task<RrhhKioscoPoncharResultDto> PoncharFacialAsync(
            RrhhKioscoFacialRequest req, int idUsuario, string? ip)
        {
            if (!req.LivenessOk)
                throw new InvalidOperationException("No se detectó prueba de vida. Mire la cámara y parpadee.");
            if (req.Embedding is null || req.Embedding.Length != 128)
                throw new ArgumentException("Descriptor facial inválido.");

            var candidatos = await _db.RrhhEmpleadoRostro.AsNoTracking()
                .Where(r => r.IdEmpresa == req.IdEmpresa && r.Activo && r.Consentimiento)
                .ToListAsync();
            if (candidatos.Count == 0)
                throw new InvalidOperationException("Nadie está enrolado. RRHH debe registrar el rostro primero.");

            var probe = Normalizar(ToFloat(req.Embedding));
            var ranking = new List<(RrhhEmpleadoRostro Row, double Dist)>();
            foreach (var c in candidatos)
            {
                var stored = JsonSerializer.Deserialize<float[]>(c.Embedding);
                if (stored is null || stored.Length != 128) continue;
                ranking.Add((c, Distancia(probe, Normalizar(stored))));
            }
            ranking = ranking.OrderBy(x => x.Dist).ToList();
            var best = ranking.FirstOrDefault();
            if (best.Row is null || best.Dist > DistanciaMaxima)
            {
                await AuditarAsync(req.IdEmpresa, null, "MATCH_FAIL", best.Row is null ? null : best.Dist,
                    idUsuario, req.Dispositivo, ip, "sin coincidencia");
                await _db.SaveChangesAsync();
                throw new InvalidOperationException("Rostro no reconocido. Acerque la cara, mire de frente y parpadee.");
            }
            if (ranking.Count > 1 && ranking[1].Dist - best.Dist < MargenSegundo)
            {
                await AuditarAsync(req.IdEmpresa, best.Row.IdEmpleados, "MATCH_AMBIGUO", best.Dist,
                    idUsuario, req.Dispositivo, ip, "margen insuficiente");
                await _db.SaveChangesAsync();
                throw new InvalidOperationException("No se pudo distinguir el rostro. Intente de nuevo, uno a la vez.");
            }

            var nombre = await NombreEmpleadoAsync(best.Row.IdEmpleados);
            var dup = await IntentarDuplicadaAsync(req.IdEmpresa, best.Row.IdEmpleados, nombre, OrigenFacial, best.Dist);
            if (dup != null)
            {
                await AuditarAsync(req.IdEmpresa, best.Row.IdEmpleados, "MATCH_DUP", best.Dist,
                    idUsuario, req.Dispositivo, ip, null);
                await _db.SaveChangesAsync();
                return dup;
            }

            var punch = await _ponchador.PoncharAsync(new RrhhPoncharRequest
            {
                IdEmpresa = req.IdEmpresa,
                IdEmpleados = best.Row.IdEmpleados,
                Origen = OrigenFacial,
                Dispositivo = Trunc(req.Dispositivo, 120),
                Nota = $"dist={best.Dist:0.000}"
            }, idUsuario, ip);

            await AuditarAsync(req.IdEmpresa, best.Row.IdEmpleados, "MATCH_OK", best.Dist,
                idUsuario, req.Dispositivo, ip, punch.Tipo);
            await _db.SaveChangesAsync();

            return new RrhhKioscoPoncharResultDto
            {
                Ok = true,
                Mensaje = $"{nombre} · {punch.Tipo}",
                IdEmpleados = punch.IdEmpleados,
                Nombre = nombre,
                Tipo = punch.Tipo,
                FechaHora = punch.FechaHora,
                Distancia = Math.Round(best.Dist, 4),
                Origen = OrigenFacial
            };
        }

        public async Task<RrhhKioscoPoncharResultDto> PoncharPinAsync(
            RrhhKioscoPinRequest req, int idUsuario, string? ip)
        {
            if (string.IsNullOrWhiteSpace(req.Motivo) || req.Motivo.Trim().Length < 8)
                throw new ArgumentException("La excepción con PIN requiere un motivo (mínimo 8 caracteres).");
            if (!PinRegex.IsMatch(req.Pin ?? ""))
                throw new ArgumentException("PIN inválido.");

            var row = await _db.RrhhEmpleadoRostro.AsTracking()
                .FirstOrDefaultAsync(r => r.IdEmpresa == req.IdEmpresa && r.IdEmpleados == req.IdEmpleados && r.Activo)
                ?? throw new InvalidOperationException("Este colaborador no tiene excepción PIN habilitada.");
            if (!row.PermitirPinExcepcion || string.IsNullOrWhiteSpace(row.PinHash) || string.IsNullOrWhiteSpace(row.PinSalt))
                throw new InvalidOperationException("RRHH no habilitó PIN de excepción para este colaborador.");
            if (row.PinBloqueadoHasta.HasValue && row.PinBloqueadoHasta.Value > DateTime.UtcNow)
                throw new InvalidOperationException("PIN bloqueado temporalmente. Use el rostro o espere.");

            var hash = HashPin(req.Pin, row.PinSalt);
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(row.PinHash)))
            {
                row.IntentosPinFallidos++;
                if (row.IntentosPinFallidos >= 5)
                {
                    row.PinBloqueadoHasta = DateTime.UtcNow.AddMinutes(10);
                    row.IntentosPinFallidos = 0;
                }
                await AuditarAsync(req.IdEmpresa, req.IdEmpleados, "PIN_FAIL", null, idUsuario, req.Dispositivo, ip,
                    Trunc(req.Motivo, 200));
                await _db.SaveChangesAsync();
                throw new InvalidOperationException("PIN incorrecto.");
            }

            row.IntentosPinFallidos = 0;
            row.PinBloqueadoHasta = null;

            var nombre = await NombreEmpleadoAsync(req.IdEmpleados);
            var dup = await IntentarDuplicadaAsync(req.IdEmpresa, req.IdEmpleados, nombre, OrigenPin, 0);
            if (dup != null)
            {
                await AuditarAsync(req.IdEmpresa, req.IdEmpleados, "PIN_DUP", null, idUsuario, req.Dispositivo, ip, null);
                await _db.SaveChangesAsync();
                return dup;
            }

            var punch = await _ponchador.PoncharAsync(new RrhhPoncharRequest
            {
                IdEmpresa = req.IdEmpresa,
                IdEmpleados = req.IdEmpleados,
                Origen = OrigenPin,
                Dispositivo = Trunc(req.Dispositivo, 120),
                Nota = "excepción PIN: " + Trunc(req.Motivo.Trim(), 200)
            }, idUsuario, ip);

            await AuditarAsync(req.IdEmpresa, req.IdEmpleados, "PIN_OK", null, idUsuario, req.Dispositivo, ip, punch.Tipo);
            await _db.SaveChangesAsync();

            return new RrhhKioscoPoncharResultDto
            {
                Ok = true,
                Mensaje = $"{nombre} · {punch.Tipo} (excepción PIN)",
                IdEmpleados = punch.IdEmpleados,
                Nombre = nombre,
                Tipo = punch.Tipo,
                FechaHora = punch.FechaHora,
                Origen = OrigenPin
            };
        }

        public async Task<IReadOnlyList<RrhhPonchadaRecienteDto>> RecientesAsync(int idEmpresa, int take = 8)
        {
            var desde = DateTime.Now.Date;
            var rows = await _db.RrhhPonchada.AsNoTracking()
                .Where(p => p.IdEmpresa == idEmpresa && p.FechaHora >= desde)
                .OrderByDescending(p => p.FechaHora)
                .Take(Math.Clamp(take, 1, 20))
                .ToListAsync();
            var ids = rows.Select(r => r.IdEmpleados).Distinct().ToList();
            var nombres = await _db.EmpleadosP.AsNoTracking()
                .Where(e => ids.Contains(e.IdEmpleados))
                .ToDictionaryAsync(e => e.IdEmpleados, e => e.Nombre ?? "");
            return rows.Select(p => new RrhhPonchadaRecienteDto
            {
                IdPonchada = p.IdPonchada,
                IdEmpleados = p.IdEmpleados,
                Nombre = nombres.TryGetValue(p.IdEmpleados, out var n) ? n : "",
                FechaHora = p.FechaHora,
                Tipo = p.Tipo,
                Origen = p.Origen
            }).ToList();
        }

        private async Task<RrhhKioscoPoncharResultDto?> IntentarDuplicadaAsync(
            int idEmpresa, int idEmpleados, string nombre, string origen, double dist)
        {
            var desde = DateTime.Now - Cooldown;
            var ultima = await _db.RrhhPonchada.AsNoTracking()
                .Where(p => p.IdEmpresa == idEmpresa && p.IdEmpleados == idEmpleados && p.FechaHora >= desde)
                .OrderByDescending(p => p.FechaHora)
                .FirstOrDefaultAsync();
            if (ultima is null) return null;
            return new RrhhKioscoPoncharResultDto
            {
                Ok = true,
                Mensaje = $"{nombre} ya marcó hace un momento.",
                IdEmpleados = idEmpleados,
                Nombre = nombre,
                Tipo = ultima.Tipo,
                FechaHora = ultima.FechaHora,
                Distancia = Math.Round(dist, 4),
                Duplicada = true,
                Origen = origen
            };
        }

        private async Task<string> NombreEmpleadoAsync(int idEmpleados) =>
            await _db.EmpleadosP.AsNoTracking()
                .Where(e => e.IdEmpleados == idEmpleados)
                .Select(e => e.Nombre)
                .FirstOrDefaultAsync() ?? "";

        private Task AuditarAsync(
            int idEmpresa, int? idEmpleados, string tipo, double? dist,
            int idUsuario, string? dispositivo, string? ip, string? detalle)
        {
            _db.RrhhKioscoEvento.Add(new RrhhKioscoEvento
            {
                IdEmpresa = idEmpresa,
                IdEmpleados = idEmpleados,
                Tipo = tipo,
                Distancia = dist,
                IdUsuario = idUsuario,
                Dispositivo = Trunc(dispositivo, 120),
                Ip = Trunc(ip, 60),
                Detalle = Trunc(detalle, 400)
            });
            return Task.CompletedTask;
        }

        private static RrhhEmpleadoRostroEstadoDto MapEstado(int idEmpleados, string nombre, RrhhEmpleadoRostro? r) =>
            new()
            {
                IdEmpleados = idEmpleados,
                Nombre = nombre,
                Enrolado = r != null,
                Activo = r?.Activo ?? false,
                PermitirPinExcepcion = r?.PermitirPinExcepcion ?? false,
                TienePin = !string.IsNullOrWhiteSpace(r?.PinHash),
                FechaEnrolamiento = r?.FechaEnrolamiento,
                Muestras = r?.Muestras ?? 0
            };

        private static float[] PromedioNormalizado(IReadOnlyList<float[]> muestras)
        {
            var acc = new float[128];
            foreach (var m in muestras)
            {
                var n = Normalizar(m);
                for (var i = 0; i < 128; i++) acc[i] += n[i];
            }
            for (var i = 0; i < 128; i++) acc[i] /= muestras.Count;
            return Normalizar(acc);
        }

        private static float[] Normalizar(float[] v)
        {
            double s = 0;
            for (var i = 0; i < v.Length; i++) s += v[i] * v[i];
            var n = Math.Sqrt(s);
            if (n < 1e-8) return v.ToArray();
            var o = new float[v.Length];
            for (var i = 0; i < v.Length; i++) o[i] = (float)(v[i] / n);
            return o;
        }

        private static double Distancia(float[] a, float[] b)
        {
            double s = 0;
            var len = Math.Min(a.Length, b.Length);
            for (var i = 0; i < len; i++)
            {
                var d = a[i] - b[i];
                s += d * d;
            }
            return Math.Sqrt(s);
        }

        private static float[] ToFloat(double[]? v)
        {
            if (v is null) return Array.Empty<float>();
            var o = new float[v.Length];
            for (var i = 0; i < v.Length; i++) o[i] = (float)v[i];
            return o;
        }

        private static string HashPin(string pin, string salt)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(salt + pin));
            return Convert.ToHexString(bytes);
        }

        private static string? Trunc(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var t = value.Trim();
            return t.Length <= max ? t : t[..max];
        }
    }
}
