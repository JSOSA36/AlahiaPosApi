-- ============================================================
-- Habilitar TICKETS (clientes) en AlahiaPos_Prod
-- ============================================================
USE AlahiaPos_Prod;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'TICKETS')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'TICKETS', N'Tickets de Soporte', N'Reportar incidencias y dar seguimiento a tickets de soporte', 0, 1, GETDATE());
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Nombre = N'Tickets de Soporte',
        Descripcion = N'Reportar incidencias y dar seguimiento a tickets de soporte',
        Activo = 1
    WHERE Codigo = N'TICKETS';
END
GO

DECLARE @IdTickets INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'TICKETS');

-- Licencia empresa (clientes, no sistema)
INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, @IdTickets, 1, GETDATE()
FROM dbo.Empresas e
WHERE ISNULL(e.EsEmpresaSistema, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdTickets
  );

UPDATE em SET Activo = 1, FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Empresas e ON e.IdEmpresa = em.EmpresaId
WHERE em.ModuloId = @IdTickets AND ISNULL(e.EsEmpresaSistema, 0) = 0;

-- Menú por perfil (todos los perfiles de clientes)
INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdTickets, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE ISNULL(e.EsEmpresaSistema, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdTickets
        AND ISNULL(pr.IdEmpresa, p.IdEmpresa) = p.IdEmpresa
  );

UPDATE pr SET Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Empresas e ON e.IdEmpresa = ISNULL(pr.IdEmpresa, e.IdEmpresa)
INNER JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil AND p.IdEmpresa = e.IdEmpresa
WHERE pr.IdModulo = @IdTickets AND ISNULL(e.EsEmpresaSistema, 0) = 0;

SELECT m.Codigo, m.Nombre,
    (SELECT COUNT(*) FROM dbo.Empresa_Modulos WHERE ModuloId = m.Id AND Activo = 1) AS Empresas,
    (SELECT COUNT(*) FROM dbo.PerfilRoles WHERE IdModulo = m.Id AND Activo = 1) AS Perfiles
FROM dbo.Modulos m
WHERE m.Codigo = N'TICKETS';

PRINT 'TICKETS habilitado para clientes en AlahiaPos_Prod.';
GO
