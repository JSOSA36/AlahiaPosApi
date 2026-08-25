-- Recetas y órdenes de producción (manufactura).
-- Distinto del Centro de Producción / KDS (ProduccionTrabajo).
-- Solo AlahiaPos_Dev.

IF OBJECT_ID(N'dbo.OrdenProduccionMaterial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecetaProduccion (
        IdReceta           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa          INT NOT NULL,
        IdProductoTerminado INT NOT NULL,
        Nombre             NVARCHAR(160) NOT NULL,
        RendimientoBase    DECIMAL(18,4) NOT NULL CONSTRAINT DF_RecetaProduccion_Rend DEFAULT (1),
        IdUnidadMedida     INT NULL,
        Activa             BIT NOT NULL CONSTRAINT DF_RecetaProduccion_Activa DEFAULT (1),
        Observacion        NVARCHAR(500) NULL,
        FechaCreacion      DATETIME NOT NULL CONSTRAINT DF_RecetaProduccion_Fecha DEFAULT (GETDATE()),
        IdUsuario          INT NULL,
        CONSTRAINT FK_RecetaProduccion_Producto FOREIGN KEY (IdProductoTerminado) REFERENCES dbo.Productos (IdProducto)
    );

    CREATE INDEX IX_RecetaProduccion_Empresa ON dbo.RecetaProduccion (IdEmpresa, IdProductoTerminado, Activa);

    CREATE TABLE dbo.RecetaProduccionItem (
        IdRecetaItem   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdReceta       INT NOT NULL,
        IdProducto     INT NOT NULL,
        Cantidad       DECIMAL(18,4) NOT NULL,
        IdUnidadMedida INT NULL,
        Orden          INT NOT NULL CONSTRAINT DF_RecetaProduccionItem_Orden DEFAULT (0),
        Activo         BIT NOT NULL CONSTRAINT DF_RecetaProduccionItem_Activo DEFAULT (1),
        CONSTRAINT FK_RecetaProduccionItem_Receta FOREIGN KEY (IdReceta) REFERENCES dbo.RecetaProduccion (IdReceta),
        CONSTRAINT FK_RecetaProduccionItem_Producto FOREIGN KEY (IdProducto) REFERENCES dbo.Productos (IdProducto)
    );

    CREATE INDEX IX_RecetaProduccionItem_Receta ON dbo.RecetaProduccionItem (IdReceta, Activo);

    CREATE TABLE dbo.OrdenProduccion (
        IdOrdenProduccion     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa             INT NOT NULL,
        Numero                NVARCHAR(40) NOT NULL,
        IdReceta              INT NOT NULL,
        IdProductoTerminado   INT NOT NULL,
        CantidadPlanificada   DECIMAL(18,4) NOT NULL,
        CantidadReal          DECIMAL(18,4) NULL,
        IdAlmacenOrigen       INT NOT NULL,
        IdAlmacenDestino      INT NOT NULL,
        Fecha                 DATETIME NOT NULL CONSTRAINT DF_OrdenProduccion_Fecha DEFAULT (GETDATE()),
        IdUsuarioResponsable  INT NULL,
        Observacion           NVARCHAR(500) NULL,
        Estado                NVARCHAR(20) NOT NULL CONSTRAINT DF_OrdenProduccion_Estado DEFAULT (N'BORRADOR'),
        IdMovimientoSalida    INT NULL,
        IdMovimientoEntrada   INT NULL,
        CostoMateriales       DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenProduccion_Costo DEFAULT (0),
        CostoUnitario         DECIMAL(18,4) NOT NULL CONSTRAINT DF_OrdenProduccion_CostoU DEFAULT (0),
        FechaInicio           DATETIME NULL,
        FechaCompletado       DATETIME NULL,
        FechaCreacion         DATETIME NOT NULL CONSTRAINT DF_OrdenProduccion_Creacion DEFAULT (GETDATE()),
        IdUsuario             INT NULL,
        CONSTRAINT FK_OrdenProduccion_Receta FOREIGN KEY (IdReceta) REFERENCES dbo.RecetaProduccion (IdReceta),
        CONSTRAINT FK_OrdenProduccion_Producto FOREIGN KEY (IdProductoTerminado) REFERENCES dbo.Productos (IdProducto),
        CONSTRAINT CK_OrdenProduccion_Estado CHECK (Estado IN (N'BORRADOR', N'PLANIFICADA', N'EN_PROCESO', N'COMPLETADA', N'CANCELADA'))
    );

    CREATE UNIQUE INDEX UX_OrdenProduccion_Empresa_Numero ON dbo.OrdenProduccion (IdEmpresa, Numero);
    CREATE INDEX IX_OrdenProduccion_Empresa_Estado ON dbo.OrdenProduccion (IdEmpresa, Estado, Fecha DESC);

    CREATE TABLE dbo.OrdenProduccionMaterial (
        IdOrdenMaterial     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdOrdenProduccion   INT NOT NULL,
        IdProducto          INT NOT NULL,
        IdUnidadMedida      INT NULL,
        CantidadTeorica     DECIMAL(18,4) NOT NULL,
        CantidadReal        DECIMAL(18,4) NULL,
        Disponible          DECIMAL(18,4) NOT NULL CONSTRAINT DF_OrdenProdMat_Disp DEFAULT (0),
        Faltante            DECIMAL(18,4) NOT NULL CONSTRAINT DF_OrdenProdMat_Falt DEFAULT (0),
        PrecioCompra        DECIMAL(18,4) NOT NULL CONSTRAINT DF_OrdenProdMat_Precio DEFAULT (0),
        CostoLinea          DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenProdMat_Costo DEFAULT (0),
        CONSTRAINT FK_OrdenProduccionMaterial_Orden FOREIGN KEY (IdOrdenProduccion) REFERENCES dbo.OrdenProduccion (IdOrdenProduccion),
        CONSTRAINT FK_OrdenProduccionMaterial_Producto FOREIGN KEY (IdProducto) REFERENCES dbo.Productos (IdProducto)
    );

    CREATE INDEX IX_OrdenProduccionMaterial_Orden ON dbo.OrdenProduccionMaterial (IdOrdenProduccion);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'MANUFACTURA_RECETAS')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'MANUFACTURA_RECETAS', N'Recetas de producción', N'Fórmulas para transformar materias primas en producto terminado.', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'MANUFACTURA_ORDENES')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'MANUFACTURA_ORDENES', N'Órdenes de producción', N'Planificar, validar faltantes y ejecutar producción contra inventario.', 0, 1, GETDATE());
GO

;WITH Mods AS (
    SELECT Id FROM dbo.Modulos WHERE Codigo IN (N'MANUFACTURA_RECETAS', N'MANUFACTURA_ORDENES')
)
INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, m.Id, 1, GETDATE()
FROM dbo.Empresas e
CROSS JOIN Mods m
WHERE ISNULL(e.EsEmpresaSistema, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = m.Id
  );

UPDATE em
SET em.Activo = 1, em.FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId
WHERE m.Codigo IN (N'MANUFACTURA_RECETAS', N'MANUFACTURA_ORDENES');
GO

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, m.Id, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Modulos m ON m.Codigo IN (N'MANUFACTURA_RECETAS', N'MANUFACTURA_ORDENES')
WHERE EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = p.IdEmpresa AND em.ModuloId = m.Id AND em.Activo = 1
)
AND NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles pr
    WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = m.Id AND pr.IdEmpresa = p.IdEmpresa
);

UPDATE pr
SET pr.Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Modulos m ON m.Id = pr.IdModulo
WHERE m.Codigo IN (N'MANUFACTURA_RECETAS', N'MANUFACTURA_ORDENES');
GO
