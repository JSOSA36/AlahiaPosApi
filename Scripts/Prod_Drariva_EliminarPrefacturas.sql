-- Activar eliminar prefacturas (órdenes) para el usuario drariva.
-- Pedido explícito: usuario administrador drariva.
USE AlahiaPos_Prod;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

PRINT N'--- drariva antes ---';
SELECT u.IdUsuario, u.UserName, u.Correo, u.IdEmpresa, e.NombreComercial,
       p.Nombre AS Perfil, u.PuedeEliminarOrden, u.Estado
FROM dbo.Usuarios u
INNER JOIN dbo.Empresas e ON e.IdEmpresa = u.IdEmpresa
LEFT JOIN dbo.Perfiles p ON p.IdPerfil = u.IdPerfil
WHERE u.UserName LIKE N'%drariva%'
   OR u.Correo LIKE N'%drariva%'
   OR u.UserName LIKE N'%dra.riva%'
   OR u.UserName LIKE N'%dra riva%';

UPDATE u
SET u.PuedeEliminarOrden = 1
FROM dbo.Usuarios u
WHERE u.UserName LIKE N'%drariva%'
   OR u.Correo LIKE N'%drariva%'
   OR u.UserName LIKE N'%dra.riva%'
   OR u.UserName LIKE N'%dra riva%';

PRINT N'Usuarios actualizados: ' + CAST(@@ROWCOUNT AS varchar(12));

PRINT N'--- drariva después ---';
SELECT u.IdUsuario, u.UserName, u.Correo, u.IdEmpresa, e.NombreComercial,
       p.Nombre AS Perfil, u.PuedeEliminarOrden, u.Estado
FROM dbo.Usuarios u
INNER JOIN dbo.Empresas e ON e.IdEmpresa = u.IdEmpresa
LEFT JOIN dbo.Perfiles p ON p.IdPerfil = u.IdPerfil
WHERE u.UserName LIKE N'%drariva%'
   OR u.Correo LIKE N'%drariva%'
   OR u.UserName LIKE N'%dra.riva%'
   OR u.UserName LIKE N'%dra riva%';
GO
