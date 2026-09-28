-- AlahiaPos_Dev: nombres de módulos con UTF-8 mal interpretado.
SET NOCOUNT ON;

UPDATE dbo.Modulos SET Nombre = N'Impresión térmica' WHERE Codigo = N'IMPRESION_TERMICA';
UPDATE dbo.Modulos SET Nombre = N'Órdenes de producción' WHERE Codigo = N'MANUFACTURA_ORDENES';
UPDATE dbo.Modulos SET Nombre = N'Recetas de producción' WHERE Codigo = N'MANUFACTURA_RECETAS';
UPDATE dbo.Modulos SET Nombre = N'Nómina' WHERE Codigo = N'RRHH_NOMINA';
UPDATE dbo.Modulos SET Nombre = N'RRHH — Corregir ponchadas' WHERE Codigo = N'RRHH_CORRECCION';
UPDATE dbo.Modulos SET Nombre = N'RRHH — Aprobar nómina' WHERE Codigo = N'RRHH_NOMINA_APROBAR';
UPDATE dbo.Modulos SET Nombre = N'RRHH — Aprobar permisos' WHERE Codigo = N'RRHH_PERMISOS_APROBAR';
UPDATE dbo.Modulos SET Nombre = N'RRHH — Pagar nómina' WHERE Codigo = N'RRHH_NOMINA_PAGAR';
