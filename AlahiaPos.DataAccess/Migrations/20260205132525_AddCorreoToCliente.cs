using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlahiaPos.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCorreoToCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    IdArea = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActivo = table.Column<bool>(type: "bit", nullable: false),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.IdArea);
                });

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    IdCategoria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImagenPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActiva = table.Column<bool>(type: "bit", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.IdCategoria);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    IDCliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CedulaRNC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreComercial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Celular = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LimiteCredito = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.IDCliente);
                });

            migrationBuilder.CreateTable(
                name: "Cocinas",
                columns: table => new
                {
                    IdCocina = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cocinas", x => x.IdCocina);
                });

            migrationBuilder.CreateTable(
                name: "DescuentoHeader",
                columns: table => new
                {
                    IdDescuentoHeader = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    NombreEvento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoDescuento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiasSemana = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: true),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: true),
                    AplicaATodos = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    AplicaATodasAreas = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DescuentoHeader", x => x.IdDescuentoHeader);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    IdEmpresa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPlanes = table.Column<int>(type: "int", nullable: true),
                    NombreComercial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RNC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Logo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorreElectronico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    FechaTerminacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecondaryColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TertiaryColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UrlCatalogo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UrlCitas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Longitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuidPublico = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    titleColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenNotificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.IdEmpresa);
                });

            migrationBuilder.CreateTable(
                name: "Gastos",
                columns: table => new
                {
                    IdGasto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoGasto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdProveedor = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Orien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdEmpleado = table.Column<int>(type: "int", nullable: true),
                    EstaCerrada = table.Column<bool>(type: "bit", nullable: true),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gastos", x => x.IdGasto);
                });

            migrationBuilder.CreateTable(
                name: "ImpresorasZonas",
                columns: table => new
                {
                    Impresoraid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImpresoraName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpresorasZonas", x => x.Impresoraid);
                });

            migrationBuilder.CreateTable(
                name: "Ingresos",
                columns: table => new
                {
                    IdIngreso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Origen = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FormaPago = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    IdFacturaHeader = table.Column<int>(type: "int", nullable: true),
                    IdCliente = table.Column<int>(type: "int", nullable: true),
                    Nota = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingresos", x => x.IdIngreso);
                });

            migrationBuilder.CreateTable(
                name: "Modulos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecioUSD = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modulos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PagosFacturasClientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdFacturaHeader = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IDCliente = table.Column<int>(type: "int", nullable: true),
                    FormaPago = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosFacturasClientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParametrosConfigs",
                columns: table => new
                {
                    IdParametrosConfig = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreParametro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Identificador = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosConfigs", x => x.IdParametrosConfig);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpleado = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expira = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetToken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanesSuscripcion",
                columns: table => new
                {
                    IdPlan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecioUSD = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CantidadUsuarios = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesSuscripcion", x => x.IdPlan);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    IdProducto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EsServicio = table.Column<bool>(type: "bit", nullable: false),
                    IdArea = table.Column<int>(type: "int", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rentado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Disponibles = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IdProveedor = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Stock = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IdUnidadMedida = table.Column<int>(type: "int", nullable: true),
                    IdCategoria = table.Column<int>(type: "int", nullable: true),
                    IdAlmacen = table.Column<int>(type: "int", nullable: true),
                    CodigoBarra = table.Column<string>(type: "VARCHAR(30)", maxLength: 30, nullable: true),
                    TipoProducto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Precio1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Precio2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Precio3 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioDolar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IdRecetasHeader = table.Column<int>(type: "int", nullable: true),
                    PorcientoDescuento = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PorcientoGanancia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioCompra = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaVencimiento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Imagen1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Imagen2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Imagen3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Itbis = table.Column<bool>(type: "bit", nullable: false),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SeCompra = table.Column<bool>(type: "bit", nullable: false),
                    SeAlquila = table.Column<bool>(type: "bit", nullable: false),
                    SeVende = table.Column<bool>(type: "bit", nullable: false),
                    ControlarStock = table.Column<bool>(type: "bit", nullable: false),
                    IsActivo = table.Column<bool>(type: "bit", nullable: false),
                    Ganancia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IdCocina = table.Column<int>(type: "int", nullable: false),
                    EsProductoBelleza = table.Column<bool>(type: "bit", nullable: true),
                    DuracionServicio = table.Column<int>(type: "int", nullable: false),
                    DisponibleEnCitas = table.Column<bool>(type: "bit", nullable: false),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.IdProducto);
                });

            migrationBuilder.CreateTable(
                name: "Servicios",
                columns: table => new
                {
                    IdAreaServicio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActivo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicios", x => x.IdAreaServicio);
                });

            migrationBuilder.CreateTable(
                name: "EmpleadoAreaComision",
                columns: table => new
                {
                    IdEmpleadoAreaComision = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpleado = table.Column<int>(type: "int", nullable: false),
                    IdArea = table.Column<int>(type: "int", nullable: false),
                    PorcientoComision = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpleadoAreaComision", x => x.IdEmpleadoAreaComision);
                    table.ForeignKey(
                        name: "FK_EmpleadoAreaComision_Areas_IdArea",
                        column: x => x.IdArea,
                        principalTable: "Areas",
                        principalColumn: "IdArea",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CorreosElectronicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IDCliente = table.Column<int>(type: "int", nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientesIDCliente = table.Column<int>(type: "int", nullable: true),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorreosElectronicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorreosElectronicos_Clientes_ClientesIDCliente",
                        column: x => x.ClientesIDCliente,
                        principalTable: "Clientes",
                        principalColumn: "IDCliente");
                });

            migrationBuilder.CreateTable(
                name: "FacturaHeaders",
                columns: table => new
                {
                    IdFacturaHeader = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Moneda = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Plazo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoFactura = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MontoPropina = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrintPending = table.Column<bool>(type: "bit", nullable: false),
                    PrintAcount = table.Column<bool>(type: "bit", nullable: true),
                    IdEmpleados = table.Column<int>(type: "int", nullable: true),
                    IdEmpleadoComision = table.Column<int>(type: "int", nullable: true),
                    Hora = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdMoso = table.Column<int>(type: "int", nullable: true),
                    IdMesa = table.Column<int>(type: "int", nullable: true),
                    IdTipoDocumentos = table.Column<int>(type: "int", nullable: true),
                    NCF = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormaPago = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IDCliente = table.Column<int>(type: "int", nullable: true),
                    Efectivo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoTarjetaVisa = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoTarjetaMasterCard = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoTransferencia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoCheques = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoEfectivo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoNotaCredito = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Cambio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalItbis = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDescuento = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstaCancelada = table.Column<bool>(type: "bit", nullable: false),
                    EstaCerrada = table.Column<bool>(type: "bit", nullable: false),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaBencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pagado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Pendiente = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AjustadoInventario = table.Column<bool>(type: "bit", nullable: false),
                    NombreCuenta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado_Orden = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoOrden = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaHeaders", x => x.IdFacturaHeader);
                    table.ForeignKey(
                        name: "FK_FacturaHeaders_Clientes_IDCliente",
                        column: x => x.IDCliente,
                        principalTable: "Clientes",
                        principalColumn: "IDCliente");
                });

            migrationBuilder.CreateTable(
                name: "DescuentoAreaDetalle",
                columns: table => new
                {
                    IdDescuentoAreaDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdDescuentoHeader = table.Column<int>(type: "int", nullable: false),
                    IdArea = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DescuentoAreaDetalle", x => x.IdDescuentoAreaDetalle);
                    table.ForeignKey(
                        name: "FK_DescuentoAreaDetalle_Areas_IdArea",
                        column: x => x.IdArea,
                        principalTable: "Areas",
                        principalColumn: "IdArea",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DescuentoAreaDetalle_DescuentoHeader_IdDescuentoHeader",
                        column: x => x.IdDescuentoHeader,
                        principalTable: "DescuentoHeader",
                        principalColumn: "IdDescuentoHeader",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmpleadosP",
                columns: table => new
                {
                    IdEmpleados = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Cedula = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Celular = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComisionServicio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ComisionProductos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    //FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.IdEmpleados);
                    table.ForeignKey(
                        name: "FK_Empleados_Empresas_IdEmpresa",
                        column: x => x.IdEmpresa,
                        principalTable: "Empresas",
                        principalColumn: "IdEmpresa",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmpresaPlan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    PrecioBaseUSD = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    UsuariosIncluidos = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpresaPlan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmpresaPlan_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "IdEmpresa",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perfiles",
                columns: table => new
                {
                    IdPerfil = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    //FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfiles", x => x.IdPerfil);
                    table.ForeignKey(
                        name: "FK_Perfiles_Empresas_IdEmpresa",
                        column: x => x.IdEmpresa,
                        principalTable: "Empresas",
                        principalColumn: "IdEmpresa",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Zonas",
                columns: table => new
                {
                    ZonaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZonaName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    ImpresoraId = table.Column<int>(type: "int", nullable: false),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zonas", x => x.ZonaId);
                    table.ForeignKey(
                        name: "FK_Zonas_ImpresorasZonas_ImpresoraId",
                        column: x => x.ImpresoraId,
                        principalTable: "ImpresorasZonas",
                        principalColumn: "Impresoraid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Empresa_Modulos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    ModuloId = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaActivacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaDesactivacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresa_Modulos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empresa_Modulos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "IdEmpresa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Empresa_Modulos_Modulos_ModuloId",
                        column: x => x.ModuloId,
                        principalTable: "Modulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DescuentoDetalle",
                columns: table => new
                {
                    IdDescuentoDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdDescuentoHeader = table.Column<int>(type: "int", nullable: false),
                    IdProducto = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DescuentoDetalle", x => x.IdDescuentoDetalle);
                    table.ForeignKey(
                        name: "FK_DescuentoDetalle_DescuentoHeader_IdDescuentoHeader",
                        column: x => x.IdDescuentoHeader,
                        principalTable: "DescuentoHeader",
                        principalColumn: "IdDescuentoHeader",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DescuentoDetalle_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalTable: "Productos",
                        principalColumn: "IdProducto",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FacturaDetalles",
                columns: table => new
                {
                    IdFacturaDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdFacturaHeader = table.Column<int>(type: "int", nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    Dias = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Itbis = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EnviadoCocina = table.Column<bool>(type: "bit", nullable: false),
                    StatuItem = table.Column<bool>(type: "bit", nullable: false),
                    PrecioOferta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IdEmpleadoComision = table.Column<int>(type: "int", nullable: true),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaDetalles", x => x.IdFacturaDetalle);
                    table.ForeignKey(
                        name: "FK_FacturaDetalles_FacturaHeaders_IdFacturaHeader",
                        column: x => x.IdFacturaHeader,
                        principalTable: "FacturaHeaders",
                        principalColumn: "IdFacturaHeader",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacturaDetalles_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalTable: "Productos",
                        principalColumn: "IdProducto",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Citas",
                columns: table => new
                {
                    IdCita = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreCliente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RutaReciboPago = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdEmpleado = table.Column<int>(type: "int", nullable: false),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hora = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Costo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citas", x => x.IdCita);
                    table.ForeignKey(
                        name: "FK_Citas_Empleados_IdEmpleado",
                        column: x => x.IdEmpleado,
                        principalTable: "Empleados",
                        principalColumn: "IdEmpleados",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Citas_Empresas_IdEmpresa",
                        column: x => x.IdEmpresa,
                        principalTable: "Empresas",
                        principalColumn: "IdEmpresa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Citas_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalTable: "Productos",
                        principalColumn: "IdProducto",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HorariosEstilistas",
                columns: table => new
                {
                    IdHorario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpleado = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FechaInseccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosEstilistas", x => x.IdHorario);
                    table.ForeignKey(
                        name: "FK_HorariosEstilistas_Empleados_IdEmpleado",
                        column: x => x.IdEmpleado,
                        principalTable: "Empleados",
                        principalColumn: "IdEmpleados",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfilRoles",
                columns: table => new
                {
                    IdPerfilRol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    IdPerfil = table.Column<int>(type: "int", nullable: false),
                    IdModulo = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilRoles", x => x.IdPerfilRol);
                    table.ForeignKey(
                        name: "FK_PerfilRoles_Modulos_IdModulo",
                        column: x => x.IdModulo,
                        principalTable: "Modulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerfilRoles_Perfiles_IdPerfil",
                        column: x => x.IdPerfil,
                        principalTable: "Perfiles",
                        principalColumn: "IdPerfil",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    IdEmpleado = table.Column<int>(type: "int", nullable: false),
                    IdPerfil = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dispositivo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TokenRecuperacion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TokenExpira = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                    table.ForeignKey(
                        name: "FK_Usuarios_Empleados_IdEmpleado",
                        column: x => x.IdEmpleado,
                        principalTable: "Empleados",
                        principalColumn: "IdEmpleados",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Usuarios_Empresas_IdEmpresa",
                        column: x => x.IdEmpresa,
                        principalTable: "Empresas",
                        principalColumn: "IdEmpresa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Usuarios_Perfiles_IdPerfil",
                        column: x => x.IdPerfil,
                        principalTable: "Perfiles",
                        principalColumn: "IdPerfil",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mesas",
                columns: table => new
                {
                    IdMesa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Numero = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActiva = table.Column<bool>(type: "bit", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZonaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesas", x => x.IdMesa);
                    table.ForeignKey(
                        name: "FK_Mesas_Zonas_ZonaId",
                        column: x => x.ZonaId,
                        principalTable: "Zonas",
                        principalColumn: "ZonaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_IdEmpleado",
                table: "Citas",
                column: "IdEmpleado");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_IdEmpresa",
                table: "Citas",
                column: "IdEmpresa");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_IdProducto",
                table: "Citas",
                column: "IdProducto");

            migrationBuilder.CreateIndex(
                name: "IX_CorreosElectronicos_ClientesIDCliente",
                table: "CorreosElectronicos",
                column: "ClientesIDCliente");

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoAreaDetalle_IdArea",
                table: "DescuentoAreaDetalle",
                column: "IdArea");

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoAreaDetalle_IdDescuentoHeader",
                table: "DescuentoAreaDetalle",
                column: "IdDescuentoHeader");

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoDetalle_IdDescuentoHeader",
                table: "DescuentoDetalle",
                column: "IdDescuentoHeader");

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoDetalle_IdProducto",
                table: "DescuentoDetalle",
                column: "IdProducto");

            migrationBuilder.CreateIndex(
                name: "IX_EmpleadoAreaComision_IdArea",
                table: "EmpleadoAreaComision",
                column: "IdArea");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_IdEmpresa",
                table: "Empleados",
                column: "IdEmpresa");

            migrationBuilder.CreateIndex(
                name: "IX_Empresa_Modulos_EmpresaId",
                table: "Empresa_Modulos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresa_Modulos_ModuloId",
                table: "Empresa_Modulos",
                column: "ModuloId");

            migrationBuilder.CreateIndex(
                name: "IX_EmpresaPlan_EmpresaId",
                table: "EmpresaPlan",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_IdFacturaHeader",
                table: "FacturaDetalles",
                column: "IdFacturaHeader");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_IdProducto",
                table: "FacturaDetalles",
                column: "IdProducto");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaHeaders_IDCliente",
                table: "FacturaHeaders",
                column: "IDCliente");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosEstilistas_IdEmpleado",
                table: "HorariosEstilistas",
                column: "IdEmpleado");

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_ZonaId",
                table: "Mesas",
                column: "ZonaId");

            migrationBuilder.CreateIndex(
                name: "IX_Perfiles_IdEmpresa",
                table: "Perfiles",
                column: "IdEmpresa");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilRoles_IdModulo",
                table: "PerfilRoles",
                column: "IdModulo");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilRoles_IdPerfil",
                table: "PerfilRoles",
                column: "IdPerfil");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdEmpleado",
                table: "Usuarios",
                column: "IdEmpleado");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdEmpresa",
                table: "Usuarios",
                column: "IdEmpresa");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdPerfil",
                table: "Usuarios",
                column: "IdPerfil");

            migrationBuilder.CreateIndex(
                name: "IX_Zonas_ImpresoraId",
                table: "Zonas",
                column: "ImpresoraId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Citas");

            migrationBuilder.DropTable(
                name: "Cocinas");

            migrationBuilder.DropTable(
                name: "CorreosElectronicos");

            migrationBuilder.DropTable(
                name: "DescuentoAreaDetalle");

            migrationBuilder.DropTable(
                name: "DescuentoDetalle");

            migrationBuilder.DropTable(
                name: "EmpleadoAreaComision");

            migrationBuilder.DropTable(
                name: "Empresa_Modulos");

            migrationBuilder.DropTable(
                name: "EmpresaPlan");

            migrationBuilder.DropTable(
                name: "FacturaDetalles");

            migrationBuilder.DropTable(
                name: "Gastos");

            migrationBuilder.DropTable(
                name: "HorariosEstilistas");

            migrationBuilder.DropTable(
                name: "Ingresos");

            migrationBuilder.DropTable(
                name: "Mesas");

            migrationBuilder.DropTable(
                name: "PagosFacturasClientes");

            migrationBuilder.DropTable(
                name: "ParametrosConfigs");

            migrationBuilder.DropTable(
                name: "PasswordResetToken");

            migrationBuilder.DropTable(
                name: "PerfilRoles");

            migrationBuilder.DropTable(
                name: "PlanesSuscripcion");

            migrationBuilder.DropTable(
                name: "Servicios");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "DescuentoHeader");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "FacturaHeaders");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Zonas");

            migrationBuilder.DropTable(
                name: "Modulos");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "Perfiles");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "ImpresorasZonas");

            migrationBuilder.DropTable(
                name: "Empresas");
        }
    }
}
