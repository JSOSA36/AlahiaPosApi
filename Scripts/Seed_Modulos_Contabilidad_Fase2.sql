IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CONTABILIDAD_BALANCE_COMPROBACION')
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('CONTABILIDAD_BALANCE_COMPROBACION', 'Balance de Comprobación', 'Consulta del balance de comprobación contable', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CONTABILIDAD_ESTADO_RESULTADOS')
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('CONTABILIDAD_ESTADO_RESULTADOS', 'Estado de Resultados', 'Consulta del estado de resultados', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CONTABILIDAD_BALANCE_GENERAL')
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('CONTABILIDAD_BALANCE_GENERAL', 'Balance General', 'Consulta del balance general', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CONTABILIDAD_CONSULTA_ASIENTOS')
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('CONTABILIDAD_CONSULTA_ASIENTOS', 'Consulta de Asientos', 'Búsqueda y consulta de asientos contables', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CONTABILIDAD_CIERRE')
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('CONTABILIDAD_CIERRE', 'Cierre Contable', 'Cierre de períodos contables', 1);
END
GO
