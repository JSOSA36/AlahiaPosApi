-- ============================================================
-- SecuenciaECFAsignacion — rangos de la empresa asignados a sucursal.
-- SecuenciasECF = autorización DGII (empresa).
-- Asignacion = qué sucursal usa qué tramo; los números no se repiten.
-- Base: AlahiaPos_Dev
-- ============================================================
USE AlahiaPos_Dev;
GO

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR('Este script es solo para AlahiaPos_Dev. Abortado.', 16, 1);
    RETURN;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.SecuenciasECF', N'U') IS NULL
BEGIN
    RAISERROR('No existe dbo.SecuenciasECF. Abortado.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH(N'dbo.SecuenciasECF', N'IdSucursal') IS NULL
BEGIN
    ALTER TABLE dbo.SecuenciasECF
        ADD IdSucursal INT NULL;
END
GO

IF OBJECT_ID(N'dbo.SecuenciaECFAsignacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SecuenciaECFAsignacion
    (
        IdAsignacion      INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_SecuenciaECFAsignacion PRIMARY KEY,
        IdSecuencia       INT NOT NULL,
        IdEmpresa         INT NOT NULL,
        TipoEcfDgii       INT NOT NULL,
        IdSucursal        INT NOT NULL,
        SecuenciaInicial  INT NOT NULL,
        SecuenciaFinal    INT NOT NULL,
        SecuenciaActual   INT NOT NULL,
        Activo            BIT NOT NULL
            CONSTRAINT DF_SecuenciaECFAsignacion_Activo DEFAULT (1),
        FechaCreacion     DATETIME NOT NULL
            CONSTRAINT DF_SecuenciaECFAsignacion_FechaCreacion DEFAULT (GETDATE()),
        CONSTRAINT FK_SecuenciaECFAsignacion_Secuencia
            FOREIGN KEY (IdSecuencia) REFERENCES dbo.SecuenciasECF (IdSecuencia),
        CONSTRAINT FK_SecuenciaECFAsignacion_Sucursal
            FOREIGN KEY (IdSucursal) REFERENCES dbo.Sucursal (IdSucursal),
        CONSTRAINT FK_SecuenciaECFAsignacion_Empresa
            FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas (IdEmpresa),
        CONSTRAINT CK_SecuenciaECFAsignacion_Rango
            CHECK (SecuenciaInicial >= 1 AND SecuenciaFinal >= SecuenciaInicial
               AND SecuenciaActual >= SecuenciaInicial AND SecuenciaActual <= SecuenciaFinal + 1)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_SecuenciaECFAsignacion_Empresa_Tipo_Sucursal_Activa'
      AND object_id = OBJECT_ID(N'dbo.SecuenciaECFAsignacion')
)
BEGIN
    CREATE UNIQUE INDEX UX_SecuenciaECFAsignacion_Empresa_Tipo_Sucursal_Activa
        ON dbo.SecuenciaECFAsignacion (IdEmpresa, TipoEcfDgii, IdSucursal)
        WHERE Activo = 1;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SecuenciaECFAsignacion_Secuencia'
      AND object_id = OBJECT_ID(N'dbo.SecuenciaECFAsignacion')
)
BEGIN
    CREATE INDEX IX_SecuenciaECFAsignacion_Secuencia
        ON dbo.SecuenciaECFAsignacion (IdSecuencia, Activo);
END
GO

-- Si el modelo anterior puso IdSucursal en la autorización, pasar ese tramo a asignación.
IF COL_LENGTH(N'dbo.SecuenciasECF', N'IdSucursal') IS NOT NULL
BEGIN
    INSERT INTO dbo.SecuenciaECFAsignacion
        (IdSecuencia, IdEmpresa, TipoEcfDgii, IdSucursal,
         SecuenciaInicial, SecuenciaFinal, SecuenciaActual, Activo, FechaCreacion)
    SELECT
        s.IdSecuencia,
        s.IdEmpresa,
        s.TipoEcfDgii,
        s.IdSucursal,
        CASE WHEN s.SecuenciaInicial < 1 THEN 1 ELSE s.SecuenciaInicial END,
        s.SecuenciaFinal,
        CASE
            WHEN s.SecuenciaActual < CASE WHEN s.SecuenciaInicial < 1 THEN 1 ELSE s.SecuenciaInicial END
                THEN CASE WHEN s.SecuenciaInicial < 1 THEN 1 ELSE s.SecuenciaInicial END
            WHEN s.SecuenciaActual > s.SecuenciaFinal + 1 THEN s.SecuenciaFinal + 1
            ELSE s.SecuenciaActual
        END,
        s.Activo,
        GETDATE()
    FROM dbo.SecuenciasECF s
    WHERE s.IdSucursal IS NOT NULL
      AND s.TipoEcfDgii IS NOT NULL
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.SecuenciaECFAsignacion a
            WHERE a.IdSecuencia = s.IdSecuencia
              AND a.IdSucursal = s.IdSucursal
      )
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.SecuenciaECFAsignacion a2
            WHERE a2.Activo = 1
              AND s.Activo = 1
              AND a2.IdEmpresa = s.IdEmpresa
              AND a2.TipoEcfDgii = s.TipoEcfDgii
              AND a2.IdSucursal = s.IdSucursal
      );

    UPDATE dbo.SecuenciasECF
    SET IdSucursal = NULL
    WHERE IdSucursal IS NOT NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_SecuenciasECF_Empresa_Tipo_Sucursal_Activa'
      AND object_id = OBJECT_ID(N'dbo.SecuenciasECF')
)
BEGIN
    DROP INDEX UX_SecuenciasECF_Empresa_Tipo_Sucursal_Activa ON dbo.SecuenciasECF;
END
GO

PRINT 'SecuenciaECFAsignacion lista en AlahiaPos_Dev.';
GO
