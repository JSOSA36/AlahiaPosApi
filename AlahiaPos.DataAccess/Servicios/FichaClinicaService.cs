using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class FichaClinicaService : IFichaClinica
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private static readonly string[] NumerosFdi =
        {
            "18","17","16","15","14","13","12","11",
            "21","22","23","24","25","26","27","28",
            "48","47","46","45","44","43","42","41",
            "31","32","33","34","35","36","37","38"
        };

        private readonly IRepository<FichasClinicas> _fichas;
        private readonly IClientes _clientes;
        private readonly IRepository<Empresas> _empresas;
        private readonly IRepository<FacturaHeaders> _facturas;
        private readonly IRepository<FacturaDetalles> _detalles;
        private readonly IRepository<Productos> _productos;

        public FichaClinicaService(
            IRepository<FichasClinicas> fichas,
            IClientes clientes,
            IRepository<Empresas> empresas,
            IRepository<FacturaHeaders> facturas,
            IRepository<FacturaDetalles> detalles,
            IRepository<Productos> productos)
        {
            _fichas = fichas;
            _clientes = clientes;
            _empresas = empresas;
            _facturas = facturas;
            _detalles = detalles;
            _productos = productos;
        }

        public async Task<FichaClinicaVistaDto?> GetVista(int idEmpresa, int idCliente)
        {
            var cliente = await _clientes.GetAllClientesById(idCliente);
            if (cliente == null || cliente.IdEmpresa != idEmpresa)
                return null;

            var entidad = (await _fichas.GetAllByExpresionAsync(
                    f => f.IdEmpresa == idEmpresa && f.IdCliente == idCliente))
                .FirstOrDefault();

            return await ArmarVista(idEmpresa, cliente, entidad);
        }

        public async Task<FichaClinicaVistaDto> Guardar(FichaClinicaDto dto)
        {
            if (dto.IdEmpresa <= 0 || dto.IdCliente <= 0)
                throw new ArgumentException("Empresa y cliente son obligatorios.");

            var cliente = await _clientes.GetAllClientesById(dto.IdCliente);
            if (cliente == null || cliente.IdEmpresa != dto.IdEmpresa)
                throw new InvalidOperationException("El paciente no pertenece a esta empresa.");

            SincronizarCliente(cliente, dto);

            var existente = (await _fichas.GetAllByExpresionAsync(
                    f => f.IdEmpresa == dto.IdEmpresa && f.IdCliente == dto.IdCliente))
                .FirstOrDefault();

            var ahora = DateTime.Now;
            if (existente == null)
            {
                existente = new FichasClinicas
                {
                    IdEmpresa = dto.IdEmpresa,
                    IdCliente = dto.IdCliente,
                    IdUsuarioCreacion = dto.IdUsuarioCreacion,
                    FechaCreacion = ahora
                };
                AplicarFicha(existente, dto, ahora);
                await _fichas.Save(existente);
            }
            else
            {
                AplicarFicha(existente, dto, ahora);
                existente.IdUsuarioModificacion = dto.IdUsuarioModificacion ?? dto.IdUsuarioCreacion;
                _fichas.Update(existente.IdFichaClinica, existente);
            }

            _clientes.UpdateClientes(cliente.IDCliente, cliente);

            var recargado = (await _fichas.GetAllByExpresionAsync(
                    f => f.IdEmpresa == dto.IdEmpresa && f.IdCliente == dto.IdCliente))
                .FirstOrDefault() ?? existente;

            var clienteActual = await _clientes.GetAllClientesById(dto.IdCliente) ?? cliente;
            return await ArmarVista(dto.IdEmpresa, clienteActual, recargado);
        }

        private async Task<FichaClinicaVistaDto> ArmarVista(
            int idEmpresa,
            Clientes cliente,
            FichasClinicas? entidad)
        {
            var empresa = await _empresas.GetByIdAsync(idEmpresa);
            var cuenta = await ObtenerDocumentos(idEmpresa, cliente.IDCliente, 1);
            var ordenes = await ObtenerDocumentos(idEmpresa, cliente.IDCliente, 10);
            var ficha = MapearFicha(entidad, cliente);

            return new FichaClinicaVistaDto
            {
                Ficha = ficha,
                Cliente = MapearCliente(cliente),
                Cuenta = cuenta,
                Ordenes = ordenes,
                TotalCosto = cuenta.Sum(c => c.Costo),
                TotalPagos = cuenta.Sum(c => c.Pagos),
                TotalBalance = cuenta.Sum(c => c.Balance),
                TotalOrdenes = ordenes.Sum(c => c.Costo),
                NombreEmpresa = empresa?.NombreComercial,
                Existe = entidad != null && entidad.IdFichaClinica > 0
            };
        }

        private static FichaClinicaDto MapearFicha(FichasClinicas? entidad, Clientes cliente)
        {
            var nombres = entidad?.Nombres;
            var apellidos = entidad?.Apellidos;
            if (string.IsNullOrWhiteSpace(nombres) && string.IsNullOrWhiteSpace(apellidos))
                nombres = cliente.NombreComercial;

            return new FichaClinicaDto
            {
                IdFichaClinica = entidad?.IdFichaClinica ?? 0,
                IdEmpresa = cliente.IdEmpresa,
                IdCliente = cliente.IDCliente,
                Nombres = nombres,
                Apellidos = apellidos,
                Sexo = entidad?.Sexo,
                EstadoCivil = entidad?.EstadoCivil,
                Nacionalidad = entidad?.Nacionalidad,
                ContactoEmergenciaNombre = entidad?.ContactoEmergenciaNombre,
                ContactoEmergenciaTelefono = entidad?.ContactoEmergenciaTelefono,
                Anamnesis = DeserializarAnamnesis(entidad?.AnamnesisJson),
                Dientes = DeserializarDientes(entidad?.OdontogramaJson),
                Medicamentos = entidad?.Medicamentos,
                Observaciones = entidad?.Observaciones,
                Color = entidad?.Color,
                TipoProtesis = entidad?.TipoProtesis,
                Laboratorio = entidad?.Laboratorio,
                IdUsuarioCreacion = entidad?.IdUsuarioCreacion ?? 0,
                IdUsuarioModificacion = entidad?.IdUsuarioModificacion,
                FechaCreacion = entidad?.FechaCreacion,
                FechaModificacion = entidad?.FechaModificacion,
                CedulaRnc = cliente.CedulaRNC,
                Telefono = cliente.Telefono,
                Celular = cliente.Celular,
                Email = cliente.Email,
                Direccion = cliente.Direccion,
                FechaNacimiento = cliente.FechaNacimiento
            };
        }

        private static FichaClinicaClienteDto MapearCliente(Clientes cliente)
        {
            return new FichaClinicaClienteDto
            {
                IdCliente = cliente.IDCliente,
                NombreComercial = cliente.NombreComercial,
                CedulaRnc = cliente.CedulaRNC,
                Telefono = cliente.Telefono,
                Celular = cliente.Celular,
                Email = cliente.Email,
                Direccion = cliente.Direccion,
                FechaNacimiento = cliente.FechaNacimiento,
                Edad = CalcularEdad(cliente.FechaNacimiento)
            };
        }

        private static void AplicarFicha(FichasClinicas entidad, FichaClinicaDto dto, DateTime ahora)
        {
            entidad.Nombres = TrimOrNull(dto.Nombres);
            entidad.Apellidos = TrimOrNull(dto.Apellidos);
            entidad.Sexo = TrimOrNull(dto.Sexo);
            entidad.EstadoCivil = TrimOrNull(dto.EstadoCivil);
            entidad.Nacionalidad = TrimOrNull(dto.Nacionalidad);
            entidad.ContactoEmergenciaNombre = TrimOrNull(dto.ContactoEmergenciaNombre);
            entidad.ContactoEmergenciaTelefono = TrimOrNull(dto.ContactoEmergenciaTelefono);
            entidad.AnamnesisJson = JsonSerializer.Serialize(dto.Anamnesis ?? new FichaClinicaAnamnesisDto(), JsonOpts);
            entidad.OdontogramaJson = JsonSerializer.Serialize(NormalizarDientes(dto.Dientes), JsonOpts);
            entidad.Medicamentos = TrimOrNull(dto.Medicamentos);
            entidad.Observaciones = TrimOrNull(dto.Observaciones);
            entidad.Color = TrimOrNull(dto.Color);
            entidad.TipoProtesis = TrimOrNull(dto.TipoProtesis);
            entidad.Laboratorio = TrimOrNull(dto.Laboratorio);
            entidad.FechaModificacion = ahora;
            if (entidad.IdUsuarioCreacion <= 0 && dto.IdUsuarioCreacion > 0)
                entidad.IdUsuarioCreacion = dto.IdUsuarioCreacion;
        }

        private static void SincronizarCliente(Clientes cliente, FichaClinicaDto dto)
        {
            var compuesto = string.Join(" ", new[] { dto.Nombres, dto.Apellidos }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim()));

            if (!string.IsNullOrWhiteSpace(compuesto))
                cliente.NombreComercial = compuesto;

            if (dto.CedulaRnc != null)
                cliente.CedulaRNC = dto.CedulaRnc.Trim();
            if (dto.Telefono != null)
                cliente.Telefono = dto.Telefono.Trim();
            if (dto.Celular != null)
                cliente.Celular = dto.Celular.Trim();
            if (dto.Email != null)
                cliente.Email = dto.Email.Trim();
            if (dto.Direccion != null)
                cliente.Direccion = dto.Direccion.Trim();
            if (dto.FechaNacimiento.HasValue)
                cliente.FechaNacimiento = dto.FechaNacimiento;
        }

        private async Task<List<FichaClinicaCuentaLineaDto>> ObtenerDocumentos(
            int idEmpresa, int idCliente, int idTipoDocumento)
        {
            var facturas = (await _facturas.GetAllByExpresionAsync(f =>
                    f.IdEmpresa == idEmpresa &&
                    f.IDCliente == idCliente &&
                    f.IdTipoDocumentos == idTipoDocumento &&
                    f.EstaCancelada != true))
                .OrderByDescending(f => f.FechaInseccion)
                .ThenByDescending(f => f.IdFacturaHeader)
                .ToList();

            if (!facturas.Any())
                return new List<FichaClinicaCuentaLineaDto>();

            var productos = (await _productos.GetAllByExpresionAsync(p => p.IdEmpresa == idEmpresa))
                .ToDictionary(p => p.IdProducto, p => p.Nombre ?? "");

            var lineas = new List<FichaClinicaCuentaLineaDto>();
            foreach (var factura in facturas)
            {
                var detalles = (await _detalles.GetAllByExpresionAsync(
                        d => d.IdFacturaHeader == factura.IdFacturaHeader && d.StatuItem != true))
                    .ToList();

                var nombres = detalles
                    .Select(d =>
                    {
                        productos.TryGetValue(d.IdProducto, out var nombre);
                        return string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();
                    })
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();

                var diente = detalles
                    .Select(d => ExtraerDiente(d.Comentario))
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                lineas.Add(new FichaClinicaCuentaLineaDto
                {
                    Fecha = factura.FechaInseccion,
                    NumeroFactura = string.IsNullOrWhiteSpace(factura.NumeroDocumento)
                        ? factura.IdFacturaHeader.ToString(CultureInfo.InvariantCulture)
                        : factura.NumeroDocumento.Trim(),
                    Diente = diente,
                    Trabajo = nombres.Count > 0 ? string.Join(", ", nombres) : (factura.Nota ?? ""),
                    Costo = factura.Total,
                    Pagos = factura.Pagado,
                    Balance = factura.Pendiente,
                    IdFacturaHeader = factura.IdFacturaHeader
                });
            }

            return lineas
                .OrderByDescending(l => l.Fecha)
                .ThenByDescending(l => l.IdFacturaHeader)
                .ToList();
        }

        private static string? ExtraerDiente(string? comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario))
                return null;
            var t = comentario.Trim();
            if (t.Length == 2 && char.IsDigit(t[0]) && char.IsDigit(t[1]))
                return t;
            return t.Length <= 12 ? t : null;
        }

        private static FichaClinicaAnamnesisDto DeserializarAnamnesis(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new FichaClinicaAnamnesisDto();
            try
            {
                return JsonSerializer.Deserialize<FichaClinicaAnamnesisDto>(json, JsonOpts)
                       ?? new FichaClinicaAnamnesisDto();
            }
            catch (JsonException)
            {
                return new FichaClinicaAnamnesisDto();
            }
        }

        private static List<FichaClinicaDienteDto> DeserializarDientes(string? json)
        {
            var mapa = new Dictionary<string, FichaClinicaDienteDto>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var lista = JsonSerializer.Deserialize<List<FichaClinicaDienteDto>>(json, JsonOpts);
                    if (lista != null)
                    {
                        foreach (var d in lista.Where(x => !string.IsNullOrWhiteSpace(x.Numero)))
                            mapa[d.Numero] = d;
                    }
                }
                catch (JsonException)
                {
                    // JSON vacío o de formato anterior: se completa con FDI por defecto.
                }
            }

            return NormalizarDientes(mapa.Values);
        }

        private static List<FichaClinicaDienteDto> NormalizarDientes(IEnumerable<FichaClinicaDienteDto>? origen)
        {
            var mapa = (origen ?? Enumerable.Empty<FichaClinicaDienteDto>())
                .Where(d => !string.IsNullOrWhiteSpace(d.Numero))
                .GroupBy(d => d.Numero.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            return NumerosFdi.Select(n =>
            {
                if (mapa.TryGetValue(n, out var d))
                {
                    d.Numero = n;
                    return d;
                }
                return new FichaClinicaDienteDto { Numero = n };
            }).ToList();
        }

        private static int? CalcularEdad(DateTime? nacimiento)
        {
            if (!nacimiento.HasValue || nacimiento.Value.Year < 1900)
                return null;
            var hoy = DateTime.Today;
            var edad = hoy.Year - nacimiento.Value.Year;
            if (nacimiento.Value.Date > hoy.AddYears(-edad))
                edad--;
            return edad < 0 || edad > 120 ? null : edad;
        }

        private static string? TrimOrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return value.Trim();
        }
    }
}
