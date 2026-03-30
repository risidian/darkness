# Darkness: Godot Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the MAUI/MonoGame frontend with a unified Godot 4.3 (.NET 10) application while preserving the `Darkness.Core` business logic.

**Architecture:** Godot will act as the "Host" application, utilizing a C# `Global` Autoload for Dependency Injection (Microsoft DI) and an explicit `IInitializable` pattern for scene-to-scene data passing.

**Tech Stack:** Godot 4.3 (C# / .NET 10), Microsoft.Extensions.DependencyInjection.

---

### Task 1: Project Scaffolding & Core Reference

**Files:**
- Create: `Darkness.Godot/Darkness.Godot.csproj`
- Create: `Darkness.Godot/project.godot`
- Modify: `Darkness.sln`

- [ ] **Step 1: Create the Godot C# project file**
- [ ] **Step 2: Create basic Godot project config**
- [ ] **Step 3: Add to Solution**
- [ ] **Step 4: Verify Build**

---

### Task 2: Dependency Injection (DI) & Global Autoload

**Files:**
- Create: `Darkness.Godot/src/Core/Global.cs`
- Modify: `Darkness.Godot/project.godot`

- [ ] **Step 1: Create Global DI Autoload**
- [ ] **Step 2: Register Autoload in project.godot**

---

### Task 3: Navigation Service Implementation

**Files:**
- Create: `Darkness.Godot/src/Core/GodotNavigationService.cs`
- Create: `Darkness.Godot/src/Core/IInitializable.cs`

- [ ] **Step 1: Define IInitializable interface**
- [ ] **Step 2: Implement Navigation Logic**

---

### Task 4: Main Menu & User Loading (UI Migration)

**Files:**
- Create: `Darkness.Godot/scenes/MainScene.tscn`
- Create: `Darkness.Godot/src/UI/MainScene.cs`
- Create: `Darkness.Godot/scenes/LoadUserScene.tscn`
- Create: `Darkness.Godot/src/UI/LoadUserScene.cs`

- [ ] **Step 1: Create MainScene UI**
- [ ] **Step 2: Implement LoadUser Logic**

---

### Task 5: Character Creator (Sprite Preview Migration)

**Files:**
- Create: `Darkness.Godot/scenes/CharacterGenScene.tscn`
- Create: `Darkness.Godot/src/UI/CharacterGenScene.cs`

- [ ] **Step 1: Port Sprite Preview**

---

### Task 6: Game World & Battle (Game Logic Migration)

**Files:**
- Create: `Darkness.Godot/scenes/WorldScene.tscn`
- Create: `Darkness.Godot/src/Game/WorldScene.cs`
- Create: `Darkness.Godot/scenes/BattleScene.tscn`
- Create: `Darkness.Godot/src/Game/BattleScene.cs`

- [ ] **Step 1: Port World Interaction**
- [ ] **Step 2: Port Battle Logic**

---

### Task 7: Final Cleanup & Android Export

- [ ] **Step 1: Remove MAUI and MonoGame projects**
- [ ] **Step 2: Configure Android Export Presets in Godot**
- [ ] **Step 3: Run Smoke Test on Windows and Android emulator**
