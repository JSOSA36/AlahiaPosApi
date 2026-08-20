@echo off
title Alahia - Servicio de impresion
cd /d "%~dp0"

net session >nul 2>&1
if %errorLevel% neq 0 (
  echo Solicitando permisos de Administrador...
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

sc query AlahiaPrinterApi >nul 2>&1
if %errorlevel% neq 0 (
  echo El servicio AlahiaPrinterApi no esta instalado. Ejecute Install.bat.
  pause
  exit /b 1
)

schtasks /Delete /TN "AlahiaPrinterAgent" /F >nul 2>&1
sc config AlahiaPrinterApi start= delayed-auto
net start AlahiaPrinterApi >nul 2>&1

echo.
echo Servicio AlahiaPrinterApi: arranque automatico. El cliente no abre el .exe.
pause
