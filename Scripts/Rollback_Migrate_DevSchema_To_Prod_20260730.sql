/*
  Rollback_Migrate_DevSchema_To_Prod_20260730.sql
  ------------------------------------------------------------
  Revierte SOLO lo añadido por Migrate_DevSchema_To_Prod_20260730.sql
  en AlahiaPos_Prod.

  - No toca PerfilRoles / Usuarios / login
  - Elimina tablas nuevas y columnas/constraints añadidos
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

PRINT '=== INICIO rollback schema Prod (migración 2026-07-30) ===';
GO

/* ---- Módulo catálogo insertado ---- */
IF EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'CONCILIACION_BANCARIA')
BEGIN
  DECLARE @IdMod INT = (SELECT TOP 1 Id FROM dbo.Modulos WHERE Codigo = N'CONCILIACION_BANCARIA');
  DELETE FROM dbo.Empresa_Modulos WHERE ModuloId = @IdMod;
  DELETE FROM dbo.Modulos WHERE Id = @IdMod;
  PRINT 'Modulo CONCILIACION_BANCARIA eliminado.';
END
GO

/* ---- Cotizador (hijos primero) ---- */
IF OBJECT_ID(N'dbo.CotizacionLead', N'U') IS NOT NULL DROP TABLE dbo.CotizacionLead;
IF OBJECT_ID(N'dbo.CotizacionDetalle', N'U') IS NOT NULL DROP TABLE dbo.CotizacionDetalle;
IF OBJECT_ID(N'dbo.Cotizacion', N'U') IS NOT NULL DROP TABLE dbo.Cotizacion;
IF OBJECT_ID(N'dbo.CotizadorTramoDocumento', N'U') IS NOT NULL DROP TABLE dbo.CotizadorTramoDocumento;
IF OBJECT_ID(N'dbo.CotizadorParametro', N'U') IS NOT NULL DROP TABLE dbo.CotizadorParametro;
IF OBJECT_ID(N'dbo.ModuloTipoNegocio', N'U') IS NOT NULL DROP TABLE dbo.ModuloTipoNegocio;
IF OBJECT_ID(N'dbo.ModuloDependencia', N'U') IS NOT NULL DROP TABLE dbo.ModuloDependencia;
IF OBJECT_ID(N'dbo.ModuloComercial', N'U') IS NOT NULL DROP TABLE dbo.ModuloComercial;
IF OBJECT_ID(N'dbo.TipoNegocio', N'U') IS NOT NULL DROP TABLE dbo.TipoNegocio;
GO

/* ---- Tesorería extracto / auditoría / reclasificación ---- */
IF OBJECT_ID(N'dbo.PagoReclasificacion', N'U') IS NOT NULL DROP TABLE dbo.PagoReclasificacion;
IF OBJECT_ID(N'dbo.TesoreriaConciliacionAuditoria', N'U') IS NOT NULL DROP TABLE dbo.TesoreriaConciliacionAuditoria;
IF OBJECT_ID(N'dbo.TesoreriaExtractoLinea', N'U') IS NOT NULL DROP TABLE dbo.TesoreriaExtractoLinea;
IF OBJECT_ID(N'dbo.TesoreriaExtractoImport', N'U') IS NOT NULL DROP TABLE dbo.TesoreriaExtractoImport;
GO

/* ---- Gastos / CategoriasGasto ---- */
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Gastos_CategoriasGasto')
  ALTER TABLE dbo.Gastos DROP CONSTRAINT FK_Gastos_CategoriasGasto;
GO

IF COL_LENGTH('dbo.Gastos', 'IdCategoriaGasto') IS NOT NULL
  ALTER TABLE dbo.Gastos DROP COLUMN IdCategoriaGasto;
IF COL_LENGTH('dbo.Gastos', 'TipoComprobante') IS NOT NULL
  ALTER TABLE dbo.Gastos DROP COLUMN TipoComprobante;
IF COL_LENGTH('dbo.Gastos', 'NumeroComprobante') IS NOT NULL
  ALTER TABLE dbo.Gastos DROP COLUMN NumeroComprobante;
IF COL_LENGTH('dbo.Gastos', 'FechaComprobante') IS NOT NULL
  ALTER TABLE dbo.Gastos DROP COLUMN FechaComprobante;
IF COL_LENGTH('dbo.Gastos', 'RncEmisorComprobante') IS NOT NULL
  ALTER TABLE dbo.Gastos DROP COLUMN RncEmisorComprobante;
IF COL_LENGTH('dbo.Gastos', 'NombreEmisorComprobante') IS NOT NULL
  ALTER TABLE dbo.Gastos DROP COLUMN NombreEmisorComprobante;
GO

IF OBJECT_ID(N'dbo.CategoriasGasto', N'U') IS NOT NULL DROP TABLE dbo.CategoriasGasto;
GO

/* ---- Ingresos / Pagos índices + columnas ---- */
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ingresos_IdMovimientoFinanciero' AND object_id = OBJECT_ID('dbo.Ingresos'))
  DROP INDEX IX_Ingresos_IdMovimientoFinanciero ON dbo.Ingresos;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PagosFacturasClientes_IdMovimientoFinanciero' AND object_id = OBJECT_ID('dbo.PagosFacturasClientes'))
  DROP INDEX IX_PagosFacturasClientes_IdMovimientoFinanciero ON dbo.PagosFacturasClientes;
GO

IF COL_LENGTH('dbo.Ingresos', 'IdMovimientoFinanciero') IS NOT NULL
  ALTER TABLE dbo.Ingresos DROP COLUMN IdMovimientoFinanciero;
IF COL_LENGTH('dbo.PagosFacturasClientes', 'IdMovimientoFinanciero') IS NOT NULL
  ALTER TABLE dbo.PagosFacturasClientes DROP COLUMN IdMovimientoFinanciero;
GO

/* ---- ECFEncabezado ---- */
IF COL_LENGTH('dbo.ECFEncabezado', 'TransmissionJobId') IS NOT NULL
  ALTER TABLE dbo.ECFEncabezado DROP COLUMN TransmissionJobId;
GO

/* ---- MovimientoFinanciero conciliación ---- */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_MovFin_EstadoConciliacion')
  ALTER TABLE dbo.MovimientoFinanciero DROP CONSTRAINT CK_MovFin_EstadoConciliacion;
GO
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_MovFin_EstadoConc' AND parent_object_id = OBJECT_ID('dbo.MovimientoFinanciero'))
  ALTER TABLE dbo.MovimientoFinanciero DROP CONSTRAINT DF_MovFin_EstadoConc;
GO

IF COL_LENGTH('dbo.MovimientoFinanciero', 'EstadoConciliacion') IS NOT NULL
  ALTER TABLE dbo.MovimientoFinanciero DROP COLUMN EstadoConciliacion;
IF COL_LENGTH('dbo.MovimientoFinanciero', 'IdTesoreriaConciliacion') IS NOT NULL
  ALTER TABLE dbo.MovimientoFinanciero DROP COLUMN IdTesoreriaConciliacion;
IF COL_LENGTH('dbo.MovimientoFinanciero', 'FechaConciliacion') IS NOT NULL
  ALTER TABLE dbo.MovimientoFinanciero DROP COLUMN FechaConciliacion;
IF COL_LENGTH('dbo.MovimientoFinanciero', 'IdUsuarioConciliacion') IS NOT NULL
  ALTER TABLE dbo.MovimientoFinanciero DROP COLUMN IdUsuarioConciliacion;
GO

/* ---- TesoreriaConciliacion columnas añadidas ---- */
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TesConc_CuentaAbierta' AND object_id = OBJECT_ID('dbo.TesoreriaConciliacion'))
  DROP INDEX UX_TesConc_CuentaAbierta ON dbo.TesoreriaConciliacion;
GO
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_TesoreriaConc_Tol' AND parent_object_id = OBJECT_ID('dbo.TesoreriaConciliacion'))
  ALTER TABLE dbo.TesoreriaConciliacion DROP CONSTRAINT DF_TesoreriaConc_Tol;
GO

IF COL_LENGTH('dbo.TesoreriaConciliacion', 'ToleranciaDiferencia') IS NOT NULL
  ALTER TABLE dbo.TesoreriaConciliacion DROP COLUMN ToleranciaDiferencia;
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'SaldoConciliado') IS NOT NULL
  ALTER TABLE dbo.TesoreriaConciliacion DROP COLUMN SaldoConciliado;
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'Diferencia') IS NOT NULL
  ALTER TABLE dbo.TesoreriaConciliacion DROP COLUMN Diferencia;
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'IdUsuarioReapertura') IS NOT NULL
  ALTER TABLE dbo.TesoreriaConciliacion DROP COLUMN IdUsuarioReapertura;
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'FechaReapertura') IS NOT NULL
  ALTER TABLE dbo.TesoreriaConciliacion DROP COLUMN FechaReapertura;
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'MotivoReapertura') IS NOT NULL
  ALTER TABLE dbo.TesoreriaConciliacion DROP COLUMN MotivoReapertura;
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'SaldoBancoInicial') IS NOT NULL
  ALTER TABLE dbo.TesoreriaConciliacion DROP COLUMN SaldoBancoInicial;
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'IdExtractoPrincipal') IS NOT NULL
  ALTER TABLE dbo.TesoreriaConciliacion DROP COLUMN IdExtractoPrincipal;
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'RowVersion') IS NOT NULL
  ALTER TABLE dbo.TesoreriaConciliacion DROP COLUMN RowVersion;
GO

/* ---- CuentaFinanciera columnas añadidas ---- */
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_CuentaFin_PermiteManual' AND parent_object_id = OBJECT_ID('dbo.CuentaFinanciera'))
  ALTER TABLE dbo.CuentaFinanciera DROP CONSTRAINT DF_CuentaFin_PermiteManual;
GO

IF COL_LENGTH('dbo.CuentaFinanciera', 'FechaSaldoInicial') IS NOT NULL
  ALTER TABLE dbo.CuentaFinanciera DROP COLUMN FechaSaldoInicial;
IF COL_LENGTH('dbo.CuentaFinanciera', 'PermiteMovimientosManuales') IS NOT NULL
  ALTER TABLE dbo.CuentaFinanciera DROP COLUMN PermiteMovimientosManuales;
IF COL_LENGTH('dbo.CuentaFinanciera', 'RowVersion') IS NOT NULL
  ALTER TABLE dbo.CuentaFinanciera DROP COLUMN RowVersion;
GO

PRINT '=== FIN rollback schema Prod ===';
GO

-- Verificación rápida login Sena
SELECT 'roles_sena' AS k, CAST(COUNT(*) AS varchar(20)) AS v
FROM PerfilRoles pr
JOIN Usuarios u ON u.IdPerfil = pr.IdPerfil AND pr.IdEmpresa = u.IdEmpresa
WHERE u.Correo = 'sena@gmail.com' AND pr.Activo = 1
UNION ALL
SELECT 'dashboard_sena', CAST(COUNT(*) AS varchar(20))
FROM PerfilRoles pr
JOIN Usuarios u ON u.IdPerfil = pr.IdPerfil AND pr.IdEmpresa = u.IdEmpresa
JOIN Modulos m ON m.Id = pr.IdModulo
WHERE u.Correo = 'sena@gmail.com' AND pr.Activo = 1 AND m.Codigo = 'DASHBOARD';
GO
