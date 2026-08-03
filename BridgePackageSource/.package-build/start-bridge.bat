@echo off
setlocal
cd /d "C:\Program Files\SmartClinic\CardReader"
if not exist "SmartClinic.CardReader.Bridge.exe" (
  echo SmartClinic.CardReader.Bridge.exe not found.
  echo Copy the compiled executable into this folder first.
  pause
  exit /b 1
)

echo Checking for stale bridge/port 9999...
taskkill /F /IM "SmartClinic.CardReader.Bridge.exe" >nul 2>&1

for /f "tokens=5" %%P in ('netstat -ano ^| findstr /R /C:":9999 .*LISTENING"') do (
  if not "%%P"=="0" (
    echo Port 9999 is in use by PID %%P. Stopping it...
    taskkill /F /PID %%P >nul 2>&1
  )
)

echo Starting SmartClinic Card Reader Bridge...
start "SmartClinic Card Reader Bridge" "SmartClinic.CardReader.Bridge.exe"
