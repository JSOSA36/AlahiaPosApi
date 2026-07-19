-- Inicializa ContabilidadConfiguracion para empresas que ya tenían
-- el módulo CONTABILIDAD antes del hook automático.

INSERT INTO ContabilidadConfiguracion (IdEmpresa, IntegracionAutomatica, GenerarCOGSAutomatico, SepararAsientoCOGS)
SELECT DISTINCT em.EmpresaId, 0, 1, 1
FROM Empresa_Modulos em
INNER JOIN Modulos m ON m.Id = em.ModuloId
WHERE m.Codigo = 'CONTABILIDAD'
  AND em.Activo = 1
  AND NOT EXISTS (
      SELECT 1 FROM ContabilidadConfiguracion c WHERE c.IdEmpresa = em.EmpresaId
  );
GO
