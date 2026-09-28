using AlahiaPos.Entities.Dto;

namespace AlahiaPos.DataAccess.Servicios.WhatsApp
{
    /// <summary>
    /// Texto exacto para crear las plantillas en Meta / Twilio (categoría UTILITY, es-DO).
    /// Variables Twilio/Meta: {{1}}…{{6}} en este orden.
    /// </summary>
    public static class WhatsAppCitasPlantillas
    {
        public const string RecibidaNombre = "alahia_cita_recibida";
        public const string ConfirmadaNombre = "alahia_cita_confirmada";
        public const string RecordatorioNombre = "alahia_cita_recordatorio";
        public const string CanceladaNombre = "alahia_cita_cancelada";

        public const string Recibida =
            "Hola {{1}}, *{{2}}* recibió tu reserva.\n\n" +
            "Servicio: {{3}}\n" +
            "Estilista: {{4}}\n" +
            "Fecha: {{5}}\n" +
            "Hora: {{6}}\n\n" +
            "Te confirmaremos por este WhatsApp.";

        public const string Confirmada =
            "Hola {{1}}, *{{2}}* confirmó tu cita.\n\n" +
            "Servicio: {{3}}\n" +
            "Estilista: {{4}}\n" +
            "Fecha: {{5}}\n" +
            "Hora: {{6}}\n\n" +
            "Te esperamos.";

        public const string Recordatorio =
            "Hola {{1}}, te recordamos tu cita de hoy en *{{2}}*.\n\n" +
            "Servicio: {{3}}\n" +
            "Estilista: {{4}}\n" +
            "Hora: {{5}}\n\n" +
            "Te esperamos.";

        public const string Cancelada =
            "Hola {{1}}, tu cita en *{{2}}* fue cancelada.\n\n" +
            "Servicio: {{3}}\n" +
            "Fecha: {{4}}\n" +
            "Hora: {{5}}\n\n" +
            "Escríbenos para reprogramar.";

        public static string Render(WhatsAppCitaMensaje m)
        {
            var c = Trunc(m.NombreCliente, 40);
            var s = Trunc(m.NombreSalon, 60);
            var srv = Trunc(m.Servicio, 60);
            var e = Trunc(m.Estilista, 40);
            var f = Trunc(m.Fecha, 20);
            var h = Trunc(m.Hora, 20);

            return m.Tipo switch
            {
                WhatsAppCitaTipo.Recibida =>
                    Recibida.Replace("{{1}}", c).Replace("{{2}}", s).Replace("{{3}}", srv)
                        .Replace("{{4}}", e).Replace("{{5}}", f).Replace("{{6}}", h),
                WhatsAppCitaTipo.Confirmada =>
                    Confirmada.Replace("{{1}}", c).Replace("{{2}}", s).Replace("{{3}}", srv)
                        .Replace("{{4}}", e).Replace("{{5}}", f).Replace("{{6}}", h),
                WhatsAppCitaTipo.Recordatorio =>
                    Recordatorio.Replace("{{1}}", c).Replace("{{2}}", s).Replace("{{3}}", srv)
                        .Replace("{{4}}", e).Replace("{{5}}", h),
                WhatsAppCitaTipo.Cancelada =>
                    Cancelada.Replace("{{1}}", c).Replace("{{2}}", s).Replace("{{3}}", srv)
                        .Replace("{{4}}", f).Replace("{{5}}", h),
                _ => $"{s}: {srv} {f} {h}"
            };
        }

        public static Dictionary<string, string> Variables(WhatsAppCitaMensaje m)
        {
            var c = Trunc(m.NombreCliente, 40);
            var s = Trunc(m.NombreSalon, 60);
            var srv = Trunc(m.Servicio, 60);
            var e = Trunc(m.Estilista, 40);
            var f = Trunc(m.Fecha, 20);
            var h = Trunc(m.Hora, 20);

            return m.Tipo switch
            {
                WhatsAppCitaTipo.Recordatorio => new Dictionary<string, string>
                {
                    ["1"] = c, ["2"] = s, ["3"] = srv, ["4"] = e, ["5"] = h
                },
                WhatsAppCitaTipo.Cancelada => new Dictionary<string, string>
                {
                    ["1"] = c, ["2"] = s, ["3"] = srv, ["4"] = f, ["5"] = h
                },
                _ => new Dictionary<string, string>
                {
                    ["1"] = c, ["2"] = s, ["3"] = srv, ["4"] = e, ["5"] = f, ["6"] = h
                }
            };
        }

        public static string Trunc(string? value, int max)
        {
            var t = string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
            return t.Length <= max ? t : t[..max];
        }

        public static string Nombre(WhatsAppCitaTipo tipo) => tipo switch
        {
            WhatsAppCitaTipo.Recibida => RecibidaNombre,
            WhatsAppCitaTipo.Confirmada => ConfirmadaNombre,
            WhatsAppCitaTipo.Recordatorio => RecordatorioNombre,
            WhatsAppCitaTipo.Cancelada => CanceladaNombre,
            _ => RecibidaNombre
        };

        public static IReadOnlyList<WhatsAppCitasPlantillaDef> Definiciones() => new[]
        {
            new WhatsAppCitasPlantillaDef
            {
                Nombre = RecibidaNombre,
                Cuerpo = Recibida,
                VariablesEjemplo = new Dictionary<string, string>
                {
                    ["1"] = "María", ["2"] = "Glam Studio", ["3"] = "Corte",
                    ["4"] = "Ana", ["5"] = "08/09/2026", ["6"] = "10:00 a. m."
                }
            },
            new WhatsAppCitasPlantillaDef
            {
                Nombre = ConfirmadaNombre,
                Cuerpo = Confirmada,
                VariablesEjemplo = new Dictionary<string, string>
                {
                    ["1"] = "María", ["2"] = "Glam Studio", ["3"] = "Corte",
                    ["4"] = "Ana", ["5"] = "08/09/2026", ["6"] = "10:00 a. m."
                }
            },
            new WhatsAppCitasPlantillaDef
            {
                Nombre = RecordatorioNombre,
                Cuerpo = Recordatorio,
                VariablesEjemplo = new Dictionary<string, string>
                {
                    ["1"] = "María", ["2"] = "Glam Studio", ["3"] = "Corte",
                    ["4"] = "Ana", ["5"] = "10:00 a. m."
                }
            },
            new WhatsAppCitasPlantillaDef
            {
                Nombre = CanceladaNombre,
                Cuerpo = Cancelada,
                VariablesEjemplo = new Dictionary<string, string>
                {
                    ["1"] = "María", ["2"] = "Glam Studio", ["3"] = "Corte",
                    ["4"] = "08/09/2026", ["5"] = "10:00 a. m."
                }
            }
        };
    }
}
