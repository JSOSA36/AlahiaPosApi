using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Enum; // donde esté EstadoCita
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using PrinterLibrary;
using System;
using System.Collections.Generic;
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
       
        public CitaServices(
            IRepository<Cita> repository,
            IRepository<Empleados> empleadoRepository,
            IRepository<Productos> productoRepository,
            IIngresos ingresos,
            IEmpresas empresas  )
        {
            _repository = repository;
            _empleadoRepository = empleadoRepository;
            _productoRepository = productoRepository;
            _Ingresos = ingresos;
            _empresas = empresas;
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
        public async Task CambiarEstadoCita(int idCita, EstadoCita nuevoEstado)
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

            if (string.IsNullOrWhiteSpace(cita.Correo))
                return;

            var servicio = await _productoRepository.GetByIdAsync(cita.IdProducto);
            var empleado = await _empleadoRepository.GetByIdAsync(cita.IdEmpleado);

            var nombreCliente = string.IsNullOrWhiteSpace(cita.NombreCliente)
                ? "cliente"
                : cita.NombreCliente;

            var nombreServicio = servicio?.Nombre ?? "Servicio";
            var nombreEstilista = empleado?.Nombre ?? "Estilista";

            var fechaTexto = cita.Fecha != default
                ? cita.Fecha.ToString("dd/MM/yyyy")
                : "—";

            var horaTexto = "—";

            if (cita.Hora != TimeSpan.Zero)
            {
                var fechaHoraLocal = cita.Fecha.Date.Add(cita.Hora);

                horaTexto = fechaHoraLocal.ToString("h:mm tt",
                    new System.Globalization.CultureInfo("es-DO"))
                    .Replace("AM", "a. m.")
                    .Replace("PM", "p. m.");
            }


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

            // ===============================
            // 💰 REGISTRAR INGRESO (SOLO UNA VEZ)
            // ===============================
            if (nuevoEstado == EstadoCita.Confirmada && cita.Abono > 0)
            {
                var existeIngreso = await _Ingresos.ExisteIngresoPorCita(cita.IdCita);

                if (!existeIngreso)
                {
                    var ingreso = new Ingresos
                    {
                        IdEmpresa = cita.IdEmpresa,
                        FechaRegistro = DateTime.Now,
                        Descripcion = "Abono por cita confirmada",
                        Categoria = "Abono por Cita",
                        Origen = nombreCliente,
                        Monto = (decimal)cita.Abono,
                        FormaPago = $"Transferencia {cita.Banco}",
                        Referencia = $"Cita #{cita.IdCita}",
                        IdCliente = cita.IdCliente,
                        Nota = $"Abono recibido al confirmar la cita #{cita.IdCita} para {nombreServicio}"
                    };

                    await _Ingresos.InsertIngreso(ingreso);
                }
            }
        }





        public async Task EnviarRecordatorioPorFecha(DateTime fecha, int idEmpresa)
        {
            var fechaInicio = fecha.Date;
            var fechaFin = fechaInicio.AddDays(1);

            // 🔥 Obtener empresa (SMTP + Nombre)
            var empresa = await _empresas.GetEmpresaById(idEmpresa);

            if (empresa == null)
                throw new Exception("Empresa no encontrada.");

            bool smtpValido =
                !string.IsNullOrWhiteSpace(empresa.CorreoSMTP) &&
                !string.IsNullOrWhiteSpace(empresa.PasswordSMTP) &&
                !string.IsNullOrWhiteSpace(empresa.ServidorSMTP);

            if (!smtpValido)
                return;

            // 🔥 Buscar citas confirmadas de ese día
            var citasConfirmadas = await _repository.GetAllByExpresionAsync(c =>
                c.IdEmpresa == idEmpresa &&
                c.Estado == "Confirmada" &&
                c.Fecha >= fechaInicio &&
                c.Fecha < fechaFin
            );

            foreach (var cita in citasConfirmadas)
            {
                if (string.IsNullOrWhiteSpace(cita.Correo))
                    continue;

                var servicio = await _productoRepository.GetByIdAsync(cita.IdProducto);
                var empleado = await _empleadoRepository.GetByIdAsync(cita.IdEmpleado);

                var nombreCliente = string.IsNullOrWhiteSpace(cita.NombreCliente)
                    ? "cliente"
                    : cita.NombreCliente;

                var nombreServicio = servicio?.Nombre ?? "Servicio";
                var nombreEstilista = empleado?.Nombre ?? "Estilista";

                var fechaTexto = cita.Fecha.ToString("dd/MM/yyyy");

                var horaTexto = "—";

                if (cita.Hora != TimeSpan.Zero)
                {
                    var fechaHoraLocal = cita.Fecha.Date.Add(cita.Hora);

                    horaTexto = fechaHoraLocal.ToString("h:mm tt",
                        new System.Globalization.CultureInfo("es-DO"))
                        .Replace("AM", "a. m.")
                        .Replace("PM", "p. m.");
                }

                // ===============================
                // 📧 ASUNTO
                // ===============================
                var asunto = $"📅 Recordatorio de tu cita en {empresa.NombreComercial}";

                // ===============================
                // 📧 MENSAJE
                // ===============================
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

                // ===============================
                // 📤 ENVIAR CORREO
                // ===============================
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
