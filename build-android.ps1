# Darkness Android Build Script

$ProjectDir = "Darkness.Godot"
$OutputDir = "bin"
$ApkName = "Darkness.apk"
$ApkPath = "$OutputDir/$ApkName"

# 1. Create output directory if it doesn't exist
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir
}

Write-Host "--- Building C# Project ---" -ForegroundColor Cyan
dotnet build $ProjectDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "C# Build Failed!"
    exit $LASTEXITCODE
}

Write-Host "--- Exporting Android APK ---" -ForegroundColor Cyan
# Adjust 'godot' command if not in your PATH (e.g., "C:\Path\To\Godot.exe")
godot --headless --path $ProjectDir --export-debug "Android" "../$ApkPath"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Android Export Failed! Ensure Android SDK and Keystores are configured in Godot Editor Settings."
    exit $LASTEXITCODE
}

Write-Host "--- Success! APK created at: $ApkPath ---" -ForegroundColor Green
