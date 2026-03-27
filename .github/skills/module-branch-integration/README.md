# Module Branch Integration Skill

Quick-start guide for integrating `module/*` branches onto `main` and syncing changes back.

## Quick Start

### Automatic (Recommended)
```powershell
# From repo root
.\orchestrate-sync.ps1
```

### With Output
```powershell
.\orchestrate-sync.ps1 -Verbose
```

### Preview Only (No Changes)
```powershell
.\orchestrate-sync.ps1 -DryRun -Verbose
```

### Validate Current Main
```powershell
.\orchestrate-sync.ps1 -ValidateOnly
```

## What It Does

1. **Fetches** latest from origin
2. **Discovers** all `module/*` branches
3. **Integrates** each module onto `main` (via rebase)
4. **Validates** with `dotnet build` + `dotnet test`
5. **Distributes** updated `main` back to each module
6. **Pushes** all changes to origin

## Manual Workflow

See [SKILL.md](SKILL.md#manual-workflow-without-script) for step-by-step git commands if you prefer manual control.

## Troubleshooting

See [SKILL.md](SKILL.md#troubleshooting) for conflict resolution strategies.

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Pre-flight check failed |
| 2 | Integration conflict |
| 3 | Validation failure |
| 4 | Distribution/push failure |

---

Full documentation: [SKILL.md](SKILL.md)
