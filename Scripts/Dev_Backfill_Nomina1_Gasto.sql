-- Registrar en Gastos la nomina 1 de Sabor Urbano (ya PAGADA en tesoreria/contabilidad).
-- No crea movimiento ni asiento: esos ya existen.
-- AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @IdEmpresa INT = 62;
DECLARE @IdNomina INT = 1;
DECLARE @Referencia NVARCHAR(100) = N'NOMINA-' + CAST(@IdNomina AS NVARCHAR(20));
DECLARE @NombreCat NVARCHAR(100) = N'N' + NCHAR(243) + N'mina'; -- Nómina
DECLARE @IdCategoria INT;
DECLARE @IdProveedor INT;
DECLARE @IdCuentaFinanciera INT;
DECLARE @IdUsuario INT;
DECLARE @Neto DECIMAL(18,2);
DECLARE @PeriodKey NVARCHAR(40);
DECLARE @FechaPago DATETIME;
DECLARE @Detalle NVARCHAR(400);
DECLARE @IdCuentaGL INT;

IF EXISTS (
    SELECT 1 FROM dbo.Gastos
    WHERE IdEmpresa = @IdEmpresa AND Referencia = @Referencia AND EstaAnulado = 0
)
BEGIN
    SELECT N'El gasto de nomina ya existe.' AS Resultado;
    RETURN;
END;

SELECT
    @IdCuentaFinanciera = p.IdCuentaFinanciera,
    @IdUsuario = p.IdUsuarioPaga,
    @PeriodKey = p.PeriodKey,
    @FechaPago = ISNULL(p.FechaPago, GETDATE()),
    @Neto = CAST(ISNULL((
        SELECT SUM(e.Neto) FROM dbo.NominaProcesoEmpleado e
        WHERE e.IdNominaProceso = p.IdNominaProceso
    ), 0) AS DECIMAL(18,2))
FROM dbo.NominaProceso p
WHERE p.IdEmpresa = @IdEmpresa AND p.IdNominaProceso = @IdNomina;

IF @Neto IS NULL OR @Neto <= 0
    THROW 50001, 'No se encontro la nomina 1 o el neto es 0.', 1;

SELECT TOP 1 @IdProveedor = IdProveedor
FROM dbo.Proveedores
WHERE IdEmpresa = @IdEmpresa AND IsActivo = 1
ORDER BY IdProveedor;

IF @IdProveedor IS NULL
    THROW 50002, 'No hay proveedor activo para Sabor Urbano.', 1;

SELECT TOP 1 @IdCuentaGL = IdCuentaContable
FROM dbo.ContabilidadCuentaMapeo
WHERE IdEmpresa = @IdEmpresa AND Activo = 1 AND CodigoConcepto = N'GASTO_NOMINA';

SELECT @IdCategoria = IdCategoriaGasto
FROM dbo.CategoriasGasto
WHERE IdEmpresa = @IdEmpresa AND Nombre = @NombreCat;

IF @IdCategoria IS NULL
BEGIN
    INSERT INTO dbo.CategoriasGasto (IdEmpresa, Nombre, Descripcion, Activo, Orden, IdCuentaContable, FechaCreacion)
    VALUES (@IdEmpresa, @NombreCat, N'Pago de nomina a colaboradores', 1, 13, @IdCuentaGL, GETUTCDATE());
    SET @IdCategoria = SCOPE_IDENTITY();
END;

SET @Detalle = N'Pago de nomina ' + ISNULL(@PeriodKey, N'') + N' (16/08/2026 - 31/08/2026)';

INSERT INTO dbo.Gastos (
    TipoGasto, IdProveedor, Monto, Orien, IdEmpleado, Detalle, EstaCerrada,
    FechaInseccion, IdEmpresa, IdUsuario, EstaAnulado, FormaPago,
    IdCuentaFinanciera, Referencia, OrigenModulo, IdCategoriaGasto, TipoComprobante
)
VALUES (
    @NombreCat, @IdProveedor, @Neto, N'NOMINA', NULL, @Detalle, 0,
    @FechaPago, @IdEmpresa, @IdUsuario, 0, N'TRANSFERENCIA',
    @IdCuentaFinanciera, @Referencia, N'NOMINA', @IdCategoria, N'Sin comprobante'
);

SELECT IdGasto, TipoGasto, Monto, Referencia, OrigenModulo, FechaInseccion
FROM dbo.Gastos
WHERE IdEmpresa = @IdEmpresa AND Referencia = @Referencia;
GO
