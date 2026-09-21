@echo off
echo.
echo ================================================
echo   PDF QUICK DEBUG - EJECUTAR
echo ================================================
echo.
cd /d "%~dp0"
dotnet run
echo.
echo ================================================
echo   Presiona cualquier tecla para salir
echo ================================================
pause > nul
