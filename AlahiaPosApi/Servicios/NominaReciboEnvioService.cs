using System.Globalization;
using System.Net;
using System.Text;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using PrinterLibrary;

namespace AlahiaPosApi.Servicios
{
    public sealed class NominaReciboEnvioService : INominaReciboEnvioService
    {
        private readonly AlahiaPosContext _db;

        public NominaReciboEnvioService(AlahiaPosContext db) => _db = db;

        public async Task<NominaRecibosEnvioResultadoDto> EnviarAsync(int idEmpresa, int idNominaProceso)
        {
            var proceso = await _db.NominaProceso.AsNoTracking()
                .Include(p => p.Empleados)
                .FirstOrDefaultAsync(p => p.IdEmpresa == idEmpresa && p.IdNominaProceso == idNominaProceso)
                ?? throw new InvalidOperationException("Proceso de nómina no encontrado.");

            if (proceso.Empleados == null || proceso.Empleados.Count == 0)
                throw new InvalidOperationException("Genere la pre-nómina antes de enviar recibos.");

            var empresa = await _db.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa)
                ?? throw new InvalidOperationException("Empresa no encontrada.");

            var smtp = ResolverSmtp(empresa);

            var ids = proceso.Empleados.Select(e => e.IdEmpleados).ToList();
            var correosLaboral = await _db.EmpleadoLaboral.AsNoTracking()
                .Where(x => x.IdEmpresa == idEmpresa && ids.Contains(x.IdEmpleados))
                .Select(x => new { x.IdEmpleados, x.Correo })
                .ToListAsync();
            var correosUsuario = await _db.Usuarios.AsNoTracking()
                .Where(u => u.IdEmpresa == idEmpresa && ids.Contains(u.IdEmpleado) && u.Correo != null && u.Correo != "")
                .Select(u => new { u.IdEmpleado, u.Correo })
                .ToListAsync();

            string CorreoDe(int idEmpleados)
            {
                var lab = correosLaboral.FirstOrDefault(x => x.IdEmpleados == idEmpleados)?.Correo;
                if (EsCorreoReal(lab)) return lab!.Trim();
                var usr = correosUsuario.FirstOrDefault(x => x.IdEmpleado == idEmpleados)?.Correo;
                if (EsCorreoReal(usr)) return usr!.Trim();
                return smtp.User;
            }

            var resultado = new NominaRecibosEnvioResultadoDto();
            var fromName = string.IsNullOrWhiteSpace(empresa.NombreRemitente)
                ? (empresa.NombreComercial ?? "Nómina")
                : empresa.NombreRemitente;

            foreach (var emp in proceso.Empleados.OrderBy(e => e.NombreEmpleado))
            {
                var item = new NominaReciboEnvioItemDto
                {
                    IdEmpleados = emp.IdEmpleados,
                    Nombre = emp.NombreEmpleado
                };
                var correo = CorreoDe(emp.IdEmpleados);
                item.Correo = correo;

                try
                {
                    var pdf = NominaReciboPdfBuilder.Crear(empresa, proceso, emp);
                    var asunto = $"Recibo de pago · {emp.NombreEmpleado} · {proceso.FechaInicio:dd/MM/yyyy} — {proceso.FechaFin:dd/MM/yyyy}";
                    var html = CuerpoHtml(empresa, proceso, emp);
                    var nombrePdf = $"Recibo-nomina-{Sanitizar(emp.NombreEmpleado)}-{proceso.PeriodKey}.pdf";
                    await Task.Run(() => Utility.Send(
                        smtp.Server,
                        smtp.Port,
                        smtp.Ssl,
                        smtp.User,
                        smtp.Password,
                        fromName,
                        correo,
                        asunto,
                        html,
                        pdf,
                        nombrePdf));
                    item.Estado = "ENVIADO";
                    resultado.Enviados++;
                }
                catch (Exception ex)
                {
                    item.Estado = "ERROR";
                    item.Error = ex.Message;
                    resultado.Errores++;
                }
                resultado.Detalle.Add(item);
            }

            resultado.Mensaje = resultado.Enviados == proceso.Empleados.Count && resultado.Errores == 0 && resultado.SinCorreo == 0
                ? $"Se enviaron {resultado.Enviados} recibos."
                : $"Enviados {resultado.Enviados} de {proceso.Empleados.Count}. Sin correo: {resultado.SinCorreo}. Errores: {resultado.Errores}.";
            return resultado;
        }

        private static SmtpEnvio ResolverSmtp(Empresas empresa)
        {
            if (!string.IsNullOrWhiteSpace(empresa.ServidorSMTP)
                && !string.IsNullOrWhiteSpace(empresa.CorreoSMTP)
                && !string.IsNullOrWhiteSpace(empresa.PasswordSMTP)
                && empresa.PuertoSMTP is > 0)
            {
                return new SmtpEnvio(
                    empresa.ServidorSMTP.Trim(),
                    empresa.PuertoSMTP.Value,
                    empresa.UsaSSL ?? true,
                    empresa.CorreoSMTP.Trim(),
                    empresa.PasswordSMTP);
            }

            return new SmtpEnvio("smtp.gmail.com", 587, true, "ing.joelarielsosa@gmail.com", "wrcsdhewqdgrtula");
        }

        private readonly record struct SmtpEnvio(string Server, int Port, bool Ssl, string User, string Password);

        private static bool EsCorreo(string? v) =>
            !string.IsNullOrWhiteSpace(v) && v.Contains('@') && v.Contains('.');

        private static bool EsCorreoReal(string? v) =>
            EsCorreo(v) && !v!.Trim().EndsWith(".demo", StringComparison.OrdinalIgnoreCase);

        private static string Sanitizar(string? nombre)
        {
            var raw = string.IsNullOrWhiteSpace(nombre) ? "colaborador" : nombre.Trim();
            var sb = new StringBuilder(raw.Length);
            foreach (var ch in raw)
                sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            return sb.ToString().Trim('_');
        }

        private static string CuerpoHtml(Empresas empresa, NominaProceso proceso, NominaProcesoEmpleado emp)
        {
            var d = NominaReciboPdfBuilder.Desglose(emp);
            var sb = new StringBuilder();
            sb.Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;max-width:640px;margin:0 auto;color:#1d2b3a;\">");
            sb.Append($"<h2 style=\"color:#1b4f72;margin:0 0 4px;\">{Enc(empresa.NombreComercial)}</h2>");
            sb.Append("<p style=\"margin:0 0 12px;color:#5b6b7c;font-size:13px;\">Recibo de pago / volante de nómina</p>");
            sb.Append($"<p><strong>Colaborador:</strong> {Enc(emp.NombreEmpleado)}<br/>");
            sb.Append($"<strong>Período:</strong> {proceso.FechaInicio:dd/MM/yyyy} — {proceso.FechaFin:dd/MM/yyyy}<br/>");
            sb.Append($"<strong>Frecuencia:</strong> {Enc(proceso.Frecuencia)}</p>");
            sb.Append(TablaHtml("Ingresos", d.Ingresos));
            sb.Append(TablaHtml("Deducciones de ley (AFP, SFS, ISR)", d.Legales));
            if (d.Otros.Count > 0)
                sb.Append(TablaHtml("Otros descuentos", d.Otros));
            sb.Append("<table style=\"width:100%;border-collapse:collapse;margin-top:12px;\">");
            sb.Append($"<tr><td style=\"padding:8px;background:#1b4f72;color:#fff;font-weight:700;\">NETO A PAGAR</td>");
            sb.Append($"<td style=\"padding:8px;background:#1b4f72;color:#fff;font-weight:700;text-align:right;\">{Moneda(d.Neto)}</td></tr></table>");
            sb.Append("<p style=\"font-size:12px;color:#5b6b7c;margin-top:12px;\">El neto a pagar ya descuenta AFP, SFS, ISR y los demás descuentos. Adjunto encontrará el recibo en PDF.</p>");
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string TablaHtml(string titulo, List<(string Label, decimal Monto)> filas)
        {
            var sb = new StringBuilder();
            sb.Append($"<h3 style=\"color:#1b4f72;font-size:14px;margin:16px 0 6px;\">{Enc(titulo)}</h3>");
            sb.Append("<table style=\"width:100%;border-collapse:collapse;font-size:13px;\">");
            foreach (var f in filas)
            {
                sb.Append("<tr>");
                sb.Append($"<td style=\"padding:6px 8px;border-bottom:1px solid #d7e3ee;\">{Enc(f.Label)}</td>");
                sb.Append($"<td style=\"padding:6px 8px;border-bottom:1px solid #d7e3ee;text-align:right;\">{Moneda(f.Monto)}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            return sb.ToString();
        }

        private static string Enc(string? v) => WebUtility.HtmlEncode(v ?? "");
        private static string Moneda(decimal v) => v.ToString("C2", CultureInfo.GetCultureInfo("es-DO"));
    }
}
