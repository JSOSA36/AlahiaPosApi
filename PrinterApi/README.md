# Alahia Printer Agent (PrinterApi)

Agente local de impresión térmica para Alahia ERP. Escucha en `http://localhost:5045`.

## Para el cliente (desde el ERP)

1. En Alahia: **Empresa → Abrir guía de instalación** (`/agente-impresion`).
2. Descargar el ZIP, descomprimir y ejecutar **Install.bat** como administrador.
3. Espere el mensaje OK. **Instale una sola vez.**

El agente queda como servicio Windows: arranca al encender el PC, se recupera si se cae y se actualiza solo desde el servidor. No hay que volver a instalar tras un reinicio o Windows Update. Si quedara parado, use `Repair.bat` (no Install.bat).

El botón de descarga apunta a:

`https://alahiaposapidemo.alahiapos.com/updates/printer/AlahiaPrinterAgent-Setup.zip`

## Empaquetar y publicar (tu PC de desarrollo)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-client-setup.ps1 -Version 1.0.6
```

Salida:

- `artifacts\printer-client-setup\AlahiaPrinterAgent-Setup-1.0.6.zip`
- `artifacts\printer-client-setup\AlahiaPrinterAgent-Setup.zip` ← **subir este** a `updates/printer/`

Self-contained: el cliente **no** instala .NET a mano.

## Endpoints útiles

- `GET /api/Printer/ping` — estado + `version`
- `GET /api/Printer/version`
- `GET /api/Printer/factura/{id}`
- `GET /api/Printer/recibo-abono/{idPago}`

## Auto-update (versiones siguientes)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-printer-release.ps1 -Version 1.1.0 -Notes "Mejoras"
```

Suba el zip + `latest.json` a `…/updates/printer/`.  
Ese zip también es **self-contained**, para que el update no dependa de .NET en la PC.

## Config local (no se pisa en updates)

`%ProgramData%\Alahia\PrinterApi\appsettings.Local.json`

## Dev (instalar desde el repo)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-printer-agent.ps1 `
  -ApiBaseUrl "http://localhost:5039" `
  -PrinterName "TuImpresora"
```

Por defecto también publica self-contained. Use `-FrameworkDependent` solo si ya tiene el runtime en esa máquina.
