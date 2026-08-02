-- Backfill Parametros + SecuenciaDocumentos + Almacén + Al Portador
-- desde empresa plantilla 60. Solo AlahiaPos_Dev.
-- Uso: EXEC con @IdEmpresa destino (ej. 70) o recorrer empresas sin params.

SET NOCOUNT ON;

DECLARE @Plantilla INT = 60;
DECLARE @IdEmpresa INT = 70; -- cambiar o usar cursor abajo

IF NOT EXISTS (SELECT 1 FROM Empresas WHERE IdEmpresa = @IdEmpresa)
BEGIN
    RAISERROR('Empresa destino no existe', 16, 1);
    RETURN;
END;

-- Parametros
IF NOT EXISTS (SELECT 1 FROM Parametros WHERE IdEmpresa = @IdEmpresa)
BEGIN
    INSERT INTO Parametros (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
    SELECT
        @IdEmpresa,
        Tipo,
        CodigoPOS,
        Clave,
        CASE WHEN UPPER(Clave) = 'FACTURACION_ELECTRONICA' THEN 'false' ELSE Valor END,
        Descripcion,
        GETDATE(),
        1
    FROM Parametros
    WHERE IdEmpresa = @Plantilla AND Activo = 1;
END;

-- Secuencias (Actual = 0)
IF NOT EXISTS (SELECT 1 FROM SecuenciaDocumentos WHERE IdEmpresa = @IdEmpresa)
BEGIN
    INSERT INTO SecuenciaDocumentos (SecuenciaInicial, SecuenciaActual, Prefijo, IdTipoDocumento, FechaInseccion, IdEmpresa)
    SELECT
        SecuenciaInicial,
        0,
        Prefijo,
        IdTipoDocumento,
        GETDATE(),
        @IdEmpresa
    FROM SecuenciaDocumentos
    WHERE IdEmpresa = @Plantilla;
END;

-- Almacén principal
IF NOT EXISTS (SELECT 1 FROM Almacenes WHERE IdEmpresa = @IdEmpresa)
BEGIN
    INSERT INTO Almacenes (Nombre, Descripcion, IdEmpresa, EsPrincipal, Activo, FechaCreacion)
    VALUES (N'Principal', N'Almacén principal', @IdEmpresa, 1, 1, GETDATE());
END;

-- Cliente Al Portador
IF NOT EXISTS (SELECT 1 FROM Clientes WHERE IdEmpresa = @IdEmpresa AND NombreComercial = N'Al Portador')
BEGIN
    INSERT INTO Clientes (NombreComercial, Estado, LimiteCredito, IdEmpresa, FechaInseccion)
    VALUES (N'Al Portador', 1, 0, @IdEmpresa, GETDATE());
END;

SELECT
    (SELECT COUNT(*) FROM Parametros WHERE IdEmpresa = @IdEmpresa) AS Params,
    (SELECT COUNT(*) FROM SecuenciaDocumentos WHERE IdEmpresa = @IdEmpresa) AS Secuencias,
    (SELECT COUNT(*) FROM Almacenes WHERE IdEmpresa = @IdEmpresa) AS Almacenes,
    (SELECT COUNT(*) FROM Clientes WHERE IdEmpresa = @IdEmpresa AND NombreComercial = N'Al Portador') AS AlPortador;
