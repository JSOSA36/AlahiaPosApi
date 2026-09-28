SET NOCOUNT ON;

UPDATE dbo.CertecfCaso
SET Estado = N'Aceptado',
    Mensaje = N'Ya Aceptado en aprobación comercial (paso 3). No reenviar.'
WHERE IdSesion = 1 AND TipoPrueba = N'ACECF';

DELETE FROM dbo.CertecfCaso
WHERE IdSesion = 1 AND TipoPrueba = N'SIMULACION';

UPDATE dbo.CertecfSesion
SET PasoActual = 4,
    Estado = N'EnCurso',
    Mensaje = N'Paso 3 listo. Genere la simulación del paso 4 con e-NCF nuevos.',
    JsonPasos = N'{"1":"Hecho","2":"Hecho","3":"Hecho","4":"EnCurso"}',
    FechaActualizacion = GETDATE()
WHERE IdSesion = 1 AND IdEmpresa = 60;

SELECT IdSesion, PasoActual, Estado, Mensaje, JsonPasos
FROM dbo.CertecfSesion WHERE IdSesion = 1;

SELECT TipoPrueba, Estado, COUNT(*) N
FROM dbo.CertecfCaso WHERE IdSesion = 1
GROUP BY TipoPrueba, Estado;
