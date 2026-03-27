# SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
# SPDX-License-Identifier: ISC

<#
.SYNOPSIS
    Module branch integration orchestrator. Integrates all module/* branches onto main
    with build + test validation, then distributes the combined main back to each module.

.DESCRIPTION
    Automates a safe, repeatable workflow for multi-branch integration:
    1. Preparation: clean state, fetch latest
    2. Discovery: list all module/* branches
    3. Integration: rebase each module onto main
    4. Validation: dotnet build + tests
    5. Distribution: pull updated main into each module
    6. Report: status and conflicts

.PARAMETER ValidateOnly
    Run validation only without integration or distribution. Useful for checking
    if the current main state compiles and tests pass.

.PARAMETER SkipValidation
    Skip build + test validation step. Not recommended unless you're in a hurry.

.PARAMETER DryRun
    Show what would happen without running actual git or dotnet commands.

.PARAMETER Verbose
    Show detailed step-by-step output, including all git commands executed.

.EXAMPLE
    .\orchestrate-sync.ps1 -Verbose
    Full integration with detailed output.

.EXAMPLE
    .\orchestrate-sync.ps1 -ValidateOnly
    Check if current main builds and tests pass.

.EXAMPLE
    .\orchestrate-sync.ps1 -DryRun -Verbose
    Safe preview of the entire workflow.

.NOTES
    Exits with:
    0 - Success
    1 - Pre-flight check failed
    2 - Integration conflict
    3 - Validation failure
    4 - Distribution failure
#>

param(
    [switch] $ValidateOnly,
    [switch] $SkipValidation,
    [switch] $DryRun,
    [switch] $Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

$script:ExitCode = 0
$script:Remote = 'origin'  # Will be auto-detected below

function Write-Stage {
    param([string] $Message, [int] $Number)
    Write-Host ""
    Write-Host "╔════ STAGE $Number ════╗" -ForegroundColor Cyan
    Write-Host "║ $($Message.PadRight(25)) ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success {
    param([string] $Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string] $Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string] $Message, [int] $ExitCode = 1)
    Write-Host "✗ $Message" -ForegroundColor Red
    $script:ExitCode = $ExitCode
}

function Invoke-Git {
    param([string[]] $Arguments)
    if ($DryRun) {
        Write-Verbose "DRY RUN: git $($Arguments -join ' ')"
        return
    }
    Write-Verbose "git $($Arguments -join ' ')"
    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Git command failed: git $($Arguments -join ' ')"
    }
}

function Invoke-Dotnet {
    param([string] $Command, [string[]] $Arguments)
    if ($DryRun) {
        Write-Verbose "DRY RUN: dotnet $Command $($Arguments -join ' ')"
        return
    }
    Write-Verbose "dotnet $Command $($Arguments -join ' ')"
    & dotnet $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Dotnet command failed"
    }
}

# ============================================================================
# Stage 1: Preparation
# ============================================================================

Write-Stage "Preparation" 1

try {
    # Auto-detect the correct remote name (origin, parent, upstream, etc.)
    Write-Host "Detecting remote name..." -ForegroundColor Gray
    $remotes = @(git remote)
    if ($remotes -contains 'origin') {
        $script:Remote = 'origin'
    } elseif ($remotes -contains 'parent') {
        $script:Remote = 'parent'
    } elseif ($remotes.Count -gt 0) {
        $script:Remote = $remotes[0]
    } else {
        Write-Error "No remotes found in this repository" 1
        exit 1
    }
    Write-Verbose "Using remote: $($script:Remote)"
    Write-Success "Using remote: $($script:Remote)"

    Write-Host "Checking git status..." -ForegroundColor Gray
    $status = git status --porcelain
    if ($status) {
        Write-Error "Working directory has uncommitted changes. Commit or stash first." 1
        exit 1
    }
    Write-Success "Working directory is clean"

    Write-Host "Fetching latest from $($script:Remote)..." -ForegroundColor Gray
    Invoke-Git 'fetch', $script:Remote
    Write-Success "Fetched latest"

    Write-Host "Ensuring on main branch..." -ForegroundColor Gray
    $currentBranch = git rev-parse --abbrev-ref HEAD
    if ($currentBranch -ne 'main') {
        Invoke-Git 'checkout', 'main'
    }
    Write-Success "On main branch"

    Write-Host "Pulling latest main..." -ForegroundColor Gray
    Invoke-Git 'pull', $script:Remote, 'main'
    Write-Success "Main is up to date"
}
catch {
    Write-Error "Preparation failed: $_" 1
    exit 1
}

# ============================================================================
# Stage 2: Discovery
# ============================================================================

Write-Stage "Discovery" 2

try {
    $modules = @(git branch -r --list "$($script:Remote)/module/*" | ForEach-Object { $_.Trim() -replace "$($script:Remote)/", '' })
    
    if ($modules.Count -eq 0) {
        Write-Warning "No module/* branches found"
        exit 0
    }

    Write-Host "Found $($modules.Count) module branches:" -ForegroundColor Gray
    $modules | ForEach-Object { Write-Host "  • $_" -ForegroundColor Gray }
}
catch {
    Write-Error "Discovery failed: $_" 1
    exit 1
}

# ============================================================================
# Stage 3: Integration
# ============================================================================

if (-not $ValidateOnly) {
    Write-Stage "Integration" 3

    foreach ($module in $modules) {
        try {
            Write-Host "Rebasing $module onto main..." -ForegroundColor Gray
            Invoke-Git 'rebase', $module
            Write-Success "Successfully rebased $module"
        }
        catch {
            Write-Error "Rebase conflict in $module" 2
            Write-Host ""
            Write-Host "Conflicting files:" -ForegroundColor Red
            git status --short --porcelain | ForEach-Object {
                Write-Host "  $_" -ForegroundColor Red
            }
            Write-Host ""
            Write-Host "To resolve manually:" -ForegroundColor Yellow
            Write-Host "  1. Open conflicted files in VS Code and resolve"
            Write-Host "  2. Run: git add <files>"
            Write-Host "  3. Run: git rebase --continue"
            Write-Host ""
            Write-Host "Or abort and retry this script:" -ForegroundColor Yellow
            Write-Host "  git rebase --abort" -ForegroundColor Yellow
            Write-Host ""
            exit 2
        }
    }

    Write-Success "All modules integrated onto main"
}

# ============================================================================
# Stage 4: Validation
# ============================================================================

if (-not $SkipValidation) {
    Write-Stage "Validation" 4

    try {
        Write-Host "Building solution..." -ForegroundColor Gray
        Invoke-Dotnet 'build'
        Write-Success "Build succeeded"

        Write-Host "Running tests..." -ForegroundColor Gray
        Invoke-Dotnet 'test'
        Write-Success "Tests passed"
    }
    catch {
        Write-Error "Validation failed: $_" 3
        if (-not $ValidateOnly) {
            Write-Host ""
            Write-Host "Rolling back integration..." -ForegroundColor Yellow
            Invoke-Git 'reset', '--hard', "$($script:Remote)/main"
            Write-Success "Rolled back to $($script:Remote)/main"
            Write-Host ""
            Write-Host "Before retrying:" -ForegroundColor Yellow
            Write-Host "  1. Fix the failing tests in the module branches"
            Write-Host "  2. Commit your fixes"
            Write-Host "  3. Push to the module branches"
            Write-Host "  4. Re-run this script"
        }
        exit 3
    }
}

# ============================================================================
# Stage 5: Distribution
# ============================================================================

if (-not $ValidateOnly) {
    Write-Stage "Distribution" 5

    foreach ($module in $modules) {
        try {
            Write-Host "Pulling updated main into $module..." -ForegroundColor Gray
            Invoke-Git 'checkout', $module
            Invoke-Git 'pull', '--rebase', $script:Remote, 'main'
            Write-Success "Successfully rebased $module onto main"
        }
        catch {
            Write-Error "Distribution conflict in $module" 4
            Write-Host ""
            Write-Host "To resolve:" -ForegroundColor Yellow
            Write-Host "  1. Resolve conflicts in VS Code"
            Write-Host "  2. Run: git add <files>"
            Write-Host "  3. Run: git rebase --continue"
            Write-Host "  4. Run: git push origin $module"
            exit 4
        }
    }

    # Return to main
    Invoke-Git 'checkout', 'main'
    Write-Success "All modules synced with main"
}

# ============================================================================
# Stage 6: Push
# ============================================================================

if (-not $ValidateOnly) {
    Write-Stage "Push" 6

    try {
        if (-not $DryRun) {
            Write-Host "Ready to push changes. Lines below would be executed:" -ForegroundColor Cyan
        }

        Write-Host "Pushing main..." -ForegroundColor Gray
        Invoke-Git 'push', $script:Remote, 'main'

        foreach ($module in $modules) {
            Write-Host "Pushing $module..." -ForegroundColor Gray
            Invoke-Git 'push', $script:Remote, $module
        }

        Write-Success "All branches pushed"
    }
    catch {
        Write-Error "Push failed: $_" 4
        exit 4
    }
}

# ============================================================================
# Summary
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║           SUCCESS                      ║" -ForegroundColor Green
Write-Host "║  All stages completed without error    ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

if ($ValidateOnly) {
    Write-Host "Validation complete. No changes were made." -ForegroundColor Cyan
}
elseif ($DryRun) {
    Write-Host "Dry-run complete. No git commands were executed." -ForegroundColor Cyan
}
else {
    Write-Host "Integration cycle complete:" -ForegroundColor Cyan
    Write-Host "  ✓ All module branches integrated onto main" -ForegroundColor Cyan
    Write-Host "  ✓ Solution builds and tests pass" -ForegroundColor Cyan
    Write-Host "  ✓ Modules synced with updated main" -ForegroundColor Cyan
    Write-Host "  ✓ Changes pushed to $($script:Remote)" -ForegroundColor Cyan
}

exit $script:ExitCode
