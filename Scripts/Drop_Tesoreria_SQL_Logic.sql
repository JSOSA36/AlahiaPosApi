/*
  Elimina lógica de negocio en SQL Server (arquitectura API-first).
  Solo ejecutar en AlahiaPos_Dev salvo migración acordada a prod.
*/
IF OBJECT_ID('dbo.vw_TesoreriaSaldos', 'V') IS NOT NULL
    DROP VIEW dbo.vw_TesoreriaSaldos;
GO
IF OBJECT_ID('dbo.sp_Tesoreria_SincronizarSaldos', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Tesoreria_SincronizarSaldos;
GO
IF OBJECT_ID('dbo.fn_Tesoreria_CalcularSaldo', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_Tesoreria_CalcularSaldo;
GO
PRINT 'Lógica SQL de Tesorería eliminada. Usar API: CuentaFinanciera/ResumenSaldos y SincronizarSaldos.';
GO
