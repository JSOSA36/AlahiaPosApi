-- ============================================================
-- Sucursales de clientes — AlahiaPos_Prod
-- Flawless Laundry (59): Alameda (actual) + Monumental (nueva).
-- Dismerling, Dra. Sena, Autoservicio, Terraza 27: una sola, la actual.
-- ============================================================
USE AlahiaPos_Prod;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script solo corre en AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

DECLARE @IdFlow INT = (
    SELECT TOP 1 IdEmpresa
    FROM dbo.Empresas
    WHERE NombreComercial = N'Flawless Laundry'
    ORDER BY IdEmpresa
);

IF @IdFlow IS NULL
BEGIN
    RAISERROR('No se encontró Flawless Laundry.', 16, 1);
    RETURN;
END

-- La sucursal actual (Principal) pasa a llamarse Alameda y sigue siendo la principal.
UPDATE dbo.Sucursal
SET Codigo = N'ALAMEDA',
    Nombre = N'Alameda'
WHERE IdEmpresa = @IdFlow
  AND EsPrincipal = 1;

DECLARE @IdAlameda INT = (
    SELECT TOP 1 IdSucursal
    FROM dbo.Sucursal
    WHERE IdEmpresa = @IdFlow AND EsPrincipal = 1
);

IF NOT EXISTS (
    SELECT 1 FROM dbo.Sucursal
    WHERE IdEmpresa = @IdFlow AND Codigo = N'MONUMENTAL'
)
BEGIN
    INSERT INTO dbo.Sucursal
    (
        IdEmpresa, Codigo, Nombre, EsPrincipal, Activa,
        Direccion, Telefono, FechaCreacion
    )
    VALUES
    (
        @IdFlow, N'MONUMENTAL', N'Monumental', 0, 1,
        N'Santo Domingo', NULL, GETDATE()
    );
END

DECLARE @IdMonumental INT = (
    SELECT TOP 1 IdSucursal
    FROM dbo.Sucursal
    WHERE IdEmpresa = @IdFlow AND Codigo = N'MONUMENTAL'
);

IF @IdMonumental IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.Almacenes
        WHERE IdEmpresa = @IdFlow AND IdSucursal = @IdMonumental
   )
BEGIN
    INSERT INTO dbo.Almacenes
    (
        Nombre, Descripcion, IdEmpresa, EsPrincipal, Activo, FechaCreacion, IdSucursal
    )
    VALUES
    (
        N'Monumental', N'Almacén sucursal Monumental', @IdFlow, 0, 1, GETDATE(), @IdMonumental
    );
END

UPDATE s
SET s.IdAlmacenPrincipal = a.IdAlmacen
FROM dbo.Sucursal s
INNER JOIN dbo.Almacenes a
    ON a.IdEmpresa = s.IdEmpresa
   AND a.IdSucursal = s.IdSucursal
WHERE s.IdSucursal = @IdMonumental
  AND s.IdAlmacenPrincipal IS NULL;

-- Acceso a Monumental para los usuarios de la lavandería (default sigue Alameda).
INSERT INTO dbo.UsuarioSucursal (IdUsuario, IdSucursal, EsDefault, Activo, FechaCreacion)
SELECT u.IdUsuario, @IdMonumental, 0, 1, GETDATE()
FROM dbo.Usuarios u
WHERE u.IdEmpresa = @IdFlow
  AND @IdMonumental IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.UsuarioSucursal us
      WHERE us.IdUsuario = u.IdUsuario AND us.IdSucursal = @IdMonumental
  );

UPDATE dbo.Almacenes
SET Nombre = N'Alameda'
WHERE IdEmpresa = @IdFlow
  AND IdSucursal = @IdAlameda
  AND EsPrincipal = 1
  AND Nombre IN (N'Principal', N'Sucursal Principal');

PRINT 'Flawless Laundry: Alameda + Monumental.';
GO

-- Las demás se quedan con la sucursal actual (Principal). Solo se confirma.
SELECT e.IdEmpresa, e.NombreComercial, s.IdSucursal, s.Codigo, s.Nombre, s.EsPrincipal
FROM dbo.Empresas e
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = e.IdEmpresa
WHERE e.IdEmpresa IN (
    SELECT IdEmpresa FROM dbo.Empresas
    WHERE NombreComercial LIKE N'Dismerling%'
       OR NombreComercial LIKE N'Centro Odontologico DRA.SENA%'
       OR NombreComercial LIKE N'AUTOSERVICIO%'
       OR NombreComercial LIKE N'Terraza%'
       OR NombreComercial = N'Flawless Laundry'
)
ORDER BY e.IdEmpresa, s.EsPrincipal DESC, s.IdSucursal;
GO
