/*
  Migrate_DevSchema_To_Prod_20260816.sql
  ------------------------------------------------------------
  Lleva a AlahiaPos_Prod el DDL de AlahiaPos_Dev que aun no esta
  en produccion. Solo cambios aditivos / idempotentes.

  NO copia datos de negocio Dev -> Prod.
  NO elimina columnas ni tablas.

  Incluye:
  - Columnas: CertificadoDigital, NotasCredito (NC e-CF 34)
  - Tablas: NC aplicaciones, saldo a favor, ficha clinica,
    RRHH/nomina, payroll, EmpresaAiConfig, SuscripcionCuentaCobro
  - Catalogo de modulos faltantes + activacion coherente
  - Usuario SQL Alahia AI (login ya existe en el servidor)

  Autorizacion: usuario pidio pasar cambios de Dev a produccion (BD).
*/
USE AlahiaPos_Prod;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
  RAISERROR('Este script solo puede ejecutarse en AlahiaPos_Prod.', 16, 1);
  RETURN;
END
GO

PRINT '=== INICIO migracion schema Dev->Prod 2026-08-16 ===';
GO

/* =====================================================================
   1) CertificadoDigital
   ===================================================================== */
IF COL_LENGTH('dbo.CertificadoDigital', 'ArchivoBytes') IS NULL
    ALTER TABLE dbo.CertificadoDigital ADD ArchivoBytes VARBINARY(MAX) NULL;
GO

IF COL_LENGTH('dbo.CertificadoDigital', 'Ambiente') IS NULL
    ALTER TABLE dbo.CertificadoDigital ADD Ambiente NVARCHAR(20) NULL;
GO

/* =====================================================================
   2) NotasCredito — trazabilidad e-CF 34 y saldo
   ===================================================================== */
IF COL_LENGTH('dbo.NotasCredito', 'TipoDocumentoOrigen') IS NULL
    ALTER TABLE dbo.NotasCredito ADD TipoDocumentoOrigen NVARCHAR(10) NULL;
GO
IF COL_LENGTH('dbo.NotasCredito', 'IdEcf') IS NULL
    ALTER TABLE dbo.NotasCredito ADD IdEcf INT NULL;
GO
IF COL_LENGTH('dbo.NotasCredito', 'TrackId') IS NULL
    ALTER TABLE dbo.NotasCredito ADD TrackId NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.NotasCredito', 'EstadoDgii') IS NULL
    ALTER TABLE dbo.NotasCredito ADD EstadoDgii NVARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.NotasCredito', 'FechaEmisionEcf') IS NULL
    ALTER TABLE dbo.NotasCredito ADD FechaEmisionEcf DATETIME NULL;
GO
IF COL_LENGTH('dbo.NotasCredito', 'MensajeEmision') IS NULL
    ALTER TABLE dbo.NotasCredito ADD MensajeEmision NVARCHAR(1000) NULL;
GO
IF COL_LENGTH('dbo.NotasCredito', 'MontoOriginal') IS NULL
    ALTER TABLE dbo.NotasCredito ADD MontoOriginal DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_NotasCredito_MontoOriginal DEFAULT (0);
GO
IF COL_LENGTH('dbo.NotasCredito', 'SaldoDisponible') IS NULL
    ALTER TABLE dbo.NotasCredito ADD SaldoDisponible DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_NotasCredito_SaldoDisponible DEFAULT (0);
GO
IF COL_LENGTH('dbo.NotasCredito', 'Estado') IS NULL
    ALTER TABLE dbo.NotasCredito ADD Estado NVARCHAR(20) NOT NULL
        CONSTRAINT DF_NotasCredito_Estado DEFAULT (N'Activa');
GO

UPDATE dbo.NotasCredito
SET MontoOriginal = Total,
    Estado = ISNULL(NULLIF(Estado, N''), N'Activa')
WHERE ISNULL(MontoOriginal, 0) = 0 AND ISNULL(Total, 0) <> 0;
GO

/* =====================================================================
   3) NotasCreditoAplicaciones + ClienteSaldoAFavor
   ===================================================================== */
IF OBJECT_ID(N'dbo.NotasCreditoAplicaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotasCreditoAplicaciones (
        IdAplicacion INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotasCreditoAplicaciones PRIMARY KEY,
        IdNotaCredito INT NOT NULL,
        IdFacturaHeader INT NULL,
        IdSaldoAFavor INT NULL,
        IdEmpresa INT NOT NULL,
        MontoAplicado DECIMAL(18,2) NOT NULL,
        TipoAplicacion NVARCHAR(30) NOT NULL,
        FechaAplicacion DATETIME NOT NULL CONSTRAINT DF_NcAplicaciones_Fecha DEFAULT (GETDATE()),
        IdUsuario INT NULL,
        CONSTRAINT FK_NcAplicaciones_NotaCredito FOREIGN KEY (IdNotaCredito)
            REFERENCES dbo.NotasCredito(IdNotaCredito)
    );
    CREATE INDEX IX_NcAplicaciones_Nota ON dbo.NotasCreditoAplicaciones(IdNotaCredito);
    CREATE INDEX IX_NcAplicaciones_Factura ON dbo.NotasCreditoAplicaciones(IdFacturaHeader);
    CREATE INDEX IX_NcAplicaciones_Empresa ON dbo.NotasCreditoAplicaciones(IdEmpresa);
END
GO

IF OBJECT_ID(N'dbo.ClienteSaldoAFavor', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClienteSaldoAFavor (
        IdSaldoAFavor INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ClienteSaldoAFavor PRIMARY KEY,
        IdEmpresa INT NOT NULL,
        IdCliente INT NOT NULL,
        IdNotaCredito INT NOT NULL,
        MontoOriginal DECIMAL(18,2) NOT NULL,
        SaldoDisponible DECIMAL(18,2) NOT NULL,
        Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_ClienteSaldoAFavor_Estado DEFAULT (N'Disponible'),
        Fecha DATETIME NOT NULL CONSTRAINT DF_ClienteSaldoAFavor_Fecha DEFAULT (GETDATE()),
        IdUsuario INT NULL,
        Observacion NVARCHAR(500) NULL,
        CONSTRAINT FK_ClienteSaldoAFavor_NotaCredito FOREIGN KEY (IdNotaCredito)
            REFERENCES dbo.NotasCredito(IdNotaCredito)
    );
    CREATE INDEX IX_ClienteSaldoAFavor_Cliente ON dbo.ClienteSaldoAFavor(IdEmpresa, IdCliente);
    CREATE INDEX IX_ClienteSaldoAFavor_Nota ON dbo.ClienteSaldoAFavor(IdNotaCredito);
END
GO

/* =====================================================================
   4) FichasClinicas
   ===================================================================== */
IF OBJECT_ID(N'dbo.FichasClinicas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FichasClinicas
    (
        IdFichaClinica              INT             IDENTITY(1,1) NOT NULL,
        IdEmpresa                   INT             NOT NULL,
        IdCliente                   INT             NOT NULL,
        Nombres                     NVARCHAR(120)   NULL,
        Apellidos                   NVARCHAR(120)   NULL,
        Sexo                        NVARCHAR(20)    NULL,
        EstadoCivil                 NVARCHAR(30)    NULL,
        Nacionalidad                NVARCHAR(80)    NULL,
        ContactoEmergenciaNombre    NVARCHAR(150)   NULL,
        ContactoEmergenciaTelefono  NVARCHAR(40)    NULL,
        AnamnesisJson               NVARCHAR(MAX)   NULL,
        OdontogramaJson             NVARCHAR(MAX)   NULL,
        Medicamentos                NVARCHAR(500)   NULL,
        Observaciones               NVARCHAR(MAX)   NULL,
        Color                       NVARCHAR(80)    NULL,
        TipoProtesis                NVARCHAR(120)   NULL,
        Laboratorio                 NVARCHAR(150)   NULL,
        IdUsuarioCreacion           INT             NOT NULL CONSTRAINT DF_FichasClinicas_UsrCre DEFAULT (0),
        IdUsuarioModificacion       INT             NULL,
        FechaCreacion               DATETIME2(0)    NOT NULL CONSTRAINT DF_FichasClinicas_Cre DEFAULT (SYSUTCDATETIME()),
        FechaModificacion           DATETIME2(0)    NOT NULL CONSTRAINT DF_FichasClinicas_Mod DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_FichasClinicas PRIMARY KEY CLUSTERED (IdFichaClinica),
        CONSTRAINT UQ_FichasClinicas_EmpresaCliente UNIQUE (IdEmpresa, IdCliente)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FichasClinicas_Empresas')
    ALTER TABLE dbo.FichasClinicas ADD CONSTRAINT FK_FichasClinicas_Empresas
        FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas (IdEmpresa);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FichasClinicas_Clientes')
    ALTER TABLE dbo.FichasClinicas ADD CONSTRAINT FK_FichasClinicas_Clientes
        FOREIGN KEY (IdCliente) REFERENCES dbo.Clientes (IDCliente);
GO

/* =====================================================================
   5) RRHH / nomina / payroll (schema vacio)
   ===================================================================== */
IF OBJECT_ID(N'dbo.EmpleadoLaboral', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.EmpleadoLaboral (
    IdEmpleadoLaboral INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdEmpresa INT NOT NULL,
    IdEmpleados INT NOT NULL,
    TipoEmpleado NVARCHAR(40) NOT NULL CONSTRAINT DF_EmpleadoLaboral_Tipo DEFAULT (N'FIJO'),
    Cargo NVARCHAR(120) NULL,
    Departamento NVARCHAR(120) NULL,
    FechaIngreso DATETIME NULL,
    EstadoLaboral NVARCHAR(40) NOT NULL CONSTRAINT DF_EmpleadoLaboral_Estado DEFAULT (N'ACTIVO'),
    FrecuenciaPago NVARCHAR(20) NOT NULL CONSTRAINT DF_EmpleadoLaboral_Freq DEFAULT (N'QUINCENAL'),
    SalarioBase DECIMAL(18,2) NOT NULL CONSTRAINT DF_EmpleadoLaboral_Salario DEFAULT (0),
    Moneda NVARCHAR(8) NOT NULL CONSTRAINT DF_EmpleadoLaboral_Moneda DEFAULT (N'DOP'),
    FechaActualizacion DATETIME NOT NULL CONSTRAINT DF_EmpleadoLaboral_Upd DEFAULT (GETUTCDATE()),
    CONSTRAINT UQ_EmpleadoLaboral_Emp UNIQUE (IdEmpresa, IdEmpleados)
  );
END
GO

IF OBJECT_ID(N'dbo.EmpleadoSalarioHistorial', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.EmpleadoSalarioHistorial (
    IdSalarioHistorial INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdEmpresa INT NOT NULL,
    IdEmpleados INT NOT NULL,
    SalarioAnterior DECIMAL(18,2) NOT NULL,
    SalarioNuevo DECIMAL(18,2) NOT NULL,
    FrecuenciaPago NVARCHAR(20) NOT NULL,
    VigenteDesde DATETIME NOT NULL,
    FechaRegistro DATETIME NOT NULL CONSTRAINT DF_EmpSalHist_Reg DEFAULT (GETUTCDATE()),
    Motivo NVARCHAR(250) NULL,
    IdUsuario INT NULL
  );
  CREATE INDEX IX_EmpSalHist_Emp ON dbo.EmpleadoSalarioHistorial (IdEmpresa, IdEmpleados);
END
GO

IF OBJECT_ID(N'dbo.NominaConcepto', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.NominaConcepto (
    IdNominaConcepto INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdEmpresa INT NOT NULL,
    ConceptCode NVARCHAR(80) NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    Categoria NVARCHAR(20) NOT NULL,
    EsLegal BIT NOT NULL CONSTRAINT DF_NominaConcepto_Legal DEFAULT (0),
    Activo BIT NOT NULL CONSTRAINT DF_NominaConcepto_Activo DEFAULT (1),
    Descripcion NVARCHAR(250) NULL,
    CONSTRAINT UQ_NominaConcepto_Code UNIQUE (IdEmpresa, ConceptCode)
  );
END
GO

IF OBJECT_ID(N'dbo.NominaConceptoAsignacion', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.NominaConceptoAsignacion (
    IdAsignacion INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdEmpresa INT NOT NULL,
    IdEmpleados INT NOT NULL,
    ConceptCode NVARCHAR(80) NOT NULL,
    MontoFijo DECIMAL(18,4) NULL,
    Tasa DECIMAL(18,8) NULL,
    Periodicidad NVARCHAR(20) NOT NULL,
    VigenteDesde DATETIME NULL,
    VigenteHasta DATETIME NULL,
    Activo BIT NOT NULL CONSTRAINT DF_NominaAsig_Activo DEFAULT (1),
    Nota NVARCHAR(250) NULL
  );
  CREATE INDEX IX_NominaAsig_Emp ON dbo.NominaConceptoAsignacion (IdEmpresa, IdEmpleados, ConceptCode);
END
GO

IF OBJECT_ID(N'dbo.Payroll_Employees', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Payroll_Employees (
    IdEmpresa INT NOT NULL,
    IdEmpleados INT NOT NULL,
    Activo BIT NOT NULL CONSTRAINT DF_PayEmp_Act DEFAULT (1),
    Nombre NVARCHAR(200) NULL,
    CONSTRAINT PK_Payroll_Employees PRIMARY KEY (IdEmpresa, IdEmpleados)
  );
END
GO

IF OBJECT_ID(N'dbo.Payroll_ContractAttributes', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Payroll_ContractAttributes (
    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdEmpresa INT NOT NULL,
    IdEmpleados INT NOT NULL,
    AttributeKey NVARCHAR(100) NOT NULL,
    AttributeValue NVARCHAR(500) NOT NULL,
    CONSTRAINT UQ_PayContractAttr UNIQUE (IdEmpresa, IdEmpleados, AttributeKey)
  );
END
GO

IF OBJECT_ID(N'dbo.Payroll_ConceptAssignments', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Payroll_ConceptAssignments (
    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdEmpresa INT NOT NULL,
    IdEmpleados INT NOT NULL,
    ConceptCode NVARCHAR(80) NOT NULL,
    FixedAmount DECIMAL(18,4) NULL,
    Rate DECIMAL(18,8) NULL,
    FormulaOrRuleId NVARCHAR(120) NULL,
    EffectiveFrom DATE NULL,
    EffectiveTo DATE NULL,
    AttributesJson NVARCHAR(MAX) NULL
  );
  CREATE INDEX IX_PayAssign ON dbo.Payroll_ConceptAssignments (IdEmpresa, IdEmpleados, ConceptCode);
END
GO

IF OBJECT_ID(N'dbo.Payroll_PeriodFacts', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Payroll_PeriodFacts (
    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdEmpresa INT NOT NULL,
    IdEmpleados INT NOT NULL,
    PeriodKey NVARCHAR(40) NOT NULL,
    FactType NVARCHAR(80) NOT NULL,
    ConceptCode NVARCHAR(80) NULL,
    Quantity DECIMAL(18,4) NOT NULL CONSTRAINT DF_PayFact_Qty DEFAULT (0),
    Amount DECIMAL(18,4) NOT NULL CONSTRAINT DF_PayFact_Amt DEFAULT (0),
    Source NVARCHAR(20) NOT NULL,
    AttributesJson NVARCHAR(MAX) NULL
  );
  CREATE INDEX IX_PayFacts ON dbo.Payroll_PeriodFacts (IdEmpresa, IdEmpleados, PeriodKey, Source);
END
GO

IF OBJECT_ID(N'dbo.Payroll_Runs', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Payroll_Runs (
    PayrollRunId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    IdEmpresa INT NOT NULL,
    PeriodKey NVARCHAR(40) NOT NULL,
    Intent NVARCHAR(40) NOT NULL,
    PeriodStart DATE NOT NULL,
    PeriodEnd DATE NOT NULL,
    RulePackId NVARCHAR(80) NOT NULL,
    RulePackVersion NVARCHAR(40) NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    CurrencyCode NVARCHAR(8) NOT NULL,
    DecimalPlaces INT NOT NULL,
    RoundingMode NVARCHAR(40) NOT NULL,
    RoundPerLine BIT NOT NULL,
    RoundAggregates BIT NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL,
    ApprovedAt DATETIMEOFFSET NULL,
    SnapshotHash NVARCHAR(128) NULL,
    SnapshotJson NVARCHAR(MAX) NULL,
    CONSTRAINT UQ_Payroll_Runs_Exec UNIQUE (IdEmpresa, PeriodKey, Intent)
  );
END
GO

IF OBJECT_ID(N'dbo.Payroll_Lines', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Payroll_Lines (
    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PayrollRunId UNIQUEIDENTIFIER NOT NULL,
    ConceptCode NVARCHAR(80) NOT NULL,
    Origin NVARCHAR(20) NOT NULL,
    Amount DECIMAL(18,4) NOT NULL,
    CurrencyCode NVARCHAR(8) NOT NULL,
    Description NVARCHAR(250) NULL,
    AttributesJson NVARCHAR(MAX) NULL,
    CONSTRAINT FK_PayLines_Run FOREIGN KEY (PayrollRunId) REFERENCES dbo.Payroll_Runs(PayrollRunId) ON DELETE CASCADE
  );
END
GO

IF OBJECT_ID(N'dbo.Payroll_Traces', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Payroll_Traces (
    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PayrollRunId UNIQUEIDENTIFIER NOT NULL,
    ConceptCode NVARCHAR(80) NOT NULL,
    BaseAmount DECIMAL(18,4) NULL,
    RuleDescription NVARCHAR(500) NOT NULL,
    ResultAmount DECIMAL(18,4) NOT NULL,
    CalculatorId NVARCHAR(120) NOT NULL,
    RulePackId NVARCHAR(80) NOT NULL,
    RulePackVersion NVARCHAR(40) NOT NULL,
    DetailsJson NVARCHAR(MAX) NULL,
    CONSTRAINT FK_PayTraces_Run FOREIGN KEY (PayrollRunId) REFERENCES dbo.Payroll_Runs(PayrollRunId) ON DELETE CASCADE
  );
END
GO

/* =====================================================================
   6) EmpresaAiConfig
   ===================================================================== */
IF OBJECT_ID(N'dbo.EmpresaAiConfig', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmpresaAiConfig
    (
        IdEmpresa INT NOT NULL CONSTRAINT PK_EmpresaAiConfig PRIMARY KEY,
        Provider NVARCHAR(40) NOT NULL CONSTRAINT DF_EmpresaAiConfig_Provider DEFAULT (N'OpenAI'),
        Model NVARCHAR(120) NOT NULL CONSTRAINT DF_EmpresaAiConfig_Model DEFAULT (N'gpt-4o-mini'),
        ApiKeyCipher NVARCHAR(MAX) NULL,
        BaseUrl NVARCHAR(300) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_EmpresaAiConfig_Activo DEFAULT (1),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_EmpresaAiConfig_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2 NULL,
        IdUsuarioActualizacion INT NULL,
        CONSTRAINT FK_EmpresaAiConfig_Empresa FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas(IdEmpresa)
    );
END
GO

/* =====================================================================
   7) SuscripcionCuentaCobro + cuentas MacroBits
   ===================================================================== */
IF OBJECT_ID(N'dbo.SuscripcionCuentaCobro', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SuscripcionCuentaCobro
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Banco NVARCHAR(120) NOT NULL,
        NumeroCuenta NVARCHAR(80) NOT NULL,
        Titular NVARCHAR(200) NOT NULL,
        Cedula NVARCHAR(40) NOT NULL,
        Correo NVARCHAR(200) NULL,
        CuentaEstandar NVARCHAR(80) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_SuscripcionCuentaCobro_Activo DEFAULT (1),
        Orden INT NOT NULL CONSTRAINT DF_SuscripcionCuentaCobro_Orden DEFAULT (0),
        FechaCreacion DATETIME NOT NULL CONSTRAINT DF_SuscripcionCuentaCobro_Fecha DEFAULT (GETDATE()),
        FechaModificacion DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SuscripcionCuentaCobro WHERE NumeroCuenta = N'32754360014')
    INSERT INTO dbo.SuscripcionCuentaCobro
        (Banco, NumeroCuenta, Titular, Cedula, Correo, CuentaEstandar, Activo, Orden)
    VALUES
        (N'Banco BHD', N'32754360014', N'ANA DE OLEO', N'00119101053',
         N'nadeysideoleo199130@gmail.com', N'DO06BCBH00000000032754360014', 1, 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SuscripcionCuentaCobro WHERE NumeroCuenta = N'0856204060')
    INSERT INTO dbo.SuscripcionCuentaCobro
        (Banco, NumeroCuenta, Titular, Cedula, Correo, CuentaEstandar, Activo, Orden)
    VALUES
        (N'Banco Popular', N'0856204060', N'ANA DE OLEO', N'00119101053', NULL, NULL, 1, 2);
GO

/* =====================================================================
   8) Catalogo de modulos + activacion
   ===================================================================== */
IF OBJECT_ID(N'tempdb..#ModSeed') IS NOT NULL DROP TABLE #ModSeed;
CREATE TABLE #ModSeed (
    Codigo NVARCHAR(80) NOT NULL PRIMARY KEY,
    Nombre NVARCHAR(200) NOT NULL,
    Descripcion NVARCHAR(400) NOT NULL,
    Scope NVARCHAR(20) NOT NULL -- ALL | CONTAB | CLINICA | CATALOG
);

INSERT INTO #ModSeed (Codigo, Nombre, Descripcion, Scope) VALUES
(N'ALAHIA_AI', N'Alahia AI', N'Asesor empresarial con IA integrada', N'ALL'),
(N'ANALISIS_COMPRAS_PRODUCTO', N'Analisis Producto-Proveedor', N'KPIs de compras desde historial FACTC', N'ALL'),
(N'ANTIGUEDAD_CXC', N'Antigüedad de Saldos CxC', N'Análisis de antigüedad de cuentas por cobrar', N'ALL'),
(N'ANTIGUEDAD_CXP', N'Antigüedad de Saldos CxP', N'Análisis de antigüedad de cuentas por pagar', N'ALL'),
(N'NOTAS_CREDITO', N'Notas de Crédito', N'Alta y consulta de notas de crédito comerciales (no devolución)', N'ALL'),
(N'SALDOS_A_FAVOR', N'Saldos a favor', N'Consulta de saldos a favor por notas de crédito', N'ALL'),
(N'REPORTE_606', N'Formato 606 (Compras)', N'Reporte DGII compras desde FACTC', N'ALL'),
(N'REPORTE_CLIENTES', N'Reporte Clientes', N'Listado de clientes', N'ALL'),
(N'REPORTE_EMPLEADOS', N'Reporte Empleados', N'Listado de empleados', N'ALL'),
(N'REPORTE_PRODUCTOS', N'Reporte Productos', N'Catálogo de productos y valor de inventario', N'ALL'),
(N'REPORTE_PROVEEDORES', N'Reporte Proveedores', N'Listado de proveedores', N'ALL'),
(N'CONTABILIDAD_ASIENTOS', N'Asientos Contables', N'Asientos manuales', N'CONTAB'),
(N'CONTABILIDAD_BALANCE_COMPROBACION', N'Balance de Comprobacion', N'Balance comprobacion', N'CONTAB'),
(N'CONTABILIDAD_BALANCE_GENERAL', N'Balance General', N'Balance general', N'CONTAB'),
(N'CONTABILIDAD_CIERRE', N'Cierre Contable', N'Cierre contable', N'CONTAB'),
(N'CONTABILIDAD_CONSULTA_ASIENTOS', N'Consulta de Asientos', N'Consulta asientos', N'CONTAB'),
(N'CONTABILIDAD_CUENTAS', N'Catalogo de Cuentas', N'Plan de cuentas', N'CONTAB'),
(N'CONTABILIDAD_ESTADO_RESULTADOS', N'Estado de Resultados', N'Estado resultados', N'CONTAB'),
(N'CONTABILIDAD_LIBRO_DIARIO', N'Libro Diario', N'Libro diario', N'CONTAB'),
(N'CONTABILIDAD_MAYOR_GENERAL', N'Mayor General', N'Mayor general', N'CONTAB'),
(N'FICHA_CLINICA', N'Ficha del paciente', N'Historia clínica dental del cliente: anamnesis, odontograma y cuenta desde facturas', N'CLINICA'),
(N'RRHH_LABORAL', N'Expediente Laboral', N'Datos laborales, salario y asignaciones de nómina', N'CATALOG');

INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
SELECT s.Codigo, s.Nombre, s.Descripcion, 0, 1, GETDATE()
FROM #ModSeed s
WHERE NOT EXISTS (SELECT 1 FROM dbo.Modulos m WHERE m.Codigo = s.Codigo);

UPDATE m
SET m.Nombre = s.Nombre,
    m.Descripcion = s.Descripcion,
    m.Activo = 1
FROM dbo.Modulos m
INNER JOIN #ModSeed s ON s.Codigo = m.Codigo;
GO

DECLARE @IdMod INT;

DECLARE seed_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT m.Id, s.Scope
    FROM #ModSeed s
    INNER JOIN dbo.Modulos m ON m.Codigo = s.Codigo;

DECLARE @Scope NVARCHAR(20);
OPEN seed_cur;
FETCH NEXT FROM seed_cur INTO @IdMod, @Scope;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF @Scope = N'ALL'
    BEGIN
        INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
        SELECT e.IdEmpresa, @IdMod, 1, GETDATE()
        FROM dbo.Empresas e
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.Empresa_Modulos em
            WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdMod
        );

        UPDATE dbo.Empresa_Modulos
        SET Activo = 1, FechaDesactivacion = NULL
        WHERE ModuloId = @IdMod;
    END
    ELSE IF @Scope = N'CONTAB'
    BEGIN
        INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
        SELECT DISTINCT em.EmpresaId, @IdMod, 1, GETDATE()
        FROM dbo.Empresa_Modulos em
        INNER JOIN dbo.Modulos pm ON pm.Id = em.ModuloId
        WHERE em.Activo = 1
          AND pm.Codigo IN (N'CONTABILIDAD', N'CONTABILIDAD_CONFIGURACION_INTEGRACION')
          AND NOT EXISTS (
              SELECT 1 FROM dbo.Empresa_Modulos x
              WHERE x.EmpresaId = em.EmpresaId AND x.ModuloId = @IdMod
          );

        UPDATE em
        SET em.Activo = 1, em.FechaDesactivacion = NULL
        FROM dbo.Empresa_Modulos em
        WHERE em.ModuloId = @IdMod
          AND EXISTS (
              SELECT 1 FROM dbo.Empresa_Modulos p
              INNER JOIN dbo.Modulos pm ON pm.Id = p.ModuloId
              WHERE p.EmpresaId = em.EmpresaId AND p.Activo = 1
                AND pm.Codigo IN (N'CONTABILIDAD', N'CONTABILIDAD_CONFIGURACION_INTEGRACION')
          );
    END
    ELSE IF @Scope = N'CLINICA'
    BEGIN
        INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
        SELECT em.EmpresaId, @IdMod, 1, GETDATE()
        FROM dbo.Empresa_Modulos em
        INNER JOIN dbo.Modulos pm ON pm.Id = em.ModuloId
        WHERE pm.Codigo = N'DOCUMENTOS_CLINICOS'
          AND em.Activo = 1
          AND NOT EXISTS (
              SELECT 1 FROM dbo.Empresa_Modulos x
              WHERE x.EmpresaId = em.EmpresaId AND x.ModuloId = @IdMod
          );

        UPDATE em
        SET em.Activo = 1, em.FechaDesactivacion = NULL
        FROM dbo.Empresa_Modulos em
        INNER JOIN dbo.Empresa_Modulos doc ON doc.EmpresaId = em.EmpresaId AND doc.Activo = 1
        INNER JOIN dbo.Modulos pm ON pm.Id = doc.ModuloId AND pm.Codigo = N'DOCUMENTOS_CLINICOS'
        WHERE em.ModuloId = @IdMod;
    END

    IF @Scope IN (N'ALL', N'CONTAB', N'CLINICA')
    BEGIN
        INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
        SELECT p.IdPerfil, @IdMod, 1, GETDATE(), p.IdEmpresa
        FROM dbo.Perfiles p
        WHERE EXISTS (
            SELECT 1 FROM dbo.Empresa_Modulos em
            WHERE em.EmpresaId = p.IdEmpresa AND em.ModuloId = @IdMod AND em.Activo = 1
        )
        AND NOT EXISTS (
            SELECT 1 FROM dbo.PerfilRoles pr
            WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdMod AND pr.IdEmpresa = p.IdEmpresa
        );

        UPDATE pr
        SET pr.Activo = 1
        FROM dbo.PerfilRoles pr
        WHERE pr.IdModulo = @IdMod
          AND EXISTS (
              SELECT 1 FROM dbo.Empresa_Modulos em
              WHERE em.EmpresaId = pr.IdEmpresa AND em.ModuloId = @IdMod AND em.Activo = 1
          );
    END

    FETCH NEXT FROM seed_cur INTO @IdMod, @Scope;
END
CLOSE seed_cur;
DEALLOCATE seed_cur;
GO

DROP TABLE #ModSeed;
GO

/* =====================================================================
   9) Alahia AI — usuario RO + schema ai en Prod
   ===================================================================== */
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'alahia_ai_ro')
    CREATE USER alahia_ai_ro FOR LOGIN alahia_ai_ro;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'ai')
    EXEC(N'CREATE SCHEMA ai AUTHORIZATION dbo;');
GO

CREATE OR ALTER FUNCTION ai.fn_IdEmpresaSesion()
RETURNS INT
AS
BEGIN
    DECLARE @v SQL_VARIANT = SESSION_CONTEXT(N'IdEmpresa');
    IF @v IS NULL RETURN NULL;
    RETURN TRY_CONVERT(INT, @v);
END;
GO

CREATE OR ALTER VIEW ai.v_FacturaHeaders AS
SELECT h.* FROM dbo.FacturaHeaders AS h
WHERE h.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_FacturaDetalles AS
SELECT d.* FROM dbo.FacturaDetalles AS d
INNER JOIN dbo.FacturaHeaders AS h ON h.IdFacturaHeader = d.IdFacturaHeader
WHERE h.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_Clientes AS
SELECT c.* FROM dbo.Clientes AS c
WHERE c.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_Productos AS
SELECT p.* FROM dbo.Productos AS p
WHERE p.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_Proveedores AS
SELECT p.* FROM dbo.Proveedores AS p
WHERE p.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_Almacenes AS
SELECT a.* FROM dbo.Almacenes AS a
WHERE a.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_AlmacenExistencias AS
SELECT e.* FROM dbo.AlmacenExistencias AS e
WHERE e.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_MovimientosInventario AS
SELECT m.* FROM dbo.MovimientosInventario AS m
WHERE m.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_OrdenCompraHeaders AS
SELECT o.* FROM dbo.OrdenCompraHeaders AS o
WHERE o.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_OrdenCompraDetalles AS
SELECT d.* FROM dbo.OrdenCompraDetalles AS d
INNER JOIN dbo.OrdenCompraHeaders AS h ON h.IdOrdenCompraHeader = d.IdOrdenCompraHeader
WHERE h.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_PagosFacturasClientes AS
SELECT p.* FROM dbo.PagosFacturasClientes AS p
WHERE p.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_PagosProveedor AS
SELECT p.* FROM dbo.PagosProveedor AS p
WHERE p.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_Ingresos AS
SELECT i.* FROM dbo.Ingresos AS i
WHERE i.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_Gastos AS
SELECT g.* FROM dbo.Gastos AS g
WHERE g.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_MovimientoFinanciero AS
SELECT m.* FROM dbo.MovimientoFinanciero AS m
WHERE m.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_CuentaFinanciera AS
SELECT c.* FROM dbo.CuentaFinanciera AS c
WHERE c.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_NotaCreditoes AS
SELECT n.* FROM dbo.NotaCreditoes AS n
WHERE n.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_CajaMovimiento AS
SELECT c.* FROM dbo.CajaMovimiento AS c
WHERE c.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_AsientosContables AS
SELECT a.* FROM dbo.AsientosContables AS a
WHERE a.IdEmpresa = ai.fn_IdEmpresaSesion() AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO
CREATE OR ALTER VIEW ai.v_Catalogo AS
SELECT v.name AS Vista, CAST(ep.value AS nvarchar(400)) AS Descripcion
FROM sys.views v
LEFT JOIN sys.extended_properties ep
    ON ep.major_id = v.object_id AND ep.minor_id = 0 AND ep.name = N'MS_Description'
WHERE SCHEMA_NAME(v.schema_id) = N'ai' AND v.name LIKE N'v_%' AND v.name <> N'v_Catalogo';
GO

DENY SELECT, INSERT, UPDATE, DELETE, EXECUTE, ALTER, CONTROL ON SCHEMA::dbo TO alahia_ai_ro;
GRANT SELECT ON SCHEMA::ai TO alahia_ai_ro;
GRANT EXECUTE ON OBJECT::ai.fn_IdEmpresaSesion TO alahia_ai_ro;
GO

PRINT '=== FIN migracion schema Dev->Prod 2026-08-16 ===';

SELECT 'TABLES' AS Kind, t.name AS Name
FROM sys.tables t
WHERE t.name IN (
  'ClienteSaldoAFavor','NotasCreditoAplicaciones','FichasClinicas',
  'EmpleadoLaboral','EmpleadoSalarioHistorial','NominaConcepto','NominaConceptoAsignacion',
  'Payroll_Employees','Payroll_Runs','EmpresaAiConfig','SuscripcionCuentaCobro'
)
ORDER BY t.name;

SELECT m.Codigo, m.Nombre,
       (SELECT COUNT(*) FROM dbo.Empresa_Modulos em WHERE em.ModuloId = m.Id AND em.Activo = 1) AS Empresas
FROM dbo.Modulos m
WHERE m.Codigo IN (
  N'ALAHIA_AI', N'FICHA_CLINICA', N'RRHH_LABORAL', N'NOTAS_CREDITO', N'SALDOS_A_FAVOR',
  N'ANTIGUEDAD_CXC', N'REPORTE_606', N'CONTABILIDAD_CUENTAS'
)
ORDER BY m.Codigo;
GO
