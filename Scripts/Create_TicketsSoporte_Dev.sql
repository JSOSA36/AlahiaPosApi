-- ============================================================
-- Tickets de Soporte — AlahiaPos_Dev
-- Tablas + módulos TICKETS (clientes) y TICKETS_ADMIN (MacroBits)
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

-- ---------- Tickets ----------
IF OBJECT_ID(N'dbo.Tickets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tickets (
        IdTicket            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tickets PRIMARY KEY,
        Numero              NVARCHAR(30)  NOT NULL,
        IdEmpresa           INT NOT NULL,
        IdUsuarioCrea       INT NOT NULL,
        NombreCliente       NVARCHAR(200) NULL,
        Asunto              NVARCHAR(250) NOT NULL,
        Descripcion         NVARCHAR(MAX) NOT NULL,
        Categoria           NVARCHAR(60)  NOT NULL,
        Prioridad           NVARCHAR(20)  NOT NULL CONSTRAINT DF_Tickets_Prioridad DEFAULT (N'MEDIA'),
        Estado              NVARCHAR(30)  NOT NULL CONSTRAINT DF_Tickets_Estado DEFAULT (N'ABIERTO'),
        VersionSistema      NVARCHAR(40)  NULL,
        Dispositivo         NVARCHAR(80)  NULL,
        Navegador           NVARCHAR(120) NULL,
        SistemaOperativo    NVARCHAR(120) NULL,
        FechaUltimaRespuestaAdmin DATETIME NULL,
        FechaResolucion     DATETIME NULL,
        FechaCierre         DATETIME NULL,
        FechaCreacion       DATETIME NOT NULL CONSTRAINT DF_Tickets_FechaCreacion DEFAULT (GETDATE()),
        FechaActualizacion  DATETIME NOT NULL CONSTRAINT DF_Tickets_FechaActualizacion DEFAULT (GETDATE()),
        CONSTRAINT UQ_Tickets_Numero UNIQUE (Numero)
    );

    CREATE INDEX IX_Tickets_IdEmpresa_Estado ON dbo.Tickets (IdEmpresa, Estado);
    CREATE INDEX IX_Tickets_Estado_Prioridad ON dbo.Tickets (Estado, Prioridad);
    CREATE INDEX IX_Tickets_FechaCreacion ON dbo.Tickets (FechaCreacion DESC);
END
GO

-- ---------- Mensajes ----------
IF OBJECT_ID(N'dbo.TicketMensajes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketMensajes (
        IdMensaje           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TicketMensajes PRIMARY KEY,
        IdTicket            INT NOT NULL,
        IdUsuario           INT NOT NULL,
        EsRespuestaAdmin    BIT NOT NULL CONSTRAINT DF_TicketMensajes_Admin DEFAULT (0),
        Mensaje             NVARCHAR(MAX) NOT NULL,
        FechaCreacion       DATETIME NOT NULL CONSTRAINT DF_TicketMensajes_Fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_TicketMensajes_Ticket FOREIGN KEY (IdTicket) REFERENCES dbo.Tickets(IdTicket)
    );

    CREATE INDEX IX_TicketMensajes_IdTicket ON dbo.TicketMensajes (IdTicket, FechaCreacion);
END
GO

-- ---------- Adjuntos ----------
IF OBJECT_ID(N'dbo.TicketAdjuntos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketAdjuntos (
        IdAdjunto           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TicketAdjuntos PRIMARY KEY,
        IdTicket            INT NOT NULL,
        IdMensaje           INT NULL,
        NombreArchivo       NVARCHAR(260) NOT NULL,
        Url                 NVARCHAR(500) NOT NULL,
        ContentType         NVARCHAR(120) NULL,
        TamanoBytes         BIGINT NULL,
        FechaCreacion       DATETIME NOT NULL CONSTRAINT DF_TicketAdjuntos_Fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_TicketAdjuntos_Ticket FOREIGN KEY (IdTicket) REFERENCES dbo.Tickets(IdTicket),
        CONSTRAINT FK_TicketAdjuntos_Mensaje FOREIGN KEY (IdMensaje) REFERENCES dbo.TicketMensajes(IdMensaje)
    );

    CREATE INDEX IX_TicketAdjuntos_IdTicket ON dbo.TicketAdjuntos (IdTicket);
    CREATE INDEX IX_TicketAdjuntos_IdMensaje ON dbo.TicketAdjuntos (IdMensaje);
END
GO

-- ---------- Notificaciones in-app ----------
IF OBJECT_ID(N'dbo.TicketNotificaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketNotificaciones (
        IdNotificacion      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TicketNotificaciones PRIMARY KEY,
        IdEmpresa           INT NOT NULL,
        IdUsuarioDestino    INT NULL,
        IdTicket            INT NOT NULL,
        Tipo                NVARCHAR(40) NOT NULL,
        Titulo              NVARCHAR(200) NOT NULL,
        Mensaje             NVARCHAR(500) NOT NULL,
        Leida               BIT NOT NULL CONSTRAINT DF_TicketNotif_Leida DEFAULT (0),
        FechaCreacion       DATETIME NOT NULL CONSTRAINT DF_TicketNotif_Fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_TicketNotif_Ticket FOREIGN KEY (IdTicket) REFERENCES dbo.Tickets(IdTicket)
    );

    CREATE INDEX IX_TicketNotif_Empresa_Leida ON dbo.TicketNotificaciones (IdEmpresa, Leida, FechaCreacion DESC);
END
GO

-- ---------- Secuencia número ticket ----------
IF OBJECT_ID(N'dbo.TicketSecuencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketSecuencia (
        Anio        INT NOT NULL CONSTRAINT PK_TicketSecuencia PRIMARY KEY,
        Ultimo      INT NOT NULL CONSTRAINT DF_TicketSecuencia_Ultimo DEFAULT (0)
    );
END
GO

-- ============================================================
-- Módulo TICKETS (clientes, no sistema)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'TICKETS')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'TICKETS', N'Tickets de Soporte', N'Reportar incidencias y dar seguimiento a tickets de soporte', 0, 1, GETDATE());
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Nombre = N'Tickets de Soporte',
        Descripcion = N'Reportar incidencias y dar seguimiento a tickets de soporte',
        Activo = 1
    WHERE Codigo = N'TICKETS';
END
GO

DECLARE @IdTickets INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'TICKETS');

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, @IdTickets, 1, GETDATE()
FROM dbo.Empresas e
WHERE ISNULL(e.EsEmpresaSistema, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdTickets
  );

UPDATE em SET Activo = 1
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Empresas e ON e.IdEmpresa = em.EmpresaId
WHERE em.ModuloId = @IdTickets AND ISNULL(e.EsEmpresaSistema, 0) = 0;

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdTickets, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE ISNULL(e.EsEmpresaSistema, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdTickets AND pr.IdEmpresa = p.IdEmpresa
  );

UPDATE pr SET Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Empresas e ON e.IdEmpresa = pr.IdEmpresa
WHERE pr.IdModulo = @IdTickets AND ISNULL(e.EsEmpresaSistema, 0) = 0;
GO

-- ============================================================
-- Módulo TICKETS_ADMIN (solo MacroBits / empresa sistema)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'TICKETS_ADMIN')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'TICKETS_ADMIN', N'Tickets (Admin)', N'Inbox de soporte MacroBits: seguimiento de todos los tickets', 0, 1, GETDATE());
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Nombre = N'Tickets (Admin)',
        Descripcion = N'Inbox de soporte MacroBits: seguimiento de todos los tickets',
        Activo = 1
    WHERE Codigo = N'TICKETS_ADMIN';
END
GO

DECLARE @IdAdmin INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'TICKETS_ADMIN');

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, @IdAdmin, 1, GETDATE()
FROM dbo.Empresas e
WHERE ISNULL(e.EsEmpresaSistema, 0) = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdAdmin
  );

UPDATE em SET Activo = 1, FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Empresas e ON e.IdEmpresa = em.EmpresaId
WHERE em.ModuloId = @IdAdmin AND ISNULL(e.EsEmpresaSistema, 0) = 1;

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdAdmin, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE ISNULL(e.EsEmpresaSistema, 0) = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdAdmin AND pr.IdEmpresa = p.IdEmpresa
  );

UPDATE pr SET Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Empresas e ON e.IdEmpresa = pr.IdEmpresa
WHERE pr.IdModulo = @IdAdmin AND ISNULL(e.EsEmpresaSistema, 0) = 1;
GO

SELECT m.Codigo, m.Nombre,
    (SELECT COUNT(*) FROM dbo.Empresa_Modulos WHERE ModuloId = m.Id AND Activo = 1) AS Empresas,
    (SELECT COUNT(*) FROM dbo.PerfilRoles WHERE IdModulo = m.Id AND Activo = 1) AS Perfiles
FROM dbo.Modulos m
WHERE m.Codigo IN (N'TICKETS', N'TICKETS_ADMIN');

PRINT 'Tickets de Soporte listo en AlahiaPos_Dev.';
GO
