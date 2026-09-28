-- Salón de prueba para empresa 55 (Terraza 27). Solo AlahiaPos_Dev.
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @ZonaId int = (
    SELECT TOP 1 ZonaId FROM dbo.Zonas WHERE IdEmpresa = 55 ORDER BY ZonaId
);

IF @ZonaId IS NULL
BEGIN
    DECLARE @ImpresoraId int = (
        SELECT TOP 1 ImpresoraId FROM dbo.Zonas WHERE ImpresoraId > 0 ORDER BY ImpresoraId
    );
    IF @ImpresoraId IS NULL SET @ImpresoraId = 1;

    INSERT INTO dbo.Zonas (ZonaName, Estado, ImpresoraId, IdEmpresa, FechaInseccion)
    VALUES (N'Salón principal', 1, @ImpresoraId, 55, GETDATE());

    SET @ZonaId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM dbo.Mesas WHERE ZonaId = @ZonaId)
BEGIN
    INSERT INTO dbo.Mesas (Tipo, Numero, ImagePath, Detalle, IsActiva, Estado, ZonaId, FechaInseccion)
    VALUES
        (N'2 Sillas', N'Mesa 1', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'4 Sillas', N'Mesa 2', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'4 Sillas', N'Mesa 3', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'4 Sillas', N'Mesa 4', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'6 Sillas', N'Mesa 5', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'6 Sillas', N'Mesa 6', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'8 Sillas', N'Mesa 7', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'2 Sillas', N'Mesa 8', N'', N'', 1, N'Libre', @ZonaId, GETDATE());
END
GO
