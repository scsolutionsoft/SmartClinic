@echo off
setlocal enabledelayedexpansion

echo SmartClinic Card Reader Bridge Installer
echo.

set INSTALL_PATH=C:\Program Files\SmartClinic\CardReader
if not exist "%INSTALL_PATH%" mkdir "%INSTALL_PATH%"

echo Copying bridge files to %INSTALL_PATH%...
copy /Y "SmartClinic.CardReader.Bridge.exe" "%INSTALL_PATH%\" >nul
copy /Y "SmartClinic.CardReader.Bridge.dll" "%INSTALL_PATH%\" >nul
copy /Y "SmartClinic.CardReader.Bridge.deps.json" "%INSTALL_PATH%\" >nul
copy /Y "SmartClinic.CardReader.Bridge.runtimeconfig.json" "%INSTALL_PATH%\" >nul
copy /Y "SmartClinic.CardReader.Bridge.pdb" "%INSTALL_PATH%\" >nul
copy /Y "start-bridge.bat" "%INSTALL_PATH%\" >nul

if errorlevel 1 (
	echo Installation failed while copying files.
	pause
	exit /b 1
)

echo.
echo Installation completed.
echo Start the bridge with:
echo   %INSTALL_PATH%\start-bridge.bat
pause
