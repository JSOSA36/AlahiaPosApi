IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CONTABILIDAD')
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('CONTABILIDAD', 'Contabilidad', 'Gestión contable integrada al ERP', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CONTABILIDAD_CUENTAS')
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('CONTABILIDAD_CUENTAS', 'Catálogo de Cuentas', 'Mantenimiento del plan de cuentas contables', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CONTABILIDAD_ASIENTOS')
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('CONTABILIDAD_ASIENTOS', 'Asientos Contables', 'Registro de asientos contables manuales', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CONTABILIDAD_LIBRO_DIARIO')
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('CONTABILIDAD_LIBRO_DIARIO', 'Libro Diario', 'Consulta del libro diario contable', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CONTABILIDAD_MAYOR_GENERAL')
BEGIN
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('CONTABILIDAD_MAYOR_GENERAL', 'Mayor General', 'Consulta del mayor general por cuenta', 1);
END
GO
