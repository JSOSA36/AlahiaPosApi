-- Activa Centro de Producción (KDS) para Terraza / MATBERT (IdEmpresa=55) en AlahiaPos_Dev.
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 55;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @IdEmpresa AND NombreComercial LIKE N'%MATBERT%'
)
BEGIN
    RAISERROR(N'Empresa 55 no es MATBERT / Terraza.', 16, 1);
    RETURN;
END;

DECLARE @Mods TABLE (ModuloId INT PRIMARY KEY, Codigo NVARCHAR(80) NOT NULL);
INSERT INTO @Mods (ModuloId, Codigo)
SELECT Id, Codigo
FROM dbo.Modulos
WHERE Codigo IN (
    N'CENTRO_PRODUCCION',
    N'PRODUCCION_GESTIONAR',
    N'PRODUCCION_CANCELAR',
    N'PRODUCCION_PRIORIDAD',
    N'PRODUCCION_CONFIG'
);

IF NOT EXISTS (SELECT 1 FROM @Mods WHERE Codigo = N'CENTRO_PRODUCCION')
BEGIN
    RAISERROR(N'No existe el módulo CENTRO_PRODUCCION.', 16, 1);
    RETURN;
END;

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT @IdEmpresa, m.ModuloId, 1, GETDATE()
FROM @Mods m
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = @IdEmpresa AND em.ModuloId = m.ModuloId
);

UPDATE em
SET em.Activo = 1, em.FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN @Mods m ON m.ModuloId = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa;

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, m.ModuloId, 1, GETDATE(), @IdEmpresa
FROM dbo.Perfiles p
CROSS JOIN @Mods m
WHERE p.IdEmpresa = @IdEmpresa
  AND p.Activo = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = p.IdPerfil
        AND pr.IdModulo = m.ModuloId
        AND pr.IdEmpresa = @IdEmpresa
  );

UPDATE pr
SET pr.Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN @Mods m ON m.ModuloId = pr.IdModulo
INNER JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
WHERE pr.IdEmpresa = @IdEmpresa
  AND p.IdEmpresa = @IdEmpresa;

IF NOT EXISTS (SELECT 1 FROM dbo.ProduccionConfiguracionEmpresa WHERE IdEmpresa = @IdEmpresa)
    INSERT INTO dbo.ProduccionConfiguracionEmpresa
        (IdEmpresa, Activo, UsarEstaciones, UsarEstadosPorItem, SonidoActivo,
         TiempoAdvertenciaSegDefault, TiempoCriticoSegDefault, PermitirCompletarDesdeEstacion,
         ModoOscuroDefault, MostrarNombreCliente, MostrarUsuarioSolicita, FechaActualizacion)
    VALUES (@IdEmpresa, 1, 0, 0, 1, 600, 900, 1, 1, 1, 1, GETDATE());
ELSE
    UPDATE dbo.ProduccionConfiguracionEmpresa
    SET Activo = 1, FechaActualizacion = GETDATE()
    WHERE IdEmpresa = @IdEmpresa;

IF NOT EXISTS (
    SELECT 1 FROM dbo.ProduccionEstacion
    WHERE IdEmpresa = @IdEmpresa AND Codigo = N'GENERAL'
)
    INSERT INTO dbo.ProduccionEstacion (IdEmpresa, Codigo, Nombre, EsDespacho, Activa, OrdenVisual)
    VALUES (@IdEmpresa, N'GENERAL', N'General', 0, 1, 0);

UPDATE c
SET IdEstacionPredeterminada = s.IdEstacion,
    FechaActualizacion = GETDATE()
FROM dbo.ProduccionConfiguracionEmpresa c
INNER JOIN dbo.ProduccionEstacion s
    ON s.IdEmpresa = c.IdEmpresa AND s.Codigo = N'GENERAL'
WHERE c.IdEmpresa = @IdEmpresa
  AND c.IdEstacionPredeterminada IS NULL;

SELECT N'Empresa_Modulos' AS Fuente, m.Codigo, em.Activo
FROM dbo.Empresa_Modulos em
JOIN dbo.Modulos m ON m.Id = em.ModuloId
JOIN @Mods x ON x.ModuloId = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa
ORDER BY m.Codigo;

SELECT p.Nombre AS Perfil, m.Codigo, pr.Activo
FROM dbo.PerfilRoles pr
JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
JOIN dbo.Modulos m ON m.Id = pr.IdModulo
JOIN @Mods x ON x.ModuloId = pr.IdModulo
WHERE pr.IdEmpresa = @IdEmpresa
ORDER BY p.Nombre, m.Codigo;
