@echo off
setlocal

echo SmartClinic Card Reader Bridge Installer
echo.

set "SOURCE_PATH=%~dp0"
set "INSTALL_PATH=C:\Program Files\SmartClinic\CardReader"

if not exist "%SOURCE_PATH%SmartClinic.CardReader.Bridge.exe" (
	echo Installation files were not found.
	echo.
	echo Please extract the ZIP file first, then run this installer
	echo as Administrator from the extracted folder:
	echo   %SOURCE_PATH%
	pause
	exit /b 1
)

if not exist "%INSTALL_PATH%" (
	mkdir "%INSTALL_PATH%"
	if errorlevel 1 (
		echo Unable to create the installation folder.
		echo Please run this installer as Administrator.
		pause
		exit /b 1
	)
)

echo Copying bridge files to %INSTALL_PATH%...
xcopy "%SOURCE_PATH%*" "%INSTALL_PATH%\" /E /I /Y /Q >nul

if errorlevel 1 (
	echo Installation failed while copying files.
	pause
	exit /b 1
)

if not exist "%INSTALL_PATH%\SmartClinic.CardReader.Bridge.exe" (
	echo Installation failed: the bridge executable was not copied.
	pause
	exit /b 1
)

echo.
echo Installation completed.
echo Start the bridge with:
echo   %INSTALL_PATH%\start-bridge.bat
pause
