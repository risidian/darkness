# Fix SkiaSharp Android Initialization Error Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the `TypeInitializationException` for `SkiaSharp.SKImageInfo` on Android by ensuring native Skia libraries are included in the APK.

**Architecture:** Add `SkiaSharp.NativeAssets.Android` to the `Darkness.Godot` project to provide the required `libSkiaSharp.so` files for Android architectures.

**Tech Stack:** .NET 10, SkiaSharp 3.119.2, Godot 4.6.1.

---

### Task 1: Add SkiaSharp Android Native Assets

**Files:**
- Modify: `Darkness.Godot/Darkness.Godot.csproj`

- [ ] **Step 1: Add package reference to `Darkness.Godot.csproj`**

```xml
<Project Sdk="Godot.NET.Sdk/4.6.1">
  <!-- ... existing property groups ... -->
  <ItemGroup>
    <ProjectReference Include="..\Darkness.Core\Darkness.Core.csproj" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.2" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.2" />
    <PackageReference Include="SkiaSharp.NativeAssets.Android" Version="3.119.2" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Commit the change**

```bash
git add Darkness.Godot/Darkness.Godot.csproj
git commit -m "fix: add SkiaSharp.NativeAssets.Android to include native libraries in APK"
```

---

### Task 2: Verification

- [ ] **Step 1: Build for Android locally**

Run: `dotnet build Darkness.Godot/Darkness.Godot.csproj -r android-arm64 --self-contained false`
Expected: SUCCESS

- [ ] **Step 2: Verify build success**

Confirm the build completed without NuGet restore errors or package version conflicts.
