-- Corrige mojibake en Modulos.Nombre/Descripcion (UTF-8 leído como Latin-1).
-- Solo AlahiaPos_Prod. Preferir aplicar con SqlClient / parámetros Unicode.
USE AlahiaPos_Prod;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
  RAISERROR('Este script solo puede ejecutarse en AlahiaPos_Prod.', 16, 1);
  RETURN;
END
GO

UPDATE dbo.Modulos SET Nombre = N'Administración MacroBits', Descripcion = N'Gestión de políticas del servicio y herramientas internas MacroBits' WHERE Codigo = N'MACROBITS_ADMIN';
UPDATE dbo.Modulos SET Nombre = N'Políticas del Servicio', Descripcion = N'Administrar versiones de políticas del servicio' WHERE Codigo = N'POLITICAS_VERSIONES';
UPDATE dbo.Modulos SET Nombre = N'Aceptaciones de Políticas', Descripcion = N'Consulta de aceptaciones de políticas por empresa' WHERE Codigo = N'POLITICAS_ACEPTACIONES';
UPDATE dbo.Modulos SET Nombre = N'Pago de Suscripción', Descripcion = N'Reportar pago de la suscripción Alahia ERP y consultar historial' WHERE Codigo = N'PAGO_SUSCRIPCION';
UPDATE dbo.Modulos SET Nombre = N'Conciliación Bancaria', Descripcion = N'Centro de trabajo: extracto vs libro banco, matching y cierre auditable.' WHERE Codigo = N'CONCILIACION_BANCARIA';
UPDATE dbo.Modulos SET Descripcion = N'Notas de entrega de mercancía vinculadas a facturas' WHERE Codigo = N'CONDUCES';
UPDATE dbo.Modulos SET Nombre = N'Proveedores', Descripcion = N'Catálogo de proveedores' WHERE Codigo = N'PROVEEDORES';
UPDATE dbo.Modulos SET Nombre = N'Centro de Producción', Descripcion = N'Tablero operativo de trabajos y flujos de producción' WHERE Codigo = N'CENTRO_PRODUCCION';
UPDATE dbo.Modulos SET Nombre = N'Producción - Gestionar', Descripcion = N'Cambiar estados de trabajos en el Centro de Producción' WHERE Codigo = N'PRODUCCION_GESTIONAR';
UPDATE dbo.Modulos SET Nombre = N'Producción - Cancelar', Descripcion = N'Cancelar trabajos en el Centro de Producción' WHERE Codigo = N'PRODUCCION_CANCELAR';
UPDATE dbo.Modulos SET Nombre = N'Producción - Prioridad', Descripcion = N'Cambiar prioridad de trabajos' WHERE Codigo = N'PRODUCCION_PRIORIDAD';
UPDATE dbo.Modulos SET Nombre = N'Producción - Configuración', Descripcion = N'Configurar Centro de Producción por empresa' WHERE Codigo = N'PRODUCCION_CONFIG';

-- Si existen en Prod (mismo fix que Dev)
IF EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'ANTIGUEDAD_CXC')
  UPDATE dbo.Modulos SET Nombre = N'Antigüedad de Saldos CxC', Descripcion = N'Análisis de antigüedad de cuentas por cobrar' WHERE Codigo = N'ANTIGUEDAD_CXC';
IF EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'ANTIGUEDAD_CXP')
  UPDATE dbo.Modulos SET Nombre = N'Antigüedad de Saldos CxP', Descripcion = N'Análisis de antigüedad de cuentas por pagar' WHERE Codigo = N'ANTIGUEDAD_CXP';

SELECT Codigo, Nombre
FROM dbo.Modulos
WHERE Codigo IN (
  N'MACROBITS_ADMIN', N'POLITICAS_VERSIONES', N'POLITICAS_ACEPTACIONES',
  N'PAGO_SUSCRIPCION', N'CONCILIACION_BANCARIA', N'CENTRO_PRODUCCION',
  N'PRODUCCION_GESTIONAR', N'PRODUCCION_CANCELAR', N'PRODUCCION_PRIORIDAD',
  N'PRODUCCION_CONFIG', N'ANTIGUEDAD_CXC', N'ANTIGUEDAD_CXP'
)
ORDER BY Codigo;
GO
