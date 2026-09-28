using AlahiaPos.DataAccess.Servicios.WhatsApp;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Enum; // donde esté EstadoCita
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using PrinterLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;


namespace AlahiaPos.DataAccess.Servicios
{
    public class CitaServices : ICitas
    {
        private readonly IRepository<Cita> _repository;
        private readonly IRepository<Empleados> _empleadoRepository;
        private readonly IRepository<Productos> _productoRepository;
        private readonly IIngresos _Ingresos;
        private readonly IEmpresas _empresas;
        private readonly INotasCredito _notasCredito;
        private readonly IWhatsAppCitas _whatsApp;

        private static readonly CultureInfo CulturaDo = new("es-DO");
       
        public CitaServices(
            IRepository<Cita> repository,
            IRepository<Empleados> empleadoRepository,
            IRepository<Productos> productoRepository,
            IIngresos ingresos,
            IEmpresas empresas,
            INotasCredito notasCredito,
            IWhatsAppCitas whatsApp)
        {
            _repository = repository;
            _empleadoRepository = empleadoRepository;
            _productoRepository = productoRepository;
            _Ingresos = ingresos;
            _empresas = empresas;
            _notasCredito = notasCredito;
            _whatsApp = whatsApp;
        }

        public void DeleteCita(int id)
        {
            _repository.Delete(id);
        }
        public async Task<Cita> GetCitaByIdFactHeader(int IdFacturaHeader)
        {
            return await _repository.GetByExpresionAsync(c=>c.IdFacturaHeader==IdFacturaHeader);
        }
        // ===========================================================
        // 🔥 Cambiar estado de una cita
        // ===========================================================
        public async Task CambiarEstadoCita(int idCita, EstadoCita nuevoEstado, int idUsuario = 0)
        {
            var cita = await _repository.GetByIdAsync(idCita);
            if (cita == null)
                throw new Exception("La cita no existe.");

            var empresa = await _empresas.GetEmpresaById(cita.IdEmpresa);

            bool smtpValido =
                empresa != null &&
                !string.IsNullOrWhiteSpace(empresa.CorreoSMTP) &&
                !string.IsNullOrWhiteSpace(empresa.PasswordSMTP) &&
                !string.IsNullOrWhiteSpace(empresa.ServidorSMTP);

            var estadoString = cita.Estado?.Replace(" ", "") ?? "";

            if (!Enum.TryParse<EstadoCita>(estadoString, true, out var estadoActual))
                throw new Exception($"Estado inválido: {cita.Estado}");

            if (!EsTransicionValida(estadoActual, nuevoEstado))
                throw new Exception($"Transición no permitida: {estadoActual} → {nuevoEstado}");

            // 🔄 Actualizar estado
            cita.Estado = nuevoEstado.ToString();
            _repository.Update(cita.IdCita, cita);

            if (nuevoEstado == EstadoCita.Confirmada)
                await RegistrarAnticipoConfirmacionAsync(cita, idUsuario);

            var servicio = await _productoRepository.GetByIdAsync(cita.IdProducto);
            var empleado = await _empleadoRepository.GetByIdAsync(cita.IdEmpleado);

            var nombreCliente = string.IsNullOrWhiteSpace(cita.NombreCliente)
                ? "cliente"
                : cita.NombreCliente;

            var nombreServicio = servicio?.Nombre ?? "Servicio";
            var nombreEstilista = empleado?.Nombre ?? "Estilista";

            var fechaTexto = FormatearFecha(cita.Fecha);
            var horaTexto = FormatearHora(cita);

            if (nuevoEstado == EstadoCita.Confirmada || nuevoEstado == EstadoCita.Cancelada)
            {
                await EnviarWhatsAppCitaAsync(
                    cita,
                    empresa,
                    nuevoEstado == EstadoCita.Confirmada
                        ? WhatsAppCitaTipo.Confirmada
                        : WhatsAppCitaTipo.Cancelada,
                    nombreCliente,
                    nombreServicio,
                    nombreEstilista,
                    fechaTexto,
                    horaTexto);
            }

            if (string.IsNullOrWhiteSpace(cita.Correo))
                return;

            // ===============================
            // 📅 GOOGLE CALENDAR LINK CORRECTO
            // ===============================
            string calendarUrl = null;

            if (nuevoEstado == EstadoCita.Confirmada)
            {
                var inicio = cita.Fecha.Date.Add(cita.Hora);

                var duracion = cita.DuracionMinutos > 0
                    ? cita.DuracionMinutos
                    : 60;

                var fin = inicio.AddMinutes(duracion);

                var inicioStr = inicio.ToString("yyyyMMdd'T'HHmmss");
                var finStr = fin.ToString("yyyyMMdd'T'HHmmss");

                calendarUrl =
                "https://www.google.com/calendar/render?action=TEMPLATE" +
                $"&text={Uri.EscapeDataString($"Cita - {nombreServicio}")}" +
                $"&dates={inicioStr}/{finStr}" +
                $"&details={Uri.EscapeDataString($"Cita con {nombreEstilista} en {empresa?.NombreComercial}")}" +
                $"&location={Uri.EscapeDataString(empresa?.Direccion ?? empresa?.NombreComercial)}" +
                $"&ctz=America/Santo_Domingo" +
                "&reminder=1440" +   // 📅 1 día antes
                "&reminder=120" +    // ⏰ 2 horas antes
                "&reminder=30";      // ⏱️ 30 min antes

            }

            // ===============================
            // 📧 CONTENIDO DEL CORREO
            // ===============================
            string asunto = null;
            string mensaje = null;

            if (nuevoEstado == EstadoCita.Confirmada)
            {
                asunto = "✅ Cita confirmada";

                var infoAgendar = string.IsNullOrWhiteSpace(empresa?.InfoAgendar)
                    ? ""
                    : $@"
<hr/>
<h4>📌 Información importante</h4>
<div style='font-size:14px; line-height:1.6;'>
{empresa.InfoAgendar}
</div>";

                mensaje = $@"
<h3>Hola {nombreCliente},</h3>
<p>Tu cita ha sido <strong>confirmada</strong>.</p>

<p>
<strong>Servicio:</strong> {nombreServicio}<br/>
<strong>Fecha:</strong> {fechaTexto}<br/>
<strong>Hora:</strong> {horaTexto}<br/>
<strong>Estilista:</strong> {nombreEstilista}
</p>

<p>
📅 <strong>Agendar en tu calendario:</strong><br/>
<a href='{calendarUrl}' target='_blank'
   style='background:#1976D2;color:white;padding:10px 14px;
          border-radius:6px;text-decoration:none;display:inline-block;'>
Agregar a Google Calendar
</a>
</p>

{infoAgendar}

<p>Te esperamos 🙌</p>";
            }
            else if (nuevoEstado == EstadoCita.Cancelada)
            {
                asunto = "❌ Cita cancelada";

                mensaje = $@"
<h3>Hola {nombreCliente},</h3>
<p>Tu cita ha sido <strong>cancelada</strong>.</p>

<p>
<strong>Servicio:</strong> {nombreServicio}<br/>
<strong>Fecha:</strong> {fechaTexto}<br/>
<strong>Hora:</strong> {horaTexto}<br/>
<strong>Estilista:</strong> {nombreEstilista}
</p>

<p>Necesita reprogramar su cita. Favor contactarnos 🙌</p>";
            }

            // ===============================
            // 📤 ENVIAR CORREO
            // ===============================
            if (!string.IsNullOrEmpty(asunto) && smtpValido)
            {
                Utility.Send(
                    empresa.ServidorSMTP,
                    (int)empresa.PuertoSMTP,
                    (bool)empresa.UsaSSL,
                    empresa.CorreoSMTP,
                    empresa.PasswordSMTP,
                    empresa.NombreRemitente ?? empresa.NombreComercial,
                    cita.Correo,
                    asunto,
                    mensaje
                );
            }
        }

        private async Task RegistrarAnticipoConfirmacionAsync(Cita cita, int idUsuario)
        {
            var abono = Math.Round(cita.Abono ?? 0m, 2);
            if (abono <= 0)
                return;

            var servicio = await _productoRepository.GetByIdAsync(cita.IdProducto);
            var nombreServicio = servicio?.Nombre ?? "Servicio";
            var nombreCliente = string.IsNullOrWhiteSpace(cita.NombreCliente)
                ? "cliente"
                : cita.NombreCliente.Trim();

            if (!await _Ingresos.ExisteIngresoPorCita(cita.IdCita))
            {
                var banco = string.IsNullOrWhiteSpace(cita.Banco) ? "" : cita.Banco.Trim();
                await _Ingresos.InsertIngreso(new Ingresos
                {
                    IdEmpresa = cita.IdEmpresa,
                    FechaRegistro = DateTime.Now,
                    Descripcion = $"Reserva cita #{cita.IdCita} — {nombreServicio}",
                    Categoria = "Abono por Cita",
                    Origen = nombreCliente,
                    Monto = abono,
                    FormaPago = string.IsNullOrWhiteSpace(banco)
                        ? "Transferencia"
                        : $"Transferencia {banco}",
                    Referencia = $"Cita #{cita.IdCita}",
                    IdCliente = cita.IdCliente,
                    Nota = $"Depósito de reserva confirmado. Cliente {nombreCliente}. Tel {cita.Telefono}. Banco {banco}."
                });
            }

            if (!cita.IdCliente.HasValue || cita.IdCliente.Value <= 0)
                return;

            if (await _notasCredito.ExisteAnticipoPorCitaAsync(cita.IdEmpresa, cita.IdCita))
                return;

            await _notasCredito.CrearNotaCreditoComercialAsync(new CrearNotaCreditoComercialDto
            {
                IdEmpresa = cita.IdEmpresa,
                IdUsuario = idUsuario,
                IdCliente = cita.IdCliente,
                Concepto = $"Anticipo reserva cita #{cita.IdCita} — {nombreServicio}",
                Monto = abono,
                MontoItbis = 0
            });
        }





        public async Task EnviarRecordatorioPorFecha(DateTime fecha, int idEmpresa)
        {
            await EnviarRecordatoriosDelDiaAsync(fecha, idEmpresa);
        }

        public async Task<int> EnviarRecordatoriosDelDiaAsync(DateTime fecha, int? idEmpresa = null)
        {
            var fechaInicio = fecha.Date;
            var fechaFin = fechaInicio.AddDays(1);
            var enviados = 0;

            var citas = await _repository.GetAllByExpresionAsync(c =>
                c.Estado == "Confirmada"
                && c.Fecha >= fechaInicio
                && c.Fecha < fechaFin
                && (idEmpresa == null || c.IdEmpresa == idEmpresa.Value));

            foreach (var cita in citas)
            {
                var empresa = await _empresas.GetEmpresaById(cita.IdEmpresa);
                if (empresa == null)
                    continue;

                var smtpValido =
                    !string.IsNullOrWhiteSpace(empresa.CorreoSMTP) &&
                    !string.IsNullOrWhiteSpace(empresa.PasswordSMTP) &&
                    !string.IsNullOrWhiteSpace(empresa.ServidorSMTP);

                var servicio = await _productoRepository.GetByIdAsync(cita.IdProducto);
                var empleado = await _empleadoRepository.GetByIdAsync(cita.IdEmpleado);
                var nombreCliente = string.IsNullOrWhiteSpace(cita.NombreCliente)
                    ? "cliente"
                    : cita.NombreCliente;
                var nombreServicio = servicio?.Nombre ?? "Servicio";
                var nombreEstilista = empleado?.Nombre ?? "Estilista";
                var fechaTexto = FormatearFecha(cita.Fecha);
                var horaTexto = FormatearHora(cita);

                var yaRecordatorioHoy = cita.FechaRecordatorioWhatsApp.HasValue
                    && cita.FechaRecordatorioWhatsApp.Value.Date == DateTime.Now.Date;

                if (!yaRecordatorioHoy)
                {
                    await EnviarWhatsAppCitaAsync(
                        cita,
                        empresa,
                        WhatsAppCitaTipo.Recordatorio,
                        nombreCliente,
                        nombreServicio,
                        nombreEstilista,
                        fechaTexto,
                        horaTexto);

                    if (smtpValido && !string.IsNullOrWhiteSpace(cita.Correo))
                    {
                        var asunto = $"📅 Recordatorio de tu cita en {empresa.NombreComercial}";
                        var mensaje = $@"
                <h3>Hola {nombreCliente},</h3>
                <p>Te recordamos tu cita programada en <strong>{empresa.NombreComercial}</strong>.</p>
                <p>
                <strong>Servicio:</strong> {nombreServicio}<br/>
                <strong>Fecha:</strong> {fechaTexto}<br/>
                <strong>Hora:</strong> {horaTexto}<br/>
                <strong>Estilista:</strong> {nombreEstilista}
                </p>
               <p>Te esperamos 🙌</p>
               <hr/>
               <small>
               Este es un recordatorio automático de tu cita en 
               <strong>{empresa.NombreComercial}</strong>.
               </small>
               ";
                        Utility.Send(
                            empresa.ServidorSMTP,
                            (int)empresa.PuertoSMTP,
                            (bool)empresa.UsaSSL,
                            empresa.CorreoSMTP,
                            empresa.PasswordSMTP,
                            empresa.NombreRemitente ?? empresa.NombreComercial,
                            cita.Correo,
                            asunto,
                            mensaje
                        );
                    }

                    cita.FechaRecordatorioWhatsApp = DateTime.Now;
                    _repository.Update(cita.IdCita, cita);
                    enviados++;
                }
            }

            return enviados;
        }

        private async Task EnviarWhatsAppCitaAsync(
            Cita cita,
            Empresas? empresa,
            WhatsAppCitaTipo tipo,
            string nombreCliente,
            string nombreServicio,
            string nombreEstilista,
            string fechaTexto,
            string horaTexto)
        {
            if (empresa != null && !empresa.NotificarCitasWhatsApp)
                return;
            if (!_whatsApp.EstaListo)
                return;

            try
            {
                await _whatsApp.EnviarCitaAsync(new WhatsAppCitaMensaje
                {
                    Tipo = tipo,
                    Telefono = cita.Telefono ?? "",
                    NombreCliente = nombreCliente,
                    NombreSalon = empresa?.NombreComercial ?? "tu salón",
                    Servicio = nombreServicio,
                    Estilista = nombreEstilista,
                    Fecha = fechaTexto,
                    Hora = horaTexto,
                    IdEmpresa = cita.IdEmpresa,
                    IdCita = cita.IdCita
                });
            }
            catch
            {
                // La cita no debe fallar si WhatsApp no sale.
            }
        }

        private static string FormatearFecha(DateTime fecha)
            => fecha == default ? "—" : fecha.ToString("dd/MM/yyyy");

        private static string FormatearHora(Cita cita)
        {
            if (cita.Hora == TimeSpan.Zero)
                return "—";
            var fechaHoraLocal = cita.Fecha.Date.Add(cita.Hora);
            return fechaHoraLocal.ToString("h:mm tt", CulturaDo)
                .Replace("AM", "a. m.")
                .Replace("PM", "p. m.");
        }

        private bool EsTransicionValida(EstadoCita actual, EstadoCita nuevo)
        {
            return actual switch
            {
                EstadoCita.Programada =>
                    nuevo == EstadoCita.Confirmada ||
                    nuevo == EstadoCita.Cancelada,

                EstadoCita.Confirmada =>
                    nuevo == EstadoCita.EnCurso || nuevo == EstadoCita.Cancelada,



                EstadoCita.EnCurso =>
                    nuevo == EstadoCita.Completada,

                EstadoCita.Completada =>
                    false, // estado final

                EstadoCita.Cancelada =>
                    false, // estado final

                _ => false
            };
        }

        public async Task<IEnumerable<CitaDto>> GetAllCitas(int IdEmpresa)
        {
            var citas = await _repository.GetAllByExpresionAsync(c => c.IdEmpresa == IdEmpresa);

            var result = new List<CitaDto>();

            foreach (var c in citas)
            {
                // Buscar estilista
                var estilista = await _empleadoRepository.GetByIdAsync(c.IdEmpleado);

                // Buscar servicio
                var producto =  _productoRepository.GetById(c.IdProducto);

                result.Add(new CitaDto
                {
                    IdCita = c.IdCita,
                    IdEmpleado = c.IdEmpleado,
                    NombreEstilista = estilista?.Nombre ?? "No asignado",
                    RutaReciboPago = c.RutaReciboPago,
                    IdProducto = c.IdProducto,
                    NombreServicio = producto?.Nombre ?? "—",

                    Fecha = c.Fecha,
                    Hora = c.Hora,
                    HoraFin = c.HoraFin,
                    DuracionMinutos = c.DuracionMinutos,
                    Banco=c.Banco,
                    Abono=c.Abono,
                    Estado = c.Estado,
                    Nota = c.Nota,
                    Costo = c.Costo,

                    NombreCliente = c.NombreCliente,
                    Telefono = c.Telefono,
                    Correo = c.Correo,

                    IdEmpresa = c.IdEmpresa
                });
            }

            return result;
        }


        public async Task<Cita> GetCitaById(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task InsertCita(Cita cita)
        {
            // Obtener producto
            var prod = await _productoRepository.GetByIdAsync(cita.IdProducto);
            if (prod == null)
                throw new Exception("El servicio no existe.");

            cita.DuracionMinutos = prod.DuracionServicio;
            cita.Costo = prod.PrecioVenta;

            // Calcular hora fin
            cita.HoraFin = cita.Hora.Add(TimeSpan.FromMinutes(cita.DuracionMinutos));

            await _repository.Save(cita);
        }

        public void UpdateCita(Cita cita)
        {
            _repository.Update(cita.IdCita, cita);
        }

        public async Task<IEnumerable<Cita>> GetCitasByEmpleado(int idEmpleado, int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(c =>
                c.IdEmpleado == idEmpleado &&
                c.IdEmpresa == idEmpresa);
        }

        public async Task<IEnumerable<Cita>> GetCitasByCliente(int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(c => c.IdEmpresa == idEmpresa);
        }

        public async Task<int> TotalCitasDelMes(int idEmpresa)
        {
            var citas = await _repository.GetAllByExpresionAsync(c =>
                c.IdEmpresa == idEmpresa &&
                c.Fecha.Month == DateTime.Now.Month &&
                c.Fecha.Year == DateTime.Now.Year);

            return citas?.Count() ?? 0;
        }

        // ===========================================================
        // 🔥 Citas del día con estilista + servicio
        // ===========================================================
        public async Task<IEnumerable<CitaDto>> GetCitasConEmpleado(int idEmpresa)
        {
            var hoy = DateTime.Today;

            var citas = await _repository.GetAllByExpresionAsync(c =>
                c.IdEmpresa == idEmpresa &&
                c.Fecha.Date == hoy);

            var empleados = await _empleadoRepository.GetAllByExpresionAsync(e =>
                e.IdEmpresa == idEmpresa);

            var productos = await _productoRepository.GetAllByExpresionAsync(p =>
                p.IdEmpresa == idEmpresa);

            var query =
                from c in citas

                    // 🔥 INNER JOIN (OBLIGATORIO)
                join e in empleados
                    on c.IdEmpleado equals e.IdEmpleados

                // Producto puede ser opcional
                join p in productos
                    on c.IdProducto equals p.IdProducto into prodJoin
                from p in prodJoin.DefaultIfEmpty()

                select new CitaDto
                {
                    IdCita = c.IdCita,
                    IdProducto = c.IdProducto,
                    NombreServicio = p?.Nombre,

                    NombreCliente = c.NombreCliente,
                    IdEmpleado = c.IdEmpleado,
                    NombreEstilista = e.Nombre, // 👈 YA NO PUEDE SER NULL

                    Fecha = c.Fecha,
                    Hora = c.Hora,
                    HoraFin = c.HoraFin,
                    DuracionMinutos = c.DuracionMinutos,

                    Estado = c.Estado,
                    Nota = c.Nota,

                    Telefono = c.Telefono,
                    Correo = c.Correo,
                    Costo = c.Costo
                };

            return query.ToList();
        }

    }
}
