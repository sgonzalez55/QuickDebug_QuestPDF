@echo off
setlocal
cd /d "%~dp0"

echo Compilando el disenador...
dotnet build PdfDesigner.sln -v q -nologo
if errorlevel 1 (
    echo.
    echo ERROR: la compilacion fallo.
    pause
    exit /b 1
)

echo Iniciando el disenador...
start "" "PdfDesigner.WinForms\bin\Debug\net8.0-windows\PdfDesigner.WinForms.exe"
endlocal