-- AlahiaPos_Dev — ficha personal del colaborador (satélite de EmpleadosP).
-- No duplica cédula: esa vive en EmpleadosP. No toca EmpleadoLaboral (puesto/nómina).

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.EmpleadoFichaPersonal', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.EmpleadoFichaPersonal (
    IdFichaPersonal INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdEmpresa INT NOT NULL,
    IdEmpleados INT NOT NULL,
    FechaNacimiento DATE NULL,
    Sexo NVARCHAR(8) NULL,
    Nacionalidad NVARCHAR(80) NULL,
    EstadoCivil NVARCHAR(40) NULL,
    Profesion NVARCHAR(120) NULL,
    Nss NVARCHAR(30) NULL,
    FechaSalida DATE NULL,
    Alergias NVARCHAR(500) NULL,
    TipoSangre NVARCHAR(8) NULL,
    ObservacionesMedicas NVARCHAR(500) NULL,
    FechaActualizacion DATETIME NOT NULL CONSTRAINT DF_EmpFicha_Upd DEFAULT (GETUTCDATE()),
    IdUsuario INT NULL,
    CONSTRAINT UQ_EmpleadoFichaPersonal UNIQUE (IdEmpresa, IdEmpleados)
  );
  CREATE INDEX IX_EmpFicha_Emp ON dbo.EmpleadoFichaPersonal (IdEmpresa, IdEmpleados);
END
GO

IF OBJECT_ID(N'dbo.EmpleadoFamiliar', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.EmpleadoFamiliar (
    IdFamiliar INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdEmpresa INT NOT NULL,
    IdEmpleados INT NOT NULL,
    Tipo NVARCHAR(20) NOT NULL,
    Nombre NVARCHAR(150) NOT NULL,
    Cedula NVARCHAR(20) NULL,
    FechaNacimiento DATE NULL,
    Sexo NVARCHAR(8) NULL,
    Parentesco NVARCHAR(40) NULL,
    Telefono NVARCHAR(30) NULL,
    Celular NVARCHAR(30) NULL,
    Direccion NVARCHAR(250) NULL,
    Ocupacion NVARCHAR(120) NULL,
    ViveConEmpleado BIT NOT NULL CONSTRAINT DF_EmpFam_Vive DEFAULT (0),
    EsDependiente BIT NOT NULL CONSTRAINT DF_EmpFam_Dep DEFAULT (0),
    Nota NVARCHAR(250) NULL,
    Orden INT NOT NULL CONSTRAINT DF_EmpFam_Orden DEFAULT (0)
  );
  CREATE INDEX IX_EmpFam_Emp ON dbo.EmpleadoFamiliar (IdEmpresa, IdEmpleados, Tipo);
END
GO
