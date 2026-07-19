# Alahia.eCF.Api — API propia DGII (estilo suplidor)

Proyecto **independiente** en la misma solución. No modifica el flujo de `AlahiaPosApi` (sigue con PG.eInvoicing).

## Contrato (compatible con PG.eInvoicing)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/Receipt/health` | Health + ambiente DGII |
| GET | `/api/Receipt` | Ping |
| POST | `/api/Receipt` | Recibe JSON e-CF (mismo shape que PG) → firma → envía a DGII |
| GET | `/api/Receipt/{trackId}` | Consulta estado |
| GET | `/api/ecf/semilla` | Debug: semilla DGII |

Header: `X-Api-Key: <AlahiaEcfApi:ApiKey>`

## Config

`appsettings.json` → `DgiiDirecto` (ambiente `testecf`/`certecf`/`ecf`) + P12 + `AlahiaEcfApi:ApiKey`.

## Arranque

```bash
dotnet run --project Alahia.eCF.Api --urls http://localhost:5203
```

Swagger: http://localhost:5203/swagger

## Cómo apuntar el ERP más adelante (sin cambiar código del gateway)

En `AlahiaPosApi` appsettings:

```json
"PgEInvoicing": {
  "BaseUrl": "http://localhost:5203",
  "ApiKey": "alahia-ecf-local-dev-key"
}
```

Así el motor fiscal sigue hablando con `IFiscalGateway` → `PgEInvoicingGateway`, pero el “suplidor” eres tú.
