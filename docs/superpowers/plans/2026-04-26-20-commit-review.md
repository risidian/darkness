# 20-Commit Deep Review and Build Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Perform an in-depth code review of the last 20 commits to identify gaps, risks, and improvements, and resolve all existing build warnings.

**Architecture:** Systematic review using parallel subagents for independent commit analysis, followed by centralized collation and remediation.

**Tech Stack:** .NET 10, git, Gemini Subagents.

---

### Task 1: Fix Build Warnings

**Files:**
- Modify: `Darkness.WebAPI\Models\Users.cs`
- Modify: `Darkness.WebAPI\Models\Characters.cs`

- [ ] **Step 1: Fix warnings in Users.cs**
Update non-nullable string properties to be `required` or nullable.
- [ ] **Step 2: Fix warnings in Characters.cs**
Update non-nullable string properties to be `required` or nullable.
- [ ] **Step 3: Verify build passes with no warnings**
Run: `dotnet build Darkness.sln --nologo /v:q /clp:ErrorsOnly;WarningsOnly`

### Task 2: Dispatch Review Subagents (Batch 1: Commits 1-10)

- [ ] **Step 1: Dispatch 10 subagents to review commits 9ab96b8e8 to 80860c0fb**
Each subagent will use `git show <hash>` and analyze for:
  - Logic errors
  - Security risks
  - Performance issues
  - Adherence to project conventions
  - Completeness of implementation

### Task 3: Dispatch Review Subagents (Batch 2: Commits 11-20)

- [ ] **Step 1: Dispatch 10 subagents to review commits 67c6b3a77 to 54cbb0c1f**
Same criteria as Batch 1.

### Task 4: Collate Findings and Recommend Improvements

- [ ] **Step 1: Assemble all subagent reports**
- [ ] **Step 2: Identify common themes, critical risks, and low-hanging fruit**
- [ ] **Step 3: Present a summary of recommended changes**

### Task 5: Implement Recommended Changes

- [ ] **Step 1: Implement fixes for identified risks**
- [ ] **Step 2: Implement improvements for code quality/gaps**
- [ ] **Step 3: Verify with tests**
