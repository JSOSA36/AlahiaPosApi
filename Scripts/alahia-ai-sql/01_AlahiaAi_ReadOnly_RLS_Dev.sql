/*
  ALAHIA AI — Fase 1 (SOLO AlahiaPos_Dev)
  - Login/usuario SQL solo lectura
  - Schema ai + vistas filtradas por SESSION_CONTEXT('IdEmpresa')
  - Deny sobre dbo para el usuario AI
  - Prueba cross-tenant al final del script (manual)

  Fail-closed: sin SESSION_CONTEXT → 0 filas.
*/
USE AlahiaPos_Dev;
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ========== 1) Login + User ========== */
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'alahia_ai_ro')
BEGIN
    CREATE LOGIN alahia_ai_ro WITH PASSWORD = N'AlahiaAi_Ro_Dev_2026!', CHECK_POLICY = ON;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'alahia_ai_ro')
BEGIN
    CREATE USER alahia_ai_ro FOR LOGIN alahia_ai_ro;
END
GO

/* ========== 2) Schema ai ========== */
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'ai')
    EXEC(N'CREATE SCHEMA ai AUTHORIZATION dbo;');
GO

/* ========== 3) Helper: IdEmpresa de sesión ========== */
CREATE OR ALTER FUNCTION ai.fn_IdEmpresaSesion()
RETURNS INT
AS
BEGIN
    DECLARE @v SQL_VARIANT = SESSION_CONTEXT(N'IdEmpresa');
    IF @v IS NULL RETURN NULL;
    RETURN TRY_CONVERT(INT, @v);
END;
GO

/* ========== 4) Vistas tenant (fail-closed) ========== */
CREATE OR ALTER VIEW ai.v_FacturaHeaders
AS
SELECT h.*
FROM dbo.FacturaHeaders AS h
WHERE h.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_FacturaDetalles
AS
SELECT d.*
FROM dbo.FacturaDetalles AS d
INNER JOIN dbo.FacturaHeaders AS h ON h.IdFacturaHeader = d.IdFacturaHeader
WHERE h.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_Clientes
AS
SELECT c.*
FROM dbo.Clientes AS c
WHERE c.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_Productos
AS
SELECT p.*
FROM dbo.Productos AS p
WHERE p.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_Proveedores
AS
SELECT p.*
FROM dbo.Proveedores AS p
WHERE p.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_Almacenes
AS
SELECT a.*
FROM dbo.Almacenes AS a
WHERE a.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_AlmacenExistencias
AS
SELECT e.*
FROM dbo.AlmacenExistencias AS e
WHERE e.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_MovimientosInventario
AS
SELECT m.*
FROM dbo.MovimientosInventario AS m
WHERE m.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_OrdenCompraHeaders
AS
SELECT o.*
FROM dbo.OrdenCompraHeaders AS o
WHERE o.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_OrdenCompraDetalles
AS
SELECT d.*
FROM dbo.OrdenCompraDetalles AS d
INNER JOIN dbo.OrdenCompraHeaders AS h ON h.IdOrdenCompraHeader = d.IdOrdenCompraHeader
WHERE h.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_PagosFacturasClientes
AS
SELECT p.*
FROM dbo.PagosFacturasClientes AS p
WHERE p.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_PagosProveedor
AS
SELECT p.*
FROM dbo.PagosProveedor AS p
WHERE p.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_Ingresos
AS
SELECT i.*
FROM dbo.Ingresos AS i
WHERE i.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_Gastos
AS
SELECT g.*
FROM dbo.Gastos AS g
WHERE g.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_MovimientoFinanciero
AS
SELECT m.*
FROM dbo.MovimientoFinanciero AS m
WHERE m.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_CuentaFinanciera
AS
SELECT c.*
FROM dbo.CuentaFinanciera AS c
WHERE c.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_NotaCreditoes
AS
SELECT n.*
FROM dbo.NotaCreditoes AS n
WHERE n.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_CajaMovimiento
AS
SELECT c.*
FROM dbo.CajaMovimiento AS c
WHERE c.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

CREATE OR ALTER VIEW ai.v_AsientosContables
AS
SELECT a.*
FROM dbo.AsientosContables AS a
WHERE a.IdEmpresa = ai.fn_IdEmpresaSesion()
  AND ai.fn_IdEmpresaSesion() IS NOT NULL;
GO

/* Catálogo semántico para el LLM (sin datos de negocio) */
CREATE OR ALTER VIEW ai.v_Catalogo
AS
SELECT
    v.name AS Vista,
    CAST(ep.value AS nvarchar(400)) AS Descripcion
FROM sys.views v
LEFT JOIN sys.extended_properties ep
    ON ep.major_id = v.object_id AND ep.minor_id = 0 AND ep.name = N'MS_Description'
WHERE SCHEMA_NAME(v.schema_id) = N'ai'
  AND v.name LIKE N'v_%'
  AND v.name <> N'v_Catalogo';
GO

/* ========== 5) Permisos ========== */
DENY SELECT, INSERT, UPDATE, DELETE, EXECUTE, ALTER, CONTROL ON SCHEMA::dbo TO alahia_ai_ro;
GO
GRANT SELECT ON SCHEMA::ai TO alahia_ai_ro;
GRANT EXECUTE ON OBJECT::ai.fn_IdEmpresaSesion TO alahia_ai_ro;
GO

/* Descripciones cortas */
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Facturas / ventas cabecera', @level0type=N'SCHEMA',@level0name=N'ai', @level1type=N'VIEW',@level1name=N'v_FacturaHeaders';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Detalle de líneas de factura', @level0type=N'SCHEMA',@level0name=N'ai', @level1type=N'VIEW',@level1name=N'v_FacturaDetalles';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Clientes de la empresa', @level0type=N'SCHEMA',@level0name=N'ai', @level1type=N'VIEW',@level1name=N'v_Clientes';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Productos y servicios', @level0type=N'SCHEMA',@level0name=N'ai', @level1type=N'VIEW',@level1name=N'v_Productos';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Proveedores', @level0type=N'SCHEMA',@level0name=N'ai', @level1type=N'VIEW',@level1name=N'v_Proveedores';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Existencias por almacén', @level0type=N'SCHEMA',@level0name=N'ai', @level1type=N'VIEW',@level1name=N'v_AlmacenExistencias';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Ingresos registrados', @level0type=N'SCHEMA',@level0name=N'ai', @level1type=N'VIEW',@level1name=N'v_Ingresos';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Gastos registrados', @level0type=N'SCHEMA',@level0name=N'ai', @level1type=N'VIEW',@level1name=N'v_Gastos';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Movimientos financieros / banco', @level0type=N'SCHEMA',@level0name=N'ai', @level1type=N'VIEW',@level1name=N'v_MovimientoFinanciero';
GO

PRINT 'Alahia AI SQL Fase 1 aplicada en AlahiaPos_Dev.';
GO
