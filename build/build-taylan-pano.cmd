@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0Build-Pano.ps1" %*
endlocal
