-- ============================================================
-- Políticas del Servicio versionadas — solo AlahiaPos_Dev
-- ============================================================
USE AlahiaPos_Dev;
GO

IF OBJECT_ID('dbo.PoliticasVersion') IS NULL
BEGIN
    CREATE TABLE dbo.PoliticasVersion
    (
        IdVersion              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PoliticasVersion PRIMARY KEY,
        NumeroVersion          NVARCHAR(20)  NOT NULL,
        Titulo                 NVARCHAR(200) NOT NULL,
        Contenido              NVARCHAR(MAX) NOT NULL,
        Estado                 NVARCHAR(20)  NOT NULL CONSTRAINT DF_PV_Estado DEFAULT(N'Borrador'),
        -- Borrador | Publicada | Archivada
        FechaCreacion          DATETIME NOT NULL CONSTRAINT DF_PV_FechaCreacion DEFAULT(GETDATE()),
        IdUsuarioCreacion      INT NULL,
        FechaPublicacion       DATETIME NULL,
        IdUsuarioPublicacion   INT NULL,
        Notas                  NVARCHAR(500) NULL,
        CONSTRAINT UQ_PoliticasVersion_Numero UNIQUE (NumeroVersion)
    );

    CREATE INDEX IX_PoliticasVersion_Estado
        ON dbo.PoliticasVersion (Estado, FechaPublicacion DESC);
END
GO

IF OBJECT_ID('dbo.PoliticasAceptacion') IS NULL
BEGIN
    CREATE TABLE dbo.PoliticasAceptacion
    (
        IdAceptacion           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PoliticasAceptacion PRIMARY KEY,
        IdVersion              INT NOT NULL,
        IdEmpresa              INT NOT NULL,
        IdUsuario              INT NOT NULL,
        FechaAceptacion        DATETIME NOT NULL CONSTRAINT DF_PA_Fecha DEFAULT(GETDATE()),
        DireccionIp            NVARCHAR(64) NULL,
        Navegador              NVARCHAR(500) NULL,
        SistemaOperativo       NVARCHAR(200) NULL,
        CorreoEnviado          BIT NOT NULL CONSTRAINT DF_PA_CorreoEnviado DEFAULT(0),
        FechaCorreoEnviado     DATETIME NULL,
        CONSTRAINT UQ_PoliticasAceptacion_EmpresaVersion UNIQUE (IdEmpresa, IdVersion),
        CONSTRAINT FK_PA_Version FOREIGN KEY (IdVersion) REFERENCES dbo.PoliticasVersion(IdVersion)
    );

    CREATE INDEX IX_PoliticasAceptacion_Empresa
        ON dbo.PoliticasAceptacion (IdEmpresa, FechaAceptacion DESC);

    CREATE INDEX IX_PoliticasAceptacion_Version
        ON dbo.PoliticasAceptacion (IdVersion);
END
GO

-- Módulo admin MacroBits (NO se asigna a todas las empresas)
IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = N'MACROBITS_ADMIN')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (
        N'MACROBITS_ADMIN',
        N'Administración MacroBits',
        N'Gestión de políticas del servicio y herramientas internas MacroBits',
        0, 1, GETDATE()
    );
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = N'POLITICAS_VERSIONES')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (
        N'POLITICAS_VERSIONES',
        N'Políticas del Servicio',
        N'Administrar versiones de políticas del servicio',
        0, 1, GETDATE()
    );
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = N'POLITICAS_ACEPTACIONES')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (
        N'POLITICAS_ACEPTACIONES',
        N'Aceptaciones de Políticas',
        N'Consulta de aceptaciones de políticas por empresa',
        0, 1, GETDATE()
    );
GO

-- Seed v1.0 publicada (contenido exacto)
IF NOT EXISTS (SELECT 1 FROM dbo.PoliticasVersion WHERE NumeroVersion = N'1.0')
BEGIN
    INSERT INTO dbo.PoliticasVersion
        (NumeroVersion, Titulo, Contenido, Estado, FechaCreacion, FechaPublicacion, Notas)
    VALUES
    (
        N'1.0',
        N'Políticas del Servicio - Alahia ERP',
        N'Las presentes políticas aplican a todos los productos y servicios ofrecidos por MacroBits SRL, incluyendo Alahia ERP, implementaciones, certificaciones de Facturación Electrónica (e-CF), desarrollos personalizados, consultorías, capacitaciones y cualquier otro servicio contratado, salvo que exista un contrato específico que establezca condiciones diferentes.

---------------------------------------------------------
0. Suspensión del Servicio y Cargos (Importante)
---------------------------------------------------------

Para garantizar la continuidad y calidad del servicio, MacroBits SRL podrá suspender temporalmente el acceso a Alahia ERP o a cualquiera de sus servicios cuando ocurra alguna de las siguientes situaciones:

• Incumplimiento en los pagos.
• Uso indebido del sistema.
• Incumplimiento de estas políticas.
• Actividades que comprometan la seguridad del servicio.
• Uso de módulos o funcionalidades no contratadas.
• Cualquier otra situación que represente un riesgo para MacroBits.

Durante el período de suspensión el cliente no podrá acceder al sistema hasta regularizar la situación.

Los siguientes servicios podrán generar cargos adicionales:

• Implementaciones.
• Configuraciones especiales.
• Migración de información.
• Visitas presenciales.
• Capacitaciones personalizadas.
• Integraciones.
• Desarrollos.
• Corrección de errores ocasionados por el cliente.
• Reprocesos.
• Recuperación de información.
• Servicios fuera del alcance contratado.

MacroBits notificará previamente cualquier costo adicional.

La suspensión por falta de pago no elimina las obligaciones económicas pendientes ni genera derecho a descuentos, devoluciones o ampliaciones del período contratado.

---------------------------------------------------------
1. Plan Contratado
---------------------------------------------------------

El cliente tendrá acceso únicamente a las funcionalidades y servicios incluidos en el plan, propuesta comercial o cotización aceptada.

---------------------------------------------------------
2. Funcionalidades Contratadas
---------------------------------------------------------

Alahia ERP es una plataforma modular.

Cada cliente tendrá acceso únicamente a los módulos contratados.

Las funcionalidades no contratadas serán consideradas servicios adicionales y podrán requerir una nueva cotización.

La existencia de un módulo dentro del ERP no implica que forme parte del servicio contratado.

---------------------------------------------------------
3. Implementación
---------------------------------------------------------

Los servicios de implementación, configuración inicial, migración de datos y puesta en marcha podrán cotizarse de forma independiente.

---------------------------------------------------------
4. Capacitación
---------------------------------------------------------

La plataforma incluye videos y material de apoyo.

Las capacitaciones personalizadas serán cotizadas por separado.

---------------------------------------------------------
5. Soporte Técnico
---------------------------------------------------------

El soporte cubre únicamente el funcionamiento de Alahia ERP.

No incluye:

• Equipos.
• Redes.
• Internet.
• Impresoras.
• Software de terceros.

El soporte se brinda exclusivamente de forma remota.

Las visitas presenciales serán consideradas servicios independientes y se cotizarán por separado.

Los gastos de traslado y viáticos serán asumidos por el cliente.

---------------------------------------------------------
6. Conectividad (Importante)
---------------------------------------------------------

Alahia ERP requiere una conexión a Internet estable y de buena calidad.

Es responsabilidad del cliente disponer de:

• Internet estable.
• Equipos adecuados.
• Energía eléctrica.
• Infraestructura tecnológica funcional.

Las fallas ocasionadas por Internet, energía, equipos del cliente o terceros no se consideran fallas del servicio.

---------------------------------------------------------
7. Seguridad y Confidencialidad
---------------------------------------------------------

MacroBits protegerá la información del cliente utilizando medidas razonables de seguridad.

La información no será compartida salvo obligación legal o autorización expresa.

---------------------------------------------------------
8. Política de Pagos y No Reembolsos (Importante)
---------------------------------------------------------

Todos los pagos realizados a MacroBits SRL son definitivos y no son reembolsables.

Aplica para:

• Suscripciones.
• Implementaciones.
• Certificaciones e-CF.
• Desarrollos.
• Integraciones.
• Consultorías.
• Capacitaciones.
• Equipos.
• Licencias.
• Soporte especializado.
• Cualquier otro servicio.

Una vez iniciado o reservado un servicio no procederán devoluciones totales ni parciales.

---------------------------------------------------------
9. Facturación y Pagos
---------------------------------------------------------

El servicio se factura por adelantado.

El incumplimiento podrá ocasionar la suspensión temporal del servicio.

---------------------------------------------------------
10. Facturación Electrónica
---------------------------------------------------------

Es responsabilidad del cliente verificar la información antes de emitir un comprobante electrónico.

Las correcciones ocasionadas por errores del cliente o por no seguir las instrucciones suministradas por MacroBits podrán generar cargos adicionales.

MacroBits no será responsable por sanciones derivadas de información incorrecta suministrada por el cliente.

---------------------------------------------------------
11. Cambios en el Servicio
---------------------------------------------------------

MacroBits podrá realizar mejoras, actualizaciones y modificaciones a la plataforma.

---------------------------------------------------------
12. Responsabilidad del Cliente
---------------------------------------------------------

El cliente será responsable de:

• La información registrada.
• Sus usuarios.
• Sus credenciales.
• El cumplimiento de sus obligaciones fiscales.
• El uso adecuado del sistema.

---------------------------------------------------------
13. Limitación de Responsabilidad
---------------------------------------------------------

MacroBits no será responsable por daños ocasionados por:

• Fallas de Internet.
• Energía eléctrica.
• Equipos del cliente.
• Servicios de terceros.
• Casos fortuitos.

---------------------------------------------------------
14. Cancelación
---------------------------------------------------------

La cancelación del servicio no genera devolución de dinero.

---------------------------------------------------------
15. Nuevas Funcionalidades
---------------------------------------------------------

Las nuevas funcionalidades podrán requerir nuevos planes, licencias o costos adicionales.

La existencia de una nueva funcionalidad no implica que esté incluida dentro del plan contratado.',
        N'Publicada',
        GETDATE(),
        GETDATE(),
        N'Seed inicial v1.0'
    );
END
GO

PRINT 'Create_PoliticasServicio_Dev OK';
GO
