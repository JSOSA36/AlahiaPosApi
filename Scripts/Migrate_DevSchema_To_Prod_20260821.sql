/*
  Migrate_DevSchema_To_Prod_20260821.sql
  ------------------------------------------------------------
  Lleva a AlahiaPos_Prod el DDL de AlahiaPos_Dev que aún no está.
  Solo cambios aditivos / idempotentes.

  NO copia datos de negocio Dev -> Prod.
  NO elimina columnas ni tablas.
  NO activa manufactura a todos los clientes (queda en catálogo;
    MacroBits la licencia por empresa).

  Incluye:
  - FacturaHeaders: consumo de colaborador / nómina
  - EmpleadoLaboral: departamento, cargo, jornada, correo
  - Tablas RRHH, ponchador, kiosco facial, NominaProceso
  - Recetas y órdenes de producción (manufactura)
  - Catálogo de módulos RRHH + manufactura
  - RRHH queda en catálogo, sin activar a ningún cliente

  Autorización: usuario pidió pasar cambios de Dev a producción (BD).
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

PRINT '=== INICIO migracion schema Dev->Prod 2026-08-21 ===';
GO

/* =====================================================================
   1) FacturaHeaders — consumo colaborador
   ===================================================================== */
IF COL_LENGTH('dbo.FacturaHeaders', 'IdEmpleadoConsumo') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD IdEmpleadoConsumo INT NULL;
GO
IF COL_LENGTH('dbo.FacturaHeaders', 'PorcentajeDescuentoEmpleado') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD PorcentajeDescuentoEmpleado DECIMAL(9,2) NULL;
GO
IF COL_LENGTH('dbo.FacturaHeaders', 'CargarConsumoNomina') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD CargarConsumoNomina BIT NOT NULL
        CONSTRAINT DF_FactHdr_CargarNom DEFAULT (0);
GO
IF COL_LENGTH('dbo.FacturaHeaders', 'IdNominaDescuento') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD IdNominaDescuento INT NULL;
GO
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_FacturaHeaders_EmpleadoConsumo'
      AND object_id = OBJECT_ID(N'dbo.FacturaHeaders')
)
    CREATE INDEX IX_FacturaHeaders_EmpleadoConsumo
        ON dbo.FacturaHeaders (IdEmpresa, IdEmpleadoConsumo, CargarConsumoNomina)
        WHERE IdEmpleadoConsumo IS NOT NULL;
GO

/* =====================================================================
   2) EmpleadoLaboral
   ===================================================================== */
IF COL_LENGTH(N'dbo.EmpleadoLaboral', N'IdDepartamento') IS NULL
    ALTER TABLE dbo.EmpleadoLaboral ADD IdDepartamento INT NULL;
IF COL_LENGTH(N'dbo.EmpleadoLaboral', N'IdCargo') IS NULL
    ALTER TABLE dbo.EmpleadoLaboral ADD IdCargo INT NULL;
IF COL_LENGTH(N'dbo.EmpleadoLaboral', N'IdJornada') IS NULL
    ALTER TABLE dbo.EmpleadoLaboral ADD IdJornada INT NULL;
IF COL_LENGTH(N'dbo.EmpleadoLaboral', N'Correo') IS NULL
    ALTER TABLE dbo.EmpleadoLaboral ADD Correo NVARCHAR(150) NULL;
GO

/* =====================================================================
   3) RRHH catálogo y operación
   ===================================================================== */
IF OBJECT_ID(N'dbo.RrhhDepartamento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhDepartamento (
        IdDepartamento   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa        INT NOT NULL,
        Codigo           NVARCHAR(40)  NOT NULL,
        Nombre           NVARCHAR(120) NOT NULL,
        Descripcion      NVARCHAR(400) NULL,
        IdResponsable    INT NULL,
        Ubicacion        NVARCHAR(200) NULL,
        Telefono         NVARCHAR(40) NULL,
        Email            NVARCHAR(120) NULL,
        Activo           BIT NOT NULL CONSTRAINT DF_RrhhDepto_Act DEFAULT (1),
        FechaCreacion    DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhDepto_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_RrhhDepto UNIQUE (IdEmpresa, Codigo)
    );
    CREATE INDEX IX_RrhhDepto_Emp ON dbo.RrhhDepartamento (IdEmpresa, Activo);
END
GO

IF OBJECT_ID(N'dbo.RrhhJornada', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhJornada (
        IdJornada                 INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa                 INT NOT NULL,
        Nombre                    NVARCHAR(120) NOT NULL,
        HorasSemanales            DECIMAL(9,2) NOT NULL CONSTRAINT DF_RrhhJornada_Hrs DEFAULT (44),
        MinutosTardanzaGracia     INT NOT NULL CONSTRAINT DF_RrhhJornada_Gracia DEFAULT (10),
        Activo                    BIT NOT NULL CONSTRAINT DF_RrhhJornada_Act DEFAULT (1),
        FechaCreacion             DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhJornada_Cre DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_RrhhJornada_Emp ON dbo.RrhhJornada (IdEmpresa, Activo);
END
GO

IF OBJECT_ID(N'dbo.RrhhJornadaDia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhJornadaDia (
        IdJornadaDia     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdJornada        INT NOT NULL,
        DiaSemana        TINYINT NOT NULL,
        EsLaborable      BIT NOT NULL CONSTRAINT DF_RrhhJorDia_Lab DEFAULT (1),
        HoraEntrada      TIME(0) NULL,
        HoraSalida       TIME(0) NULL,
        RecesoInicio     TIME(0) NULL,
        RecesoFin        TIME(0) NULL,
        MinutosEsperados INT NOT NULL CONSTRAINT DF_RrhhJorDia_Min DEFAULT (0),
        CONSTRAINT UQ_RrhhJornadaDia UNIQUE (IdJornada, DiaSemana),
        CONSTRAINT FK_RrhhJornadaDia_Jornada FOREIGN KEY (IdJornada)
            REFERENCES dbo.RrhhJornada (IdJornada) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.RrhhCargo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhCargo (
        IdCargo          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa        INT NOT NULL,
        IdDepartamento   INT NULL,
        Codigo           NVARCHAR(40)  NOT NULL,
        Nombre           NVARCHAR(120) NOT NULL,
        Descripcion      NVARCHAR(400) NULL,
        IdJornada        INT NULL,
        SalarioBase      DECIMAL(18,2) NOT NULL CONSTRAINT DF_RrhhCargo_Sal DEFAULT (0),
        Moneda           NVARCHAR(8) NOT NULL CONSTRAINT DF_RrhhCargo_Mon DEFAULT (N'DOP'),
        FrecuenciaPago   NVARCHAR(20) NOT NULL CONSTRAINT DF_RrhhCargo_Freq DEFAULT (N'QUINCENAL'),
        TipoEmpleado     NVARCHAR(40) NOT NULL CONSTRAINT DF_RrhhCargo_Tipo DEFAULT (N'FIJO'),
        Activo           BIT NOT NULL CONSTRAINT DF_RrhhCargo_Act DEFAULT (1),
        FechaCreacion    DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhCargo_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_RrhhCargo UNIQUE (IdEmpresa, Codigo)
    );
    CREATE INDEX IX_RrhhCargo_Emp ON dbo.RrhhCargo (IdEmpresa, Activo);
END
GO

IF OBJECT_ID(N'dbo.RrhhTurno', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhTurno (
        IdTurno          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa        INT NOT NULL,
        Codigo           NVARCHAR(40)  NOT NULL,
        Nombre           NVARCHAR(120) NOT NULL,
        HoraEntrada      TIME(0) NOT NULL,
        HoraSalida       TIME(0) NOT NULL,
        RecesoInicio     TIME(0) NULL,
        RecesoFin        TIME(0) NULL,
        Activo           BIT NOT NULL CONSTRAINT DF_RrhhTurno_Act DEFAULT (1),
        FechaCreacion    DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhTurno_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_RrhhTurno UNIQUE (IdEmpresa, Codigo)
    );
END
GO

IF OBJECT_ID(N'dbo.RrhhEmpleadoHorario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhEmpleadoHorario (
        IdEmpleadoHorario INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa         INT NOT NULL,
        IdEmpleados       INT NOT NULL,
        IdJornada         INT NOT NULL,
        IdTurno           INT NULL,
        VigenteDesde      DATE NOT NULL,
        VigenteHasta      DATE NULL,
        Activo            BIT NOT NULL CONSTRAINT DF_RrhhEmpHor_Act DEFAULT (1),
        FechaCreacion     DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhEmpHor_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_RrhhEmpHor_Jornada FOREIGN KEY (IdJornada) REFERENCES dbo.RrhhJornada (IdJornada)
    );
    CREATE INDEX IX_RrhhEmpHor_Emp ON dbo.RrhhEmpleadoHorario (IdEmpresa, IdEmpleados, Activo, VigenteDesde);
END
GO

IF OBJECT_ID(N'dbo.RrhhPonchada', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhPonchada (
        IdPonchada         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa          INT NOT NULL,
        IdEmpleados        INT NOT NULL,
        FechaHora          DATETIME2(0) NOT NULL,
        Tipo               NVARCHAR(20) NOT NULL,
        Origen             NVARCHAR(20) NOT NULL CONSTRAINT DF_RrhhPonch_Ori DEFAULT (N'WEB'),
        Dispositivo        NVARCHAR(120) NULL,
        Ip                 NVARCHAR(60) NULL,
        IdUsuarioRegistra  INT NOT NULL CONSTRAINT DF_RrhhPonch_Usr DEFAULT (0),
        FechaRegistro      DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhPonch_Reg DEFAULT (SYSUTCDATETIME()),
        Nota               NVARCHAR(250) NULL,
        ClaveExterna       NVARCHAR(80) NULL
    );
    CREATE INDEX IX_RrhhPonch_EmpDia ON dbo.RrhhPonchada (IdEmpresa, IdEmpleados, FechaHora);
END
GO
IF COL_LENGTH(N'dbo.RrhhPonchada', N'ClaveExterna') IS NULL
    ALTER TABLE dbo.RrhhPonchada ADD ClaveExterna NVARCHAR(80) NULL;
GO
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_RrhhPonchada_Clave' AND object_id = OBJECT_ID(N'dbo.RrhhPonchada')
)
    CREATE UNIQUE INDEX UX_RrhhPonchada_Clave
        ON dbo.RrhhPonchada (IdEmpresa, ClaveExterna)
        WHERE ClaveExterna IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.RrhhPonchadaCorreccion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhPonchadaCorreccion (
        IdCorreccion       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa          INT NOT NULL,
        IdEmpleados        INT NOT NULL,
        IdPonchada         INT NULL,
        TipoCorreccion     NVARCHAR(30) NOT NULL,
        Estado             NVARCHAR(20) NOT NULL CONSTRAINT DF_RrhhCorr_Est DEFAULT (N'PENDIENTE'),
        FechaHoraOriginal  DATETIME2(0) NULL,
        TipoOriginal       NVARCHAR(20) NULL,
        FechaHoraNueva     DATETIME2(0) NULL,
        TipoNuevo          NVARCHAR(20) NULL,
        Motivo             NVARCHAR(400) NOT NULL,
        IdUsuarioSolicita  INT NOT NULL,
        FechaSolicitud     DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhCorr_Sol DEFAULT (SYSUTCDATETIME()),
        IdUsuarioAprueba   INT NULL,
        FechaDecision      DATETIME2(0) NULL,
        MotivoDecision     NVARCHAR(400) NULL,
        CONSTRAINT FK_RrhhCorr_Ponch FOREIGN KEY (IdPonchada) REFERENCES dbo.RrhhPonchada (IdPonchada)
    );
    CREATE INDEX IX_RrhhCorr_Emp ON dbo.RrhhPonchadaCorreccion (IdEmpresa, IdEmpleados, Estado);
END
GO

IF OBJECT_ID(N'dbo.RrhhTipoAusencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhTipoAusencia (
        IdTipoAusencia       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa            INT NOT NULL,
        Codigo               NVARCHAR(40)  NOT NULL,
        Nombre               NVARCHAR(120) NOT NULL,
        Categoria            NVARCHAR(20)  NOT NULL,
        UnidadDefault        NVARCHAR(10)  NOT NULL CONSTRAINT DF_RrhhTipoAus_Uni DEFAULT (N'DIAS'),
        ConGoceSueldo        BIT NOT NULL CONSTRAINT DF_RrhhTipoAus_Goce DEFAULT (1),
        RequiereAprobacion   BIT NOT NULL CONSTRAINT DF_RrhhTipoAus_Apr DEFAULT (1),
        AfectaAsistencia     BIT NOT NULL CONSTRAINT DF_RrhhTipoAus_Af DEFAULT (1),
        Activo               BIT NOT NULL CONSTRAINT DF_RrhhTipoAus_Act DEFAULT (1),
        CONSTRAINT UQ_RrhhTipoAus UNIQUE (IdEmpresa, Codigo)
    );
END
GO

IF OBJECT_ID(N'dbo.RrhhSolicitudAusencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhSolicitudAusencia (
        IdSolicitud          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa            INT NOT NULL,
        IdEmpleados          INT NOT NULL,
        IdTipoAusencia       INT NOT NULL,
        FechaInicio          DATE NOT NULL,
        FechaFin             DATE NOT NULL,
        HoraInicio           TIME(0) NULL,
        HoraFin              TIME(0) NULL,
        Unidad               NVARCHAR(10) NOT NULL,
        Cantidad             DECIMAL(9,2) NOT NULL,
        ConGoceSueldo        BIT NOT NULL,
        Estado               NVARCHAR(20) NOT NULL CONSTRAINT DF_RrhhSolAus_Est DEFAULT (N'PENDIENTE'),
        Motivo               NVARCHAR(400) NULL,
        IdUsuarioSolicita    INT NOT NULL,
        FechaSolicitud       DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhSolAus_Sol DEFAULT (SYSUTCDATETIME()),
        IdUsuarioAprueba     INT NULL,
        FechaDecision        DATETIME2(0) NULL,
        ComentarioDecision   NVARCHAR(400) NULL,
        CONSTRAINT FK_RrhhSolAus_Tipo FOREIGN KEY (IdTipoAusencia) REFERENCES dbo.RrhhTipoAusencia (IdTipoAusencia)
    );
    CREATE INDEX IX_RrhhSolAus_Emp ON dbo.RrhhSolicitudAusencia (IdEmpresa, IdEmpleados, FechaInicio, FechaFin);
END
GO

IF OBJECT_ID(N'dbo.RrhhAsistenciaDia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhAsistenciaDia (
        IdAsistenciaDia           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa                 INT NOT NULL,
        IdEmpleados               INT NOT NULL,
        Fecha                     DATE NOT NULL,
        IdJornada                 INT NULL,
        IdTurno                   INT NULL,
        HoraEntradaEsperada       TIME(0) NULL,
        HoraSalidaEsperada        TIME(0) NULL,
        HoraEntradaReal           DATETIME2(0) NULL,
        HoraSalidaReal            DATETIME2(0) NULL,
        MinutosTrabajados         INT NOT NULL CONSTRAINT DF_RrhhAsis_Trab DEFAULT (0),
        MinutosEsperados          INT NOT NULL CONSTRAINT DF_RrhhAsis_Esp DEFAULT (0),
        MinutosTardanza           INT NOT NULL CONSTRAINT DF_RrhhAsis_Tar DEFAULT (0),
        MinutosSalidaAnticipada   INT NOT NULL CONSTRAINT DF_RrhhAsis_Ant DEFAULT (0),
        MinutosExtra              INT NOT NULL CONSTRAINT DF_RrhhAsis_Ext DEFAULT (0),
        Estado                    NVARCHAR(30) NOT NULL,
        IdSolicitudAusencia       INT NULL,
        EsJustificado             BIT NOT NULL CONSTRAINT DF_RrhhAsis_Jus DEFAULT (0),
        CalculadoEn               DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhAsis_Cal DEFAULT (SYSUTCDATETIME()),
        Observacion               NVARCHAR(400) NULL,
        CONSTRAINT UQ_RrhhAsistenciaDia UNIQUE (IdEmpresa, IdEmpleados, Fecha)
    );
    CREATE INDEX IX_RrhhAsis_Emp ON dbo.RrhhAsistenciaDia (IdEmpresa, Fecha);
END
GO

IF OBJECT_ID(N'dbo.RrhhPrestamo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhPrestamo (
        IdPrestamo       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa        INT NOT NULL,
        IdEmpleados      INT NOT NULL,
        Monto            DECIMAL(18,2) NOT NULL,
        Saldo            DECIMAL(18,2) NOT NULL,
        Cuotas           INT NOT NULL,
        MontoCuota       DECIMAL(18,2) NOT NULL,
        FechaInicio      DATE NOT NULL,
        Estado           NVARCHAR(20) NOT NULL CONSTRAINT DF_RrhhPrest_Est DEFAULT (N'ACTIVO'),
        Motivo           NVARCHAR(250) NULL,
        IdUsuarioCrea    INT NOT NULL CONSTRAINT DF_RrhhPrest_Usr DEFAULT (0),
        FechaCreacion    DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhPrest_Cre DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_RrhhPrest_Emp ON dbo.RrhhPrestamo (IdEmpresa, IdEmpleados, Estado);
END
GO

IF OBJECT_ID(N'dbo.RrhhPrestamoCuota', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhPrestamoCuota (
        IdCuota          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdPrestamo       INT NOT NULL,
        Numero           INT NOT NULL,
        FechaProgramada  DATE NOT NULL,
        Monto            DECIMAL(18,2) NOT NULL,
        PeriodKey        NVARCHAR(40) NULL,
        Estado           NVARCHAR(20) NOT NULL CONSTRAINT DF_RrhhCuota_Est DEFAULT (N'PENDIENTE'),
        CONSTRAINT FK_RrhhCuota_Prest FOREIGN KEY (IdPrestamo)
            REFERENCES dbo.RrhhPrestamo (IdPrestamo) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.RrhhAnticipo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhAnticipo (
        IdAnticipo       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa        INT NOT NULL,
        IdEmpleados      INT NOT NULL,
        Monto            DECIMAL(18,2) NOT NULL,
        Fecha            DATE NOT NULL,
        PeriodKey        NVARCHAR(40) NULL,
        Estado           NVARCHAR(20) NOT NULL CONSTRAINT DF_RrhhAnt_Est DEFAULT (N'PENDIENTE'),
        Motivo           NVARCHAR(250) NULL,
        IdUsuarioCrea    INT NOT NULL CONSTRAINT DF_RrhhAnt_Usr DEFAULT (0),
        FechaCreacion    DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhAnt_Cre DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_RrhhAnt_Emp ON dbo.RrhhAnticipo (IdEmpresa, IdEmpleados, Estado);
END
GO

IF OBJECT_ID(N'dbo.RrhhBeneficio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhBeneficio (
        IdBeneficio              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa                INT NOT NULL,
        Codigo                   NVARCHAR(40)  NOT NULL,
        Nombre                   NVARCHAR(120) NOT NULL,
        Descripcion              NVARCHAR(400) NULL,
        TipoCalculo              NVARCHAR(30)  NOT NULL CONSTRAINT DF_RrhhBen_Tipo DEFAULT (N'MONTO_FIJO'),
        Monto                    DECIMAL(18,4) NOT NULL CONSTRAINT DF_RrhhBen_Mon DEFAULT (0),
        Periodicidad             NVARCHAR(20)  NOT NULL CONSTRAINT DF_RrhhBen_Per DEFAULT (N'MENSUAL'),
        AfectaNomina             BIT NOT NULL CONSTRAINT DF_RrhhBen_Nom DEFAULT (1),
        EnEspecie                BIT NOT NULL CONSTRAINT DF_RrhhBen_Esp DEFAULT (0),
        FormaDesembolso          NVARCHAR(20) NOT NULL CONSTRAINT DF_RrhhBen_Forma DEFAULT (N'NOMINA'),
        DiaPagoMes               TINYINT NULL,
        MetodoPago               NVARCHAR(30) NULL,
        DescontarConsumoNomina   BIT NOT NULL CONSTRAINT DF_RrhhBen_DescNom DEFAULT (0),
        Activo                   BIT NOT NULL CONSTRAINT DF_RrhhBen_Act DEFAULT (1),
        FechaCreacion            DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhBen_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_RrhhBeneficio UNIQUE (IdEmpresa, Codigo)
    );
    CREATE INDEX IX_RrhhBen_Emp ON dbo.RrhhBeneficio (IdEmpresa, Activo);
END
GO

IF OBJECT_ID(N'dbo.RrhhCargoBeneficio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhCargoBeneficio (
        IdCargoBeneficio INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdCargo          INT NOT NULL,
        IdBeneficio      INT NOT NULL,
        CONSTRAINT UQ_RrhhCargoBen UNIQUE (IdCargo, IdBeneficio),
        CONSTRAINT FK_RrhhCargoBen_Cargo FOREIGN KEY (IdCargo)
            REFERENCES dbo.RrhhCargo (IdCargo) ON DELETE CASCADE,
        CONSTRAINT FK_RrhhCargoBen_Ben FOREIGN KEY (IdBeneficio)
            REFERENCES dbo.RrhhBeneficio (IdBeneficio)
    );
END
GO

IF OBJECT_ID(N'dbo.RrhhCargoSalarioHistorial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhCargoSalarioHistorial (
        IdCargoSalarioHistorial INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdCargo          INT NOT NULL,
        IdEmpresa        INT NOT NULL,
        SalarioAnterior  DECIMAL(18,2) NOT NULL,
        SalarioNuevo     DECIMAL(18,2) NOT NULL,
        FrecuenciaPago   NVARCHAR(20) NOT NULL,
        VigenteDesde     DATE NOT NULL,
        FechaRegistro    DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhCargoSal_Reg DEFAULT (SYSUTCDATETIME()),
        Motivo           NVARCHAR(250) NULL,
        IdUsuario        INT NULL,
        CONSTRAINT FK_RrhhCargoSal_Cargo FOREIGN KEY (IdCargo)
            REFERENCES dbo.RrhhCargo (IdCargo)
    );
    CREATE INDEX IX_RrhhCargoSal_Cargo ON dbo.RrhhCargoSalarioHistorial (IdCargo, VigenteDesde);
END
GO

IF OBJECT_ID(N'dbo.NominaProceso', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NominaProceso (
        IdNominaProceso          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa                INT NOT NULL,
        PeriodKey                NVARCHAR(40) NOT NULL,
        Intent                   NVARCHAR(40) NOT NULL CONSTRAINT DF_NomProc_Int DEFAULT (N'REGULAR'),
        FechaInicio              DATE NOT NULL,
        FechaFin                 DATE NOT NULL,
        Frecuencia               NVARCHAR(20) NOT NULL CONSTRAINT DF_NomProc_Freq DEFAULT (N'QUINCENAL'),
        Estado                   NVARCHAR(20) NOT NULL CONSTRAINT DF_NomProc_Est DEFAULT (N'BORRADOR'),
        PayrollRunId             UNIQUEIDENTIFIER NULL,
        Observacion              NVARCHAR(400) NULL,
        IdUsuarioCrea            INT NOT NULL CONSTRAINT DF_NomProc_Usr DEFAULT (0),
        FechaCreacion            DATETIME2(0) NOT NULL CONSTRAINT DF_NomProc_Cre DEFAULT (SYSUTCDATETIME()),
        IdUsuarioRevision        INT NULL,
        FechaRevision            DATETIME2(0) NULL,
        IdUsuarioAprueba         INT NULL,
        FechaAprobacion          DATETIME2(0) NULL,
        IdUsuarioPaga            INT NULL,
        FechaPago                DATETIME2(0) NULL,
        IdUsuarioCierra          INT NULL,
        FechaCierre              DATETIME2(0) NULL,
        IdCuentaFinanciera       INT NULL,
        IdMovimientoFinanciero   INT NULL,
        CONSTRAINT UQ_NominaProceso UNIQUE (IdEmpresa, PeriodKey, Intent)
    );
    CREATE INDEX IX_NomProc_Emp ON dbo.NominaProceso (IdEmpresa, Estado);
END
GO
IF COL_LENGTH(N'dbo.NominaProceso', N'IdCuentaFinanciera') IS NULL
    ALTER TABLE dbo.NominaProceso ADD IdCuentaFinanciera INT NULL;
IF COL_LENGTH(N'dbo.NominaProceso', N'IdMovimientoFinanciero') IS NULL
    ALTER TABLE dbo.NominaProceso ADD IdMovimientoFinanciero INT NULL;
GO

IF OBJECT_ID(N'dbo.NominaProcesoEmpleado', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NominaProcesoEmpleado (
        IdNominaProcesoEmpleado INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdNominaProceso         INT NOT NULL,
        IdEmpleados             INT NOT NULL,
        NombreEmpleado          NVARCHAR(200) NULL,
        SalarioBase             DECIMAL(18,4) NOT NULL CONSTRAINT DF_NomPE_Sal DEFAULT (0),
        HorasExtra              DECIMAL(18,4) NOT NULL CONSTRAINT DF_NomPE_HE DEFAULT (0),
        Comisiones              DECIMAL(18,4) NOT NULL CONSTRAINT DF_NomPE_Com DEFAULT (0),
        Bonificaciones          DECIMAL(18,4) NOT NULL CONSTRAINT DF_NomPE_Bon DEFAULT (0),
        DescuentosAsistencia    DECIMAL(18,4) NOT NULL CONSTRAINT DF_NomPE_Des DEFAULT (0),
        Prestamos               DECIMAL(18,4) NOT NULL CONSTRAINT DF_NomPE_Pre DEFAULT (0),
        Anticipos               DECIMAL(18,4) NOT NULL CONSTRAINT DF_NomPE_Ant DEFAULT (0),
        OtrosIngresos           DECIMAL(18,4) NOT NULL CONSTRAINT DF_NomPE_Oi DEFAULT (0),
        OtrosDescuentos         DECIMAL(18,4) NOT NULL CONSTRAINT DF_NomPE_Od DEFAULT (0),
        DeduccionesLegales      DECIMAL(18,4) NOT NULL CONSTRAINT DF_NomPE_Leg DEFAULT (0),
        Bruto                   DECIMAL(18,4) NOT NULL CONSTRAINT DF_NomPE_Bru DEFAULT (0),
        Neto                    DECIMAL(18,4) NOT NULL CONSTRAINT DF_NomPE_Net DEFAULT (0),
        DiasAusenteSinGoce      DECIMAL(9,2) NOT NULL CONSTRAINT DF_NomPE_Aus DEFAULT (0),
        MinutosExtra            INT NOT NULL CONSTRAINT DF_NomPE_MinX DEFAULT (0),
        LineasJson              NVARCHAR(MAX) NULL,
        CONSTRAINT UQ_NominaProcesoEmp UNIQUE (IdNominaProceso, IdEmpleados),
        CONSTRAINT FK_NomPE_Proc FOREIGN KEY (IdNominaProceso)
            REFERENCES dbo.NominaProceso (IdNominaProceso) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.NominaProcesoEvento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NominaProcesoEvento (
        IdEvento         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdNominaProceso  INT NOT NULL,
        EstadoAnterior   NVARCHAR(20) NULL,
        EstadoNuevo      NVARCHAR(20) NOT NULL,
        IdUsuario        INT NOT NULL,
        Fecha            DATETIME2(0) NOT NULL CONSTRAINT DF_NomEvt_Fec DEFAULT (SYSUTCDATETIME()),
        Comentario       NVARCHAR(400) NULL,
        CONSTRAINT FK_NomEvt_Proc FOREIGN KEY (IdNominaProceso)
            REFERENCES dbo.NominaProceso (IdNominaProceso) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.RrhhPonchadorDispositivo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhPonchadorDispositivo (
        IdDispositivo        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa            INT NOT NULL,
        Serial               NVARCHAR(80)  NOT NULL,
        Nombre               NVARCHAR(120) NOT NULL,
        Proveedor            NVARCHAR(30)  NOT NULL CONSTRAINT DF_RrhhReloj_Prov DEFAULT (N'ZKTECO'),
        Token                NVARCHAR(80)  NULL,
        DireccionIp          NVARCHAR(80) NULL,
        Puerto               INT NULL,
        ClaveComunicacion    NVARCHAR(40) NULL,
        Activo               BIT NOT NULL CONSTRAINT DF_RrhhReloj_Act DEFAULT (1),
        UltimaComunicacion   DATETIME2(0) NULL,
        FechaCreacion        DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhReloj_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_RrhhReloj_Serial UNIQUE (Serial)
    );
    CREATE INDEX IX_RrhhReloj_Emp ON dbo.RrhhPonchadorDispositivo (IdEmpresa, Activo);
END
GO
IF COL_LENGTH(N'dbo.RrhhPonchadorDispositivo', N'DireccionIp') IS NULL
    ALTER TABLE dbo.RrhhPonchadorDispositivo ADD DireccionIp NVARCHAR(80) NULL;
IF COL_LENGTH(N'dbo.RrhhPonchadorDispositivo', N'Puerto') IS NULL
    ALTER TABLE dbo.RrhhPonchadorDispositivo ADD Puerto INT NULL;
IF COL_LENGTH(N'dbo.RrhhPonchadorDispositivo', N'ClaveComunicacion') IS NULL
    ALTER TABLE dbo.RrhhPonchadorDispositivo ADD ClaveComunicacion NVARCHAR(40) NULL;
GO

IF OBJECT_ID(N'dbo.RrhhPonchadorPersona', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhPonchadorPersona (
        IdPersonaDispositivo INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa            INT NOT NULL,
        IdEmpleados          INT NOT NULL,
        CodigoDispositivo    NVARCHAR(40) NOT NULL,
        Activo               BIT NOT NULL CONSTRAINT DF_RrhhRelojPer_Act DEFAULT (1),
        FechaCreacion        DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhRelojPer_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_RrhhRelojPer UNIQUE (IdEmpresa, CodigoDispositivo)
    );
    CREATE INDEX IX_RrhhRelojPer_Emp ON dbo.RrhhPonchadorPersona (IdEmpresa, IdEmpleados);
END
GO

IF OBJECT_ID(N'dbo.RrhhPonchadorIngesta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhPonchadorIngesta (
        IdIngesta         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa         INT NULL,
        IdDispositivo     INT NULL,
        Serial            NVARCHAR(80) NOT NULL,
        CodigoDispositivo NVARCHAR(40) NULL,
        IdEmpleados       INT NULL,
        FechaHoraReloj    DATETIME2(0) NULL,
        Estado            NVARCHAR(30) NOT NULL,
        ClaveExterna      NVARCHAR(80) NULL,
        IdPonchada        INT NULL,
        Detalle           NVARCHAR(400) NULL,
        Fecha             DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhRelojIng_Fec DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_RrhhRelojIng_Emp ON dbo.RrhhPonchadorIngesta (IdEmpresa, Fecha DESC);
END
GO

IF OBJECT_ID(N'dbo.RrhhEmpleadoRostro', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhEmpleadoRostro (
        IdRostro                 INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa                INT NOT NULL,
        IdEmpleados              INT NOT NULL,
        Embedding                NVARCHAR(MAX) NOT NULL,
        Muestras                 INT NOT NULL CONSTRAINT DF_RrhhRostro_Muestras DEFAULT (1),
        Consentimiento           BIT NOT NULL CONSTRAINT DF_RrhhRostro_Cons DEFAULT (0),
        FechaEnrolamiento        DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhRostro_Enr DEFAULT (SYSUTCDATETIME()),
        IdUsuarioEnrolamiento    INT NOT NULL,
        Activo                   BIT NOT NULL CONSTRAINT DF_RrhhRostro_Act DEFAULT (1),
        PermitirPinExcepcion     BIT NOT NULL CONSTRAINT DF_RrhhRostro_Pin DEFAULT (0),
        PinHash                  NVARCHAR(128) NULL,
        PinSalt                  NVARCHAR(64) NULL,
        IntentosPinFallidos      INT NOT NULL CONSTRAINT DF_RrhhRostro_PinFail DEFAULT (0),
        PinBloqueadoHasta        DATETIME2(0) NULL,
        CONSTRAINT UQ_RrhhEmpleadoRostro UNIQUE (IdEmpresa, IdEmpleados)
    );
    CREATE INDEX IX_RrhhRostro_Emp ON dbo.RrhhEmpleadoRostro (IdEmpresa, Activo);
END
GO

IF OBJECT_ID(N'dbo.RrhhKioscoEvento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhKioscoEvento (
        IdEvento      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa     INT NOT NULL,
        IdEmpleados   INT NULL,
        Tipo          NVARCHAR(30) NOT NULL,
        Distancia     FLOAT NULL,
        IdUsuario     INT NOT NULL,
        Dispositivo   NVARCHAR(120) NULL,
        Ip            NVARCHAR(60) NULL,
        Detalle       NVARCHAR(400) NULL,
        Fecha         DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhKioscoEvt_Fec DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_RrhhKioscoEvt_Emp ON dbo.RrhhKioscoEvento (IdEmpresa, Fecha DESC);
END
GO

/* =====================================================================
   4) Manufactura (recetas / órdenes de producción)
   ===================================================================== */
IF OBJECT_ID(N'dbo.RecetaProduccion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecetaProduccion (
        IdReceta            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa           INT NOT NULL,
        IdProductoTerminado INT NOT NULL,
        Nombre              NVARCHAR(160) NOT NULL,
        RendimientoBase     DECIMAL(18,4) NOT NULL CONSTRAINT DF_RecetaProduccion_Rend DEFAULT (1),
        IdUnidadMedida      INT NULL,
        Activa              BIT NOT NULL CONSTRAINT DF_RecetaProduccion_Activa DEFAULT (1),
        Observacion         NVARCHAR(500) NULL,
        FechaCreacion       DATETIME NOT NULL CONSTRAINT DF_RecetaProduccion_Fecha DEFAULT (GETDATE()),
        IdUsuario           INT NULL,
        CONSTRAINT FK_RecetaProduccion_Producto FOREIGN KEY (IdProductoTerminado) REFERENCES dbo.Productos (IdProducto)
    );
    CREATE INDEX IX_RecetaProduccion_Empresa ON dbo.RecetaProduccion (IdEmpresa, IdProductoTerminado, Activa);
END
GO

IF OBJECT_ID(N'dbo.RecetaProduccionItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecetaProduccionItem (
        IdRecetaItem   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdReceta       INT NOT NULL,
        IdProducto     INT NOT NULL,
        Cantidad       DECIMAL(18,4) NOT NULL,
        IdUnidadMedida INT NULL,
        Orden          INT NOT NULL CONSTRAINT DF_RecetaProduccionItem_Orden DEFAULT (0),
        Activo         BIT NOT NULL CONSTRAINT DF_RecetaProduccionItem_Activo DEFAULT (1),
        CONSTRAINT FK_RecetaProduccionItem_Receta FOREIGN KEY (IdReceta) REFERENCES dbo.RecetaProduccion (IdReceta),
        CONSTRAINT FK_RecetaProduccionItem_Producto FOREIGN KEY (IdProducto) REFERENCES dbo.Productos (IdProducto)
    );
    CREATE INDEX IX_RecetaProduccionItem_Receta ON dbo.RecetaProduccionItem (IdReceta, Activo);
END
GO

IF OBJECT_ID(N'dbo.OrdenProduccion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenProduccion (
        IdOrdenProduccion    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa            INT NOT NULL,
        Numero               NVARCHAR(40) NOT NULL,
        IdReceta             INT NOT NULL,
        IdProductoTerminado  INT NOT NULL,
        CantidadPlanificada  DECIMAL(18,4) NOT NULL,
        CantidadReal         DECIMAL(18,4) NULL,
        IdAlmacenOrigen      INT NOT NULL,
        IdAlmacenDestino     INT NOT NULL,
        Fecha                DATETIME NOT NULL CONSTRAINT DF_OrdenProduccion_Fecha DEFAULT (GETDATE()),
        IdUsuarioResponsable INT NULL,
        Observacion          NVARCHAR(500) NULL,
        Estado               NVARCHAR(20) NOT NULL CONSTRAINT DF_OrdenProduccion_Estado DEFAULT (N'BORRADOR'),
        IdMovimientoSalida   INT NULL,
        IdMovimientoEntrada  INT NULL,
        CostoMateriales      DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenProduccion_Costo DEFAULT (0),
        CostoUnitario        DECIMAL(18,4) NOT NULL CONSTRAINT DF_OrdenProduccion_CostoU DEFAULT (0),
        FechaInicio          DATETIME NULL,
        FechaCompletado      DATETIME NULL,
        FechaCreacion        DATETIME NOT NULL CONSTRAINT DF_OrdenProduccion_Creacion DEFAULT (GETDATE()),
        IdUsuario            INT NULL,
        CONSTRAINT FK_OrdenProduccion_Receta FOREIGN KEY (IdReceta) REFERENCES dbo.RecetaProduccion (IdReceta),
        CONSTRAINT FK_OrdenProduccion_Producto FOREIGN KEY (IdProductoTerminado) REFERENCES dbo.Productos (IdProducto),
        CONSTRAINT CK_OrdenProduccion_Estado CHECK (Estado IN (N'BORRADOR', N'PLANIFICADA', N'EN_PROCESO', N'COMPLETADA', N'CANCELADA'))
    );
    CREATE UNIQUE INDEX UX_OrdenProduccion_Empresa_Numero ON dbo.OrdenProduccion (IdEmpresa, Numero);
    CREATE INDEX IX_OrdenProduccion_Empresa_Estado ON dbo.OrdenProduccion (IdEmpresa, Estado, Fecha DESC);
END
GO

IF OBJECT_ID(N'dbo.OrdenProduccionMaterial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenProduccionMaterial (
        IdOrdenMaterial   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdOrdenProduccion INT NOT NULL,
        IdProducto        INT NOT NULL,
        IdUnidadMedida    INT NULL,
        CantidadTeorica   DECIMAL(18,4) NOT NULL,
        CantidadReal      DECIMAL(18,4) NULL,
        Disponible        DECIMAL(18,4) NOT NULL CONSTRAINT DF_OrdenProdMat_Disp DEFAULT (0),
        Faltante          DECIMAL(18,4) NOT NULL CONSTRAINT DF_OrdenProdMat_Falt DEFAULT (0),
        PrecioCompra      DECIMAL(18,4) NOT NULL CONSTRAINT DF_OrdenProdMat_Precio DEFAULT (0),
        CostoLinea        DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenProdMat_Costo DEFAULT (0),
        CONSTRAINT FK_OrdenProduccionMaterial_Orden FOREIGN KEY (IdOrdenProduccion) REFERENCES dbo.OrdenProduccion (IdOrdenProduccion),
        CONSTRAINT FK_OrdenProduccionMaterial_Producto FOREIGN KEY (IdProducto) REFERENCES dbo.Productos (IdProducto)
    );
    CREATE INDEX IX_OrdenProduccionMaterial_Orden ON dbo.OrdenProduccionMaterial (IdOrdenProduccion);
END
GO

/* =====================================================================
   5) Catálogo de módulos (sin volcar menú a quien no corresponde)
   ===================================================================== */
IF OBJECT_ID(N'tempdb..#ModSeed') IS NOT NULL DROP TABLE #ModSeed;
CREATE TABLE #ModSeed (
    Codigo NVARCHAR(80) NOT NULL PRIMARY KEY,
    Nombre NVARCHAR(200) NOT NULL,
    Descripcion NVARCHAR(400) NULL,
    Scope NVARCHAR(20) NOT NULL
);

INSERT INTO #ModSeed (Codigo, Nombre, Descripcion, Scope) VALUES
(N'RRHH_ASISTENCIA', N'Asistencia', N'Cálculo de asistencia, tardanzas y extras', N'CATALOG'),
(N'RRHH_BENEFICIOS', N'Beneficios', N'Catálogo de beneficios (gasolina, seguro, etc.)', N'CATALOG'),
(N'RRHH_CARGOS', N'Cargos', N'Cargos con salario, jornada y beneficios', N'CATALOG'),
(N'RRHH_CORRECCION', N'RRHH — Corregir ponchadas', N'Aprobar correcciones de marcación', N'CATALOG'),
(N'RRHH_DEPARTAMENTOS', N'Departamentos', N'Mantenimiento de departamentos RRHH', N'CATALOG'),
(N'RRHH_NOMINA', N'Nómina', N'Pre-nómina, revisión, aprobación y pago', N'CATALOG'),
(N'RRHH_NOMINA_APROBAR', N'RRHH — Aprobar nómina', N'Aprobar y cerrar nómina', N'CATALOG'),
(N'RRHH_NOMINA_PAGAR', N'RRHH — Pagar nómina', N'Marcar nómina como pagada', N'CATALOG'),
(N'RRHH_PERMISOS', N'Permisos y licencias', N'Solicitud y aprobación de permisos, vacaciones y licencias', N'CATALOG'),
(N'RRHH_PERMISOS_APROBAR', N'RRHH — Aprobar permisos', N'Aprobar o rechazar permisos y licencias', N'CATALOG'),
(N'RRHH_PONCHADOR', N'Ponchador', N'Registro de entrada y salida', N'CATALOG'),
(N'MANUFACTURA_RECETAS', N'Recetas de producción', N'Fórmulas para transformar materias primas en producto terminado.', N'CATALOG'),
(N'MANUFACTURA_ORDENES', N'Órdenes de producción', N'Planificar, validar faltantes y ejecutar producción contra inventario.', N'CATALOG');

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

DECLARE @IdMod INT, @Scope NVARCHAR(20);
DECLARE seed_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT m.Id, s.Scope
    FROM #ModSeed s
    INNER JOIN dbo.Modulos m ON m.Codigo = s.Codigo;

OPEN seed_cur;
FETCH NEXT FROM seed_cur INTO @IdMod, @Scope;
WHILE @@FETCH_STATUS = 0
BEGIN
    -- Scope CATALOG: solo existe el módulo; MacroBits lo licencia por empresa.

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

    FETCH NEXT FROM seed_cur INTO @IdMod, @Scope;
END
CLOSE seed_cur;
DEALLOCATE seed_cur;
GO

/* Catálogo base RRHH solo en empresas con el módulo activo */
INSERT INTO dbo.RrhhTipoAusencia
    (IdEmpresa, Codigo, Nombre, Categoria, UnidadDefault, ConGoceSueldo, RequiereAprobacion, AfectaAsistencia, Activo)
SELECT e.IdEmpresa, t.Codigo, t.Nombre, t.Categoria, t.Unidad, t.Goce, 1, 1, 1
FROM dbo.Empresas e
INNER JOIN dbo.Empresa_Modulos em ON em.EmpresaId = e.IdEmpresa AND em.Activo = 1
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId AND m.Codigo = N'RRHH_PERMISOS'
CROSS JOIN (VALUES
    (N'PERMISO_HORAS', N'Permiso por horas', N'PERMISO', N'HORAS', 1),
    (N'PERMISO_DIAS_GOCE', N'Permiso con disfrute de sueldo', N'PERMISO', N'DIAS', 1),
    (N'PERMISO_DIAS_SIN_GOCE', N'Permiso sin disfrute de sueldo', N'PERMISO', N'DIAS', 0),
    (N'VACACIONES', N'Vacaciones', N'VACACIONES', N'DIAS', 1),
    (N'LICENCIA', N'Licencia', N'LICENCIA', N'DIAS', 1),
    (N'AUSENCIA_JUSTIFICADA', N'Ausencia justificada', N'AUSENCIA', N'DIAS', 1),
    (N'AUSENCIA_NO_JUSTIFICADA', N'Ausencia no justificada', N'AUSENCIA', N'DIAS', 0)
) t(Codigo, Nombre, Categoria, Unidad, Goce)
WHERE ISNULL(e.EsEmpresaSistema, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RrhhTipoAusencia x
      WHERE x.IdEmpresa = e.IdEmpresa AND x.Codigo = t.Codigo
  );
GO

INSERT INTO dbo.RrhhBeneficio (IdEmpresa, Codigo, Nombre, Descripcion, TipoCalculo, Monto, Periodicidad, AfectaNomina, EnEspecie, Activo)
SELECT e.IdEmpresa, t.Codigo, t.Nombre, t.Descripcion, t.Tipo, 0, N'MENSUAL', t.Nomina, t.Especie, 1
FROM dbo.Empresas e
INNER JOIN dbo.Empresa_Modulos em ON em.EmpresaId = e.IdEmpresa AND em.Activo = 1
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId AND m.Codigo = N'RRHH_BENEFICIOS'
CROSS JOIN (VALUES
    (N'GASOLINA', N'Gasolina / combustible', N'Ayuda de combustible', N'MONTO_FIJO', 1, 0),
    (N'SEGURO_MEDICO', N'Seguro médico privado', N'Complemento de seguro médico', N'MONTO_FIJO', 1, 0),
    (N'TELEFONO', N'Teléfono / data', N'Ayuda de telefonía', N'MONTO_FIJO', 1, 0),
    (N'ALIMENTACION', N'Alimentación', N'Ayuda de alimentación', N'MONTO_FIJO', 1, 1)
) t(Codigo, Nombre, Descripcion, Tipo, Nomina, Especie)
WHERE ISNULL(e.EsEmpresaSistema, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RrhhBeneficio b
      WHERE b.IdEmpresa = e.IdEmpresa AND b.Codigo = t.Codigo
  );
GO

PRINT '=== FIN migracion schema Dev->Prod 2026-08-21 ===';
SELECT m.Codigo, m.Nombre,
       (SELECT COUNT(*) FROM dbo.Empresa_Modulos em WHERE em.ModuloId = m.Id AND em.Activo = 1) AS Empresas
FROM dbo.Modulos m
WHERE m.Codigo IN (SELECT Codigo FROM #ModSeed)
ORDER BY m.Codigo;
GO
