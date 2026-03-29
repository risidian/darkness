# Gemini Auto-Code Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a GitHub Action that triggers the Gemini CLI to implement code changes when an issue is labeled with "Ready".

**Architecture:** A GitHub Action workflow file that listens for `issues` events, installs the Gemini CLI, and runs it in headless mode with the issue's context.

**Tech Stack:** GitHub Actions, YAML, Node.js (npm), Gemini CLI.

---

### Task 1: Create Workflow File Structure

**Files:**
- Create: `.github/workflows/gemini-auto-code.yml`

- [ ] **Step 1: Write the base workflow configuration**

```yaml
name: Gemini Auto-Code Workflow

on:
  issues:
    types: [labeled]

jobs:
  auto-code:
    if: github.event.label.name == 'Ready'
    runs-on: ubuntu-latest
    steps:
      - name: Checkout Repository
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install Gemini CLI
        run: npm install -g @google/gemini-cli
```

- [ ] **Step 2: Commit base configuration**

```bash
git add .github/workflows/gemini-auto-code.yml
git commit -m "feat: add initial gemini-auto-code workflow structure"
```

---

### Task 2: Add CLI Execution Logic

**Files:**
- Modify: `.github/workflows/gemini-auto-code.yml`

- [ ] **Step 1: Add the execution step with environment variables and prompt**

```yaml
      - name: Run Gemini Task
        env:
          GEMINI_API_KEY: ${{ secrets.GEMINI_API_KEY }}
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          # Construct the prompt with issue context
          PROMPT="The status of this issue is 'Ready'. Issue #${{ github.event.issue.number }}: ${{ github.event.issue.title }}\n\n${{ github.event.issue.body }}\n\nResearch the codebase, implement the fix/feature, and create a Pull Request on a new branch."
          
          # Run Gemini CLI in headless mode
          echo -e "$PROMPT" | gemini -p
```

- [ ] **Step 2: Commit execution logic**

```bash
git add .github/workflows/gemini-auto-code.yml
git commit -m "feat: add execution logic to gemini-auto-code workflow"
```

---

### Task 3: Verification & Finalization

- [ ] **Step 1: Verify workflow syntax**

Run: `actionlint .github/workflows/gemini-auto-code.yml` (if available) or manually review the file.

- [ ] **Step 2: Verify GEMINI_API_KEY requirement in documentation**

- Modify: `docs/superpowers/specs/2026-03-29-gemini-auto-code-workflow-design.md` to include a "Setup" section about adding the secret.

```markdown
## Setup Requirements
1. **GitHub Secret**: Add `GEMINI_API_KEY` to your repository's Actions secrets.
2. **Permissions**: Ensure the `GITHUB_TOKEN` has `write` permissions for `contents` and `pull-requests` in the repository settings.
```

- [ ] **Step 3: Commit documentation update**

```bash
git add docs/superpowers/specs/2026-03-29-gemini-auto-code-workflow-design.md
git commit -m "docs: add setup requirements for auto-code workflow"
```
