# Actualiza plantillas predeterminadas en AlahiaPos_Dev desde Scripts/PlantillasHtml.
$ErrorActionPreference = 'Stop'
$SqlServer = '144.126.143.154\SQLEXPRESS,1433'
$Db = 'AlahiaPos_Dev'
$SqlUser = 'sa'
$SqlPassword = 'JoelAriel8787'
$Root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $PSScriptRoot 'PlantillasHtml'))) {
  $Root = $PSScriptRoot
}
$HtmlDir = Join-Path $PSScriptRoot 'PlantillasHtml'

function Update-Plantilla([string]$TipoLike, [string]$HtmlFile) {
  $path = Join-Path $HtmlDir $HtmlFile
  if (-not (Test-Path $path)) { throw "No existe $path" }
  $html = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)
  $escaped = $html.Replace("'", "''")
  $sql = @"
SET NOCOUNT ON;
UPDATE dbo.PlantillasDocumentosClinicos
SET ContenidoHTML = N'$escaped', Activa = 1
WHERE TipoDocumento LIKE N'$TipoLike' AND EsPredeterminada = 1;
SELECT TipoDocumento, @@ROWCOUNT AS Filas, LEN(ContenidoHTML) AS HtmlLength
FROM dbo.PlantillasDocumentosClinicos
WHERE TipoDocumento LIKE N'$TipoLike' AND EsPredeterminada = 1;
"@
  $tmp = Join-Path $env:TEMP ("alahia_plantilla_{0}.sql" -f ($HtmlFile -replace '[^\w]','_'))
  $utf16 = New-Object System.Text.UnicodeEncoding $false, $true
  [IO.File]::WriteAllText($tmp, $sql, $utf16)
  Write-Host "Actualizando $TipoLike desde $HtmlFile ..."
  sqlcmd -S $SqlServer -U $SqlUser -P $SqlPassword -d $Db -C -I -i $tmp
  if ($LASTEXITCODE -ne 0) { throw "sqlcmd fallo para $TipoLike (exit $LASTEXITCODE)" }
}

Update-Plantilla 'Receta%' 'receta-medica.html'
Update-Plantilla 'Certificado%' 'certificado-medico.html'
Write-Host 'Listo (solo AlahiaPos_Dev).'
