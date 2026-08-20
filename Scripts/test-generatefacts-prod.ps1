$ErrorActionPreference = 'Continue'
$loginBody = @{
  userName = 'demo.belleza@alahiapos.com'
  password = 'DemoAlahia2026'
  deviceId = 'cursor-test-prod'
} | ConvertTo-Json

function Test-Api($base) {
  Write-Output "==== $base ===="
  $sw = [Diagnostics.Stopwatch]::StartNew()
  try {
    $login = Invoke-RestMethod -Uri "$base/Login/login" -Method POST -Body $loginBody -ContentType 'application/json' -TimeoutSec 30
    $sw.Stop()
    Write-Output ("LOGIN {0}ms keys={1}" -f $sw.ElapsedMilliseconds, ($login.PSObject.Properties.Name -join ','))
    $token = $login.token
    if (-not $token) { $token = $login.Token }
    if (-not $token) { Write-Output ($login | ConvertTo-Json -Depth 4); return }

    $headers = @{ Authorization = "Bearer $token" }
    $sw = [Diagnostics.Stopwatch]::StartNew()
    try {
      $dto = @{
        idFactura = 13823
        tipoFactura = 'Contado'
        imprimirFactura = $false
        formaPago = 'EFECTIVO'
        detallePagos = @(@{ metodo = 'EFECTIVO'; monto = 800 })
        detalleAbono = @()
      } | ConvertTo-Json -Depth 5
      $resp = Invoke-WebRequest -Uri "$base/FacturaHeader/GenerateFacts" -Method POST -Headers $headers -Body $dto -ContentType 'application/json' -TimeoutSec 90
      $sw.Stop()
      Write-Output ("GENERATE {0}ms status={1} body={2}" -f $sw.ElapsedMilliseconds, $resp.StatusCode, $resp.Content)
    } catch {
      $sw.Stop()
      $ex = $_.Exception
      $respErr = $_.Exception.Response
      Write-Output ("GENERATE FAIL {0}ms {1}" -f $sw.ElapsedMilliseconds, $ex.Message)
      if ($respErr) {
        try {
          $reader = New-Object IO.StreamReader($respErr.GetResponseStream())
          Write-Output ("BODY " + $reader.ReadToEnd())
        } catch {}
      }
    }
  } catch {
    $sw.Stop()
    Write-Output ("LOGIN FAIL {0}ms {1}" -f $sw.ElapsedMilliseconds, $_.Exception.Message)
  }
}

Test-Api 'https://alahiaposapidemo.alahiapos.com/api'
Test-Api 'https://alahiabeautyapiprod.alahiapos.com/api'
