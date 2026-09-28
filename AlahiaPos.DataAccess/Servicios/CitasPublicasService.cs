using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using PrinterLibrary;

namespace AlahiaPos.DataAccess.Servicios
{
    public class CitasPublicasService : ICitasPublicasService
    {
        private readonly IEmpresas _empresas;
        private readonly IProductos _productos;
        private readonly IEmpleados _empleados;
        private readonly IHorariosEstilista _horarios;
        private readonly ICitas _citas;
        private readonly IClientes _clientes;
        private readonly INotification _notification;
        private readonly IWhatsAppCitas _whatsApp;

        public CitasPublicasService(
            IEmpresas empresas,
            IProductos productos,
            IEmpleados empleados,
            IHorariosEstilista horarios,
            ICitas citas,
            IClientes clientes,
            INotification notification,
            IWhatsAppCitas whatsApp)
        {
            _empresas = empresas;
            _productos = productos;
            _empleados = empleados;
            _horarios = horarios;
            _citas = citas;
            _clientes = clientes;
            _notification = notification;
            _whatsApp = whatsApp;
        }

        public async Task<CitaPublicaSalonDto> ObtenerSalonAsync(string guid)
        {
            var empresa = await ResolverEmpresa(guid);
            var servicios = (await _productos.GetAllProductos(empresa.IdEmpresa))
                .Where(p => p.DisponibleEnCitas)
                .OrderBy(p => p.Nombre)
                .Select(p => new CitaPublicaServicioDto
                {
                    IdProducto = p.IdProducto,
                    Nombre = p.Nombre ?? "",
                    Descripcion = p.Descripcion,
                    Precio = p.PrecioVenta,
                    DuracionMinutos = p.DuracionServicio > 0 ? p.DuracionServicio : 60,
                    Imagen = p.Imagen1
                })
                .ToList();

            var empleados = (await _empleados.GetAllEmpleados(empresa.IdEmpresa))
                .Where(e => e.Estado)
                .ToList();

            var estilistas = new List<CitaPublicaEstilistaDto>();
            foreach (var emp in empleados)
            {
                var horarios = await _horarios.GetHorariosByEmpleadoByEmpresa(emp.IdEmpleados, empresa.IdEmpresa);
                var dias = (horarios ?? Enumerable.Empty<HorariosEstilista>())
                    .Select(h => h.DiaSemana)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToArray();
                if (dias.Length == 0) continue;
                estilistas.Add(new CitaPublicaEstilistaDto
                {
                    IdEmpleado = emp.IdEmpleados,
                    Nombre = emp.Nombre ?? "",
                    DiasDisponibles = dias
                });
            }

            return new CitaPublicaSalonDto
            {
                GuidPublico = empresa.GuidPublico,
                NombreComercial = empresa.NombreComercial ?? "",
                Telefono = empresa.Telefono,
                Direccion = empresa.Direccion,
                LogoUrl = empresa.Logo,
                InfoAgendar = empresa.InfoAgendar,
                PedirVoucherCitas = empresa.PedirVoucherCitas,
                MontoReservaCitas = empresa.MontoReservaCitas,
                PrimaryColor = empresa.PrimaryColor,
                TitleColor = empresa.titleColor,
                Latitude = empresa.Latitude,
                Longitude = empresa.Longitude,
                Servicios = servicios,
                Estilistas = estilistas
            };
        }

        public async Task<CitaPublicaDisponibilidadDto> ObtenerDisponibilidadAsync(
            string guid, int idEmpleado, string fecha)
        {
            var empresa = await ResolverEmpresa(guid);
            if (!TryParseFecha(fecha, out var dia))
                throw new ArgumentException("Fecha inválida. Use yyyy-MM-dd.");

            var horas = await CalcularHorasDisponibles(empresa.IdEmpresa, idEmpleado, dia);
            return new CitaPublicaDisponibilidadDto
            {
                Fecha = dia.ToString("yyyy-MM-dd"),
                HorasDisponibles = horas
            };
        }

        public async Task<CitaPublicaConfirmacionDto> CrearCitaAsync(string guid, CitaPublicaCrearRequest request)
        {
            var empresa = await ResolverEmpresa(guid);
            request ??= new CitaPublicaCrearRequest();

            var nombre = (request.NombreCliente ?? "").Trim();
            var telefono = SoloDigitos(request.Telefono);
            if (nombre.Length < 3)
                throw new ArgumentException("Indique su nombre y apellido.");
            if (telefono.Length != 10)
                throw new ArgumentException("Indique un teléfono válido (10 dígitos).");
            if (request.IdProducto <= 0)
                throw new ArgumentException("Seleccione un servicio.");
            if (request.IdEmpleado <= 0)
                throw new ArgumentException("Seleccione un estilista.");
            if (!TryParseFecha(request.Fecha, out var fecha))
                throw new ArgumentException("Fecha inválida.");
            if (!TimeSpan.TryParse(request.Hora, CultureInfo.InvariantCulture, out var hora))
                throw new ArgumentException("Hora inválida.");

            var producto = await _productos.GetAllProductosById(request.IdProducto);
            if (producto == null || producto.IdEmpresa != empresa.IdEmpresa || !producto.DisponibleEnCitas)
                throw new ArgumentException("El servicio no está disponible para citas.");

            var empleado = await _empleados.GetEmpleadoById(request.IdEmpleado);
            if (empleado == null || empleado.IdEmpresa != empresa.IdEmpresa || !empleado.Estado)
                throw new ArgumentException("El estilista no está disponible.");

            var duracion = producto.DuracionServicio > 0 ? producto.DuracionServicio : 60;
            var horaFin = hora.Add(TimeSpan.FromMinutes(duracion));

            var horarios = await _horarios.GetHorariosByEmpleadoByEmpresa(request.IdEmpleado, empresa.IdEmpresa);
            var diaSemana = (int)fecha.DayOfWeek;
            var horarioValido = (horarios ?? Enumerable.Empty<HorariosEstilista>()).Any(h =>
                h.DiaSemana == diaSemana &&
                hora >= h.HoraInicio &&
                horaFin <= h.HoraFin);
            if (!horarioValido)
                throw new ArgumentException("El estilista no trabaja en este rango horario.");

            var horasLibres = await CalcularHorasDisponibles(empresa.IdEmpresa, request.IdEmpleado, fecha);
            var horaStr = hora.ToString(@"hh\:mm");
            if (!HoraCubreDuracion(horaStr, duracion, horasLibres))
                throw new ArgumentException("Este horario ya no está disponible.");

            var cliente = await _clientes.BuscarPorTelefono(telefono, empresa.IdEmpresa);
            if (cliente == null)
            {
                cliente = new Clientes
                {
                    NombreComercial = nombre,
                    Telefono = telefono,
                    Celular = telefono,
                    Email = (request.Correo ?? "").Trim(),
                    Estado = true,
                    FechaInseccion = DateTime.Now,
                    IdEmpresa = empresa.IdEmpresa,
                    LimiteCredito = 0
                };
                await _clientes.InsertClientes(cliente);
            }

            var pedirVoucher = empresa.PedirVoucherCitas;
            decimal abono = 0;
            string? banco = null;
            string? rutaRecibo = null;

            if (pedirVoucher)
            {
                var monto = Math.Round(empresa.MontoReservaCitas, 2);
                if (monto <= 0)
                    throw new ArgumentException("El salón no configuró el monto de la reserva.");

                if (request.Abono.HasValue && Math.Round(request.Abono.Value, 2) != monto)
                    throw new ArgumentException($"El monto de la reserva debe ser RD$ {monto:N2}.");

                if (request.ReciboPago == null || request.ReciboPago.Length <= 0)
                    throw new ArgumentException("Debe subir el voucher del depósito.");

                if (request.ReciboPago.Length > 8 * 1024 * 1024)
                    throw new ArgumentException("El voucher no puede superar 8 MB.");

                var ext = Path.GetExtension(request.ReciboPago.FileName ?? "").ToLowerInvariant();
                if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".pdf"))
                    throw new ArgumentException("El voucher debe ser imagen o PDF.");

                using var ms = new MemoryStream();
                await request.ReciboPago.CopyToAsync(ms);
                rutaRecibo = Utility.UploadFileFtp(ms.ToArray(), $"{Guid.NewGuid()}{ext}");
                abono = monto;
                banco = string.IsNullOrWhiteSpace(request.Banco) ? null : request.Banco.Trim();
            }

            var cita = new Cita
            {
                IdEmpresa = empresa.IdEmpresa,
                IdEmpleado = request.IdEmpleado,
                IdProducto = request.IdProducto,
                IdCliente = cliente.IDCliente,
                NombreCliente = nombre,
                Telefono = telefono,
                Correo = (request.Correo ?? "").Trim(),
                Fecha = fecha,
                Hora = hora,
                HoraFin = horaFin,
                DuracionMinutos = duracion,
                Costo = producto.PrecioVenta,
                Estado = "Programada",
                Nota = (request.Nota ?? "").Trim(),
                esSeguimiento = !pedirVoucher,
                Abono = abono,
                Banco = banco,
                RutaReciboPago = rutaRecibo,
                FechaInseccion = DateTime.Now
            };

            await _citas.InsertCita(cita);

            var fechaTxt = fecha.ToString("dd/MM/yyyy");
            var horaTxt = hora.ToString(@"hh\:mm");
            try
            {
                await _notification.EnviarNotificacionPorTagAsync(
                    "empresa_id",
                    empresa.IdEmpresa.ToString(),
                    "📅 Nueva cita creada",
                    $"{nombre} reservó {producto.Nombre} el {fechaTxt} a las {horaTxt}");
            }
            catch
            {
                // La cita ya quedó guardada.
            }

            try
            {
                if (empresa.NotificarCitasWhatsApp && _whatsApp.EstaListo)
                {
                    var horaCliente = fecha.Date.Add(hora).ToString(
                        "h:mm tt",
                        new CultureInfo("es-DO"));
                    await _whatsApp.EnviarCitaAsync(new WhatsAppCitaMensaje
                    {
                        Tipo = WhatsAppCitaTipo.Recibida,
                        Telefono = telefono,
                        NombreCliente = nombre,
                        NombreSalon = empresa.NombreComercial ?? "tu salón",
                        Servicio = producto.Nombre ?? "Servicio",
                        Estilista = empleado.Nombre ?? "Estilista",
                        Fecha = fecha.ToString("dd/MM/yyyy"),
                        Hora = horaCliente,
                        IdEmpresa = empresa.IdEmpresa,
                        IdCita = cita.IdCita
                    });
                }
            }
            catch
            {
                // La cita ya quedó guardada.
            }

            return new CitaPublicaConfirmacionDto
            {
                IdCita = cita.IdCita,
                NombreCliente = nombre,
                Servicio = producto.Nombre ?? "",
                Estilista = empleado.Nombre ?? "",
                Fecha = fecha.ToString("yyyy-MM-dd"),
                Hora = horaTxt,
                HoraFin = horaFin.ToString(@"hh\:mm"),
                Estado = "Programada",
                Mensaje = pedirVoucher
                    ? "Tu cita fue recibida. El salón confirmará el voucher y te avisa."
                    : "Tu cita fue recibida por el salón. Te confirmarán por correo o WhatsApp."
            };
        }

        public async Task<List<CitaPublicaItemDto>> ListarCitasClienteAsync(string guid, string telefono)
        {
            var empresa = await ResolverEmpresa(guid);
            var tel = SoloDigitos(telefono);
            if (tel.Length < 10)
                return new List<CitaPublicaItemDto>();

            var citas = await _citas.GetAllCitas(empresa.IdEmpresa);
            return (citas ?? Enumerable.Empty<CitaDto>())
                .Where(c => SoloDigitos(c.Telefono) == tel || SoloDigitos(c.Telefono).EndsWith(tel.Substring(tel.Length - 10)))
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.Hora)
                .Take(20)
                .Select(c => new CitaPublicaItemDto
                {
                    IdCita = c.IdCita,
                    Servicio = c.NombreServicio ?? "",
                    Estilista = c.NombreEstilista ?? "",
                    Fecha = c.Fecha.ToString("yyyy-MM-dd"),
                    Hora = c.Hora.ToString(@"hh\:mm"),
                    Estado = c.Estado ?? "",
                    Nota = c.Nota
                })
                .ToList();
        }

        private async Task<Empresas> ResolverEmpresa(string guid)
        {
            if (!Guid.TryParse((guid ?? "").Trim(), out var g))
                throw new ArgumentException("Link de citas inválido.");
            var empresa = await _empresas.GetEmpresaByGUID(g);
            if (empresa == null)
                throw new ArgumentException("Este salón no existe.");
            return empresa;
        }

        private async Task<List<string>> CalcularHorasDisponibles(int idEmpresa, int idEmpleado, DateTime fecha)
        {
            var horarios = await _horarios.GetHorariosByEmpleado(idEmpleado);
            var diaSemana = (int)fecha.DayOfWeek;
            var horarioDia = (horarios ?? Enumerable.Empty<HorariosEstilista>())
                .Where(h => h.DiaSemana == diaSemana && h.IdEmpresa == idEmpresa)
                .ToList();
            if (!horarioDia.Any())
                return new List<string>();

            var citas = await _citas.GetCitasByEmpleado(idEmpleado, idEmpresa);
            var citasDelDia = (citas ?? Enumerable.Empty<Cita>())
                .Where(c => c.Fecha.Date == fecha.Date)
                .ToList();

            var horasBloqueadas = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in citasDelDia)
            {
                var actual = c.Hora;
                var fin = c.HoraFin;
                while (actual < fin)
                {
                    horasBloqueadas.Add(actual.ToString(@"hh\:mm"));
                    actual += TimeSpan.FromMinutes(30);
                }
            }

            var disponibles = new List<string>();
            foreach (var h in horarioDia)
            {
                var horaActual = h.HoraInicio;
                while (horaActual < h.HoraFin)
                {
                    if (h.RecesoInicio.HasValue && h.RecesoFin.HasValue
                        && horaActual >= h.RecesoInicio.Value && horaActual < h.RecesoFin.Value)
                    {
                        horaActual += TimeSpan.FromMinutes(30);
                        continue;
                    }

                    var horaStr = horaActual.ToString(@"hh\:mm");
                    if (!horasBloqueadas.Contains(horaStr))
                        disponibles.Add(horaStr);
                    horaActual += TimeSpan.FromMinutes(30);
                }
            }

            if (fecha.Date == DateTime.Now.Date)
            {
                var ahora = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
                disponibles = disponibles.Where(h =>
                {
                    var p = h.Split(':');
                    return (int.Parse(p[0]) * 60 + int.Parse(p[1])) >= ahora;
                }).ToList();
            }

            return disponibles.Distinct().OrderBy(h => h).ToList();
        }

        private static bool HoraCubreDuracion(string horaInicio, int duracion, List<string> horas)
        {
            if (!horas.Contains(horaInicio)) return false;
            var partes = horaInicio.Split(':');
            var inicio = int.Parse(partes[0]) * 60 + int.Parse(partes[1]);
            var fin = inicio + duracion;
            for (var t = inicio + 30; t < fin; t += 30)
            {
                var slot = $"{t / 60:00}:{t % 60:00}";
                if (!horas.Contains(slot)) return false;
            }
            return true;
        }

        private static bool TryParseFecha(string? raw, out DateTime fecha)
        {
            return DateTime.TryParseExact(
                (raw ?? "").Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fecha);
        }

        private static string SoloDigitos(string? s)
        {
            return new string((s ?? "").Where(char.IsDigit).ToArray());
        }
    }
}
