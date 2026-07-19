-- ============================================================
-- Seed MacroBits (empresa sistema) — AlahiaPos_Dev
-- Usuario: admin@macrobits.com / MacroBits2026!
-- ============================================================
USE AlahiaPos_Dev;
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

-- 1) Empresa MacroBits
SELECT @IdEmpresa = IdEmpresa
FROM dbo.Empresas
WHERE NombreComercial = N'MacroBits'
   OR CorreElectronico = @Correo
   OR EsEmpresaSistema = 1;

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
        1, 1, N'ACTIVA', 6, -- Elite
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

-- 3) Empleado admin
SELECT TOP 1 @IdEmpleado = IdEmpleados
FROM dbo.Empleados
WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Administrador MacroBits';

IF @IdEmpleado IS NULL
BEGIN
    INSERT INTO dbo.Empleados
    (
        Nombre, Posicion, Estado, IdEmpresa,
        FechaInseccion, FechaIngreso, FechaNacimiento,
        ComisionServicio, ComisionProductos, Salario, Correo
    )
    VALUES
    (
        N'Administrador MacroBits', N'Administrador', 1, @IdEmpresa,
        CAST(GETDATE() AS date), GETDATE(), '1990-01-01',
        0, 0, 0, @Correo
    );
    SET @IdEmpleado = SCOPE_IDENTITY();
    PRINT CONCAT('Empleado creado Id=', @IdEmpleado);
END

-- 4) Usuario admin
SELECT @IdUsuario = IdUsuario
FROM dbo.Usuarios
WHERE IdEmpresa = @IdEmpresa AND (UserName = @UserName OR Correo = @Correo);

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
        IdPerfil = @IdPerfil,
        IdEmpleado = ISNULL(IdEmpleado, @IdEmpleado),
        Correo = @Correo,
        UserName = @UserName
    WHERE IdUsuario = @IdUsuario;
    PRINT CONCAT('Usuario actualizado Id=', @IdUsuario);
END

-- 5) Empresa_Modulos (licencia) — módulos admin MacroBits + base
;WITH Mods AS (
    SELECT Id AS ModuloId
    FROM dbo.Modulos
    WHERE Codigo IN (
        N'MACROBITS_ADMIN',
        N'POLITICAS_VERSIONES',
        N'POLITICAS_ACEPTACIONES',
        N'SUSCRIPCIONES_COBROS',
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
        N'EMPRESA',
        N'USUARIOS',
        N'PERFILES',
        N'LISTADO_PAGOS',
        N'DASHBOARD',
        N'PARAMETROS'
  );

-- 6) PerfilRoles (menú visible)
;WITH Mods AS (
    SELECT Id AS ModuloId
    FROM dbo.Modulos
    WHERE Codigo IN (
        N'MACROBITS_ADMIN',
        N'POLITICAS_VERSIONES',
        N'POLITICAS_ACEPTACIONES',
        N'SUSCRIPCIONES_COBROS',
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
    N'EMPRESA', N'USUARIOS', N'PERFILES', N'LISTADO_PAGOS', N'DASHBOARD', N'PARAMETROS'
)
ORDER BY mo.Codigo;

PRINT 'Seed_MacroBits_Dev OK';
GO
