/*
  Migrate_DevSchema_To_Prod_20260826.sql
  ------------------------------------------------------------
  Lleva a AlahiaPos_Prod el DDL de AlahiaPos_Dev que aún no está.
  Solo cambios aditivos / idempotentes.

  NO copia datos de negocio Dev -> Prod.
  NO elimina columnas ni tablas.
  NO activa IR-3 / IR-17 a todos los clientes (quedan en catálogo;
    el pack fiscal 606/607 ya los muestra en menú).

  Incluye:
  - Gastos.IdTipoBienesServicios (606 en gastos menores)
  - EmpleadoFichaPersonal / EmpleadoFamiliar (RRHH)
  - Catálogo de módulos IR3 e IR17
  - Refresh de ai.v_Gastos por la columna nueva

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

PRINT '=== INICIO migracion schema Dev->Prod 2026-08-26 ===';
GO

/* =====================================================================
   1) Gastos — tipo de bienes y servicios DGII (606)
   ===================================================================== */
IF COL_LENGTH('dbo.Gastos', 'IdTipoBienesServicios') IS NULL
    ALTER TABLE dbo.Gastos ADD IdTipoBienesServicios INT NULL;
GO

IF OBJECT_ID(N'ai.v_Gastos', N'V') IS NOT NULL
    EXEC sys.sp_refreshview N'ai.v_Gastos';
GO

/* =====================================================================
   2) RRHH — ficha personal y familiares
   ===================================================================== */
IF OBJECT_ID(N'dbo.EmpleadoFichaPersonal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmpleadoFichaPersonal (
        IdFichaPersonal         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa               INT NOT NULL,
        IdEmpleados             INT NOT NULL,
        FechaNacimiento         DATE NULL,
        Sexo                    NVARCHAR(8) NULL,
        Nacionalidad            NVARCHAR(80) NULL,
        EstadoCivil             NVARCHAR(40) NULL,
        Profesion               NVARCHAR(120) NULL,
        Nss                     NVARCHAR(30) NULL,
        FechaSalida             DATE NULL,
        Alergias                NVARCHAR(500) NULL,
        TipoSangre              NVARCHAR(8) NULL,
        ObservacionesMedicas    NVARCHAR(500) NULL,
        FechaActualizacion      DATETIME NOT NULL CONSTRAINT DF_EmpFicha_Upd DEFAULT (GETUTCDATE()),
        IdUsuario               INT NULL,
        CONSTRAINT UQ_EmpleadoFichaPersonal UNIQUE (IdEmpresa, IdEmpleados)
    );
    CREATE INDEX IX_EmpFicha_Emp ON dbo.EmpleadoFichaPersonal (IdEmpresa, IdEmpleados);
END
GO

IF OBJECT_ID(N'dbo.EmpleadoFamiliar', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmpleadoFamiliar (
        IdFamiliar          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa           INT NOT NULL,
        IdEmpleados         INT NOT NULL,
        Tipo                NVARCHAR(20) NOT NULL,
        Nombre              NVARCHAR(150) NOT NULL,
        Cedula              NVARCHAR(20) NULL,
        FechaNacimiento     DATE NULL,
        Sexo                NVARCHAR(8) NULL,
        Parentesco          NVARCHAR(40) NULL,
        Telefono            NVARCHAR(30) NULL,
        Celular             NVARCHAR(30) NULL,
        Direccion           NVARCHAR(250) NULL,
        Ocupacion           NVARCHAR(120) NULL,
        ViveConEmpleado     BIT NOT NULL CONSTRAINT DF_EmpFam_Vive DEFAULT (0),
        EsDependiente       BIT NOT NULL CONSTRAINT DF_EmpFam_Dep DEFAULT (0),
        Nota                NVARCHAR(250) NULL,
        Orden               INT NOT NULL CONSTRAINT DF_EmpFam_Orden DEFAULT (0)
    );
    CREATE INDEX IX_EmpFam_Emp ON dbo.EmpleadoFamiliar (IdEmpresa, IdEmpleados, Tipo);
END
GO

/* =====================================================================
   3) Catálogo IR-3 / IR-17 (sin volcar a todos los clientes)
   ===================================================================== */
IF OBJECT_ID(N'tempdb..#ModSeed') IS NOT NULL DROP TABLE #ModSeed;
CREATE TABLE #ModSeed (
    Codigo NVARCHAR(80) NOT NULL PRIMARY KEY,
    Nombre NVARCHAR(200) NOT NULL,
    Descripcion NVARCHAR(400) NULL
);

INSERT INTO #ModSeed (Codigo, Nombre, Descripcion) VALUES
(N'IR17', N'Declaración IR-17', N'Liquidación mensual IR-17 2026'),
(N'IR3',  N'Declaración IR-3',  N'Retenciones ISR de asalariados (nómina)');

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
    SELECT m.Id
    FROM #ModSeed s
    INNER JOIN dbo.Modulos m ON m.Codigo = s.Codigo;

OPEN seed_cur;
FETCH NEXT FROM seed_cur INTO @IdMod;
WHILE @@FETCH_STATUS = 0
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

    FETCH NEXT FROM seed_cur INTO @IdMod;
END
CLOSE seed_cur;
DEALLOCATE seed_cur;
GO

PRINT '=== FIN migracion schema Dev->Prod 2026-08-26 ===';
SELECT
    CASE WHEN COL_LENGTH('dbo.Gastos', 'IdTipoBienesServicios') IS NOT NULL THEN 'OK' ELSE 'FALTA' END AS Gastos_Tipo606,
    CASE WHEN OBJECT_ID(N'dbo.EmpleadoFichaPersonal', N'U') IS NOT NULL THEN 'OK' ELSE 'FALTA' END AS FichaPersonal,
    CASE WHEN OBJECT_ID(N'dbo.EmpleadoFamiliar', N'U') IS NOT NULL THEN 'OK' ELSE 'FALTA' END AS Familiar;

SELECT m.Codigo, m.Nombre,
       (SELECT COUNT(*) FROM dbo.Empresa_Modulos em WHERE em.ModuloId = m.Id AND em.Activo = 1) AS Empresas
FROM dbo.Modulos m
WHERE m.Codigo IN (N'IR3', N'IR17')
ORDER BY m.Codigo;
GO
