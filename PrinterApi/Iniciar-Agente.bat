@echo off
cd /d "%~dp0"

sc query AlahiaPrinterApi >nul 2>&1
if %errorlevel%==0 (
  sc config AlahiaPrinterApi start= delayed-auto >nul 2>&1
  net start AlahiaPrinterApi >nul 2>&1
  exit /b 0
)

start "" /MIN AlahiaPrinterApi.exe
exit /b 0
