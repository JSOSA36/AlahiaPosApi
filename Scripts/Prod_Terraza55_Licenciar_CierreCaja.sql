-- Solo Terraza Prolongación 27 (IdEmpresa = 55).
-- El menú exige Empresa_Modulos + PerfilRoles; sin licencia no aparece CIERRE_CAJA.
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: no es AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 55;
DECLARE @Nombre NVARCHAR(200) = (
    SELECT NombreComercial FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresa
);

IF @Nombre IS NULL OR @Nombre NOT LIKE N'%Terraza%'
BEGIN
    RAISERROR(N'Abortado: IdEmpresa 55 no es Terraza.', 16, 1);
    RETURN;
END;

DECLARE @IdCierre INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'CIERRE_CAJA');
DECLARE @IdListado INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'LISTADO_CAJA');

IF @IdCierre IS NULL
BEGIN
    RAISERROR(N'No existe módulo CIERRE_CAJA.', 16, 1);
    RETURN;
END;

-- Licencia empresa (solo 55)
IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos
    WHERE EmpresaId = @IdEmpresa AND ModuloId = @IdCierre
)
    INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
    VALUES (@IdEmpresa, @IdCierre, 1, GETDATE());
ELSE
    UPDATE dbo.Empresa_Modulos
    SET Activo = 1, FechaActivacion = ISNULL(FechaActivacion, GETDATE()), FechaDesactivacion = NULL
    WHERE EmpresaId = @IdEmpresa AND ModuloId = @IdCierre;

IF @IdListado IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM dbo.Empresa_Modulos
        WHERE EmpresaId = @IdEmpresa AND ModuloId = @IdListado
    )
        INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
        VALUES (@IdEmpresa, @IdListado, 1, GETDATE());
    ELSE
        UPDATE dbo.Empresa_Modulos
        SET Activo = 1, FechaActivacion = ISNULL(FechaActivacion, GETDATE()), FechaDesactivacion = NULL
        WHERE EmpresaId = @IdEmpresa AND ModuloId = @IdListado;
END;

-- Perfil Principal (María): darle CIERRE_CAJA si no lo tiene
DECLARE @IdPrincipal INT = (
    SELECT TOP 1 IdPerfil
    FROM dbo.Perfiles
    WHERE IdEmpresa = @IdEmpresa
      AND Activo = 1
      AND (Nombre LIKE N'%Principal%' OR Nombre LIKE N'%Admin%')
    ORDER BY CASE WHEN Nombre LIKE N'%Principal%' THEN 0 ELSE 1 END, IdPerfil
);

IF @IdPrincipal IS NOT NULL AND @IdCierre IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.PerfilRoles
        WHERE IdPerfil = @IdPrincipal AND IdModulo = @IdCierre AND IdEmpresa = @IdEmpresa
   )
BEGIN
    INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
    VALUES (@IdPrincipal, @IdCierre, 1, GETDATE(), @IdEmpresa);
END
ELSE IF @IdPrincipal IS NOT NULL AND @IdCierre IS NOT NULL
BEGIN
    UPDATE dbo.PerfilRoles
    SET Activo = 1
    WHERE IdPerfil = @IdPrincipal AND IdModulo = @IdCierre AND IdEmpresa = @IdEmpresa;
END;

SELECT N'OK Terraza' AS Resultado, @IdEmpresa AS IdEmpresa, @Nombre AS Empresa;

SELECT m.Codigo, em.Activo AS Licencia
FROM dbo.Modulos m
JOIN dbo.Empresa_Modulos em ON em.ModuloId = m.Id AND em.EmpresaId = @IdEmpresa
WHERE m.Codigo IN (N'CIERRE_CAJA', N'LISTADO_CAJA', N'POS');

SELECT p.Nombre AS Perfil, m.Codigo, pr.Activo
FROM dbo.PerfilRoles pr
JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
JOIN dbo.Modulos m ON m.Id = pr.IdModulo
WHERE p.IdEmpresa = @IdEmpresa
  AND m.Codigo IN (N'CIERRE_CAJA', N'LISTADO_CAJA', N'POS')
ORDER BY p.Nombre, m.Codigo;
GO
