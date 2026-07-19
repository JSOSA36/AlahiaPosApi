# Índice — Documentación e-CF DGII

Fuente: https://dgii.gov.do/cicloContribuyente/facturacion/comprobantesFiscalesElectronicosE-CF/Paginas/documentacionSobreE-CF.aspx

Descargado localmente el 2026-07-18 (solo referencia; no reemplaza el portal oficial).

## Principios del ecosistema Alahia (largo plazo)

| Código | Documento | Idea clave |
|--------|-----------|------------|
| **ALAHIA-TE-01** | [Principio-Transmission-Engine-Estrategico.md](./Principio-Transmission-Engine-Estrategico.md) | El Engine garantiza entrega confiable a **cualquier** destino externo; DGII es solo el primer Provider |

## Carpetas

| Carpeta | Contenido |
|---------|-----------|
| `definiciones/` | **Biblioteca de conocimiento Alahia** (DoD por tipo, ejemplos XML, historial de certificación) |
| `pdfs/` | 17 PDFs oficiales |
| `xsds/` | 15 XSD (esquemas XML) |
| `txt/` | Texto extraído de los PDFs |
| `pages/` | Páginas HTML relacionadas (texto plano) |

> Estándar Alahia: un comprobante no está terminado solo por aceptación DGII. Ver [definiciones/README.md](./definiciones/README.md).

## Arquitectura Alahia (además del material oficial)

| Documento | Contenido |
|-----------|-----------|
| [Principio-Transmission-Engine-Estrategico.md](./Principio-Transmission-Engine-Estrategico.md) | **ALAHIA-TE-01** — visión estratégica multi-destino |
| [Arquitectura-Transmission-Engine.md](./Arquitectura-Transmission-Engine.md) | Motor genérico: cola, retry, auditoría |
| [Arquitectura-Transmission-Providers.md](./Arquitectura-Transmission-Providers.md) | Providers / desacople; checklist pre-implementación |
| [Arquitectura-Contingencia-Reintentos.md](./Arquitectura-Contingencia-Reintentos.md) | Normativa DGII RD → perfil Provider DGII |
| [definiciones/](./definiciones/) | Biblioteca de conocimiento por tipo e-CF (Motor XML) |


## Catálogo oficial en la página

### Informe y Descripción Técnica
- Informe Técnico e-CF v1.0.pdf
- Descripcion Tecnica Emisores Electronicos.pdf
- Descripcion Tecnica Servicios DGII.pdf
- Representación Impresa (Modelos ilustrativos).pdf

### Formatos XML
- Formato Comprobante Fiscal Electrónico (e-CF) V1.0.pdf
- Formato Acuse de Recibo v 1.0.pdf
- Formato Aprobación Comercial v1.0.pdf
- Formato Anulación de e-NCF v1.0.pdf
- Formato Resumen Factura Consumo Electrónica v1.0.pdf

### Instructivos
- Firmado de e-CF.pdf
- Instructivo App Firma Digital.pdf
- Instructivo Delegaciones de Roles de Facturaciòn Electrónica.pdf
- Instructivo-Contingencia-FE.pdf
- Instructivo-Facturador-Gratuito-de-FE.pdf
- Solicitud Usuario Administrador de e-CF.pdf

### Proceso de Certificación
- Proceso de Certificacion para ser Emisor Electronico.pdf
- Proceso-Certificacion-EmisorElectronico-Proveedor-Servicios-FECertificado.pdf

### XSD
- e-CF 31..47, ACECF, ANECF, ARECF, RFCE 32, Semilla

## Páginas hermanas
- TipoyEstructurae-CF.aspx
- marcoLegal.aspx
- preguntasFrecuentes.aspx (enlaza FAQs PDF aparte)
- Novedades-FE.aspx
