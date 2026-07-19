-- ============================================================
-- Cargos recurrentes SaaS — AlahiaPos_Dev
-- Plan + Σ(EmpresaCargoRecurrente activos) = SuscripcionCiclo.Monto
-- Independiente de Empresa_Modulos (licencia)
-- ============================================================
USE AlahiaPos_Dev;
GO

IF OBJECT_ID('dbo.EmpresaCargoRecurrente') IS NULL
BEGIN
    CREATE TABLE dbo.EmpresaCargoRecurrente
    (
        Id                    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmpresaCargoRecurrente PRIMARY KEY,
        IdEmpresa             INT NOT NULL,
        TipoCargo             NVARCHAR(40) NOT NULL,
        -- MODULO | USUARIOS | ALMACENAMIENTO | INTEGRACION | SERVICIO | OTRO
        IdModulo              INT NULL,
        Codigo                NVARCHAR(80) NOT NULL,
        Nombre                NVARCHAR(200) NOT NULL,
        MontoMensual          DECIMAL(18,2) NOT NULL CONSTRAINT DF_ECR_Monto DEFAULT(0),
        FechaInicio           DATETIME NOT NULL CONSTRAINT DF_ECR_Inicio DEFAULT(GETDATE()),
        FechaFin              DATETIME NULL,
        Activo                BIT NOT NULL CONSTRAINT DF_ECR_Activo DEFAULT(1),
        Observacion           NVARCHAR(500) NULL,
        IdUsuarioCreacion     INT NULL,
        FechaCreacion         DATETIME NOT NULL CONSTRAINT DF_ECR_Creacion DEFAULT(GETDATE()),
        IdUsuarioModificacion INT NULL,
        FechaModificacion     DATETIME NULL
    );

    CREATE INDEX IX_EmpresaCargoRecurrente_Empresa
        ON dbo.EmpresaCargoRecurrente (IdEmpresa, Activo, FechaInicio, FechaFin);

    CREATE INDEX IX_EmpresaCargoRecurrente_Tipo
        ON dbo.EmpresaCargoRecurrente (TipoCargo, IdEmpresa);
END
GO

IF OBJECT_ID('dbo.SuscripcionCicloDetalle') IS NULL
BEGIN
    CREATE TABLE dbo.SuscripcionCicloDetalle
    (
        Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SuscripcionCicloDetalle PRIMARY KEY,
        IdCiclo    INT NOT NULL,
        TipoLinea  NVARCHAR(40) NOT NULL,
        -- PLAN | MODULO | USUARIOS | ALMACENAMIENTO | INTEGRACION | SERVICIO | OTRO
        IdCargo    INT NULL,
        IdModulo   INT NULL,
        Codigo     NVARCHAR(80) NULL,
        Nombre     NVARCHAR(200) NOT NULL,
        Monto      DECIMAL(18,2) NOT NULL CONSTRAINT DF_SCD_Monto DEFAULT(0),
        CONSTRAINT FK_SuscripcionCicloDetalle_Ciclo
            FOREIGN KEY (IdCiclo) REFERENCES dbo.SuscripcionCiclo(IdCiclo)
    );

    CREATE INDEX IX_SuscripcionCicloDetalle_Ciclo
        ON dbo.SuscripcionCicloDetalle (IdCiclo);
END
GO

PRINT 'Create_EmpresaCargoRecurrente_Dev OK';
GO
