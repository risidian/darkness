# Game Balance QA Audit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Identify root causes of "unwinnable battles" and logic soft-locks through a tri-lens sub-agent audit and cross-critique.

**Architecture:** Three `generalist` sub-agents will be dispatched to analyze specific engine domains (Combat, Growth, Logic). They will create empirical simulation scripts in the `Darkness.Tests` project to validate their findings. A final cross-critique phase will synthesize their reports into a prioritized risk heatmap.

**Tech Stack:** .NET 10, XUnit, LiteDB, PowerShell.

---

### Task 1: Setup Audit Environment

**Files:**
- Create: `Darkness.Tests/Audit/.gitkeep`
- Modify: `Darkness.sln` (verify project references)

- [ ] **Step 1: Create audit directory**
Run: `New-Item -ItemType Directory -Path Darkness.Tests/Audit -Force`

- [ ] **Step 2: Verify test project builds**
Run: `dotnet build Darkness.Tests/Darkness.Tests.csproj`
Expected: SUCCESS

- [ ] **Step 3: Commit setup**
Run: `git add Darkness.Tests/Audit; git commit -m "chore: setup audit environment"`

---

### Task 2: Combat Math Specialist Audit (Agent 1)

**Files:**
- Create: `Darkness.Tests/Audit/CombatSim.cs`
- Create: `docs/superpowers/audit/report-agent-1.md`

- [ ] **Step 1: Dispatch Agent 1 to analyze Combat Math**
Task: "Analyze `CombatEngine.cs`, `Enemy.cs`, `Skill.cs` and `assets/data/skills.json`. Create a simulation test `CombatSim.cs` in `Darkness.Tests/Audit` that runs 1,000 rounds of Player vs Enemy for levels 1-20. Identify specific level gaps where hit chance or damage output drops below 15%. Save findings to `docs/superpowers/audit/report-agent-1.md`."

- [ ] **Step 2: Review Agent 1's report and simulation results**
Check `docs/superpowers/audit/report-agent-1.md` for specific "Math Walls."

- [ ] **Step 3: Commit Agent 1 findings**
Run: `git add Darkness.Tests/Audit/CombatSim.cs docs/superpowers/audit/report-agent-1.md; git commit -m "audit: combat math specialist findings"`

---

### Task 3: Growth & Economy Auditor Audit (Agent 2)

**Files:**
- Create: `docs/superpowers/audit/report-agent-2.md`

- [ ] **Step 1: Dispatch Agent 2 to analyze Growth Scaling**
Task: "Analyze `LevelingService.cs`, `TalentService.cs`, `level-table.json`, and `talent-trees.json`. Compare the attribute/talent point gain rate against the 'Math Walls' identified in Agent 1's report. Determine if 'Accuracy Decay' occurs (enemies gaining AC faster than players gain Accuracy). Check if a 'strength-only' or 'magic-only' build becomes unplayable at any point. Save findings to `docs/superpowers/audit/report-agent-2.md`."

- [ ] **Step 2: Review Agent 2's report**
Check for alignment between player power curves and enemy scaling.

- [ ] **Step 3: Commit Agent 2 findings**
Run: `git add docs/superpowers/audit/report-agent-2.md; git commit -m "audit: growth & economy auditor findings"`

---

### Task 4: Logic & Integrity Guard Audit (Agent 3)

**Files:**
- Create: `Darkness.Tests/Audit/QuestGraphAudit.cs`
- Create: `docs/superpowers/audit/report-agent-3.md`

- [ ] **Step 1: Dispatch Agent 3 to analyze Quest Logic**
Task: "Analyze `QuestService.cs`, `ConditionEvaluator.cs`, and all JSON files in `assets/data/quests/`. Create a tool/test `QuestGraphAudit.cs` to scan for dead-end dialogue branches or steps where all `BranchOptions` have impossible conditions (e.g. requires Morality > 100). Verify that 'Combat Loss' steps correctly handle state to prevent soft-locks. Save findings to `docs/superpowers/audit/report-agent-3.md`."

- [ ] **Step 2: Review Agent 3's report**
Check for unreachable content or state-traps.

- [ ] **Step 3: Commit Agent 3 findings**
Run: `git add Darkness.Tests/Audit/QuestGraphAudit.cs docs/superpowers/audit/report-agent-3.md; git commit -m "audit: logic & integrity guard findings"`

---

### Task 5: Cross-Critique & Synthesis

**Files:**
- Create: `docs/superpowers/audit/final-balance-report.md`

- [ ] **Step 1: Conduct Cross-Critique**
Use a final sub-agent pass to identify contradictions between reports (e.g. Agent 1 says a battle is unwinnable, but Agent 2 identifies a Talent that makes it easy).

- [ ] **Step 2: Synthesize Final Report**
Consolidate all verified findings into `docs/superpowers/audit/final-balance-report.md` with a prioritized "Fix Map."

- [ ] **Step 3: Final Commit**
Run: `git add docs/superpowers/audit/final-balance-report.md; git commit -m "audit: complete game balance qa report"`
