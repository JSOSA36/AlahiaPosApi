-- ============================================================
-- Parámetro ESTATUS_ORDENES (Dev)
-- Si Valor = 'true', el Listado de Órdenes muestra el estado
-- del Centro de Producción (Pendiente, En preparación, Lista…).
-- ============================================================
SET NOCOUNT ON;

DECLARE @IdEmpresa INT = 59; -- ajustar si aplica
DECLARE @Clave NVARCHAR(100) = N'ESTATUS_ORDENES';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Parametros
    WHERE IdEmpresa = @IdEmpresa AND Clave = @Clave
)
BEGIN
    INSERT INTO dbo.Parametros
        (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
    VALUES
        (@IdEmpresa, N'EMPRESA', NULL, @Clave, N'true',
         N'Muestra estados de producción (Preparación, Lista, etc.) en el listado de órdenes',
         GETDATE(), 1);
END
ELSE
BEGIN
    UPDATE dbo.Parametros
    SET Valor = N'true',
        Activo = 1,
        Descripcion = N'Muestra estados de producción (Preparación, Lista, etc.) en el listado de órdenes'
    WHERE IdEmpresa = @IdEmpresa AND Clave = @Clave;
END

SELECT IdParametro, IdEmpresa, Clave, Valor, Activo
FROM dbo.Parametros
WHERE IdEmpresa = @IdEmpresa AND Clave = @Clave;
