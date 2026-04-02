# Darkness Android Build Script

$ProjectDir = "Darkness.Godot"
$OutputDir = "bin"
$ApkName = "Darkness.apk"
$ApkPath = "$OutputDir/$ApkName"
$PresetsPath = "$ProjectDir/export_presets.cfg"

# 1. Create output directory if it doesn't exist
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir
}

# 2. Auto-increment version/code in export_presets.cfg
if (Test-Path $PresetsPath) {
    $Content = Get-Content $PresetsPath -Raw
    if ($Content -match '(?m)^version/code=(\d+)') {
        $OldCode = [int]$Matches[1]
        $NewCode = $OldCode + 1
        $Content = $Content -replace "(?m)^version/code=\d+", "version/code=$NewCode"
        $Content | Set-Content $PresetsPath -NoNewline
        Write-Host "--- Incremented Version Code to: $NewCode ---" -ForegroundColor Yellow
    } else {
        Write-Host "--- Version Code not found in presets ---" -ForegroundColor Gray
    }
}

Write-Host "--- Building C# Project for Android ---" -ForegroundColor Cyan
dotnet build "$ProjectDir/$ProjectDir.csproj" -r android-arm64 --self-contained false

if ($LASTEXITCODE -ne 0) {
    Write-Error "C# Build Failed!"
    exit $LASTEXITCODE
}

Write-Host "--- Exporting Android APK ---" -ForegroundColor Cyan

# Try to find Godot executable
$GodotPath = "godot"
if (!(Get-Command $GodotPath -ErrorAction SilentlyContinue)) {
    # Search common paths
    $SearchPaths = @(
        "C:\Users\Mayce\Documents\GitHub\Godot_v4.6.1-stable_mono_win64\Godot.exe",
        "C:\Program Files\Godot\Godot_v4.6.1-stable_mono_win64.exe"
    )
    foreach ($Path in $SearchPaths) {
        if (Test-Path $Path) {
            $GodotPath = $Path
            break
        }
    }
}

Write-Host "Using Godot at: $GodotPath" -ForegroundColor Gray
& $GodotPath --headless --path $ProjectDir --export-debug "Android" "../$ApkPath"

# Verify file exists
if (Test-Path $ApkPath) {
    $FileSize = (Get-Item $ApkPath).Length / 1MB
    $FormattedSize = "{0:N2}" -f $FileSize
    Write-Host "--- Success! APK created at: $ApkPath ($FormattedSize MB) ---" -ForegroundColor Green
} else {
    Write-Error "Android Export Failed! The APK was not created. Check Godot's internal export settings and templates."
    exit 1
}
