using System.Data;
using System.Security.Cryptography;
using System.Text;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PosTerminalService : IPosTerminalService
    {
        public const string CodigoModuloPos = "POS";

        private readonly AlahiaPosContext _db;
        private readonly IModulo _modulos;
        private readonly IEmpresaModulos _empresaModulos;

        public PosTerminalService(
            AlahiaPosContext db,
            IModulo modulos,
            IEmpresaModulos empresaModulos)
        {
            _db = db;
            _modulos = modulos;
            _empresaModulos = empresaModulos;
        }

        public async Task<PosTerminalClaimResult> ClaimAsync(
            int idEmpresa,
            int idUsuario,
            PosTerminalClaimRequest req,
            bool esEmpresaSistema,
            CancellationToken ct = default)
        {
            var deviceId = (req?.DeviceId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Denegar(PosTerminalCodigos.SinHuella, 0, 0,
                    "Este equipo no se pudo identificar. Cierre el navegador, vuelva a entrar y si persiste contacte a MacroBits.");
            }

            if (esEmpresaSistema)
            {
                var terminalMb = await AsegurarTerminalAsync(
                    idEmpresa, idUsuario, deviceId, req ?? new PosTerminalClaimRequest(), ct);
                return Ok(Map(terminalMb), int.MaxValue, await ContarActivosAsync(idEmpresa, ct));
            }

            var modulo = await _modulos.GetModuloByCodigo(CodigoModuloPos);
            if (modulo == null || !await _empresaModulos.EmpresaTieneModulo(idEmpresa, modulo.Id))
            {
                return Denegar(PosTerminalCodigos.SinModulo, 0, 0,
                    "Esta empresa no tiene el módulo POS contratado.");
            }

            var empresa = await _db.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa, ct);
            if (empresa == null)
            {
                return Denegar(PosTerminalCodigos.SinModulo, 0, 0, "Empresa no encontrada.");
            }

            var limite = empresa.LimiteTerminalesPos < 1 ? 1 : empresa.LimiteTerminalesPos;
            var huella = HashHuella(deviceId);

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                var existente = await _db.PosTerminal
                    .FirstOrDefaultAsync(t => t.IdEmpresa == idEmpresa && t.Huella == huella, ct);

                if (existente != null && existente.Estado == PosTerminalEstados.Activo)
                {
                    ActualizarHuellaMetadata(existente, req, idUsuario);
                    existente.FechaUltimoAcceso = DateTime.Now;
                    existente.IdUsuarioUltimoAcceso = idUsuario;
                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    var usadasActivo = await ContarActivosAsync(idEmpresa, ct);
                    return Ok(Map(existente), limite, usadasActivo);
                }

                var usadas = await _db.PosTerminal
                    .CountAsync(t => t.IdEmpresa == idEmpresa && t.Estado == PosTerminalEstados.Activo, ct);

                if (usadas >= limite)
                {
                    await tx.RollbackAsync(ct);
                    var nombrePc = NombreSugerido(req);
                    return Denegar(PosTerminalCodigos.Limite, limite, usadas,
                        $"Esta empresa tiene {limite} licencia(s) POS y ya están en uso en otros equipos. " +
                        $"Este PC ({nombrePc}) no puede abrir caja. Contacte a MacroBits para liberar un equipo o contratar más cajas.");
                }

                if (existente != null)
                {
                    existente.Estado = PosTerminalEstados.Activo;
                    existente.FechaActivacion = DateTime.Now;
                    existente.IdUsuarioActivacion = idUsuario;
                    existente.FechaRevocacion = null;
                    existente.IdUsuarioRevocacion = null;
                    existente.FechaUltimoAcceso = DateTime.Now;
                    existente.IdUsuarioUltimoAcceso = idUsuario;
                    ActualizarHuellaMetadata(existente, req, idUsuario);
                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return Ok(Map(existente), limite, usadas + 1);
                }

                var nuevo = new PosTerminal
                {
                    IdEmpresa = idEmpresa,
                    Huella = huella,
                    Nombre = NombreSugerido(req),
                    Plataforma = Trim(req?.Plataforma, 30),
                    Modelo = Trim(req?.Modelo, 80),
                    Fabricante = Trim(req?.Fabricante, 80),
                    Estado = PosTerminalEstados.Activo,
                    FechaActivacion = DateTime.Now,
                    IdUsuarioActivacion = idUsuario,
                    FechaUltimoAcceso = DateTime.Now,
                    IdUsuarioUltimoAcceso = idUsuario
                };
                _db.PosTerminal.Add(nuevo);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return Ok(Map(nuevo), limite, usadas + 1);
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(ct);
                var ya = await _db.PosTerminal.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.IdEmpresa == idEmpresa && t.Huella == huella
                        && t.Estado == PosTerminalEstados.Activo, ct);
                if (ya != null)
                    return Ok(Map(ya), limite, await ContarActivosAsync(idEmpresa, ct));
                throw;
            }
        }

        public async Task<List<PosTerminalDto>> ListarAsync(int idEmpresa, CancellationToken ct = default)
        {
            var rows = await _db.PosTerminal.AsNoTracking()
                .Where(t => t.IdEmpresa == idEmpresa)
                .OrderBy(t => t.Estado == PosTerminalEstados.Activo ? 0 : 1)
                .ThenByDescending(t => t.FechaUltimoAcceso)
                .ToListAsync(ct);
            return rows.Select(Map).ToList();
        }

        public async Task RevocarAsync(int idEmpresa, int idPosTerminal, int idUsuarioAdmin, CancellationToken ct = default)
        {
            var t = await _db.PosTerminal
                .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa && x.IdPosTerminal == idPosTerminal, ct)
                ?? throw new InvalidOperationException("Equipo POS no encontrado.");

            if (t.Estado == PosTerminalEstados.Revocado)
                return;

            t.Estado = PosTerminalEstados.Revocado;
            t.FechaRevocacion = DateTime.Now;
            t.IdUsuarioRevocacion = idUsuarioAdmin;
            await _db.SaveChangesAsync(ct);
        }

        public Task<int> ContarActivosAsync(int idEmpresa, CancellationToken ct = default)
        {
            return _db.PosTerminal
                .CountAsync(t => t.IdEmpresa == idEmpresa && t.Estado == PosTerminalEstados.Activo, ct);
        }

        private async Task<PosTerminal> AsegurarTerminalAsync(
            int idEmpresa,
            int idUsuario,
            string deviceId,
            PosTerminalClaimRequest req,
            CancellationToken ct)
        {
            var huella = HashHuella(deviceId);
            var existente = await _db.PosTerminal
                .FirstOrDefaultAsync(t => t.IdEmpresa == idEmpresa && t.Huella == huella, ct);
            if (existente != null)
            {
                existente.Estado = PosTerminalEstados.Activo;
                existente.FechaUltimoAcceso = DateTime.Now;
                existente.IdUsuarioUltimoAcceso = idUsuario;
                ActualizarHuellaMetadata(existente, req, idUsuario);
                await _db.SaveChangesAsync(ct);
                return existente;
            }

            var nuevo = new PosTerminal
            {
                IdEmpresa = idEmpresa,
                Huella = huella,
                Nombre = NombreSugerido(req),
                Plataforma = Trim(req?.Plataforma, 30),
                Modelo = Trim(req?.Modelo, 80),
                Fabricante = Trim(req?.Fabricante, 80),
                Estado = PosTerminalEstados.Activo,
                FechaActivacion = DateTime.Now,
                IdUsuarioActivacion = idUsuario,
                FechaUltimoAcceso = DateTime.Now,
                IdUsuarioUltimoAcceso = idUsuario
            };
            _db.PosTerminal.Add(nuevo);
            await _db.SaveChangesAsync(ct);
            return nuevo;
        }

        private static void ActualizarHuellaMetadata(PosTerminal t, PosTerminalClaimRequest? req, int idUsuario = 0)
        {
            if (req == null) return;
            if (!string.IsNullOrWhiteSpace(req.Nombre))
                t.Nombre = Trim(req.Nombre, 80);
            else if (string.IsNullOrWhiteSpace(t.Nombre))
                t.Nombre = NombreSugerido(req);
            if (!string.IsNullOrWhiteSpace(req.Plataforma))
                t.Plataforma = Trim(req.Plataforma, 30);
            if (!string.IsNullOrWhiteSpace(req.Modelo))
                t.Modelo = Trim(req.Modelo, 80);
            if (!string.IsNullOrWhiteSpace(req.Fabricante))
                t.Fabricante = Trim(req.Fabricante, 80);
        }

        private static string NombreSugerido(PosTerminalClaimRequest? req)
        {
            if (!string.IsNullOrWhiteSpace(req?.Nombre))
                return Trim(req!.Nombre, 80)!;
            var fab = (req?.Fabricante ?? "").Trim();
            var mod = (req?.Modelo ?? "").Trim();
            var plat = (req?.Plataforma ?? "").Trim();
            var label = string.Join(" ", new[] { fab, mod }.Where(s => s.Length > 0));
            if (label.Length == 0)
                label = plat.Length > 0 ? plat : "PC";
            return Trim("POS · " + label, 80)!;
        }

        private static string HashHuella(string deviceId)
        {
            var bytes = Encoding.UTF8.GetBytes(deviceId.Trim().ToLowerInvariant());
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static string? Trim(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var v = value.Trim();
            return v.Length <= max ? v : v[..max];
        }

        private static PosTerminalDto Map(PosTerminal t) => new()
        {
            IdPosTerminal = t.IdPosTerminal,
            IdEmpresa = t.IdEmpresa,
            Nombre = t.Nombre ?? "POS",
            Plataforma = t.Plataforma,
            Modelo = t.Modelo,
            Fabricante = t.Fabricante,
            Estado = t.Estado,
            FechaActivacion = t.FechaActivacion,
            FechaUltimoAcceso = t.FechaUltimoAcceso,
            FechaRevocacion = t.FechaRevocacion,
            IdUsuarioActivacion = t.IdUsuarioActivacion,
            IdUsuarioUltimoAcceso = t.IdUsuarioUltimoAcceso
        };

        private static PosTerminalClaimResult Ok(PosTerminalDto terminal, int limite, int usadas) => new()
        {
            Permitido = true,
            Codigo = PosTerminalCodigos.Ok,
            Mensaje = "",
            Limite = limite,
            Usadas = usadas,
            Terminal = terminal
        };

        private static PosTerminalClaimResult Denegar(string codigo, int limite, int usadas, string mensaje) => new()
        {
            Permitido = false,
            Codigo = codigo,
            Mensaje = mensaje,
            Limite = limite,
            Usadas = usadas
        };
    }
}
