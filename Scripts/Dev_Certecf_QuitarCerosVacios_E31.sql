-- CerteCF: celdas vacías del Excel no son 0.00. Solo Dev / sesión Sena.
USE AlahiaPos_Dev;
GO
SET NOCOUNT ON;
IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
    RETURN;
END

DECLARE @IdSesion INT = (
    SELECT TOP 1 IdSesion FROM dbo.CertecfSesion
    WHERE IdEmpresa = 60
    ORDER BY IdSesion DESC
);

IF @IdSesion IS NULL
BEGIN
    RAISERROR(N'No hay sesión CerteCF de Sena (60).', 16, 1);
    RETURN;
END

UPDATE dbo.CertecfCaso
SET PayloadJson = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
    PayloadJson,
    '"valorPagar":0.00', '"valorPagar":null'),
    '"saldoAnterior":0.00', '"saldoAnterior":null'),
    '"montoAvancePago":0.00', '"montoAvancePago":null'),
    '"valorPagar":0,', '"valorPagar":null,'),
    '"saldoAnterior":0,', '"saldoAnterior":null,'),
    '"montoAvancePago":0,', '"montoAvancePago":null,')
WHERE IdSesion = @IdSesion;

SELECT @IdSesion AS IdSesion, @@ROWCOUNT AS Filas;

SELECT Encf, Estado,
       CASE
         WHEN PayloadJson LIKE '%"valorPagar":0%' THEN N'AUN0'
         ELSE N'ok'
       END AS ValorPagar
FROM dbo.CertecfCaso
WHERE IdSesion = @IdSesion AND Encf = N'E310000000001';
GO
