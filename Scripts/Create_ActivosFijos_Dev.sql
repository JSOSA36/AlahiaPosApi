-- ============================================================
-- Activos Fijos (fase 1) — solo AlahiaPos_Dev
-- Separado de AlmacenExistencia / valor de inventario
-- ============================================================
USE AlahiaPos_Dev;
GO

IF OBJECT_ID('dbo.ActivosFijos') IS NULL
BEGIN
    CREATE TABLE dbo.ActivosFijos
    (
        IdActivoFijo           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ActivosFijos PRIMARY KEY,
        IdEmpresa              INT NOT NULL,
        IdProducto             INT NULL,
        IdOrdenCompraHeader    INT NULL,
        IdOrdenCompraDetalle   INT NULL,
        CodigoActivo           NVARCHAR(40) NOT NULL,
        Descripcion            NVARCHAR(250) NOT NULL,
        Marca                  NVARCHAR(100) NULL,
        Modelo                 NVARCHAR(100) NULL,
        NumeroSerie            NVARCHAR(100) NULL,
        FechaAdquisicion       DATE NOT NULL,
        FechaRecepcion         DATETIME NULL,
        ValorAdquisicion       DECIMAL(18,2) NOT NULL CONSTRAINT DF_AF_ValorAdq DEFAULT(0),
        ValorResidual          DECIMAL(18,2) NOT NULL CONSTRAINT DF_AF_ValorRes DEFAULT(0),
        VidaUtilMeses          INT NULL,
        Estado                 NVARCHAR(30) NOT NULL CONSTRAINT DF_AF_Estado DEFAULT(N'PENDIENTE_DATOS'),
        -- PENDIENTE_DATOS | ACTIVO | BAJA | EN_MANTENIMIENTO (futuro)
        IdCuentaContable       INT NULL,
        IdAlmacenRecepcion     INT NULL,
        Ubicacion              NVARCHAR(200) NULL,
        Responsable            NVARCHAR(150) NULL,
        Observacion            NVARCHAR(500) NULL,
        FechaCreacion          DATETIME NOT NULL CONSTRAINT DF_AF_FechaCreacion DEFAULT(GETDATE()),
        IdUsuarioCreacion      INT NULL,
        Activo                 BIT NOT NULL CONSTRAINT DF_AF_Activo DEFAULT(1),
        FechaInseccion         DATE NULL,
        CONSTRAINT UQ_ActivosFijos_CodigoEmpresa UNIQUE (IdEmpresa, CodigoActivo)
    );

    CREATE INDEX IX_ActivosFijos_Empresa_Estado
        ON dbo.ActivosFijos (IdEmpresa, Estado, Activo);

    CREATE INDEX IX_ActivosFijos_Compra
        ON dbo.ActivosFijos (IdOrdenCompraHeader, IdOrdenCompraDetalle);
END
GO

-- Módulo menú
IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'ACTIVOS_FIJOS')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES ('ACTIVOS_FIJOS', 'Activos Fijos', 'Registro de activos fijos separados del inventario', 0, 1, GETDATE());
GO

INSERT INTO Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, m.Id, 1, GETDATE()
FROM Empresas e
CROSS JOIN Modulos m
WHERE m.Codigo = 'ACTIVOS_FIJOS'
  AND NOT EXISTS (
      SELECT 1 FROM Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = m.Id
  );
GO

PRINT 'Create_ActivosFijos_Dev OK';
GO
