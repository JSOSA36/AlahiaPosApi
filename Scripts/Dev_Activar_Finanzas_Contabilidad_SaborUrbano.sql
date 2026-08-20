-- Activar Finanzas (gastos/tesorería) y Contabilidad para Sabor Urbano.
-- AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @IdEmpresa INT = (
    SELECT TOP 1 IdEmpresa FROM dbo.Empresas WHERE NombreComercial = N'Sabor Urbano'
);
DECLARE @IdPerfil INT = (
    SELECT TOP 1 IdPerfil FROM dbo.Perfiles
    WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Administrador' AND Activo = 1
);

IF @IdEmpresa IS NULL OR @IdPerfil IS NULL
BEGIN
    RAISERROR(N'No se encontró Sabor Urbano o su perfil Administrador.', 16, 1);
    RETURN;
END;

DECLARE @Codigos TABLE (Codigo NVARCHAR(80) PRIMARY KEY);
INSERT INTO @Codigos (Codigo) VALUES
    (N'GASTOS'),
    (N'INGRESOS'),
    (N'CUENTAS_FINANCIERAS'),
    (N'MOVIMIENTO_FINANCIERO'),
    (N'CONCILIACION_BANCARIA'),
    (N'METODO_PAGO_CUENTAS'),
    (N'LISTADO_PAGOS'),
    (N'LISTADO_CAJA'),
    (N'MOVIMIENTO_CAJA'),
    (N'CIERRE_CAJA'),
    (N'CONTABILIDAD'),
    (N'CONTABILIDAD_CUENTAS'),
    (N'CONTABILIDAD_ASIENTOS'),
    (N'CONTABILIDAD_LIBRO_DIARIO'),
    (N'CONTABILIDAD_MAYOR_GENERAL'),
    (N'CONTABILIDAD_BALANCE_COMPROBACION'),
    (N'CONTABILIDAD_ESTADO_RESULTADOS'),
    (N'CONTABILIDAD_BALANCE_GENERAL'),
    (N'CONTABILIDAD_CONSULTA_ASIENTOS'),
    (N'CONTABILIDAD_CIERRE'),
    (N'CONTABILIDAD_CONFIGURACION_INTEGRACION');

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT @IdEmpresa, m.Id, 1, GETDATE()
FROM dbo.Modulos m
INNER JOIN @Codigos c ON c.Codigo = m.Codigo
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = @IdEmpresa AND em.ModuloId = m.Id
);

UPDATE em
SET em.Activo = 1, em.FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId
INNER JOIN @Codigos c ON c.Codigo = m.Codigo
WHERE em.EmpresaId = @IdEmpresa;

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT @IdPerfil, m.Id, 1, GETDATE(), @IdEmpresa
FROM dbo.Modulos m
INNER JOIN @Codigos c ON c.Codigo = m.Codigo
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles pr
    WHERE pr.IdPerfil = @IdPerfil AND pr.IdModulo = m.Id AND pr.IdEmpresa = @IdEmpresa
);

UPDATE pr SET pr.Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Modulos m ON m.Id = pr.IdModulo
INNER JOIN @Codigos c ON c.Codigo = m.Codigo
WHERE pr.IdEmpresa = @IdEmpresa AND pr.IdPerfil = @IdPerfil;

IF NOT EXISTS (SELECT 1 FROM dbo.ContabilidadConfiguracion WHERE IdEmpresa = @IdEmpresa)
    INSERT INTO dbo.ContabilidadConfiguracion (
        IdEmpresa, IntegracionAutomatica, GenerarCOGSAutomatico, SepararAsientoCOGS, FechaInseccion
    )
    VALUES (@IdEmpresa, 1, 1, 1, GETDATE());
ELSE
    UPDATE dbo.ContabilidadConfiguracion
    SET IntegracionAutomatica = 1,
        GenerarCOGSAutomatico = 1,
        SepararAsientoCOGS = 1,
        FechaActualizacion = GETDATE()
    WHERE IdEmpresa = @IdEmpresa;

DECLARE @Cat TABLE (
    Codigo NVARCHAR(30) PRIMARY KEY,
    Nombre NVARCHAR(200) NOT NULL,
    Tipo NVARCHAR(30) NOT NULL,
    Permite BIT NOT NULL,
    Padre NVARCHAR(30) NULL,
    Nivel INT NOT NULL
);
INSERT INTO @Cat (Codigo, Nombre, Tipo, Permite, Padre, Nivel) VALUES
    (N'1',     N'ACTIVO',                    N'Activo',   0, NULL,   1),
    (N'1.1',   N'ACTIVO CORRIENTE',          N'Activo',   0, N'1',   2),
    (N'1.1.1', N'Caja',                      N'Activo',   1, N'1.1', 3),
    (N'1.1.2', N'Banco',                     N'Activo',   1, N'1.1', 3),
    (N'1.1.3', N'Inventario',                N'Activo',   1, N'1.1', 3),
    (N'1.1.4', N'Cuentas por Cobrar',        N'Activo',   1, N'1.1', 3),
    (N'1.1.5', N'ITBIS por Cobrar',          N'Activo',   1, N'1.1', 3),
    (N'1.2',   N'ACTIVO NO CORRIENTE',       N'Activo',   0, N'1',   2),
    (N'1.2.1', N'Mobiliario y Equipo',       N'Activo',   1, N'1.2', 3),
    (N'1.2.2', N'Depreciación Acumulada',    N'Activo',   1, N'1.2', 3),
    (N'2',     N'PASIVO',                    N'Pasivo',   0, NULL,   1),
    (N'2.1',   N'PASIVO CORRIENTE',          N'Pasivo',   0, N'2',   2),
    (N'2.1.1', N'Cuentas por Pagar',         N'Pasivo',   1, N'2.1', 3),
    (N'2.1.2', N'ITBIS por Pagar',           N'Pasivo',   1, N'2.1', 3),
    (N'2.1.3', N'Nómina por Pagar',          N'Pasivo',   1, N'2.1', 3),
    (N'2.2',   N'PASIVO NO CORRIENTE',       N'Pasivo',   0, N'2',   2),
    (N'2.2.1', N'Préstamos a Largo Plazo',   N'Pasivo',   1, N'2.2', 3),
    (N'3',     N'CAPITAL',                   N'Capital',  0, NULL,   1),
    (N'3.1',   N'Capital Social',            N'Capital',  1, N'3',   2),
    (N'3.2',   N'Utilidades Retenidas',      N'Capital',  1, N'3',   2),
    (N'3.3',   N'Utilidad del Ejercicio',    N'Capital',  1, N'3',   2),
    (N'4',     N'INGRESOS',                  N'Ingresos', 0, NULL,   1),
    (N'4.1',   N'Ventas',                    N'Ingresos', 1, N'4',   2),
    (N'4.2',   N'Ingresos por Servicios',    N'Ingresos', 1, N'4',   2),
    (N'4.3',   N'Otros Ingresos',            N'Ingresos', 1, N'4',   2),
    (N'5',     N'GASTOS',                    N'Gastos',   0, NULL,   1),
    (N'5.1',   N'Gastos Operativos',         N'Gastos',   1, N'5',   2),
    (N'5.2',   N'Gastos Administrativos',    N'Gastos',   1, N'5',   2),
    (N'5.3',   N'Gastos de Personal',        N'Gastos',   1, N'5',   2),
    (N'5.4',   N'Depreciación',              N'Gastos',   1, N'5',   2),
    (N'6',     N'COSTOS',                    N'Costos',   0, NULL,   1),
    (N'6.1',   N'Costo de Ventas',           N'Costos',   1, N'6',   2),
    (N'6.2',   N'Costo de Servicios',        N'Costos',   1, N'6',   2);

DECLARE @Nivel INT = 1;
WHILE @Nivel <= 3
BEGIN
    INSERT INTO dbo.CuentasContables (
        IdEmpresa, Codigo, Nombre, TipoCuenta, IdCuentaPadre, Nivel, PermiteMovimiento, Activa, FechaInseccion
    )
    SELECT
        @IdEmpresa,
        c.Codigo,
        c.Nombre,
        c.Tipo,
        p.IdCuentaContable,
        c.Nivel,
        c.Permite,
        1,
        GETDATE()
    FROM @Cat c
    LEFT JOIN dbo.CuentasContables p
        ON p.IdEmpresa = @IdEmpresa AND p.Codigo = c.Padre
    WHERE c.Nivel = @Nivel
      AND NOT EXISTS (
          SELECT 1 FROM dbo.CuentasContables x
          WHERE x.IdEmpresa = @IdEmpresa AND x.Codigo = c.Codigo
      );

    SET @Nivel += 1;
END;

DECLARE @Map TABLE (Concepto NVARCHAR(50), Codigo NVARCHAR(30));
INSERT INTO @Map (Concepto, Codigo) VALUES
    (N'CAJA', N'1.1.1'),
    (N'BANCO', N'1.1.2'),
    (N'INVENTARIO', N'1.1.3'),
    (N'CXC', N'1.1.4'),
    (N'ITBIS_COBRAR', N'1.1.5'),
    (N'CXP', N'2.1.1'),
    (N'ITBIS_PAGAR', N'2.1.2'),
    (N'VENTAS', N'4.1'),
    (N'OTROS_INGRESOS', N'4.3'),
    (N'GASTO_OPERATIVO', N'5.1'),
    (N'GASTO_NOMINA', N'5.3'),
    (N'COSTO_VENTAS', N'6.1'),
    (N'ACTIVO_FIJO', N'1.2.1');

INSERT INTO dbo.ContabilidadCuentaMapeo (IdEmpresa, CodigoConcepto, IdCuentaContable, Activo)
SELECT @IdEmpresa, mp.Concepto, cc.IdCuentaContable, 1
FROM @Map mp
INNER JOIN dbo.CuentasContables cc
    ON cc.IdEmpresa = @IdEmpresa AND cc.Codigo = mp.Codigo
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ContabilidadCuentaMapeo x
    WHERE x.IdEmpresa = @IdEmpresa AND x.CodigoConcepto = mp.Concepto
);

UPDATE x
SET x.IdCuentaContable = cc.IdCuentaContable, x.Activo = 1
FROM dbo.ContabilidadCuentaMapeo x
INNER JOIN @Map mp ON mp.Concepto = x.CodigoConcepto
INNER JOIN dbo.CuentasContables cc
    ON cc.IdEmpresa = @IdEmpresa AND cc.Codigo = mp.Codigo
WHERE x.IdEmpresa = @IdEmpresa;

UPDATE cf
SET cf.IdCuentaContable = cc.IdCuentaContable
FROM dbo.CuentaFinanciera cf
INNER JOIN dbo.CuentasContables cc
    ON cc.IdEmpresa = cf.IdEmpresa AND cc.Codigo = N'1.1.1'
WHERE cf.IdEmpresa = @IdEmpresa AND cf.Nombre = N'Caja General';

UPDATE cf
SET cf.IdCuentaContable = cc.IdCuentaContable
FROM dbo.CuentaFinanciera cf
INNER JOIN dbo.CuentasContables cc
    ON cc.IdEmpresa = cf.IdEmpresa AND cc.Codigo = N'1.1.2'
WHERE cf.IdEmpresa = @IdEmpresa AND cf.Nombre = N'Banco Popular';

SELECT m.Codigo, em.Activo AS Licencia, pr.Activo AS Perfil
FROM dbo.Modulos m
INNER JOIN @Codigos c ON c.Codigo = m.Codigo
LEFT JOIN dbo.Empresa_Modulos em ON em.ModuloId = m.Id AND em.EmpresaId = @IdEmpresa
LEFT JOIN dbo.PerfilRoles pr ON pr.IdModulo = m.Id AND pr.IdEmpresa = @IdEmpresa AND pr.IdPerfil = @IdPerfil
ORDER BY m.Codigo;

SELECT IntegracionAutomatica, GenerarCOGSAutomatico
FROM dbo.ContabilidadConfiguracion
WHERE IdEmpresa = @IdEmpresa;

SELECT COUNT(*) AS CuentasGL FROM dbo.CuentasContables WHERE IdEmpresa = @IdEmpresa;
SELECT CodigoConcepto, cc.Codigo, cc.Nombre
FROM dbo.ContabilidadCuentaMapeo mp
JOIN dbo.CuentasContables cc ON cc.IdCuentaContable = mp.IdCuentaContable
WHERE mp.IdEmpresa = @IdEmpresa
ORDER BY mp.CodigoConcepto;
GO
