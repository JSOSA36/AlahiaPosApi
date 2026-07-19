IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CONTABILIDAD_CONFIGURACION_INTEGRACION')
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES (
        'CONTABILIDAD_CONFIGURACION_INTEGRACION',
        'Configuración de Integración',
        'Activar integración automática y parámetros del motor contable',
        1
    );
END
GO
