# ALAHIA-FE-EXT-01 — Camino canónico: proveedor externo (Invoice / PG.eInvoicing)

**Estado:** Validado en sandbox (2026-08-05) con Terraza / MATBERT SRL (`IdEmpresa = 55`, RNC `133176076`) en `AlahiaPos_Dev`.  
**Alcance:** Cómo conectar Alahia ERP a un **suplidor externo** de e-CF **sin** pasar por `Alahia.eCF.Api`.  
**No confundir con:** modo `DGII_DIRECTO` (Alahia firma y envía a DGII vía nuestra API).

---

## 1. Principio

El Motor Fiscal / ERP solo habla `IFiscalGateway`. El adaptador HTTP envía el **mismo wire** que entiende Invoice:

- `POST /api/Receipt` — emitir
- `GET /api/Receipt/{trackId}` — consultar estado
- Header `X-Api-Key: <clave del tenant>`
- Body JSON `DgiiDocumentDto` (camelCase; `eNCF` explícito)

**No** inventar otro contrato ni otro adaptador “por suplidor” si el suplidor es compatible con `/api/Receipt`.

Código de referencia (no duplicar):

| Pieza | Archivo |
|-------|---------|
| Resolución por empresa | `EmpresaFiscalGatewayResolver.cs` |
| HTTP wire | `HttpReceiptFiscalGateway.cs` |
| Mapeo Alahia → Invoice | `PgEInvoicing/PgEInvoicingMapper.cs` |
| DTOs wire | `PgEInvoicing/PgEInvoicingModels.cs` |
| Builder fiscal | `FiscalDocumentoBuilder.cs` |
| Emisión síncrona | `POST /api/FacturacionElectronica/emitir-enviar` |

---

## 2. Configuración por empresa (única fuente)

Tabla `Empresas` — **no** poner la URL del suplidor en `FiscalGateway` global si solo una empresa lo usa.

```text
ProveedorFE           = PROVEEDOR_EXTERNO
ProveedorFE_Nombre    = PG.eInvoicing   (o nombre del suplidor)
ProveedorFE_BaseUrl   = https://sbx-ecf.einvoicing.com.do   (sin slash final obligatorio)
ProveedorFE_ApiKey    = <API key del portal del tenant>
ProveedorFE_Usuario   = (opcional Basic)
ProveedorFE_Password  = (opcional Basic)
EsEmisorElectronico   = 1
AmbienteFE            = testecf | certecf | ecf   (informativo en modo externo)
```

También requerido:

- `DgiiConfiguracionEmpresa.FacturacionElectronicaActiva = 1`
- Filas activas en `SecuenciasECF` por tipo (`TipoEcfDgii` 31 / 32 / 34, `Serie` E31/E32/E34, `fechaVencimiento` vigente)

API de UI/admin: `PUT /api/FacturacionElectronica/proveedor/{idEmpresa}`.

Sandbox Invoice validado:

- Base: `https://sbx-ecf.einvoicing.com.do`
- Tenant de prueba: MATBERT / RNC `133176076`
- OpenAPI: `https://sbx-ecf.einvoicing.com.do/api/client/specification.json`

---

## 3. Flujo de emisión (exactamente este)

```text
POS / NC comercial
  → FacturacionElectronicaService.EmitirYEnviarAsync
  → FiscalDocumentoBuilder.Build
  → HttpReceiptFiscalGateway.EnviarDocumentoAsync
       ResolveAsync(idEmpresa) → PROVEEDOR_EXTERNO + BaseUrl + ApiKey
       PgEInvoicingMapper.ToProviderModel(documento)
       POST {BaseUrl}/api/Receipt  + X-Api-Key
  → Actualiza ECFEncabezado (TrackId, EstadoDGII, UrlQR, SecurityCode)
  → Propaga e-NCF al documento comercial
```

Endpoint de prueba:

```http
POST /api/FacturacionElectronica/emitir-enviar
Content-Type: application/json

{
  "idEmpresa": 55,
  "tipoEcfDgii": 31,
  "origenDocumento": 1,
  "idOrigen": <IdFacturaHeader>,
  "idUsuario": <id>
}
```

| Tipo | `tipoEcfDgii` | `origenDocumento` | Notas |
|------|---------------|-------------------|--------|
| Consumo E32 | 32 | `1` Pos | Suele **no** traer trackId real (GUID cero); validar con `GET .../facturasconsumo?ecf=` |
| Crédito fiscal E31 | 31 | `1` Pos | Requiere RNC comprador válido; `IndicadorMontoGravado` 0/1 si hay gravados |
| Nota crédito E34 | 34 | `3` NotaCredito | Obligatorio `NCFModificado` + fecha + `CodigoModificacion`; ver §4 |

---

## 4. Reglas de trama que ya funcionaron (no romper)

### Auth y URL

- Solo `X-Api-Key` (y/o Basic si el suplidor lo pide).
- **No** enviar `X-Dgii-Ambiente` en modo `PROVEEDOR_EXTERNO` (eso es para Alahia.eCF.Api).

### IdDoc — orden y campos (JSON → XSD del suplidor)

Orden efectivo en `PgIdDocDto` (respetar):

1. `tipoeCF`
2. `eNCF`
3. `indicadorNotaCredito` (solo E34)
4. `indicadorMontoGravado`
5. `fechaVencimientoSecuencia` (**omitir en E34**)
6. `tipoIngresos`
7. `tipoPago`
8. `tablaFormasPago` (**omitir en E34**)

### E32 (consumo)

- Invoice puede responder `Aceptado` con `trackId = 00000000-0000-0000-0000-000000000000`.
- Eso es **esperado** en resumen/consumo; no tratarlo como fallo.
- Confirmación: `GET /api/Receipt/facturasconsumo?ecf=E32...`

### E31 (crédito fiscal)

- Comprador con RNC real del sandbox/producción del tenant.
- Líneas con ITBIS coherente (`IndicadorMontoGravado = 0` → montos **sin** ITBIS en ítem).
- Debe devolver `trackId` UUID real.

### E34 (nota de crédito)

- Siempre mapear `IndicadorNotaCredito` (0 ≤30 días / 1 >30 días respecto al NCF modificado).
- **No** enviar `fechaVencimientoSecuencia` ni `tablaFormasPago`.
- `InformacionReferencia`: `ncfModificado`, `fechaNCFModificado`, `codigoModificacion`, `razonModificacion`.
- Origen comercial: tabla `NotasCredito` + `NotaCreditoDocumentoResolver`.

### Secuencias

- La secuencia local (`SecuenciasECF`) y la del portal Invoice deben estar alineadas.
- Si Invoice responde *“Este número de secuencia ya ha sido utilizado”*, avanzar `SecuenciaActual` local y reintentar con e-NCF nuevo (no reusar el rechazado).

---

## 5. Health / conectividad

Invoice **no** expone `GET /api/Receipt` (solo POST) → un GET puro da **405**. Eso **no** significa que el suplidor esté caído.

Chequeos válidos:

- `GET /api/Receipt/pendienteAprobacion` con `X-Api-Key`, o
- Emisión de prueba, o
- `GET /api/Receipt/facturasconsumo?ecf=...`

No marcar “desconectado” solo por 405 en GET `/api/Receipt`.

---

## 6. Pruebas ya aceptadas (referencia)

Empresa 55 · Dev · `https://sbx-ecf.einvoicing.com.do`:

| Tipo | e-NCF | Resultado Invoice |
|------|-------|-------------------|
| E32 | `E320000000003` | Aceptado (sin trackId útil) |
| E31 | `E310000000020` | Aceptado (comprador RNC `133307847`) |
| E34 | `E340000000011` | Aceptado (modifica `E310000000020`) |

---

## 7. Checklist al conectar un cliente nuevo al suplidor

1. Tenant e API key creados en el portal del suplidor (mismo RNC emisor que `Empresas.RNC`).
2. `ProveedorFE = PROVEEDOR_EXTERNO` + `BaseUrl` + `ApiKey` en esa empresa.
3. FE activa + secuencias E31/E32/E34 con rango libre **alineado** al portal.
4. Probar `emitir-enviar` E32 → E31 → E34 (NC sobre el E31).
5. No cambiar el mapper wire ni el path `/api/Receipt` sin actualizar este documento.

---

## 8. Qué no hacer

- No apuntar `FiscalGateway:BaseUrl` global a Invoice “para todas” si otras empresas siguen en Alahia.eCF.Api — usar **por empresa**.
- No reintroducir un segundo gateway paralelo (`PgEInvoicingGateway` directo) en el ERP; el camino es `HttpReceiptFiscalGateway` + resolver.
- No editar montos/NCF fuera del builder/mapper para “arreglar” un rechazo: corregir datos comerciales o reglas de tipo (E31/E34).
- No usar NCF tradicionales B01/B02 en pruebas fiscales (solo e-CF `E*`).
