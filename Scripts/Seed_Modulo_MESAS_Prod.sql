-- ============================================================
-- Módulo MESAS (Salón) — AlahiaPos_Prod
-- Solo registra el módulo y lo activa en Sabor Urbano (73),
-- igual que en Dev (empresa 62). No copia mesas de prueba.
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

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'MESAS')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (
        N'MESAS',
        N'Salon',
        N'Salon de mesas para restaurante (piso / atencion en mesa).',
        0,
        1,
        GETDATE()
    );
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Nombre = N'Salon',
        Descripcion = N'Salon de mesas para restaurante (piso / atencion en mesa).',
        Activo = 1
    WHERE Codigo = N'MESAS';
END
GO

DECLARE @IdMod INT = (SELECT TOP 1 Id FROM dbo.Modulos WHERE Codigo = N'MESAS');
DECLARE @IdEmpresa INT = (
    SELECT TOP 1 IdEmpresa FROM dbo.Empresas WHERE NombreComercial = N'Sabor Urbano'
);

IF @IdMod IS NULL
BEGIN
    RAISERROR('No se pudo resolver el módulo MESAS.', 16, 1);
    RETURN;
END

IF @IdEmpresa IS NULL
BEGIN
    PRINT 'MESAS registrado. No hay empresa Sabor Urbano para activar.';
    RETURN;
END

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos
    WHERE EmpresaId = @IdEmpresa AND ModuloId = @IdMod
)
BEGIN
    INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
    VALUES (@IdEmpresa, @IdMod, 1, GETDATE());
END
ELSE
BEGIN
    UPDATE dbo.Empresa_Modulos
    SET Activo = 1, FechaDesactivacion = NULL
    WHERE EmpresaId = @IdEmpresa AND ModuloId = @IdMod;
END

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdMod, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
WHERE p.IdEmpresa = @IdEmpresa
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdMod AND pr.IdEmpresa = p.IdEmpresa
  );

UPDATE dbo.PerfilRoles
SET Activo = 1
WHERE IdModulo = @IdMod AND IdEmpresa = @IdEmpresa;

PRINT 'MESAS listo en AlahiaPos_Prod para Sabor Urbano.';
GO
