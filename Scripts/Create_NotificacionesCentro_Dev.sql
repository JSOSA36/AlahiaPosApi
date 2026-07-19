-- ============================================================
-- Centro de Notificaciones — AlahiaPos_Dev (v2: destino/prioridad/archivo)
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notificaciones (
        IdNotificacion      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notificaciones PRIMARY KEY,
        IdEmpresa           INT NOT NULL,
        DestinoTipo         NVARCHAR(20)  NOT NULL CONSTRAINT DF_Notif_DestinoTipo DEFAULT (N'EMPRESA'),
        IdUsuarioDestino    INT NULL,
        IdRolDestino        INT NULL,
        RolCodigo           NVARCHAR(60)  NULL,
        Tipo                NVARCHAR(60)  NOT NULL,
        Prioridad           NVARCHAR(20)  NOT NULL CONSTRAINT DF_Notif_Prioridad DEFAULT (N'INFO'),
        Titulo              NVARCHAR(200) NOT NULL,
        Mensaje             NVARCHAR(500) NOT NULL,
        Ruta                NVARCHAR(200) NULL,
        ReferenciaTipo      NVARCHAR(60)  NULL,
        ReferenciaId        INT NULL,
        MetadataJson        NVARCHAR(MAX) NULL,
        Leida               BIT NOT NULL CONSTRAINT DF_Notificaciones_Leida DEFAULT (0),
        Archivada           BIT NOT NULL CONSTRAINT DF_Notificaciones_Archivada DEFAULT (0),
        FechaCreacion       DATETIME NOT NULL CONSTRAINT DF_Notificaciones_Fecha DEFAULT (GETDATE()),
        FechaLeida          DATETIME NULL,
        FechaArchivada      DATETIME NULL
    );

    CREATE INDEX IX_Notificaciones_Empresa_Leida
        ON dbo.Notificaciones (IdEmpresa, Leida, Archivada, FechaCreacion DESC);
    CREATE INDEX IX_Notificaciones_Usuario_Leida
        ON dbo.Notificaciones (IdUsuarioDestino, Leida, Archivada);
END
GO

-- Alteraciones si la tabla ya existía (MVP v1)
IF COL_LENGTH(N'dbo.Notificaciones', N'DestinoTipo') IS NULL
    ALTER TABLE dbo.Notificaciones ADD DestinoTipo NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Notif_DestinoTipo2 DEFAULT (N'EMPRESA');
GO
IF COL_LENGTH(N'dbo.Notificaciones', N'IdRolDestino') IS NULL
    ALTER TABLE dbo.Notificaciones ADD IdRolDestino INT NULL;
GO
IF COL_LENGTH(N'dbo.Notificaciones', N'RolCodigo') IS NULL
    ALTER TABLE dbo.Notificaciones ADD RolCodigo NVARCHAR(60) NULL;
GO
IF COL_LENGTH(N'dbo.Notificaciones', N'Prioridad') IS NULL
    ALTER TABLE dbo.Notificaciones ADD Prioridad NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Notif_Prioridad2 DEFAULT (N'INFO');
GO
IF COL_LENGTH(N'dbo.Notificaciones', N'Archivada') IS NULL
    ALTER TABLE dbo.Notificaciones ADD Archivada BIT NOT NULL
        CONSTRAINT DF_Notificaciones_Archivada2 DEFAULT (0);
GO
IF COL_LENGTH(N'dbo.Notificaciones', N'FechaArchivada') IS NULL
    ALTER TABLE dbo.Notificaciones ADD FechaArchivada DATETIME NULL;
GO

UPDATE dbo.Notificaciones
SET DestinoTipo = CASE WHEN IdUsuarioDestino IS NOT NULL THEN N'USUARIO' ELSE N'EMPRESA' END
WHERE DestinoTipo IS NULL OR DestinoTipo = N'';
GO

IF OBJECT_ID(N'dbo.NotificacionCanalLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotificacionCanalLog (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotificacionCanalLog PRIMARY KEY,
        IdNotificacion      INT NOT NULL,
        Canal               NVARCHAR(40) NOT NULL,
        FechaEnvio          DATETIME NOT NULL CONSTRAINT DF_NotifCanalLog_Fecha DEFAULT (GETDATE()),
        Exito               BIT NOT NULL CONSTRAINT DF_NotifCanalLog_Exito DEFAULT (1),
        Detalle             NVARCHAR(500) NULL,
        CONSTRAINT FK_NotifCanalLog_Notif FOREIGN KEY (IdNotificacion)
            REFERENCES dbo.Notificaciones(IdNotificacion)
    );
END
GO

-- Migrar no leídas de TicketNotificaciones
IF OBJECT_ID(N'dbo.TicketNotificaciones', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.Notificaciones (
        IdEmpresa, DestinoTipo, IdUsuarioDestino, Tipo, Prioridad, Titulo, Mensaje, Ruta,
        ReferenciaTipo, ReferenciaId, Leida, Archivada, FechaCreacion
    )
    SELECT
        t.IdEmpresa,
        CASE WHEN t.IdUsuarioDestino IS NOT NULL THEN N'USUARIO' ELSE N'EMPRESA' END,
        t.IdUsuarioDestino,
        CASE
            WHEN t.Tipo = N'NUEVO' THEN N'TICKET_NUEVO'
            WHEN t.Tipo IN (N'RESPUESTA', N'RESPUESTA_CLIENTE') THEN N'TICKET_NUEVO_MENSAJE'
            WHEN t.Tipo IN (N'ESTADO', N'RESUELTO', N'CERRADO', N'REABIERTO') THEN N'TICKET_ESTADO_CAMBIADO'
            ELSE N'TICKET_NUEVO_MENSAJE'
        END,
        N'INFO',
        t.Titulo,
        t.Mensaje,
        N'/tickets',
        N'Ticket',
        t.IdTicket,
        0,
        0,
        t.FechaCreacion
    FROM dbo.TicketNotificaciones t
    WHERE t.Leida = 0
      AND NOT EXISTS (
          SELECT 1 FROM dbo.Notificaciones n
          WHERE n.ReferenciaTipo = N'Ticket'
            AND n.ReferenciaId = t.IdTicket
            AND n.Titulo = t.Titulo
            AND n.FechaCreacion = t.FechaCreacion
      );
END
GO

PRINT 'Centro de Notificaciones v2 listo en AlahiaPos_Dev.';
GO
