-- ============================================================
-- RRHH: departamento, cargo y beneficio como fuente de verdad
-- El empleado solo se asigna a departamento + cargo.
-- Salario, jornada y beneficios viven en el cargo.
-- AlahiaPos_Dev
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.RrhhDepartamento', N'Descripcion') IS NULL
    ALTER TABLE dbo.RrhhDepartamento ADD Descripcion NVARCHAR(400) NULL;
IF COL_LENGTH(N'dbo.RrhhDepartamento', N'IdResponsable') IS NULL
    ALTER TABLE dbo.RrhhDepartamento ADD IdResponsable INT NULL;
IF COL_LENGTH(N'dbo.RrhhDepartamento', N'Ubicacion') IS NULL
    ALTER TABLE dbo.RrhhDepartamento ADD Ubicacion NVARCHAR(200) NULL;
IF COL_LENGTH(N'dbo.RrhhDepartamento', N'Telefono') IS NULL
    ALTER TABLE dbo.RrhhDepartamento ADD Telefono NVARCHAR(40) NULL;
IF COL_LENGTH(N'dbo.RrhhDepartamento', N'Email') IS NULL
    ALTER TABLE dbo.RrhhDepartamento ADD Email NVARCHAR(120) NULL;
GO

IF COL_LENGTH(N'dbo.RrhhCargo', N'Descripcion') IS NULL
    ALTER TABLE dbo.RrhhCargo ADD Descripcion NVARCHAR(400) NULL;
IF COL_LENGTH(N'dbo.RrhhCargo', N'IdJornada') IS NULL
    ALTER TABLE dbo.RrhhCargo ADD IdJornada INT NULL;
IF COL_LENGTH(N'dbo.RrhhCargo', N'SalarioBase') IS NULL
    ALTER TABLE dbo.RrhhCargo ADD SalarioBase DECIMAL(18,2) NOT NULL CONSTRAINT DF_RrhhCargo_Sal DEFAULT (0);
IF COL_LENGTH(N'dbo.RrhhCargo', N'Moneda') IS NULL
    ALTER TABLE dbo.RrhhCargo ADD Moneda NVARCHAR(8) NOT NULL CONSTRAINT DF_RrhhCargo_Mon DEFAULT (N'DOP');
IF COL_LENGTH(N'dbo.RrhhCargo', N'FrecuenciaPago') IS NULL
    ALTER TABLE dbo.RrhhCargo ADD FrecuenciaPago NVARCHAR(20) NOT NULL CONSTRAINT DF_RrhhCargo_Freq DEFAULT (N'QUINCENAL');
IF COL_LENGTH(N'dbo.RrhhCargo', N'TipoEmpleado') IS NULL
    ALTER TABLE dbo.RrhhCargo ADD TipoEmpleado NVARCHAR(40) NOT NULL CONSTRAINT DF_RrhhCargo_Tipo DEFAULT (N'FIJO');
GO

IF OBJECT_ID(N'dbo.RrhhBeneficio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhBeneficio (
        IdBeneficio    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa      INT NOT NULL,
        Codigo         NVARCHAR(40)  NOT NULL,
        Nombre         NVARCHAR(120) NOT NULL,
        Descripcion    NVARCHAR(400) NULL,
        TipoCalculo    NVARCHAR(30)  NOT NULL CONSTRAINT DF_RrhhBen_Tipo DEFAULT (N'MONTO_FIJO'),
        Monto          DECIMAL(18,4) NOT NULL CONSTRAINT DF_RrhhBen_Mon DEFAULT (0),
        Periodicidad   NVARCHAR(20)  NOT NULL CONSTRAINT DF_RrhhBen_Per DEFAULT (N'MENSUAL'),
        AfectaNomina   BIT NOT NULL CONSTRAINT DF_RrhhBen_Nom DEFAULT (1),
        EnEspecie      BIT NOT NULL CONSTRAINT DF_RrhhBen_Esp DEFAULT (0),
        Activo         BIT NOT NULL CONSTRAINT DF_RrhhBen_Act DEFAULT (1),
        FechaCreacion  DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhBen_Cre DEFAULT (SYSUTCDATETIME()),
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

-- Trasladar salario/jornada que estaban en el empleado hacia el cargo
UPDATE c
SET
    c.SalarioBase = CASE WHEN ISNULL(c.SalarioBase, 0) = 0 THEN x.SalarioBase ELSE c.SalarioBase END,
    c.FrecuenciaPago = CASE WHEN ISNULL(c.FrecuenciaPago, N'') = N'' THEN x.FrecuenciaPago ELSE c.FrecuenciaPago END,
    c.Moneda = CASE WHEN ISNULL(c.Moneda, N'') = N'' THEN x.Moneda ELSE c.Moneda END,
    c.IdJornada = CASE WHEN c.IdJornada IS NULL THEN x.IdJornada ELSE c.IdJornada END,
    c.TipoEmpleado = CASE WHEN ISNULL(c.TipoEmpleado, N'') IN (N'', N'FIJO') AND ISNULL(x.TipoEmpleado, N'') <> N'' THEN x.TipoEmpleado ELSE c.TipoEmpleado END
FROM dbo.RrhhCargo c
INNER JOIN (
    SELECT
        el.IdCargo,
        MAX(el.SalarioBase) AS SalarioBase,
        MAX(el.FrecuenciaPago) AS FrecuenciaPago,
        MAX(el.Moneda) AS Moneda,
        MAX(el.IdJornada) AS IdJornada,
        MAX(el.TipoEmpleado) AS TipoEmpleado
    FROM dbo.EmpleadoLaboral el
    WHERE el.IdCargo IS NOT NULL
    GROUP BY el.IdCargo
) x ON x.IdCargo = c.IdCargo;
GO

INSERT INTO dbo.RrhhBeneficio (IdEmpresa, Codigo, Nombre, Descripcion, TipoCalculo, Monto, Periodicidad, AfectaNomina, EnEspecie, Activo)
SELECT e.IdEmpresa, t.Codigo, t.Nombre, t.Descripcion, t.Tipo, 0, N'MENSUAL', t.Nomina, t.Especie, 1
FROM dbo.Empresas e
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

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_DEPARTAMENTOS')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_DEPARTAMENTOS', N'Departamentos', N'Mantenimiento de departamentos RRHH', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_CARGOS')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_CARGOS', N'Cargos', N'Cargos con salario, jornada y beneficios', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'RRHH_BENEFICIOS')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'RRHH_BENEFICIOS', N'Beneficios', N'Catálogo de beneficios (gasolina, seguro, etc.)', 0, 1, GETDATE());
GO

;WITH Mods AS (
    SELECT Id FROM dbo.Modulos
    WHERE Codigo IN (N'RRHH_DEPARTAMENTOS', N'RRHH_CARGOS', N'RRHH_BENEFICIOS')
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
WHERE m.Codigo IN (N'RRHH_DEPARTAMENTOS', N'RRHH_CARGOS', N'RRHH_BENEFICIOS');
GO

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, m.Id, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Modulos m ON m.Codigo IN (N'RRHH_DEPARTAMENTOS', N'RRHH_CARGOS', N'RRHH_BENEFICIOS')
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
WHERE m.Codigo IN (N'RRHH_DEPARTAMENTOS', N'RRHH_CARGOS', N'RRHH_BENEFICIOS');
GO

PRINT 'RRHH cargo como fuente de verdad listo en AlahiaPos_Dev.';
GO
