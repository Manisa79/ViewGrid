@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0Build-ViewGrid.ps1" %*
endlocal
