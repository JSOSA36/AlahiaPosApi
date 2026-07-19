-- Conduces de entrega (warehouse delivery notes) — AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

IF OBJECT_ID('dbo.ConduceDetalle', 'U') IS NOT NULL
    DROP TABLE dbo.ConduceDetalle;
GO

IF OBJECT_ID('dbo.ConduceHeader', 'U') IS NOT NULL
    DROP TABLE dbo.ConduceHeader;
GO

CREATE TABLE dbo.ConduceHeader (
    IdConduceHeader INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdFacturaHeader INT NOT NULL,
    Numero NVARCHAR(50) NOT NULL,
    Fecha DATETIME NOT NULL CONSTRAINT DF_ConduceHeader_Fecha DEFAULT (GETDATE()),
    QuienEntrega NVARCHAR(150) NULL,
    QuienRecibe NVARCHAR(150) NULL,
    Observacion NVARCHAR(MAX) NULL,
    IdAlmacen INT NULL,
    IdUsuario INT NULL,
    IdEmpresa INT NOT NULL,
    Activo BIT NOT NULL CONSTRAINT DF_ConduceHeader_Activo DEFAULT (1),
    FechaInseccion DATETIME NOT NULL CONSTRAINT DF_ConduceHeader_FechaInseccion DEFAULT (GETDATE()),
    CONSTRAINT FK_ConduceHeader_Factura
        FOREIGN KEY (IdFacturaHeader) REFERENCES dbo.FacturaHeaders(IdFacturaHeader)
);
GO

CREATE INDEX IX_ConduceHeader_Empresa ON dbo.ConduceHeader(IdEmpresa);
CREATE INDEX IX_ConduceHeader_Factura ON dbo.ConduceHeader(IdFacturaHeader);
CREATE INDEX IX_ConduceHeader_Fecha ON dbo.ConduceHeader(IdEmpresa, Fecha);
GO

CREATE TABLE dbo.ConduceDetalle (
    IdConduceDetalle INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdConduceHeader INT NOT NULL,
    IdFacturaDetalle INT NOT NULL,
    IdProducto INT NOT NULL,
    CantidadEntregada DECIMAL(18,4) NOT NULL,
    FechaInseccion DATETIME NOT NULL CONSTRAINT DF_ConduceDetalle_FechaInseccion DEFAULT (GETDATE()),
    IdEmpresa INT NOT NULL CONSTRAINT DF_ConduceDetalle_IdEmpresa DEFAULT (0),
    CONSTRAINT FK_ConduceDetalle_Header
        FOREIGN KEY (IdConduceHeader) REFERENCES dbo.ConduceHeader(IdConduceHeader),
    CONSTRAINT FK_ConduceDetalle_FacturaDet
        FOREIGN KEY (IdFacturaDetalle) REFERENCES dbo.FacturaDetalles(IdFacturaDetalle)
);
GO

CREATE INDEX IX_ConduceDetalle_Header ON dbo.ConduceDetalle(IdConduceHeader);
CREATE INDEX IX_ConduceDetalle_FacturaDet ON dbo.ConduceDetalle(IdFacturaDetalle);
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CONDUCES')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES ('CONDUCES', 'Conduces', 'Notas de entrega de mercancía vinculadas a facturas', 0, 1, GETDATE());
GO

INSERT INTO Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, m.Id, 1, GETDATE()
FROM Empresas e
CROSS JOIN Modulos m
WHERE m.Codigo = 'CONDUCES'
  AND NOT EXISTS (
      SELECT 1 FROM Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = m.Id
  );
GO

PRINT 'Conduces: tablas y módulo CONDUCES listos.';
GO
