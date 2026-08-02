-- AlahiaPos_Dev — Activar Contabilidad completa (empresa 60 Clinica Dental Sena)
-- Incluye PrecioUSD (requerido en Modulos) y asignación a Empresa_Modulos + PerfilRoles.
USE AlahiaPos_Dev;
GO

DECLARE @IdEmpresa INT = 60;
DECLARE @IdPerfilAdmin INT = 16;
DECLARE @IdPerfilContable INT = 18;

-- Catálogo de módulos (PrecioUSD + FechaCreacion obligatorios)
INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
SELECT v.Codigo, v.Nombre, v.Descripcion, 0, 1, GETDATE()
FROM (VALUES
  ('CONTABILIDAD', N'Contabilidad', N'Gestion contable integrada al ERP'),
  ('CONTABILIDAD_CUENTAS', N'Catalogo de Cuentas', N'Plan de cuentas contables'),
  ('CONTABILIDAD_ASIENTOS', N'Asientos Contables', N'Registro de asientos manuales'),
  ('CONTABILIDAD_LIBRO_DIARIO', N'Libro Diario', N'Consulta libro diario'),
  ('CONTABILIDAD_MAYOR_GENERAL', N'Mayor General', N'Mayor general por cuenta'),
  ('CONTABILIDAD_BALANCE_COMPROBACION', N'Balance de Comprobacion', N'Balance de comprobacion'),
  ('CONTABILIDAD_ESTADO_RESULTADOS', N'Estado de Resultados', N'Estado de resultados'),
  ('CONTABILIDAD_BALANCE_GENERAL', N'Balance General', N'Balance general'),
  ('CONTABILIDAD_CONSULTA_ASIENTOS', N'Consulta de Asientos', N'Consulta de asientos'),
  ('CONTABILIDAD_CIERRE', N'Cierre Contable', N'Cierre contable'),
  ('CONTABILIDAD_CONFIGURACION_INTEGRACION', N'Config. Integracion Contable', N'Integracion automatica')
) v(Codigo, Nombre, Descripcion)
WHERE NOT EXISTS (SELECT 1 FROM Modulos m WHERE m.Codigo = v.Codigo);

-- Empresa_Modulos
INSERT INTO Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT @IdEmpresa, m.Id, 1, GETDATE()
FROM Modulos m
WHERE m.Codigo LIKE 'CONTABILIDAD%'
  AND NOT EXISTS (
    SELECT 1 FROM Empresa_Modulos em
    WHERE em.EmpresaId = @IdEmpresa AND em.ModuloId = m.Id
  );

UPDATE em
SET em.Activo = 1, em.FechaDesactivacion = NULL
FROM Empresa_Modulos em
INNER JOIN Modulos m ON m.Id = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa
  AND m.Codigo LIKE 'CONTABILIDAD%';

-- PerfilRoles (Admin + Contable)
INSERT INTO PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, m.Id, 1, GETDATE(), @IdEmpresa
FROM (VALUES (@IdPerfilAdmin), (@IdPerfilContable)) p(IdPerfil)
CROSS JOIN Modulos m
WHERE m.Codigo LIKE 'CONTABILIDAD%'
  AND NOT EXISTS (
    SELECT 1 FROM PerfilRoles pr
    WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = m.Id AND pr.IdEmpresa = @IdEmpresa
  );

-- Config: integración automática ON
IF NOT EXISTS (SELECT 1 FROM ContabilidadConfiguracion WHERE IdEmpresa = @IdEmpresa)
BEGIN
  INSERT INTO ContabilidadConfiguracion (IdEmpresa, IntegracionAutomatica, GenerarCOGSAutomatico, SepararAsientoCOGS)
  VALUES (@IdEmpresa, 1, 1, 1);
END
ELSE
BEGIN
  UPDATE ContabilidadConfiguracion
  SET IntegracionAutomatica = 1,
      GenerarCOGSAutomatico = 1,
      SepararAsientoCOGS = 1,
      FechaActualizacion = GETDATE()
  WHERE IdEmpresa = @IdEmpresa;
END

SELECT e.IdEmpresa, e.NombreComercial, m.Codigo, em.Activo
FROM Empresa_Modulos em
JOIN Modulos m ON m.Id = em.ModuloId
JOIN Empresas e ON e.IdEmpresa = em.EmpresaId
WHERE e.IdEmpresa = @IdEmpresa AND m.Codigo LIKE 'CONTABILIDAD%'
ORDER BY m.Codigo;

SELECT * FROM ContabilidadConfiguracion WHERE IdEmpresa = @IdEmpresa;
GO
