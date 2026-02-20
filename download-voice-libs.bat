@echo off
REM Batch script to download Discord.Net voice native libraries
REM Run this script if you get "Voice libraries missing" errors

echo ==========================================
echo Discord.Net Voice Libraries Installer
echo ==========================================
echo.

set TARGET_DIR=bin\Debug\net8.0-windows

if not exist "%TARGET_DIR%" (
    echo Error: Build directory not found!
    echo Please build the project first with: dotnet build
    pause
    exit /b 1
)

echo Target directory: %TARGET_DIR%
echo.

echo Downloading libsodium.dll...
powershell -Command "Invoke-WebRequest -Uri 'https://github.com/RogueException/Discord.Net/raw/dev/voice-natives/win-x64/libsodium.dll' -OutFile '%TARGET_DIR%\libsodium.dll'"
if %ERRORLEVEL% NEQ 0 (
    echo Failed to download libsodium.dll
    pause
    exit /b 1
)
echo OK - libsodium.dll downloaded

echo Downloading opus.dll...
powershell -Command "Invoke-WebRequest -Uri 'https://github.com/RogueException/Discord.Net/raw/dev/voice-natives/win-x64/opus.dll' -OutFile '%TARGET_DIR%\opus.dll'"
if %ERRORLEVEL% NEQ 0 (
    echo Failed to download opus.dll
    pause
    exit /b 1
)
echo OK - opus.dll downloaded

echo.
echo ==========================================
echo Voice libraries installed successfully!
echo ==========================================
echo.
echo Files installed to: %TARGET_DIR%
echo - libsodium.dll
echo - opus.dll
echo.
echo You can now run MultiHost with voice support!
echo Run with: dotnet run
echo.
pause
