/*
  Cotizador Inteligente — DDL + seed (AlahiaPos_Dev only)
  Precios: escenario exploratorio configurable (NO oficiales).
  Piso comercial: PISO_MENSUAL_USD = 30
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
  RAISERROR('Este script solo puede ejecutarse en AlahiaPos_Dev.', 16, 1);
  RETURN;
END
GO

/* ========== DDL ========== */
IF OBJECT_ID(N'dbo.ModuloComercial', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.ModuloComercial (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ModuloComercial PRIMARY KEY,
    ModuloId INT NOT NULL,
    CategoriaComercial NVARCHAR(80) NOT NULL,
    DescripcionComercial NVARCHAR(500) NULL,
    Nivel NVARCHAR(20) NOT NULL CONSTRAINT DF_ModuloComercial_Nivel DEFAULT(N'BASE'),
    VisibleCotizador BIT NOT NULL CONSTRAINT DF_ModuloComercial_Visible DEFAULT(0),
    ParticipaPrecio BIT NOT NULL CONSTRAINT DF_ModuloComercial_Participa DEFAULT(0),
    PrecioBaseUSD DECIMAL(18,2) NOT NULL CONSTRAINT DF_ModuloComercial_Precio DEFAULT(0),
    Orden INT NOT NULL CONSTRAINT DF_ModuloComercial_Orden DEFAULT(0),
    Icono NVARCHAR(60) NULL,
    Activo BIT NOT NULL CONSTRAINT DF_ModuloComercial_Activo DEFAULT(1),
    CONSTRAINT FK_ModuloComercial_Modulos FOREIGN KEY (ModuloId) REFERENCES dbo.Modulos(Id),
    CONSTRAINT UX_ModuloComercial_ModuloId UNIQUE (ModuloId)
  );
END
GO

IF OBJECT_ID(N'dbo.ModuloDependencia', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.ModuloDependencia (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ModuloDependencia PRIMARY KEY,
    ModuloId INT NOT NULL,
    ModuloRequeridoId INT NOT NULL,
    Tipo NVARCHAR(20) NOT NULL CONSTRAINT DF_ModuloDependencia_Tipo DEFAULT(N'RECOMIENDA'),
    Mensaje NVARCHAR(400) NULL,
    Activo BIT NOT NULL CONSTRAINT DF_ModuloDependencia_Activo DEFAULT(1),
    CONSTRAINT FK_ModuloDependencia_Modulo FOREIGN KEY (ModuloId) REFERENCES dbo.Modulos(Id),
    CONSTRAINT FK_ModuloDependencia_Requerido FOREIGN KEY (ModuloRequeridoId) REFERENCES dbo.Modulos(Id),
    CONSTRAINT UX_ModuloDependencia UNIQUE (ModuloId, ModuloRequeridoId, Tipo)
  );
END
GO

IF OBJECT_ID(N'dbo.TipoNegocio', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.TipoNegocio (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TipoNegocio PRIMARY KEY,
    Codigo NVARCHAR(40) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    Orden INT NOT NULL CONSTRAINT DF_TipoNegocio_Orden DEFAULT(0),
    Activo BIT NOT NULL CONSTRAINT DF_TipoNegocio_Activo DEFAULT(1),
    CONSTRAINT UX_TipoNegocio_Codigo UNIQUE (Codigo)
  );
END
GO

IF OBJECT_ID(N'dbo.ModuloTipoNegocio', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.ModuloTipoNegocio (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ModuloTipoNegocio PRIMARY KEY,
    ModuloId INT NOT NULL,
    TipoNegocioId INT NOT NULL,
    Preseleccionado BIT NOT NULL CONSTRAINT DF_ModuloTipoNegocio_Pre DEFAULT(0),
    CONSTRAINT FK_ModuloTipoNegocio_Modulo FOREIGN KEY (ModuloId) REFERENCES dbo.Modulos(Id),
    CONSTRAINT FK_ModuloTipoNegocio_Tipo FOREIGN KEY (TipoNegocioId) REFERENCES dbo.TipoNegocio(Id),
    CONSTRAINT UX_ModuloTipoNegocio UNIQUE (ModuloId, TipoNegocioId)
  );
END
GO

IF OBJECT_ID(N'dbo.CotizadorParametro', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CotizadorParametro (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CotizadorParametro PRIMARY KEY,
    Clave NVARCHAR(80) NOT NULL,
    Valor NVARCHAR(200) NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    VisibleCliente BIT NOT NULL CONSTRAINT DF_CotizadorParametro_Visible DEFAULT(0),
    CONSTRAINT UX_CotizadorParametro_Clave UNIQUE (Clave)
  );
END
GO

IF OBJECT_ID(N'dbo.CotizadorTramoDocumento', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CotizadorTramoDocumento (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CotizadorTramoDocumento PRIMARY KEY,
    DesdeDocs INT NOT NULL,
    HastaDocs INT NULL,
    CargoUSD DECIMAL(18,2) NOT NULL,
    Etiqueta NVARCHAR(200) NULL,
    Orden INT NOT NULL CONSTRAINT DF_CotizadorTramo_Orden DEFAULT(0),
    Activo BIT NOT NULL CONSTRAINT DF_CotizadorTramo_Activo DEFAULT(1)
  );
END
GO

IF OBJECT_ID(N'dbo.Cotizacion', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Cotizacion (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cotizacion PRIMARY KEY,
    Folio NVARCHAR(40) NOT NULL,
    TipoNegocioCodigo NVARCHAR(40) NULL,
    Usuarios INT NOT NULL,
    Sucursales INT NOT NULL,
    UsaFacturacionElectronica BIT NOT NULL,
    DocumentosElectronicosMensuales INT NOT NULL,
    PrecioMensualUSD DECIMAL(18,2) NOT NULL,
    SnapshotJson NVARCHAR(MAX) NOT NULL,
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Cotizacion_Fecha DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT UX_Cotizacion_Folio UNIQUE (Folio)
  );
END
GO

IF OBJECT_ID(N'dbo.CotizacionDetalle', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CotizacionDetalle (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CotizacionDetalle PRIMARY KEY,
    CotizacionId INT NOT NULL,
    ModuloId INT NULL,
    CodigoModulo NVARCHAR(50) NULL,
    Concepto NVARCHAR(120) NOT NULL,
    MontoUSD DECIMAL(18,2) NOT NULL,
    TipoLinea NVARCHAR(30) NOT NULL,
    CONSTRAINT FK_CotizacionDetalle_Cotizacion FOREIGN KEY (CotizacionId) REFERENCES dbo.Cotizacion(Id) ON DELETE CASCADE
  );
END
GO

IF OBJECT_ID(N'dbo.CotizacionLead', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CotizacionLead (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CotizacionLead PRIMARY KEY,
    CotizacionId INT NULL,
    Tipo NVARCHAR(30) NOT NULL,
    Nombre NVARCHAR(150) NULL,
    Correo NVARCHAR(150) NULL,
    Telefono NVARCHAR(40) NULL,
    Mensaje NVARCHAR(1000) NULL,
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_CotizacionLead_Fecha DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT FK_CotizacionLead_Cotizacion FOREIGN KEY (CotizacionId) REFERENCES dbo.Cotizacion(Id)
  );
END
GO

/* ========== Parámetros (exploratorios) ========== */
MERGE dbo.CotizadorParametro AS t
USING (VALUES
  (N'PISO_MENSUAL_USD', N'30', N'Piso comercial mensual USD antes de impuestos', 1),
  (N'MONEDA', N'USD', N'Moneda de cotización', 1),
  (N'USUARIOS_INCLUIDOS', N'1', N'Usuarios incluidos en el precio base', 1),
  (N'PRECIO_USUARIO_EXTRA', N'8', N'Cargo exploratorio por usuario adicional', 0),
  (N'SUCURSALES_INCLUIDAS', N'1', N'Sucursales incluidas', 1),
  (N'PRECIO_SUCURSAL_EXTRA', N'15', N'Cargo exploratorio por sucursal adicional', 0)
) AS s(Clave, Valor, Descripcion, VisibleCliente)
ON t.Clave = s.Clave
WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Descripcion = s.Descripcion, VisibleCliente = s.VisibleCliente
WHEN NOT MATCHED THEN INSERT (Clave, Valor, Descripcion, VisibleCliente)
VALUES (s.Clave, s.Valor, s.Descripcion, s.VisibleCliente);
GO

/* Tramos e-CF (exploratorios) */
IF NOT EXISTS (SELECT 1 FROM dbo.CotizadorTramoDocumento)
BEGIN
  INSERT INTO dbo.CotizadorTramoDocumento (DesdeDocs, HastaDocs, CargoUSD, Etiqueta, Orden, Activo)
  VALUES
    (0, 100, 5.00, N'e-CF hasta 100 docs/mes', 1, 1),
    (101, 500, 15.00, N'e-CF 101–500 docs/mes', 2, 1),
    (501, 2000, 35.00, N'e-CF 501–2000 docs/mes', 3, 1),
    (2001, NULL, 60.00, N'e-CF más de 2000 docs/mes', 4, 1);
END
GO

/* Tipos de negocio */
MERGE dbo.TipoNegocio AS t
USING (VALUES
  (N'RESTAURANTE', N'Restaurante', N'Comida, delivery y punto de venta', 1),
  (N'CLINICA', N'Clínica', N'Servicios de salud y documentos clínicos', 2),
  (N'FERRETERIA', N'Ferretería', N'Retail con inventario y compras', 3),
  (N'REPOSTERIA', N'Repostería', N'Encargos y producción ligera', 4),
  (N'LAVANDERIA', N'Lavandería', N'Servicios y consumo de lavadores', 5),
  (N'COLMADO', N'Colmado', N'Retail de cercanía', 6),
  (N'OTRO', N'Otro', N'Configuración a medida', 99)
) AS s(Codigo, Nombre, Descripcion, Orden)
ON t.Codigo = s.Codigo
WHEN MATCHED THEN UPDATE SET Nombre = s.Nombre, Descripcion = s.Descripcion, Orden = s.Orden, Activo = 1
WHEN NOT MATCHED THEN INSERT (Codigo, Nombre, Descripcion, Orden, Activo)
VALUES (s.Codigo, s.Nombre, s.Descripcion, s.Orden, 1);
GO

/* Helper: upsert ModuloComercial by Codigo */
;WITH C AS (
  SELECT m.Id AS ModuloId, m.Codigo,
    v.Categoria, v.Descripcion, v.Nivel, v.Visible, v.Participa, v.Precio, v.Orden, v.Icono
  FROM (VALUES
    -- Operar / Ventas
    (N'POS', N'Ventas', N'Punto de venta para cobrar y facturar.', N'BASE', 1, 1, 20.00, 10, N'cart'),
    (N'CLIENTES', N'Ventas', N'Gestión de clientes y contacto comercial.', N'BASE', 1, 1, 5.00, 20, N'people'),
    (N'CUENTAS_COBRAR', N'Ventas', N'Cuentas por cobrar y seguimiento de saldos.', N'AVANZADO', 1, 1, 10.00, 30, N'cash'),
    (N'PRODUCTOS', N'Inventario', N'Catálogo de productos y servicios.', N'BASE', 1, 1, 8.00, 40, N'cube'),
    (N'CATEGORIAS', N'Inventario', N'Organización del catálogo.', N'BASE', 1, 0, 0.00, 45, N'grid'),
    (N'ALMACENES', N'Inventario', N'Multi-almacén y existencias.', N'AVANZADO', 1, 1, 12.00, 50, N'business'),
    (N'MOVIMIENTO_INVENTARIO', N'Inventario', N'Entradas, salidas y transferencias.', N'AVANZADO', 1, 1, 8.00, 55, N'swap'),
    (N'PROVEEDORES', N'Compras', N'Proveedores y datos de compra.', N'BASE', 1, 1, 6.00, 60, N'truck'),
    (N'ORDENES_COMPRA', N'Compras', N'Órdenes de compra.', N'AVANZADO', 1, 1, 8.00, 65, N'document'),
    (N'FACTURAS_COMPRA', N'Compras', N'Facturas de compra y recepción.', N'AVANZADO', 1, 1, 10.00, 70, N'receipt'),
    (N'CUENTAS_PAGAR_PROVEEDOR', N'Compras', N'Cuentas por pagar a proveedores.', N'AVANZADO', 1, 1, 10.00, 75, N'wallet'),
    (N'CUENTAS_FINANCIERAS', N'Finanzas', N'Bancos, cajas y cuentas financieras.', N'AVANZADO', 1, 1, 12.00, 80, N'card'),
    (N'MOVIMIENTO_FINANCIERO', N'Finanzas', N'Libro de movimientos financieros.', N'AVANZADO', 1, 1, 8.00, 85, N'list'),
    (N'CONCILIACION_BANCARIA', N'Finanzas', N'Conciliación bancaria profesional.', N'AVANZADO', 1, 1, 15.00, 90, N'balance'),
    (N'CONTABILIDAD', N'Contabilidad', N'Suite contable y reportes financieros.', N'AVANZADO', 1, 1, 25.00, 100, N'calculator'),
    (N'CENTRO_PRODUCCION', N'Producción', N'Centro de producción / KDS operativo.', N'AVANZADO', 1, 1, 18.00, 110, N'construct'),
    (N'EMPLEADOS', N'Recursos Humanos', N'Empleados y datos de personal.', N'BASE', 1, 1, 8.00, 120, N'person'),
    (N'EMPLEADOS_COMISION', N'Recursos Humanos', N'Comisiones de empleados.', N'AVANZADO', 1, 1, 10.00, 125, N'cash'),
    (N'NCF_SECUENCIAS', N'Fiscal', N'Comprobantes fiscales electrónicos (e-CF).', N'BASE', 1, 1, 10.00, 130, N'document-text'),
    (N'CITAS', N'Verticales', N'Agenda y citas (salón / clínica).', N'AVANZADO', 1, 1, 10.00, 140, N'calendar'),
    (N'DOCUMENTOS_CLINICOS', N'Verticales', N'Documentos clínicos.', N'AVANZADO', 1, 1, 12.00, 145, N'medkit'),
    (N'BIZCOCHO_ENCARGO', N'Verticales', N'Encargos de repostería.', N'AVANZADO', 1, 1, 10.00, 150, N'cafe'),
    (N'CONSUMO_LAVADORES', N'Verticales', N'Consumo de lavadores.', N'AVANZADO', 1, 1, 10.00, 155, N'water'),
    (N'DASHBOARD', N'Dirección', N'Panel gerencial operativo.', N'BASE', 1, 0, 0.00, 5, N'speedometer')
  ) AS v(Codigo, Categoria, Descripcion, Nivel, Visible, Participa, Precio, Orden, Icono)
  INNER JOIN dbo.Modulos m ON m.Codigo = v.Codigo
)
MERGE dbo.ModuloComercial AS t
USING C AS s ON t.ModuloId = s.ModuloId
WHEN MATCHED THEN UPDATE SET
  CategoriaComercial = s.Categoria,
  DescripcionComercial = s.Descripcion,
  Nivel = s.Nivel,
  VisibleCotizador = s.Visible,
  ParticipaPrecio = s.Participa,
  PrecioBaseUSD = s.Precio,
  Orden = s.Orden,
  Icono = s.Icono,
  Activo = 1
WHEN NOT MATCHED THEN INSERT
  (ModuloId, CategoriaComercial, DescripcionComercial, Nivel, VisibleCotizador, ParticipaPrecio, PrecioBaseUSD, Orden, Icono, Activo)
VALUES
  (s.ModuloId, s.Categoria, s.Descripcion, s.Nivel, s.Visible, s.Participa, s.Precio, s.Orden, s.Icono, 1);
GO

/* Dependencias comerciales */
;WITH D AS (
  SELECT m1.Id AS ModuloId, m2.Id AS ModuloRequeridoId, v.Tipo, v.Mensaje
  FROM (VALUES
    (N'CONCILIACION_BANCARIA', N'CUENTAS_FINANCIERAS', N'REQUIERE',
      N'Hemos recomendado Cuentas Financieras (Banco) porque seleccionó Conciliación Bancaria.'),
    (N'CONCILIACION_BANCARIA', N'MOVIMIENTO_FINANCIERO', N'RECOMIENDA',
      N'Para aprovechar Conciliación Bancaria recomendamos Movimientos Financieros.'),
    (N'CUENTAS_FINANCIERAS', N'CONTABILIDAD', N'RECOMIENDA',
      N'Seleccionó Banco. Para aprovechar estas funcionalidades recomendamos Contabilidad.'),
    (N'MOVIMIENTO_FINANCIERO', N'CUENTAS_FINANCIERAS', N'REQUIERE',
      N'Movimientos Financieros requiere Cuentas Financieras.'),
    (N'CENTRO_PRODUCCION', N'PRODUCTOS', N'REQUIERE',
      N'Producción requiere Productos para operar.'),
    (N'CENTRO_PRODUCCION', N'ALMACENES', N'RECOMIENDA',
      N'Si seleccionó Producción y no Inventario/Almacenes, recomendamos Almacenes.'),
    (N'CENTRO_PRODUCCION', N'MOVIMIENTO_INVENTARIO', N'RECOMIENDA',
      N'Para producción con control de existencias recomendamos Movimiento de Inventario.'),
    (N'EMPLEADOS_COMISION', N'EMPLEADOS', N'REQUIERE',
      N'Comisiones requiere el módulo de Empleados (Recursos Humanos).'),
    (N'CUENTAS_COBRAR', N'CLIENTES', N'REQUIERE',
      N'Cuentas por Cobrar requiere Clientes.'),
    (N'FACTURAS_COMPRA', N'PROVEEDORES', N'REQUIERE',
      N'Facturas de Compra requiere Proveedores.'),
    (N'CUENTAS_PAGAR_PROVEEDOR', N'PROVEEDORES', N'REQUIERE',
      N'Cuentas por Pagar requiere Proveedores.'),
    (N'ORDENES_COMPRA', N'PROVEEDORES', N'REQUIERE',
      N'Órdenes de Compra requiere Proveedores.'),
    (N'MOVIMIENTO_INVENTARIO', N'ALMACENES', N'RECOMIENDA',
      N'Para control multi-bodega recomendamos Almacenes junto a Movimiento de Inventario.'),
    (N'CITAS', N'EMPLEADOS', N'RECOMIENDA',
      N'Para citas recomendamos Empleados.'),
    (N'DOCUMENTOS_CLINICOS', N'CLIENTES', N'REQUIERE',
      N'Documentos Clínicos requiere Clientes.')
  ) AS v(Codigo, CodigoReq, Tipo, Mensaje)
  INNER JOIN dbo.Modulos m1 ON m1.Codigo = v.Codigo
  INNER JOIN dbo.Modulos m2 ON m2.Codigo = v.CodigoReq
)
MERGE dbo.ModuloDependencia AS t
USING D AS s ON t.ModuloId = s.ModuloId AND t.ModuloRequeridoId = s.ModuloRequeridoId AND t.Tipo = s.Tipo
WHEN MATCHED THEN UPDATE SET Mensaje = s.Mensaje, Activo = 1
WHEN NOT MATCHED THEN INSERT (ModuloId, ModuloRequeridoId, Tipo, Mensaje, Activo)
VALUES (s.ModuloId, s.ModuloRequeridoId, s.Tipo, s.Mensaje, 1);
GO

/* Preselecciones por vertical */
;WITH P AS (
  SELECT m.Id AS ModuloId, t.Id AS TipoNegocioId, v.Pre
  FROM (VALUES
    (N'RESTAURANTE', N'POS', 1),
    (N'RESTAURANTE', N'PRODUCTOS', 1),
    (N'RESTAURANTE', N'CLIENTES', 1),
    (N'RESTAURANTE', N'CENTRO_PRODUCCION', 1),
    (N'RESTAURANTE', N'NCF_SECUENCIAS', 1),
    (N'CLINICA', N'POS', 1),
    (N'CLINICA', N'CLIENTES', 1),
    (N'CLINICA', N'CITAS', 1),
    (N'CLINICA', N'DOCUMENTOS_CLINICOS', 1),
    (N'CLINICA', N'NCF_SECUENCIAS', 1),
    (N'FERRETERIA', N'POS', 1),
    (N'FERRETERIA', N'PRODUCTOS', 1),
    (N'FERRETERIA', N'ALMACENES', 1),
    (N'FERRETERIA', N'PROVEEDORES', 1),
    (N'FERRETERIA', N'FACTURAS_COMPRA', 1),
    (N'REPOSTERIA', N'POS', 1),
    (N'REPOSTERIA', N'BIZCOCHO_ENCARGO', 1),
    (N'REPOSTERIA', N'PRODUCTOS', 1),
    (N'REPOSTERIA', N'CLIENTES', 1),
    (N'LAVANDERIA', N'POS', 1),
    (N'LAVANDERIA', N'CONSUMO_LAVADORES', 1),
    (N'LAVANDERIA', N'CLIENTES', 1),
    (N'COLMADO', N'POS', 1),
    (N'COLMADO', N'PRODUCTOS', 1),
    (N'COLMADO', N'ALMACENES', 1),
    (N'COLMADO', N'PROVEEDORES', 1),
    (N'OTRO', N'POS', 1),
    (N'OTRO', N'DASHBOARD', 1)
  ) AS v(TipoCodigo, ModCodigo, Pre)
  INNER JOIN dbo.TipoNegocio t ON t.Codigo = v.TipoCodigo
  INNER JOIN dbo.Modulos m ON m.Codigo = v.ModCodigo
)
MERGE dbo.ModuloTipoNegocio AS t
USING P AS s ON t.ModuloId = s.ModuloId AND t.TipoNegocioId = s.TipoNegocioId
WHEN MATCHED THEN UPDATE SET Preseleccionado = s.Pre
WHEN NOT MATCHED THEN INSERT (ModuloId, TipoNegocioId, Preseleccionado)
VALUES (s.ModuloId, s.TipoNegocioId, s.Pre);
GO

PRINT 'Cotizador Dev: DDL + seed OK';
SELECT COUNT(*) ModulosComerciales FROM dbo.ModuloComercial WHERE VisibleCotizador = 1;
SELECT Clave, Valor FROM dbo.CotizadorParametro WHERE Clave = N'PISO_MENSUAL_USD';
GO
