# Update LiteDB Test Initialization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Update all service test files to use a private `BsonMapper` instance when creating a `LiteDatabase` to prevent thread interference in the global LiteDB type cache.

**Architecture:** Each test class's constructor will be modified to pass a `new BsonMapper()` instance to the `LiteDatabase` constructor. This ensures that each test run has its own mapping context.

**Tech Stack:** .NET 10, LiteDB, XUnit.

---

### Task 1: Update TriggerServiceTests

**Files:**
- Modify: `Darkness.Tests/Services/TriggerServiceTests.cs:16`

- [ ] **Step 1: Update LiteDatabase constructor call**

```csharp
<<<<
        _db = new LiteDatabase(_dbPath);
====
        _db = new LiteDatabase(_dbPath, new BsonMapper());
>>>>
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Tests/Services/TriggerServiceTests.cs
git commit -m "test: use private BsonMapper in TriggerServiceTests"
```

### Task 2: Update SpriteSeederTests

**Files:**
- Modify: `Darkness.Tests/Services/SpriteSeederTests.cs:21`

- [ ] **Step 1: Update LiteDatabase constructor call**

```csharp
<<<<
        _db = new LiteDatabase(_dbPath);
====
        _db = new LiteDatabase(_dbPath, new BsonMapper());
>>>>
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Tests/Services/SpriteSeederTests.cs
git commit -m "test: use private BsonMapper in SpriteSeederTests"
```

### Task 3: Update QuestServiceTests

**Files:**
- Modify: `Darkness.Tests/Services/QuestServiceTests.cs:16`

- [ ] **Step 1: Update LiteDatabase constructor call**

```csharp
<<<<
        _db = new LiteDatabase(_dbPath);
====
        _db = new LiteDatabase(_dbPath, new BsonMapper());
>>>>
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Tests/Services/QuestServiceTests.cs
git commit -m "test: use private BsonMapper in QuestServiceTests"
```

### Task 4: Update QuestSeederTests

**Files:**
- Modify: `Darkness.Tests/Services/QuestSeederTests.cs:21`

- [ ] **Step 1: Update LiteDatabase constructor call**

```csharp
<<<<
        _db = new LiteDatabase(_dbPath);
====
        _db = new LiteDatabase(_dbPath, new BsonMapper());
>>>>
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Tests/Services/QuestSeederTests.cs
git commit -m "test: use private BsonMapper in QuestSeederTests"
```

### Task 5: Update LevelSeederTests

**Files:**
- Modify: `Darkness.Tests/Services/LevelSeederTests.cs:20`

- [ ] **Step 1: Update LiteDatabase constructor call**

```csharp
<<<<
        _db = new LiteDatabase(_dbPath);
====
        _db = new LiteDatabase(_dbPath, new BsonMapper());
>>>>
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Tests/Services/LevelSeederTests.cs
git commit -m "test: use private BsonMapper in LevelSeederTests"
```

### Task 6: Update LevelingServiceTests

**Files:**
- Modify: `Darkness.Tests/Services/LevelingServiceTests.cs:16`

- [ ] **Step 1: Update LiteDatabase constructor call**

```csharp
<<<<
        _db = new LiteDatabase(_dbPath);
====
        _db = new LiteDatabase(_dbPath, new BsonMapper());
>>>>
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Tests/Services/LevelingServiceTests.cs
git commit -m "test: use private BsonMapper in LevelingServiceTests"
```

### Task 7: Verification

- [ ] **Step 1: Run all tests to ensure stability**

Run: `dotnet test Darkness.Tests`
Expected: All tests pass.
