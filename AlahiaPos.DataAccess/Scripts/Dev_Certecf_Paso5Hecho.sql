-- AlahiaPos_Dev. Paso 5 (RI) ya se subió al portal; queda Hecho y se avanza al 6.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

UPDATE dbo.CertecfSesion
SET JsonPasos = N'{"1":"Hecho","2":"Hecho","3":"Hecho","4":"Hecho","5":"Hecho"}',
    PasoActual = 6,
    Mensaje = N'RI subidas al portal. Esperando validación DGII (paso 6).',
    FechaActualizacion = GETDATE()
WHERE IdSesion = 1 AND IdEmpresa = 60;

IF @@ROWCOUNT <> 1
BEGIN
    ROLLBACK;
    RAISERROR('No se actualizo la sesion 1 empresa 60.', 16, 1);
    RETURN;
END

COMMIT;

SELECT IdSesion, PasoActual, JsonPasos, Mensaje
FROM dbo.CertecfSesion
WHERE IdSesion = 1;
