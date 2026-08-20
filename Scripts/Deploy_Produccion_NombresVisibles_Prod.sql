-- ============================================================
-- Centro de Producción — nombres visibles (AlahiaPos_Prod)
-- Corrige mojibake UTF-8 (Ã“ / Ã³) y deja EN_PREPARACION = Preparación.
-- Idempotente.
-- ============================================================
USE AlahiaPos_Prod;
GO

SET NOCOUNT ON;
GO

DECLARE @CONFIRMO_PROD BIT = 0;

IF DB_NAME() <> N'AlahiaPos_Prod' OR ISNULL(@CONFIRMO_PROD, 0) <> 1
BEGIN
    RAISERROR(
        N'Abortado: este script solo corre en AlahiaPos_Prod con @CONFIRMO_PROD = 1.',
        16, 1);
    SET NOEXEC ON;
END
GO

UPDATE dbo.ProduccionFlujo
SET Nombre = NCHAR(0x00D3) + N'rdenes POS'
WHERE TipoTrabajoCodigo = N'POS_ORDEN';

UPDATE dbo.ProduccionFlujoEstado
SET NombreVisible = N'Preparaci' + NCHAR(0x00F3) + N'n'
WHERE Codigo = N'EN_PREPARACION';

SELECT IdFlujo, TipoTrabajoCodigo, Nombre
FROM dbo.ProduccionFlujo
WHERE TipoTrabajoCodigo = N'POS_ORDEN';

SELECT IdFlujo, Codigo, NombreVisible
FROM dbo.ProduccionFlujoEstado
WHERE Codigo = N'EN_PREPARACION';
GO

SET NOEXEC OFF;
GO
