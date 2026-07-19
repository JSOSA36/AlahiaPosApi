-- ============================================================
-- Parámetro ESTATUS_ORDENES — Deploy AlahiaPos_Prod
-- Idempotente (upsert por IdEmpresa).
--
-- Si Valor = 'true', el Listado de Órdenes muestra el estado
-- del Centro de Producción (Pendiente, En preparación, Lista…).
-- En Prod el default es 'false' (opt-in por empresa).
--
-- Orden: ejecutar DESPUÉS de Deploy_CentroProduccion_Etapa1_Prod.sql
--        y del deploy de API/FE (este script solo toca Parametros).
-- ============================================================
USE AlahiaPos_Prod;
GO

SET NOCOUNT ON;
GO

-- >>> CAMBIAR A 1 solo con autorización explícita para ejecutar en Prod <<<
DECLARE @CONFIRMO_PROD BIT = 0;

IF DB_NAME() <> N'AlahiaPos_Prod' OR ISNULL(@CONFIRMO_PROD, 0) <> 1
BEGIN
    RAISERROR(
        N'Abortado: este script solo corre en AlahiaPos_Prod con @CONFIRMO_PROD = 1.',
        16, 1);
    SET NOEXEC ON;
END
GO

-- Empresa destino:
--   @IdEmpresa = 0  → no hace nada (seguro por defecto)
--   @IdEmpresa = N  → upsert ESTATUS_ORDENES para esa empresa
DECLARE @IdEmpresa INT = 0; -- Ejemplo: SET @IdEmpresa = 59;
DECLARE @Clave NVARCHAR(100) = N'ESTATUS_ORDENES';
DECLARE @ValorDefault NVARCHAR(20) = N'false'; -- Prod: opt-in (no activar en masa)

IF ISNULL(@IdEmpresa, 0) = 0
BEGIN
    PRINT N'Skip: @IdEmpresa = 0. Asigne el IdEmpresa real y re-ejecute (Valor default = false).';
END
ELSE IF NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresa)
BEGIN
    RAISERROR(N'Abortado: IdEmpresa %d no existe en Empresas.', 16, 1, @IdEmpresa);
END
ELSE
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM dbo.Parametros
        WHERE IdEmpresa = @IdEmpresa AND Clave = @Clave
    )
    BEGIN
        INSERT INTO dbo.Parametros
            (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
        VALUES
            (@IdEmpresa, N'EMPRESA', NULL, @Clave, @ValorDefault,
             N'Muestra estados de producción (Preparación, Lista, etc.) en el listado de órdenes',
             GETDATE(), 1);
        PRINT N'Insertado ESTATUS_ORDENES = false para IdEmpresa ' + CAST(@IdEmpresa AS NVARCHAR(20));
    END
    ELSE
    BEGIN
        UPDATE dbo.Parametros
        SET Valor = @ValorDefault,
            Activo = 1,
            Descripcion = N'Muestra estados de producción (Preparación, Lista, etc.) en el listado de órdenes'
        WHERE IdEmpresa = @IdEmpresa AND Clave = @Clave;
        PRINT N'Actualizado ESTATUS_ORDENES = false para IdEmpresa ' + CAST(@IdEmpresa AS NVARCHAR(20));
    END

    SELECT IdParametro, IdEmpresa, Clave, Valor, Activo, Descripcion
    FROM dbo.Parametros
    WHERE IdEmpresa = @IdEmpresa AND Clave = @Clave;
END
GO

SET NOEXEC OFF;
GO
