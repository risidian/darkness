# Fix Android Build Asset Limit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve the Android build failure caused by exceeding the ZIP entry limit (65,535 files) by removing accidental folders and unused sprite assets.

**Architecture:** Identify and remove the accidental `AppData` folder and unused LPC sprite directories that are not referenced in `sprite-catalog.json`. This reduces the total file count in the APK to well within Android's limits.

**Tech Stack:** PowerShell, Godot 4.6.1, .NET 10.

---

### Task 1: Remove Accidental AppData Folder

**Files:**
- Delete: `Darkness.Godot/AppData`

- [ ] **Step 1: Verify AppData folder exists**

Run: `Test-Path Darkness.Godot/AppData`
Expected: True

- [ ] **Step 2: Remove AppData folder**

Run: `Remove-Item -Recurse -Force Darkness.Godot/AppData`

- [ ] **Step 3: Verify AppData folder is gone**

Run: `Test-Path Darkness.Godot/AppData`
Expected: False

- [ ] **Step 4: Commit**

```bash
git commit -m "chore: remove accidental AppData folder from project"
```

### Task 2: Clean Up Unused Sprites

**Files:**
- Modify: `Darkness.Godot/assets/sprites/full/` (delete unused subfolders)

- [ ] **Step 1: Identify unused sprite folders**

Based on `sprite-catalog.json`, we only need:
- `body/male`, `body/female`
- `armor/leather`, `armor/plate`
- `arms/gloves`
- `eyes/human/adult/anger`, `eyes/human/adult/default`, `eyes/human/adult/neutral`, `eyes/human/adult/sad`, `eyes/human/adult/shock`
- `face/female`, `face/male`
- `feet/shoes/basic`
- `hair/afro/adult`, `hair/bob/adult`, `hair/curly_long/adult`, `hair/long/adult`, `hair/plain/adult`, `hair/shorthawk/adult`, `hair/spiked/adult`
- `head/human/female`, `head/human/male`
- `legs/cuffed`, `legs/formal`, `legs/leggings`, `legs/pantaloons`, `legs/pants`
- `shields/crusader/bg`, `shields/spartan/bg`
- `torso/robes/female` (blue, red, white)
- `weapons/magic/wand`, `weapons/ranged/bow/normal/walk/foreground`, `weapons/sword/arming/universal/bg`, `weapons/sword/dagger`

We will move everything else to a temporary backup outside the repo, then delete if build passes.

Run:
```powershell
$keep = @(
    "body", "armor", "arms", "eyes", "face", "feet", "hair", "head", "legs", "shields", "torso", "weapons"
)
```

- [ ] **Step 2: Remove unused hair styles**

Run:
```powershell
$usedHair = @("afro", "bob", "curly_long", "long", "plain", "shorthawk", "spiked")
Get-ChildItem Darkness.Godot/assets/sprites/full/hair -Directory | Where-Object { $usedHair -notcontains $_.Name } | Remove-Item -Recurse -Force
```

- [ ] **Step 3: Remove unused shields**

Run:
```powershell
$usedShields = @("crusader", "spartan")
Get-ChildItem Darkness.Godot/assets/sprites/full/shields -Directory | Where-Object { $usedShields -notcontains $_.Name } | Remove-Item -Recurse -Force
```

- [ ] **Step 4: Remove unused weapons**

Run:
```powershell
$usedWeapons = @("magic", "ranged", "sword")
Get-ChildItem Darkness.Godot/assets/sprites/full/weapons -Directory | Where-Object { $usedWeapons -notcontains $_.Name } | Remove-Item -Recurse -Force
```

- [ ] **Step 5: Verify file count in .godot/imported**

Run:
```powershell
# We must first trigger Godot to update its cache by running it once
godot --headless --path Darkness.Godot --editor --quit
(Get-ChildItem Darkness.Godot/.godot/imported -Recurse | Measure-Object).Count
```
Expected: A number significantly less than 65,535.

- [ ] **Step 6: Commit changes**

```bash
git add .
git commit -m "chore: remove unused sprite assets to stay under Android ZIP limit"
```

### Task 3: Verify Build

**Files:**
- Run: `build-android.ps1`

- [ ] **Step 1: Clean build artifacts**

Run: `Remove-Item -Recurse -Force Darkness.Godot/android/build/build`

- [ ] **Step 2: Run build script**

Run: `powershell.exe -ExecutionPolicy Bypass -File .\build-android.ps1`

- [ ] **Step 3: Verify APK exists**

Run: `Test-Path bin/Darkness.apk`
Expected: True

- [ ] **Step 4: Commit build script changes (if any remaining)**

```bash
git add build-android.ps1 Darkness.Godot/android/build/build.gradle
git commit -m "build: fix build script and gradle config for Android"
```
