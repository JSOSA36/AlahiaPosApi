-- AlahiaPos_Dev — Categorías de gasto + campos de comprobante en Gastos
-- Solo Desarrollo

USE AlahiaPos_Dev;
GO

IF OBJECT_ID(N'dbo.CategoriasGasto', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CategoriasGasto (
    IdCategoriaGasto INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CategoriasGasto PRIMARY KEY,
    IdEmpresa INT NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    Activo BIT NOT NULL CONSTRAINT DF_CategoriasGasto_Activo DEFAULT (1),
    Orden INT NOT NULL CONSTRAINT DF_CategoriasGasto_Orden DEFAULT (0),
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_CategoriasGasto_Fecha DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_CategoriasGasto_Empresa_Nombre UNIQUE (IdEmpresa, Nombre)
  );

  CREATE INDEX IX_CategoriasGasto_Empresa_Activo
    ON dbo.CategoriasGasto (IdEmpresa, Activo, Orden);
END
GO

IF COL_LENGTH('dbo.Gastos', 'IdCategoriaGasto') IS NULL
  ALTER TABLE dbo.Gastos ADD IdCategoriaGasto INT NULL;
GO

IF COL_LENGTH('dbo.Gastos', 'TipoComprobante') IS NULL
  ALTER TABLE dbo.Gastos ADD TipoComprobante NVARCHAR(80) NULL;
GO

IF COL_LENGTH('dbo.Gastos', 'NumeroComprobante') IS NULL
  ALTER TABLE dbo.Gastos ADD NumeroComprobante NVARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.Gastos', 'FechaComprobante') IS NULL
  ALTER TABLE dbo.Gastos ADD FechaComprobante DATE NULL;
GO

IF COL_LENGTH('dbo.Gastos', 'RncEmisorComprobante') IS NULL
  ALTER TABLE dbo.Gastos ADD RncEmisorComprobante NVARCHAR(20) NULL;
GO

IF COL_LENGTH('dbo.Gastos', 'NombreEmisorComprobante') IS NULL
  ALTER TABLE dbo.Gastos ADD NombreEmisorComprobante NVARCHAR(150) NULL;
GO

IF NOT EXISTS (
  SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Gastos_CategoriasGasto'
)
AND COL_LENGTH('dbo.Gastos', 'IdCategoriaGasto') IS NOT NULL
BEGIN
  ALTER TABLE dbo.Gastos WITH NOCHECK
  ADD CONSTRAINT FK_Gastos_CategoriasGasto
    FOREIGN KEY (IdCategoriaGasto) REFERENCES dbo.CategoriasGasto (IdCategoriaGasto);
END
GO
