-- Solo AlahiaPos_Dev: mapeo categoría de gasto → cuenta contable
IF COL_LENGTH('dbo.CategoriasGasto', 'IdCuentaContable') IS NULL
BEGIN
    ALTER TABLE dbo.CategoriasGasto
        ADD IdCuentaContable INT NULL;

    PRINT 'CategoriasGasto.IdCuentaContable agregada.';
END
ELSE
    PRINT 'CategoriasGasto.IdCuentaContable ya existe.';
GO
