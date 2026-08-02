-- Corrige mojibake en Modulos.Nombre/Descripcion (UTF-8 leído como Latin-1 al insertar).
-- Solo AlahiaPos_Dev. Usar archivo UTF-16 LE o sqlcmd -f 65001.
USE AlahiaPos_Dev;
GO

UPDATE dbo.Modulos
SET Nombre = N'Antigüedad de Saldos CxC',
    Descripcion = N'Análisis de antigüedad de cuentas por cobrar'
WHERE Codigo = N'ANTIGUEDAD_CXC';

UPDATE dbo.Modulos
SET Nombre = N'Antigüedad de Saldos CxP',
    Descripcion = N'Análisis de antigüedad de cuentas por pagar'
WHERE Codigo = N'ANTIGUEDAD_CXP';

UPDATE dbo.Modulos
SET Nombre = N'Administración MacroBits',
    Descripcion = N'Gestión de políticas del servicio y herramientas internas MacroBits'
WHERE Codigo = N'MACROBITS_ADMIN';

UPDATE dbo.Modulos
SET Nombre = N'Políticas del Servicio',
    Descripcion = N'Administrar versiones de políticas del servicio'
WHERE Codigo = N'POLITICAS_VERSIONES';

UPDATE dbo.Modulos
SET Nombre = N'Aceptaciones de Políticas',
    Descripcion = N'Consulta de aceptaciones de políticas por empresa'
WHERE Codigo = N'POLITICAS_ACEPTACIONES';

UPDATE dbo.Modulos
SET Nombre = N'Pago de Suscripción',
    Descripcion = N'Reportar pago de la suscripción Alahia ERP y consultar historial'
WHERE Codigo = N'PAGO_SUSCRIPCION';

UPDATE dbo.Modulos
SET Nombre = N'Conciliación Bancaria',
    Descripcion = N'Centro de trabajo: extracto vs libro banco, matching y cierre auditable.'
WHERE Codigo = N'CONCILIACION_BANCARIA';

UPDATE dbo.Modulos
SET Descripcion = N'Notas de entrega de mercancía vinculadas a facturas'
WHERE Codigo = N'CONDUCES'
  AND Descripcion LIKE N'%mercanc%';

-- Verificación
SELECT Codigo, Nombre, Descripcion
FROM dbo.Modulos
WHERE Codigo IN (
    N'ANTIGUEDAD_CXC', N'ANTIGUEDAD_CXP', N'MACROBITS_ADMIN',
    N'POLITICAS_VERSIONES', N'POLITICAS_ACEPTACIONES',
    N'PAGO_SUSCRIPCION', N'CONCILIACION_BANCARIA', N'CONDUCES'
)
ORDER BY Codigo;
GO
