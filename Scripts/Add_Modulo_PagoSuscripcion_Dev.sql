-- ============================================================
-- Módulo PAGO_SUSCRIPCION — licencia clientes + menú solo Admin
-- (reemplaza asignación a todos los perfiles / cajeros)
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'PAGO_SUSCRIPCION')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (
        N'PAGO_SUSCRIPCION',
        N'Pago de Suscripción',
        N'Reportar pago de la suscripción Alahia ERP y consultar historial',
        0, 1, GETDATE()
    );
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Nombre = N'Pago de Suscripción',
        Descripcion = N'Reportar pago de la suscripción Alahia ERP y consultar historial',
        Activo = 1
    WHERE Codigo = N'PAGO_SUSCRIPCION';
END
GO

DECLARE @IdModulo INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'PAGO_SUSCRIPCION');

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, @IdModulo, 1, GETDATE()
FROM dbo.Empresas e
WHERE ISNULL(e.EsEmpresaSistema, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdModulo
  );

UPDATE em SET Activo = 1, FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Empresas e ON e.IdEmpresa = em.EmpresaId
WHERE em.ModuloId = @IdModulo AND ISNULL(e.EsEmpresaSistema, 0) = 0;

-- Solo Administrador en menú; cajeros/otros quedan fuera
UPDATE pr SET Activo = 0
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
INNER JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE pr.IdModulo = @IdModulo
  AND ISNULL(e.EsEmpresaSistema, 0) = 0
  AND NOT (
        p.Nombre = N'Administrador'
     OR p.Nombre LIKE N'%Administrador%'
     OR (p.Nombre LIKE N'%ADMIN%' AND p.Nombre NOT LIKE N'%CAJERO%')
  );

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdModulo, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE ISNULL(e.EsEmpresaSistema, 0) = 0
  AND (
        p.Nombre = N'Administrador'
     OR p.Nombre LIKE N'%Administrador%'
     OR (p.Nombre LIKE N'%ADMIN%' AND p.Nombre NOT LIKE N'%CAJERO%')
  )
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdModulo
        AND ISNULL(pr.IdEmpresa, p.IdEmpresa) = p.IdEmpresa
  );

UPDATE pr SET Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
INNER JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE pr.IdModulo = @IdModulo
  AND ISNULL(e.EsEmpresaSistema, 0) = 0
  AND (
        p.Nombre = N'Administrador'
     OR p.Nombre LIKE N'%Administrador%'
     OR (p.Nombre LIKE N'%ADMIN%' AND p.Nombre NOT LIKE N'%CAJERO%')
  );

PRINT 'PAGO_SUSCRIPCION: licencia + menú solo Administrador.';
GO
