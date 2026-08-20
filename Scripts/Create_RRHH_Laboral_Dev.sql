-- AlahiaPos_Dev — expediente laboral / nómina (corte 1)
-- Anclado a EmpleadosP. Sin lógica AFP/ISR aquí.

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

-- Tablas Payroll_* (Infrastructure) — mismo Dev DB
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

-- Módulo menú RRHH (si existe tabla Modulos)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Modulos')
BEGIN
  IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_LABORAL')
  BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_LABORAL', N'Expediente Laboral', N'Datos laborales, salario y asignaciones de nómina', 0, 1, GETUTCDATE());
  END
END
GO
