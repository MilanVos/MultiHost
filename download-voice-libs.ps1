# PowerShell script to download Discord.Net voice native libraries
# Run this script if you get "Voice libraries missing" errors

Write-Host "Discord.Net Voice Libraries Installer" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$targetDir = ".\bin\Debug\net8.0-windows\"
$libsodiumUrl = "https://github.com/RogueException/Discord.Net/raw/dev/voice-natives/win-x64/libsodium.dll"
$opusUrl = "https://github.com/RogueException/Discord.Net/raw/dev/voice-natives/win-x64/opus.dll"

# Check if target directory exists
if (-Not (Test-Path $targetDir)) {
    Write-Host "Error: Build directory not found!" -ForegroundColor Red
    Write-Host "Please build the project first with: dotnet build" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Target directory: $targetDir" -ForegroundColor Green
Write-Host ""

# Download libsodium.dll
Write-Host "Downloading libsodium.dll..." -ForegroundColor Yellow
try {
    $webClient = New-Object System.Net.WebClient
    $webClient.DownloadFile($libsodiumUrl, "$targetDir\libsodium.dll")
    Write-Host "libsodium.dll downloaded successfully" -ForegroundColor Green
} catch {
    Write-Host "Failed to download libsodium.dll" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Manual download instructions:" -ForegroundColor Yellow
    Write-Host "1. Go to: $libsodiumUrl" -ForegroundColor Yellow
    Write-Host "2. Save as: $targetDir\libsodium.dll" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Download opus.dll
Write-Host "Downloading opus.dll..." -ForegroundColor Yellow
try {
    $webClient = New-Object System.Net.WebClient
    $webClient.DownloadFile($opusUrl, "$targetDir\opus.dll")
    Write-Host "opus.dll downloaded successfully" -ForegroundColor Green
} catch {
    Write-Host "Failed to download opus.dll" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Manual download instructions:" -ForegroundColor Yellow
    Write-Host "1. Go to: $opusUrl" -ForegroundColor Yellow
    Write-Host "2. Save as: $targetDir\opus.dll" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Voice libraries installed!" -ForegroundColor Green
Write-Host ""
Write-Host "Files installed to: $targetDir" -ForegroundColor Yellow
Write-Host "- libsodium.dll"
Write-Host "- opus.dll"
Write-Host ""
Write-Host "You can now run MultiHost with voice support!" -ForegroundColor Green
Write-Host "Run with: dotnet run" -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to exit"
