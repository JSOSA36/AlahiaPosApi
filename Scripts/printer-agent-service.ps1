# Alahia Printer Agent — registro resiliente del servicio Windows.
# No borra el servicio. Arranque delayed-auto, dependencia Spooler,
# recovery y tareas watchdog (boot + logon).
# Solo ASCII: Windows PowerShell 5.1.

function Ensure-AlahiaPrinterWindowsService {
  param(
    [Parameter(Mandatory = $true)][string]$ServiceName,
    [Parameter(Mandatory = $true)][string]$ExePath,
    [int]$Port = 5045
  )

  if (-not (Test-Path $ExePath)) {
    throw "No se encontro el ejecutable: $ExePath"
  }

  $bin = ('"{0}"' -f $ExePath)
  $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

  if ($svc) {
    Write-Host "Servicio $ServiceName ya existe: se actualiza (no se reinstala)." -ForegroundColor DarkGray
    if ($svc.Status -ne 'Stopped') {
      Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
      $deadline = (Get-Date).AddSeconds(20)
      do {
        Start-Sleep -Seconds 1
        $svc.Refresh()
      } while ($svc.Status -ne 'Stopped' -and (Get-Date) -lt $deadline)
    }
    sc.exe config $ServiceName binPath= $bin | Out-Null
  } else {
    Write-Host "Creando servicio $ServiceName..." -ForegroundColor Cyan
    New-Service -Name $ServiceName `
      -BinaryPathName $bin `
      -DisplayName "Alahia Printer Agent" `
      -Description "Agente local de impresion termica Alahia ERP. Arranca solo al encender el PC y se actualiza desde el servidor." `
      -StartupType Automatic | Out-Null
  }

  Set-AlahiaPrinterServiceResilience -ServiceName $ServiceName
  Ensure-AlahiaPrinterWatchdog -ServiceName $ServiceName

  $ruleName = "Alahia PrinterApi $Port"
  if (-not (Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow | Out-Null
  }

  Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
  Start-Sleep -Seconds 3
  $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
  if ($svc -and $svc.Status -eq 'Running') {
    Write-Host "Servicio $ServiceName en ejecucion (arranque automatico)." -ForegroundColor Green
  } else {
    Write-Host "Servicio registrado; si no arranca ahora, Windows lo levantara al iniciar (delayed-auto + watchdog)." -ForegroundColor Yellow
  }
}

function Set-AlahiaPrinterServiceResilience {
  param([Parameter(Mandatory = $true)][string]$ServiceName)

  # Delayed auto: espera Spooler/red. Si el servicio falla al boot, Windows lo reintenta.
  sc.exe config $ServiceName start= delayed-auto | Out-Null
  sc.exe config $ServiceName depend= Spooler | Out-Null
  sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/15000/restart/60000 | Out-Null
  sc.exe failureflag $ServiceName 1 | Out-Null
}

function Ensure-AlahiaPrinterWatchdog {
  param([Parameter(Mandatory = $true)][string]$ServiceName)

  # Si Windows Update deja el servicio parado, estas tareas lo vuelven a arrancar.
  $tr = "cmd.exe /c sc.exe start $ServiceName"
  schtasks.exe /Create /TN "AlahiaPrinterApi-Watchdog-Boot" /SC ONSTART /DELAY 0001:00 /RU SYSTEM /RL HIGHEST /F /TR $tr | Out-Null
  $logon = schtasks.exe /Create /TN "AlahiaPrinterApi-Watchdog-Logon" /SC ONLOGON /DELAY 0000:20 /RU SYSTEM /RL HIGHEST /F /TR $tr 2>&1
  if ($LASTEXITCODE -ne 0) {
    schtasks.exe /Create /TN "AlahiaPrinterApi-Watchdog-Logon" /SC ONLOGON /RU SYSTEM /RL HIGHEST /F /TR $tr | Out-Null
  }
}
