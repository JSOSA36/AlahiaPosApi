-- ============================================================
-- RRHH: ponchador, horarios, permisos y proceso de nómina
-- AlahiaPos_Dev — anclado a EmpleadosP + EmpleadoLaboral.
-- No duplica personas. Payroll_Runs sigue siendo el motor.
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

-- Expediente: FKs de catálogo (nombres se sincronizan, no se duplican como fuente)
IF COL_LENGTH(N'dbo.EmpleadoLaboral', N'IdDepartamento') IS NULL
    ALTER TABLE dbo.EmpleadoLaboral ADD IdDepartamento INT NULL;
IF COL_LENGTH(N'dbo.EmpleadoLaboral', N'IdCargo') IS NULL
    ALTER TABLE dbo.EmpleadoLaboral ADD IdCargo INT NULL;
IF COL_LENGTH(N'dbo.EmpleadoLaboral', N'IdJornada') IS NULL
    ALTER TABLE dbo.EmpleadoLaboral ADD IdJornada INT NULL;
GO

IF OBJECT_ID(N'dbo.RrhhDepartamento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhDepartamento (
        IdDepartamento   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa        INT NOT NULL,
        Codigo           NVARCHAR(40)  NOT NULL,
        Nombre           NVARCHAR(120) NOT NULL,
        Activo           BIT NOT NULL CONSTRAINT DF_RrhhDepto_Act DEFAULT (1),
        FechaCreacion    DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhDepto_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_RrhhDepto UNIQUE (IdEmpresa, Codigo)
    );
    CREATE INDEX IX_RrhhDepto_Emp ON dbo.RrhhDepartamento (IdEmpresa, Activo);
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
        Activo           BIT NOT NULL CONSTRAINT DF_RrhhCargo_Act DEFAULT (1),
        FechaCreacion    DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhCargo_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_RrhhCargo UNIQUE (IdEmpresa, Codigo)
    );
    CREATE INDEX IX_RrhhCargo_Emp ON dbo.RrhhCargo (IdEmpresa, Activo);
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
        DiaSemana        TINYINT NOT NULL, -- 1=Lunes … 7=Domingo
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

-- Ponchada original: inmutable. Correcciones en tabla aparte.
IF OBJECT_ID(N'dbo.RrhhPonchada', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhPonchada (
        IdPonchada         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa          INT NOT NULL,
        IdEmpleados        INT NOT NULL,
        FechaHora          DATETIME2(0) NOT NULL,
        Tipo               NVARCHAR(20) NOT NULL, -- ENTRADA | SALIDA | SALIDA_RECESO | RETORNO_RECESO
        Origen             NVARCHAR(20) NOT NULL CONSTRAINT DF_RrhhPonch_Ori DEFAULT (N'WEB'),
        Dispositivo        NVARCHAR(120) NULL,
        Ip                 NVARCHAR(60) NULL,
        IdUsuarioRegistra  INT NOT NULL CONSTRAINT DF_RrhhPonch_Usr DEFAULT (0),
        FechaRegistro      DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhPonch_Reg DEFAULT (SYSUTCDATETIME()),
        Nota               NVARCHAR(250) NULL
    );
    CREATE INDEX IX_RrhhPonch_EmpDia ON dbo.RrhhPonchada (IdEmpresa, IdEmpleados, FechaHora);
END
GO

IF OBJECT_ID(N'dbo.RrhhPonchadaCorreccion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhPonchadaCorreccion (
        IdCorreccion       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa          INT NOT NULL,
        IdEmpleados        INT NOT NULL,
        IdPonchada         INT NULL, -- NULL = inserción de marcación omitida
        TipoCorreccion     NVARCHAR(30) NOT NULL, -- CAMBIO_HORA | CAMBIO_TIPO | ANULACION | INSERCION_OMITIDA
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
        Categoria            NVARCHAR(20)  NOT NULL, -- PERMISO | VACACIONES | LICENCIA | AUSENCIA
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
        Unidad               NVARCHAR(10) NOT NULL, -- HORAS | DIAS
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
        Estado                    NVARCHAR(30) NOT NULL, -- PRESENTE | AUSENTE | PERMISO | VACACIONES | LICENCIA | DESCANSO | INCOMPLETO
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

-- Flujo ERP alrededor del PayrollRun (Draft/Approved del motor no se altera)
IF OBJECT_ID(N'dbo.NominaProceso', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NominaProceso (
        IdNominaProceso     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa           INT NOT NULL,
        PeriodKey           NVARCHAR(40) NOT NULL,
        Intent              NVARCHAR(40) NOT NULL CONSTRAINT DF_NomProc_Int DEFAULT (N'REGULAR'),
        FechaInicio         DATE NOT NULL,
        FechaFin            DATE NOT NULL,
        Frecuencia          NVARCHAR(20) NOT NULL CONSTRAINT DF_NomProc_Freq DEFAULT (N'QUINCENAL'),
        Estado              NVARCHAR(20) NOT NULL CONSTRAINT DF_NomProc_Est DEFAULT (N'BORRADOR'),
        PayrollRunId        UNIQUEIDENTIFIER NULL,
        Observacion         NVARCHAR(400) NULL,
        IdUsuarioCrea       INT NOT NULL CONSTRAINT DF_NomProc_Usr DEFAULT (0),
        FechaCreacion       DATETIME2(0) NOT NULL CONSTRAINT DF_NomProc_Cre DEFAULT (SYSUTCDATETIME()),
        IdUsuarioRevision   INT NULL,
        FechaRevision       DATETIME2(0) NULL,
        IdUsuarioAprueba    INT NULL,
        FechaAprobacion     DATETIME2(0) NULL,
        IdUsuarioPaga       INT NULL,
        FechaPago           DATETIME2(0) NULL,
        IdUsuarioCierra     INT NULL,
        FechaCierre         DATETIME2(0) NULL,
        CONSTRAINT UQ_NominaProceso UNIQUE (IdEmpresa, PeriodKey, Intent)
    );
    CREATE INDEX IX_NomProc_Emp ON dbo.NominaProceso (IdEmpresa, Estado);
END
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

-- Tipos de ausencia base (por empresa no sistema)
INSERT INTO dbo.RrhhTipoAusencia
    (IdEmpresa, Codigo, Nombre, Categoria, UnidadDefault, ConGoceSueldo, RequiereAprobacion, AfectaAsistencia, Activo)
SELECT e.IdEmpresa, t.Codigo, t.Nombre, t.Categoria, t.Unidad, t.Goce, 1, 1, 1
FROM dbo.Empresas e
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

-- Módulos menú + permisos hijos (mismo patrón que Centro de Producción)
IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_LABORAL')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_LABORAL', N'Expediente Laboral', N'Datos laborales, salario y catálogos RRHH', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_PONCHADOR')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_PONCHADOR', N'Ponchador', N'Registro de entrada y salida', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_ASISTENCIA')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_ASISTENCIA', N'Asistencia', N'Cálculo de asistencia, tardanzas y extras', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_PERMISOS')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_PERMISOS', N'Permisos y licencias', N'Solicitud y aprobación de permisos, vacaciones y licencias', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_NOMINA')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_NOMINA', N'Nómina', N'Pre-nómina, revisión, aprobación y pago', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_CORRECCION')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_CORRECCION', N'RRHH — Corregir ponchadas', N'Aprobar correcciones de marcación', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_PERMISOS_APROBAR')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_PERMISOS_APROBAR', N'RRHH — Aprobar permisos', N'Aprobar o rechazar permisos y licencias', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_NOMINA_APROBAR')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_NOMINA_APROBAR', N'RRHH — Aprobar nómina', N'Aprobar y cerrar nómina', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_NOMINA_PAGAR')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_NOMINA_PAGAR', N'RRHH — Pagar nómina', N'Marcar nómina como pagada', 0, 1, GETDATE());
GO

;WITH Mods AS (
    SELECT Id FROM dbo.Modulos
    WHERE Codigo IN (
        N'RRHH_LABORAL', N'RRHH_PONCHADOR', N'RRHH_ASISTENCIA', N'RRHH_PERMISOS', N'RRHH_NOMINA',
        N'RRHH_CORRECCION', N'RRHH_PERMISOS_APROBAR', N'RRHH_NOMINA_APROBAR', N'RRHH_NOMINA_PAGAR'
    )
)
INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, m.Id, 1, GETDATE()
FROM dbo.Empresas e
CROSS JOIN Mods m
WHERE ISNULL(e.EsEmpresaSistema, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = m.Id
  );

UPDATE em
SET em.Activo = 1, em.FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId
WHERE m.Codigo IN (
    N'RRHH_LABORAL', N'RRHH_PONCHADOR', N'RRHH_ASISTENCIA', N'RRHH_PERMISOS', N'RRHH_NOMINA',
    N'RRHH_CORRECCION', N'RRHH_PERMISOS_APROBAR', N'RRHH_NOMINA_APROBAR', N'RRHH_NOMINA_PAGAR'
);
GO

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, m.Id, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Modulos m ON m.Codigo IN (
    N'RRHH_LABORAL', N'RRHH_PONCHADOR', N'RRHH_ASISTENCIA', N'RRHH_PERMISOS', N'RRHH_NOMINA',
    N'RRHH_CORRECCION', N'RRHH_PERMISOS_APROBAR', N'RRHH_NOMINA_APROBAR', N'RRHH_NOMINA_PAGAR'
)
WHERE EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = p.IdEmpresa AND em.ModuloId = m.Id AND em.Activo = 1
)
AND NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles pr
    WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = m.Id AND pr.IdEmpresa = p.IdEmpresa
);

UPDATE pr
SET pr.Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Modulos m ON m.Id = pr.IdModulo
WHERE m.Codigo IN (
    N'RRHH_LABORAL', N'RRHH_PONCHADOR', N'RRHH_ASISTENCIA', N'RRHH_PERMISOS', N'RRHH_NOMINA',
    N'RRHH_CORRECCION', N'RRHH_PERMISOS_APROBAR', N'RRHH_NOMINA_APROBAR', N'RRHH_NOMINA_PAGAR'
);
GO

SELECT m.Codigo, m.Nombre,
    (SELECT COUNT(*) FROM dbo.Empresa_Modulos WHERE ModuloId = m.Id AND Activo = 1) AS Empresas,
    (SELECT COUNT(*) FROM dbo.PerfilRoles WHERE IdModulo = m.Id AND Activo = 1) AS Perfiles
FROM dbo.Modulos m
WHERE m.Codigo LIKE N'RRHH_%'
ORDER BY m.Codigo;

PRINT 'RRHH ponchador/asistencia/nómina listo en AlahiaPos_Dev.';
GO
