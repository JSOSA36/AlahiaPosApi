-- ============================================================
-- Seed MacroBits (empresa sistema) — AlahiaPos_Prod
-- Usuario: admin@macrobits.com / MacroBits2026!
-- Autorizado por usuario 2026-07-18
-- ============================================================
USE AlahiaPos_Prod;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

DECLARE @IdEmpresa INT;
DECLARE @IdPerfil INT;
DECLARE @IdEmpleado INT;
DECLARE @IdUsuario INT;
DECLARE @PasswordHash NVARCHAR(128) =
    N'3a2955c2b8a6bf1f2e1dc9e5b221585785bbf1e9930e6a0e733fe6e297175847'; -- MacroBits2026!
DECLARE @UserName NVARCHAR(150) = N'admin@macrobits.com';
DECLARE @Correo NVARCHAR(150) = N'admin@macrobits.com';

-- 0) Módulos admin (si faltan en Prod)
IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'MACROBITS_ADMIN')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'MACROBITS_ADMIN', N'Administración MacroBits',
            N'Gestión de políticas del servicio y herramientas internas MacroBits', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'POLITICAS_VERSIONES')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'POLITICAS_VERSIONES', N'Políticas del Servicio',
            N'Administrar versiones de políticas del servicio', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'POLITICAS_ACEPTACIONES')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'POLITICAS_ACEPTACIONES', N'Aceptaciones de Políticas',
            N'Consulta de aceptaciones de políticas por empresa', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'SUSCRIPCIONES_COBROS')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'SUSCRIPCIONES_COBROS', N'Cobros y Suscripciones',
            N'Panel MacroBits de cobros SaaS', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'TICKETS')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'TICKETS', N'Tickets de Soporte',
            N'Reportar incidencias y dar seguimiento a tickets de soporte', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'TICKETS_ADMIN')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'TICKETS_ADMIN', N'Tickets (Admin)',
            N'Inbox de soporte MacroBits: seguimiento de todos los tickets', 0, 1, GETDATE());

-- 1) Empresa MacroBits (NO tocar otras empresas; no buscar solo EsEmpresaSistema)
SELECT @IdEmpresa = IdEmpresa
FROM dbo.Empresas
WHERE NombreComercial = N'MacroBits'
   OR CorreElectronico = @Correo;

IF @IdEmpresa IS NULL
BEGIN
    INSERT INTO dbo.Empresas
    (
        NombreComercial, RNC, Direccion, Telefono, CorreElectronico,
        Estado, FechaTerminacion, GuidPublico, UsaSSL, PoliticasAceptadas,
        EsEmpresaSistema, PagadoServicio, EstadoServicio, IdPlan,
        FechaInicioPlan, FechaVencimientoPlan, EstadoPlan,
        LimiteUsuario, PrimaryColor, SecondaryColor, TertiaryColor, titleColor,
        FechaInseccion
    )
    VALUES
    (
        N'MacroBits', N'000000000', N'Santo Domingo, RD', N'809-000-0000', @Correo,
        1, DATEADD(YEAR, 50, GETDATE()), NEWID(), 1, 1,
        1, 1, N'ACTIVA', 6,
        GETDATE(), DATEADD(YEAR, 50, GETDATE()), N'ACTIVO',
        50, N'#1d4ed8', N'#0f172a', N'#64748b', N'#ffffff',
        GETDATE()
    );
    SET @IdEmpresa = SCOPE_IDENTITY();
    PRINT CONCAT('Empresa MacroBits creada Id=', @IdEmpresa);
END
ELSE
BEGIN
    UPDATE dbo.Empresas
    SET NombreComercial = N'MacroBits',
        EsEmpresaSistema = 1,
        Estado = 1,
        PagadoServicio = 1,
        EstadoServicio = N'ACTIVA',
        PoliticasAceptadas = 1,
        FechaTerminacion = DATEADD(YEAR, 50, GETDATE()),
        IdPlan = ISNULL(IdPlan, 6),
        CorreElectronico = ISNULL(NULLIF(CorreElectronico, N''), @Correo)
    WHERE IdEmpresa = @IdEmpresa;
    PRINT CONCAT('Empresa MacroBits actualizada Id=', @IdEmpresa);
END

-- 2) Perfil Administrador
SELECT @IdPerfil = IdPerfil
FROM dbo.Perfiles
WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Administrador';

IF @IdPerfil IS NULL
BEGIN
    INSERT INTO dbo.Perfiles (IdEmpresa, Nombre, Descripcion, Activo)
    VALUES (@IdEmpresa, N'Administrador', N'Administrador MacroBits plataforma', 1);
    SET @IdPerfil = SCOPE_IDENTITY();
    PRINT CONCAT('Perfil Administrador creado Id=', @IdPerfil);
END

-- 3) Empleado admin (tabla real del API: EmpleadosP)
SELECT TOP 1 @IdEmpleado = IdEmpleados
FROM dbo.EmpleadosP
WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Administrador MacroBits';

IF @IdEmpleado IS NULL
BEGIN
    -- Reutilizar IdEmpleado del usuario si ya apunta a una fila vacía
    SELECT @IdEmpleado = IdEmpleado FROM dbo.Usuarios WHERE UserName = @UserName;

    IF @IdEmpleado IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.EmpleadosP WHERE IdEmpleados = @IdEmpleado)
    BEGIN
        UPDATE dbo.EmpleadosP
        SET IdEmpresa = @IdEmpresa,
            Nombre = N'Administrador MacroBits',
            Ocupacion = N'Administrador',
            Estado = 1,
            ComisionServicio = 0,
            ComisionProductos = 0,
            Nota = N''
        WHERE IdEmpleados = @IdEmpleado;
        PRINT CONCAT('EmpleadosP actualizado Id=', @IdEmpleado);
    END
    ELSE
    BEGIN
        INSERT INTO dbo.EmpleadosP
        (
            IdEmpresa, Nombre, Ocupacion, Estado,
            ComisionServicio, ComisionProductos, Nota
        )
        VALUES
        (
            @IdEmpresa, N'Administrador MacroBits', N'Administrador', 1,
            0, 0, N''
        );
        SET @IdEmpleado = SCOPE_IDENTITY();
        PRINT CONCAT('EmpleadosP creado Id=', @IdEmpleado);
    END
END
ELSE
BEGIN
    UPDATE dbo.EmpleadosP
    SET Ocupacion = N'Administrador',
        Estado = 1,
        Nombre = N'Administrador MacroBits'
    WHERE IdEmpleados = @IdEmpleado;
END

-- (legacy Empleados table intentionally skipped; API uses EmpleadosP)

-- 4) Usuario admin
SELECT @IdUsuario = IdUsuario
FROM dbo.Usuarios
WHERE UserName = @UserName OR Correo = @Correo;

IF @IdUsuario IS NULL
BEGIN
    INSERT INTO dbo.Usuarios
    (
        IdEmpresa, UserName, Correo, PasswordHash, Estado, IdPerfil, IdEmpleado,
        FechaCreacion,
        PuedeEliminarOrden, PuedeEliminarItemCarrito, PuedeDisminuirCantidadCarrito, PuedeEditarPrecioCarrito
    )
    VALUES
    (
        @IdEmpresa, @UserName, @Correo, @PasswordHash, 1, @IdPerfil, @IdEmpleado,
        GETDATE(),
        1, 1, 1, 1
    );
    SET @IdUsuario = SCOPE_IDENTITY();
    PRINT CONCAT('Usuario creado Id=', @IdUsuario);
END
ELSE
BEGIN
    UPDATE dbo.Usuarios
    SET PasswordHash = @PasswordHash,
        Estado = 1,
        IdEmpresa = @IdEmpresa,
        IdPerfil = @IdPerfil,
        IdEmpleado = ISNULL(IdEmpleado, @IdEmpleado),
        Correo = @Correo,
        UserName = @UserName
    WHERE IdUsuario = @IdUsuario;
    PRINT CONCAT('Usuario actualizado Id=', @IdUsuario);
END

-- 5) Empresa_Modulos
;WITH Mods AS (
    SELECT Id AS ModuloId
    FROM dbo.Modulos
    WHERE Codigo IN (
        N'MACROBITS_ADMIN',
        N'POLITICAS_VERSIONES',
        N'POLITICAS_ACEPTACIONES',
        N'SUSCRIPCIONES_COBROS',
        N'TICKETS_ADMIN',
        N'EMPRESA',
        N'USUARIOS',
        N'PERFILES',
        N'LISTADO_PAGOS',
        N'DASHBOARD',
        N'PARAMETROS'
    )
)
INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT @IdEmpresa, m.ModuloId, 1, GETDATE()
FROM Mods m
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = @IdEmpresa AND em.ModuloId = m.ModuloId
);

UPDATE em
SET Activo = 1, FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Modulos mo ON mo.Id = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa
  AND mo.Codigo IN (
        N'MACROBITS_ADMIN',
        N'POLITICAS_VERSIONES',
        N'POLITICAS_ACEPTACIONES',
        N'SUSCRIPCIONES_COBROS',
        N'TICKETS_ADMIN',
        N'EMPRESA',
        N'USUARIOS',
        N'PERFILES',
        N'LISTADO_PAGOS',
        N'DASHBOARD',
        N'PARAMETROS'
  );

-- 6) PerfilRoles
;WITH Mods AS (
    SELECT Id AS ModuloId
    FROM dbo.Modulos
    WHERE Codigo IN (
        N'MACROBITS_ADMIN',
        N'POLITICAS_VERSIONES',
        N'POLITICAS_ACEPTACIONES',
        N'SUSCRIPCIONES_COBROS',
        N'TICKETS_ADMIN',
        N'EMPRESA',
        N'USUARIOS',
        N'PERFILES',
        N'LISTADO_PAGOS',
        N'DASHBOARD',
        N'PARAMETROS'
    )
)
INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT @IdPerfil, m.ModuloId, 1, GETDATE(), @IdEmpresa
FROM Mods m
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles pr
    WHERE pr.IdPerfil = @IdPerfil AND pr.IdModulo = m.ModuloId AND ISNULL(pr.IdEmpresa, @IdEmpresa) = @IdEmpresa
);

UPDATE pr
SET Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Modulos mo ON mo.Id = pr.IdModulo
WHERE pr.IdPerfil = @IdPerfil
  AND mo.Codigo IN (
        N'MACROBITS_ADMIN',
        N'POLITICAS_VERSIONES',
        N'POLITICAS_ACEPTACIONES',
        N'SUSCRIPCIONES_COBROS',
        N'TICKETS_ADMIN',
        N'EMPRESA',
        N'USUARIOS',
        N'PERFILES',
        N'LISTADO_PAGOS',
        N'DASHBOARD',
        N'PARAMETROS'
  );

SELECT
    e.IdEmpresa,
    e.NombreComercial,
    e.EsEmpresaSistema,
    e.EstadoServicio,
    u.IdUsuario,
    u.UserName,
    p.IdPerfil,
    p.Nombre AS Perfil
FROM dbo.Empresas e
JOIN dbo.Usuarios u ON u.IdEmpresa = e.IdEmpresa AND u.UserName = @UserName
JOIN dbo.Perfiles p ON p.IdPerfil = u.IdPerfil
WHERE e.IdEmpresa = @IdEmpresa;

SELECT mo.Codigo, mo.Nombre, em.Activo AS Licencia, pr.Activo AS PerfilRol
FROM dbo.Modulos mo
LEFT JOIN dbo.Empresa_Modulos em ON em.ModuloId = mo.Id AND em.EmpresaId = @IdEmpresa
LEFT JOIN dbo.PerfilRoles pr ON pr.IdModulo = mo.Id AND pr.IdPerfil = @IdPerfil
WHERE mo.Codigo IN (
    N'MACROBITS_ADMIN', N'POLITICAS_VERSIONES', N'POLITICAS_ACEPTACIONES', N'SUSCRIPCIONES_COBROS',
    N'TICKETS_ADMIN', N'EMPRESA', N'USUARIOS', N'PERFILES', N'LISTADO_PAGOS', N'DASHBOARD', N'PARAMETROS'
)
ORDER BY mo.Codigo;

PRINT 'Seed_MacroBits_Prod OK';
GO
