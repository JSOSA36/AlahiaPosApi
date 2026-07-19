IF NOT EXISTS (
    SELECT 1 FROM Modulos WHERE Codigo = 'LISTADO_DEVOLUCIONES'
)
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES (
        'LISTADO_DEVOLUCIONES',
        'Listado de Devoluciones',
        'Consulta de devoluciones y notas de crédito generadas',
        1
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM Modulos WHERE Codigo = 'NOTAS_CREDITO_APLICADAS'
)
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES (
        'NOTAS_CREDITO_APLICADAS',
        'Notas de Crédito Aplicadas',
        'Comprobantes fiscales de notas de crédito emitidos',
        1
    );
END
GO
