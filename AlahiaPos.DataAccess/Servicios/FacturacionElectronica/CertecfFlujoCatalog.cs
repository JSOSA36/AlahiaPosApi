namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    /// <summary>
    /// Flujo CerteCF 1–15 según
    /// «Proceso de Certificación para ser Emisor Electrónico» (DGII, ago-2025)
    /// y Descripción Técnica Servicios DGII / Emisores (may-2026).
    /// Fuente: https://dgii.gov.do/.../documentacionSobreE-CF.aspx
    /// </summary>
    public static class CertecfFlujoCatalog
    {
        public static IReadOnlyList<CertecfPasoDef> Pasos { get; } = new List<CertecfPasoDef>
        {
            new(1, "Registrado",
                "En el portal: complete software (PROPIO / AlahiaERP), pegue las URLs, pulse GENERAR ARCHIVO y después ENVIAR ARCHIVO con el XML firmado.",
                "El XML de postulación lo genera CerteCF. Se firma con el certificado digital .p12/.pfx del contribuyente y se carga de vuelta en el portal.",
                "Pide el certificado y la contraseña, copia las URLs al portal, firma el XML que usted sube y le entrega el archivo listo para ENVIAR ARCHIVO."),
            new(2, "Pruebas de Datos e-CF",
                "En CerteCF: DESCARGAR COMPROBANTES. El marcador 0/21 es el envío por Recepción. El 3/4 de resúmenes es RecepcionFC (RFCE). El 0/4 de «Facturas de consumo < 250 mil» es el XML íntegro firmado, en esa caja Browse + ENVIAR, después del RFCE.",
                "Etapa en la que se comprueba la capacidad de su sistema para generar Comprobantes Fiscales Electrónicos (e-CF), con datos previamente suministrados por DGII. Hay E32 ≥ 250 mil (e-CF a Recepción) y E32 < 250 mil: primero el Resumen (RFCE) a RecepcionFC; cuando esos resúmenes estén Aceptados, cargar las Facturas de Consumo íntegras en el recuadro ENVIAR del portal.",
                "Alahia envía por EncApi: e-CF a Recepción (el 21) y RFCE a RecepcionFC (los resúmenes). El recuadro 0/4 del portal no es el RFCE: son los 4 XML íntegros firmados (nombre RNC+eNCF.xml). El Aceptado local no cuenta si DGII reinició el set."),
            new(3, "Pruebas de Datos Aprobación Comercial",
                "Set Excel de Aprobaciones o Rechazos Comerciales (ACECF).",
                "Generar XML ACECF (Formato Aprobación Comercial v1.0) y remitirlo a POST /certecf/aprobacioncomercial/api/aprobacioncomercial. Respuesta OK / Error.",
                "Cargar Excel ACECF del portal → XML + firma → envío al servicio oficial de aprobación comercial."),
            new(4, "Pruebas Simulación e-CF",
                "Las mismas cantidades del portal (4 E31, 2 E32 ≥ 250 mil, 4 E32 RFCE, notas y tipos 41–47), con e-NCF nuevos. E32 < 250 mil: primero RFCE, después el XML íntegro en Browse + ENVIAR.",
                "Generar e-CF en formato XML y enviarlos a recepción CerteCF. Mismos estados: Aceptado / Rechazado / Aceptado Condicional / En Proceso.",
                "Alahia clona cada e-CF aceptado del paso 2, asigna secuencias nuevas y reescribe las notas para que apunten al e-NCF nuevo."),
            new(5, "Pruebas Simulación Representación Impresa",
                "11 recuadros (un PDF por tipo, no uno por cada e-CF): 31, 32 ≥ 250 mil, 33, 34, 41, 43–47 y 32 < 250 mil. Suma ≤ 10 MB.",
                "PDF de la RI del e-CF de simulación (paso 4), según modelos ilustrativos DGII. Título con «Electrónica», Razón Social del RNC (no el nombre comercial), e-NCF y QR ConsultaTimbre.",
                "Alahia genera las 11 RI en PDF en el escritorio. La Razón Social sale del XML firmado / padrón DGII. Súbalas al portal; no HTML ni fotos."),
            new(6, "Validación Representación Impresa",
                "Respuesta del portal a los PDF.",
                "Aprobada o Rechazada. Si rechaza, corregir y volver a subir.",
                "Marcar en Alahia el resultado que dio el portal."),
            new(7, "URL Servicios Prueba",
                "Actualizar URLs de prueba si cambiaron respecto a la postulación.",
                "Recepción y Aprobación comercial (obligatorias). Autenticación opcional.",
                "Mostrar y guardar las URLs públicas que DGII llamará."),
            new(8, "Inicio Prueba Recepción e-CF",
                "Indicar que el receptor está listo. Opcional: validar certificado raíz DGII.",
                "En el portal: «Estoy listo para la recepción». Descargar certificado raíz si se usa autenticación.",
                "Alahia deja el listener HTTPS publicado y registra el listo."),
            new(9, "Recepción e-CF",
                "DGII envía e-CF al contribuyente.",
                "Responder con ARECF firmado (Formato Acuse de Recibo v1.0). Estado 0 = Recibido.",
                "Listener POST .../fe/recepcion/api/ecf → ARECF firmado."),
            new(10, "Inicio Prueba Recepción Aprobación Comercial",
                "Indicar listo y pedir a DGII que envíe las ACECF de prueba.",
                "Portal: «Enviar prueba de Aprobaciones Comerciales».",
                "Mismo listener, ruta de aprobación comercial."),
            new(11, "Recepción Aprobación Comercial",
                "DGII envía ACECF al emisor.",
                "Responder OK o Error (no es validación DGII del contenido).",
                "Listener POST .../fe/aprobacioncomercial/api/ecf."),
            new(12, "URL Servicios Producción",
                "URLs que quedarán en Mantenimiento de directorio OFV.",
                "Autenticación, Recepción y Aprobación comercial de producción. No usar las de CerteCF.",
                "Guardar URLs de producción (no se publican hasta el go-live)."),
            new(13, "Declaración Jurada",
                "Formulario electrónico bajo fe de juramento.",
                "Generar XML, firmar con el certificado del representante (App Firma Digital) y cargar en el portal.",
                "Alahia genera el XML; la firma y carga las hace el representante."),
            new(14, "Verificación Estatus",
                "DGII revisa el RNC.",
                "Al día en obligaciones, OFV, alta NCF, representante registrado.",
                "Checklist para confirmar antes de esperar el alta."),
            new(15, "Finalizado",
                "Autorización como Emisor Electrónico.",
                "OFV habilita menú FE: contingencia, delegación, consultas, mantenimiento de directorio con las URL de producción.",
                "Cerrar el laboratorio. No cambiar directorio de producción hasta el corte real."),
        };
    }

    public sealed record CertecfPasoDef(
        int Numero,
        string Titulo,
        string QuePidePortal,
        string QuePideNorma,
        string QueHaceAlahia);
}
