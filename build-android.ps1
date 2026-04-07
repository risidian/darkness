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

# 2. Auto-increment version/code and version/name in export_presets.cfg
if (Test-Path $PresetsPath) {
    $Content = Get-Content $PresetsPath -Raw
    
    # Increment version/code (Integer)
    if ($Content -match '(?m)^version/code=(\d+)') {
        $OldCode = [int]$Matches[1]
        $NewCode = $OldCode + 1
        $Content = $Content -replace "(?m)^version/code=\d+", "version/code=$NewCode"
        Write-Host "--- Incremented Version Code to: $NewCode ---" -ForegroundColor Yellow
    }

    # Increment version/name (String e.g. "1.0.19" -> "1.0.20")
    if ($Content -match '(?m)^version/name="(\d+)\.(\d+)\.(\d+)"') {
        $Major = $Matches[1]
        $Minor = $Matches[2]
        $Patch = [int]$Matches[3] + 1
        $NewName = "$Major.$Minor.$Patch"
        $Content = $Content -replace '(?m)^version/name=".*"', "version/name=`"$NewName`""
        Write-Host "--- Incremented Version Name to: $NewName ---" -ForegroundColor Yellow
    }

    $Content | Set-Content $PresetsPath -NoNewline
} else {
    Write-Host "--- export_presets.cfg not found at $PresetsPath ---" -ForegroundColor Gray
}

# 3. Ensure Gradle build fix for transient files is applied (Godot 4 + Gradle 8.x)
$GradlePath = "$ProjectDir/android/build/build.gradle"
if (Test-Path $GradlePath) {
    $GradleContent = Get-Content $GradlePath -Raw
    
    $Changed = $false

    # Fix aaptOptions to ignore .tmp files
    if ($GradleContent -match 'ignoreAssetsPattern "(!\.svn:!\.git:!\.gitignore:!\.ds_store:!\*\.scc:!CVS:!thumbs\.db:!picasa\.ini:!\*~)"') {
        $OldPattern = $Matches[1]
        $NewPattern = $OldPattern + ":*.tmp:*.TMP"
        $GradleContent = $GradleContent -replace [regex]::Escape($OldPattern), $NewPattern
        $Changed = $true
        Write-Host "--- Updated aaptOptions to ignore .tmp files ---" -ForegroundColor Yellow
    }

    # Fix packagingOptions to exclude .tmp files
    if ($GradleContent -match 'exclude ''META-INF/NOTICE''' -and !($GradleContent -match "exclude '\*\*/\*\.tmp'")) {
        $GradleContent = $GradleContent -replace "exclude 'META-INF/NOTICE'", "exclude 'META-INF/NOTICE'`n        exclude '**/*.tmp'`n        exclude '**/*.TMP'"
        $Changed = $true
        Write-Host "--- Updated packagingOptions to exclude .tmp files ---" -ForegroundColor Yellow
    }

    if ($Changed) {
        $GradleContent | Set-Content $GradlePath -NoNewline
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
