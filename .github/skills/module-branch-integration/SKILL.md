---
name: module-branch-integration
description: "Use when: integrating changes from module/* branches onto main, then syncing the combined changes back to modules. Orchestrates discovery, merge, validation, and sync in one workflow."
---

# Module Branch Integration Skill

**Use for**: Regularly integrating work from `module/terrain`, `module/simulation`, `module/server` branches onto `main`, then distributing the unified `main` back to each module branch.

## Overview

This skill provides a safe, repeatable workflow for:
1. **Discover** all active `module/*` branches in the repository
2. **Integrate** each module branch onto `main` via rebase + fast-forward merge
3. **Validate** the combined `main` with full build + test suite
4. **Distribute** the updated `main` back to each module branch
5. **Report** status and conflicts throughout

## Workflow

### Stage 1: Preparation
- Ensure working directory is clean (no uncommitted changes)
- Fetch latest from origin
- Switch to `main` and pull latest upstream

### Stage 2: Discover Modules
Query all `module/*` branches in the repository.

### Stage 3: Integrate to Main
For each module branch:
1. Merge with `--no-ff` (rebase + fast-forward)
2. On conflict → display conflicting files and pause for manual resolution
3. Continue to next module only after conflict is resolved or skipped

### Stage 4: Validate
After all modules are integrated:
- Run `dotnet build` to compile
- Run `dotnet test` to validate functionality
- If validation fails → abort merge and report failures

### Stage 5: Distribute
For each module branch:
1. Rebase onto the updated `main` (pull with rebase)
2. Report success or conflicts
3. Module branch is now ready for next development cycle

## PowerShell Script

A bundled orchestration script (`orchestrate-sync.ps1`) automates the entire workflow:

```powershell
# Run from repo root
.\orchestrate-sync.ps1 -Verbose
```

### Script Parameters

| Parameter | Default | Purpose |
|-----------|---------|---------|
| **-ValidateOnly** | false | Run validation (Stage 4) without integrating or distributing |
| **-Verbose** | false | Show detailed step-by-step output and git commands |
| **-SkipValidation** | false | Skip build + test validation (not recommended) |
| **-DryRun** | false | Show what would happen without actually running git commands |

### Script Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success — all stages complete |
| 1 | Pre-flight check failed (dirty working directory, etc.) |
| 2 | Integration conflict that manual resolution could not auto-merge |
| 3 | Validation failed (build or test error) |
| 4 | Distribution stage failed |

## Manual Workflow (Without Script)

If you prefer step-by-step control:

### 1. Set up
```powershell
git fetch origin
git checkout main
git pull origin main
```

### 2. Integrate each module
```powershell
git rebase module/terrain
# If conflict: resolve, then `git rebase --continue`
# Or skip: `git rebase --abort`

git rebase module/simulation
# ...repeat for each module
```

### 3. Validate
```powershell
dotnet build
dotnet test
```

### 4. Distribute back
```powershell
git checkout module/terrain
git rebase main
git checkout module/simulation
git rebase main
# ...repeat for each module
```

### 5. Push
```powershell
git push origin main
git push origin module/terrain
git push origin module/simulation
# ...push all updated modules
```

## Troubleshooting

### Merge Conflict During Integration
The script pauses and lists conflicting files. Resolve manually in VS Code, then:
- `git add <resolved-files>`
- `git rebase --continue` (for rebase conflicts)
- Or restart: `git rebase --abort` and retry the script

### Validation Failure
If build or test fails after integration:
1. The script rolls back the integration
2. Fix the issue in the pre-integration module branch
3. Commit the fix to the module branch
4. Re-run the script

### Distribution Conflict
If a module branch can't rebase onto updated `main`:
1. Resolve manually: `git checkout module/<name> && git rebase main`
2. Fix conflicts in VS Code
3. `git rebase --continue`
4. `git push origin module/<name>` to update remote

## When NOT to Use This Skill

- **Single module**: Use standard git merge/rebase for one-off updates
- **Feature branches to PRs**: Use GitHub PR workflow instead
- **Hot-fix on main**: Apply directly; module updates can wait for next cycle
- **Emergency rollback**: Use `git revert` on deployed commit

## See Also

- [Git Worktrees](../git-worktree-setup/SKILL.md) — if using git worktrees for parallel module work
- Copilot Instructions: [copilot-instructions.md](../../copilot-instructions.md) — module architecture and namespace rules
