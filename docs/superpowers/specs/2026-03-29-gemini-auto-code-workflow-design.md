# Gemini Auto-Code Workflow Design

> **Status:** Draft
> **Author:** Gemini CLI
> **Date:** 2026-03-29

## Goal
Automate the implementation of features and bug fixes by triggering the Gemini CLI whenever a GitHub issue is labeled with "Ready". The CLI will autonomously research the codebase, implement the requested changes, and create a Pull Request.

## Architecture

### Workflow Trigger
- **Event:** `issues`
- **Types:** `labeled`
- **Condition:** `github.event.label.name == 'Ready'`

### Environment Requirements
- **`GEMINI_API_KEY`**: Authenticates the CLI with the Gemini API.
- **`GITHUB_TOKEN`**: Provides repository access for reading issues, creating branches, and opening PRs.
- **Runner**: `ubuntu-latest`
- **Node.js**: Required to install and run the Gemini CLI.

### Workflow Execution Flow
1. **Trigger:** A user adds the "Ready" label to a GitHub issue.
2. **Setup:** The GitHub Action installs the Gemini CLI (`npm install -g @google/gemini-cli`).
3. **Execution:** The CLI is invoked in headless mode (`-p`) with the following prompt:
   "The status of this issue is 'Ready'. Research the codebase, implement the fix/feature, and create a Pull Request on a new branch."
4. **Context:** The issue title and body are piped into the CLI as part of the prompt.
5. **Action:** The CLI uses its `mcp_github_*` tools to interact with the repository and perform the task.

## Setup Requirements
1. **GitHub Secret**: Add `GEMINI_API_KEY` to your repository's Actions secrets.
2. **Permissions**: Ensure the `GITHUB_TOKEN` has `write` permissions for `contents` and `pull-requests` in the repository settings (found under **Settings > Actions > General > Workflow permissions**).

## Security & Safety
- The `GITHUB_TOKEN` is automatically scoped to the current repository.
- Changes are pushed to a **new branch** and a **Pull Request** is created, ensuring a human review step before merging.

## Testing Strategy
- Manual verification: Add the "Ready" label to a test issue and observe the GitHub Action's execution and the resulting PR.
- Log inspection: Review the Action's logs to ensure the CLI is correctly interpreting the prompt and using its tools.

## Success Criteria
- [ ] A GitHub Action is successfully triggered by the "Ready" label.
- [ ] The Gemini CLI starts correctly in headless mode.
- [ ] The CLI creates a new branch and a Pull Request that addresses the issue content.
