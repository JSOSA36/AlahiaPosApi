-- Layout visual de mesas + comensales en órdenes.
-- AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.Mesas', 'Capacidad') IS NULL
    ALTER TABLE dbo.Mesas ADD Capacidad INT NULL;
GO

IF COL_LENGTH('dbo.Mesas', 'Forma') IS NULL
    ALTER TABLE dbo.Mesas ADD Forma NVARCHAR(20) NULL;
GO

IF COL_LENGTH('dbo.Mesas', 'PosX') IS NULL
    ALTER TABLE dbo.Mesas ADD PosX DECIMAL(9,2) NULL;
GO

IF COL_LENGTH('dbo.Mesas', 'PosY') IS NULL
    ALTER TABLE dbo.Mesas ADD PosY DECIMAL(9,2) NULL;
GO

IF COL_LENGTH('dbo.Mesas', 'Rotacion') IS NULL
    ALTER TABLE dbo.Mesas ADD Rotacion DECIMAL(9,2) NULL;
GO

IF COL_LENGTH('dbo.Mesas', 'Escala') IS NULL
    ALTER TABLE dbo.Mesas ADD Escala DECIMAL(9,2) NULL;
GO

IF COL_LENGTH('dbo.FacturaHeaders', 'Comensales') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD Comensales INT NULL;
GO

UPDATE dbo.Mesas
SET Capacidad = COALESCE(
    TRY_CONVERT(int, NULLIF(
        LEFT(LTRIM(Tipo), PATINDEX('%[^0-9]%', LTRIM(Tipo) + 'a') - 1),
        '')),
    4)
WHERE Capacidad IS NULL;
GO

UPDATE dbo.Mesas
SET Forma = CASE
        WHEN ISNULL(Capacidad, 4) <= 2 THEN N'rect'
        WHEN ISNULL(Capacidad, 4) <= 4 THEN N'square'
        ELSE N'round'
    END
WHERE Forma IS NULL OR LTRIM(RTRIM(Forma)) = N'';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_FacturaHeaders_MesaAbierta'
      AND object_id = OBJECT_ID(N'dbo.FacturaHeaders')
)
    CREATE INDEX IX_FacturaHeaders_MesaAbierta
        ON dbo.FacturaHeaders (IdEmpresa, IdMesa, IdTipoDocumentos, EstaCancelada)
        WHERE IdTipoDocumentos = 10;
GO

-- Salón de prueba (empresa 55 / Terraza 27) si aún no tiene zonas propias.
IF NOT EXISTS (SELECT 1 FROM dbo.Zonas WHERE IdEmpresa = 55)
BEGIN
    DECLARE @ImpresoraId int = (
        SELECT TOP 1 ImpresoraId FROM dbo.Zonas WHERE ImpresoraId > 0 ORDER BY ImpresoraId
    );
    IF @ImpresoraId IS NULL SET @ImpresoraId = 1;

    INSERT INTO dbo.Zonas (ZonaName, Estado, ImpresoraId, IdEmpresa, FechaInseccion)
    VALUES (N'Salón principal', 1, @ImpresoraId, 55, GETDATE());

    DECLARE @ZonaId int = SCOPE_IDENTITY();

    INSERT INTO dbo.Mesas (Tipo, Numero, ImagePath, Detalle, IsActiva, Estado, ZonaId, Capacidad, Forma)
    VALUES
        (N'2 Sillas', N'Mesa 1', N'', N'', 1, N'Libre', @ZonaId, 2, N'rect'),
        (N'4 Sillas', N'Mesa 2', N'', N'', 1, N'Libre', @ZonaId, 4, N'square'),
        (N'4 Sillas', N'Mesa 3', N'', N'', 1, N'Libre', @ZonaId, 4, N'square'),
        (N'4 Sillas', N'Mesa 4', N'', N'', 1, N'Libre', @ZonaId, 4, N'square'),
        (N'6 Sillas', N'Mesa 5', N'', N'', 1, N'Libre', @ZonaId, 6, N'round'),
        (N'6 Sillas', N'Mesa 6', N'', N'', 1, N'Libre', @ZonaId, 6, N'round'),
        (N'8 Sillas', N'Mesa 7', N'', N'', 1, N'Libre', @ZonaId, 8, N'round'),
        (N'2 Sillas', N'Mesa 8', N'', N'', 1, N'Libre', @ZonaId, 2, N'rect');
END
GO

