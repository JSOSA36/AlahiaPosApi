-- Sprint B.1 — endurecimiento fiscal (solo AlahiaPos_Dev)
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
PRINT '=== Sprint B.1 START ===';
PRINT 'DB: ' + DB_NAME();

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'ABORT: solo AlahiaPos_Dev.', 16, 1);
    RETURN;
END
GO

SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.EventosOutbox', 'IdempotencyKey') IS NULL
    ALTER TABLE dbo.EventosOutbox ADD IdempotencyKey NVARCHAR(200) NULL;
GO
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH('dbo.EventosOutbox', 'LockedUntil') IS NULL
    ALTER TABLE dbo.EventosOutbox ADD LockedUntil DATETIME NULL;
GO
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH('dbo.EventosOutbox', 'LockedBy') IS NULL
    ALTER TABLE dbo.EventosOutbox ADD LockedBy NVARCHAR(100) NULL;
GO

SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_EventosOutbox_FiscalClaim' AND object_id = OBJECT_ID(N'dbo.EventosOutbox'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EventosOutbox_FiscalClaim
        ON dbo.EventosOutbox (TipoEvento, Estado, FechaCreacion)
        INCLUDE (IdEmpresa, ReferenciaId, Intentos, LockedUntil, IdempotencyKey);
END
GO

SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_EventosOutbox_IdempotencyActivo' AND object_id = OBJECT_ID(N'dbo.EventosOutbox'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_EventosOutbox_IdempotencyActivo
        ON dbo.EventosOutbox (IdempotencyKey)
        WHERE IdempotencyKey IS NOT NULL
          AND Estado IN (N'Pendiente', N'Procesando');
END
GO

IF OBJECT_ID(N'dbo.DgiiConfiguracionAuditoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DgiiConfiguracionAuditoria (
        IdAuditoria       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DgiiConfigAud PRIMARY KEY,
        IdEmpresa         INT NOT NULL,
        IdUsuario         INT NOT NULL,
        Fecha             DATETIME NOT NULL CONSTRAINT DF_DgiiConfigAud_Fecha DEFAULT (GETDATE()),
        Accion            NVARCHAR(40) NOT NULL,
        ValorAnteriorJson NVARCHAR(MAX) NULL,
        ValorNuevoJson    NVARCHAR(MAX) NULL,
        Motivo            NVARCHAR(500) NULL
    );
    PRINT 'Creada DgiiConfiguracionAuditoria';
END
GO

PRINT '=== Sprint B.1 DONE ===';
GO
