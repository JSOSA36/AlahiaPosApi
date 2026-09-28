-- AlahiaPos_Dev — Tipo de Bienes y Servicios DGII (606) en Gastos
-- Solo Desarrollo

USE AlahiaPos_Dev;
GO

IF COL_LENGTH('dbo.Gastos', 'IdTipoBienesServicios') IS NULL
  ALTER TABLE dbo.Gastos ADD IdTipoBienesServicios INT NULL;
GO
