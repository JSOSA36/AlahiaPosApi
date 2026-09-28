-- Sabor Urbano en Prod: usuario demo + todos los módulos de producto.
-- Autorizado por el usuario (2026-08-22): probar en producción con todas las funciones.
-- Login: admin@saborurbano.demo / DemoFood2026!
USE AlahiaPos_Prod;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END

DECLARE @IdEmpresa INT;
DECLARE @IdPerfil INT;
DECLARE @IdEmpleado INT;
DECLARE @IdUsuario INT;
DECLARE @PasswordHash NVARCHAR(128) =
    N'02d87bd6b67f1c51e5a0c51294c1df1a4e41701a0b5ec6fbbba401edb8f55af0'; -- DemoFood2026!
DECLARE @UserName NVARCHAR(150) = N'admin@saborurbano.demo';
DECLARE @Correo NVARCHAR(150) = N'admin@saborurbano.demo';
DECLARE @IdPlan INT = (SELECT TOP 1 IdPlan FROM dbo.PlanesCloud ORDER BY IdPlan DESC);

SELECT @IdEmpresa = IdEmpresa
FROM dbo.Empresas
WHERE NombreComercial = N'Sabor Urbano'
   OR CorreElectronico = @Correo;

IF @IdEmpresa IS NULL
BEGIN
    INSERT INTO dbo.Empresas
    (
        NombreComercial, RNC, Direccion, Telefono, CorreElectronico, Logo,
        Estado, FechaTerminacion, GuidPublico, UsaSSL, PoliticasAceptadas,
        EsEmpresaSistema, PagadoServicio, EstadoServicio, IdPlan,
        FechaInicioPlan, FechaVencimientoPlan, EstadoPlan,
        LimiteUsuario, PrimaryColor, SecondaryColor, TertiaryColor, titleColor,
        FechaInseccion
    )
    VALUES
    (
        N'Sabor Urbano', N'132458796', N'Av. Winston Churchill, Santo Domingo, RD',
        N'809-555-0180', @Correo, N'https://alahiaupdate.alahiapos.com/demo-sabor-urbano-logo.jpg',
        1, DATEADD(YEAR, 5, GETDATE()), NEWID(), 1, 1,
        0, 1, N'ACTIVA', ISNULL(@IdPlan, 1),
        GETDATE(), DATEADD(YEAR, 5, GETDATE()), N'ACTIVO',
        25, N'#ea580c', N'#1c1917', N'#f97316', N'#ffffff',
        GETDATE()
    );
    SET @IdEmpresa = SCOPE_IDENTITY();
    PRINT CONCAT('Empresa creada Id=', @IdEmpresa);
END
ELSE
BEGIN
    UPDATE dbo.Empresas
    SET Estado = 1,
        PagadoServicio = 1,
        EstadoServicio = N'ACTIVA',
        PoliticasAceptadas = 1,
        FechaTerminacion = DATEADD(YEAR, 5, GETDATE()),
        FechaVencimientoPlan = DATEADD(YEAR, 5, GETDATE()),
        EstadoPlan = N'ACTIVO',
        LimiteUsuario = CASE WHEN ISNULL(LimiteUsuario, 0) < 10 THEN 25 ELSE LimiteUsuario END
    WHERE IdEmpresa = @IdEmpresa;
    PRINT CONCAT('Empresa actualizada Id=', @IdEmpresa);
END

SELECT @IdPerfil = IdPerfil
FROM dbo.Perfiles
WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Administrador' AND Activo = 1;

IF @IdPerfil IS NULL
BEGIN
    INSERT INTO dbo.Perfiles (IdEmpresa, Nombre, Descripcion, Activo)
    VALUES (@IdEmpresa, N'Administrador', N'Administrador Sabor Urbano', 1);
    SET @IdPerfil = SCOPE_IDENTITY();
END

SELECT TOP 1 @IdEmpleado = IdEmpleados
FROM dbo.Empleados
WHERE IdEmpresa = @IdEmpresa AND (Correo = @Correo OR Nombre = N'Admin Sabor Urbano');

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
        N'Admin Sabor Urbano', N'Administrador', 1, @IdEmpresa,
        CAST(GETDATE() AS date), GETDATE(), '1990-01-01',
        0, 0, 0, @Correo
    );
    SET @IdEmpleado = SCOPE_IDENTITY();
END

SELECT @IdUsuario = IdUsuario
FROM dbo.Usuarios
WHERE UserName = @UserName OR (IdEmpresa = @IdEmpresa AND Correo = @Correo);

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
        GETDATE(), 1, 1, 1, 1
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
        IdEmpresa = @IdEmpresa,
        IdEmpleado = ISNULL(IdEmpleado, @IdEmpleado),
        Correo = @Correo,
        UserName = @UserName
    WHERE IdUsuario = @IdUsuario;
    PRINT CONCAT('Usuario actualizado Id=', @IdUsuario);
END

-- Licencia + menú: todo el catálogo activo, menos administración interna MacroBits.
DECLARE @Internos TABLE (Codigo NVARCHAR(80) PRIMARY KEY);
INSERT INTO @Internos (Codigo) VALUES
    (N'EMPRESAS_ADMIN'),
    (N'MACROBITS_ADMIN'),
    (N'SUSCRIPCIONES_COBROS'),
    (N'TICKETS_ADMIN'),
    (N'POLITICAS_VERSIONES'),
    (N'POLITICAS_ACEPTACIONES');

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT @IdEmpresa, m.Id, 1, GETDATE()
FROM dbo.Modulos m
WHERE m.Activo = 1
  AND m.Codigo NOT IN (SELECT Codigo FROM @Internos)
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = @IdEmpresa AND em.ModuloId = m.Id
  );

UPDATE em
SET em.Activo = 1, em.FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa
  AND m.Codigo NOT IN (SELECT Codigo FROM @Internos);

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT @IdPerfil, m.Id, 1, GETDATE(), @IdEmpresa
FROM dbo.Modulos m
WHERE m.Activo = 1
  AND m.Codigo NOT IN (SELECT Codigo FROM @Internos)
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = @IdPerfil AND pr.IdModulo = m.Id AND pr.IdEmpresa = @IdEmpresa
  );

UPDATE pr SET pr.Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Modulos m ON m.Id = pr.IdModulo
WHERE pr.IdEmpresa = @IdEmpresa
  AND pr.IdPerfil = @IdPerfil
  AND m.Codigo NOT IN (SELECT Codigo FROM @Internos);

SELECT
    u.IdUsuario,
    u.UserName,
    e.IdEmpresa,
    e.NombreComercial,
    p.Nombre AS Perfil,
    (SELECT COUNT(*) FROM dbo.Empresa_Modulos em WHERE em.EmpresaId = e.IdEmpresa AND em.Activo = 1) AS ModulosEmpresa,
    (SELECT COUNT(*) FROM dbo.PerfilRoles pr WHERE pr.IdPerfil = p.IdPerfil AND pr.IdEmpresa = e.IdEmpresa AND pr.Activo = 1) AS ModulosPerfil
FROM dbo.Usuarios u
INNER JOIN dbo.Empresas e ON e.IdEmpresa = u.IdEmpresa
INNER JOIN dbo.Perfiles p ON p.IdPerfil = u.IdPerfil
WHERE u.IdUsuario = @IdUsuario;

SELECT m.Codigo
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa AND em.Activo = 1
ORDER BY m.Codigo;
GO
