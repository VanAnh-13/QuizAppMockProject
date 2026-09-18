<#
.SYNOPSIS
    Automated Agent Verification Harness for QuizApp Frontend.
.DESCRIPTION
    Runs architectural guardrail rules, unit test suite, production build,
    and git whitespace checks to ensure compliance with AGENTS.md and DESIGN.MD.
#>

$ErrorActionPreference = 'Stop'
$RepoRoot = Resolve-Path "$PSScriptRoot/.."
$FrontendDir = Join-Path $RepoRoot "frontend"

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "🛡️  QuizApp Frontend Agent Verification Harness" -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Cyan

# Step 1: Rule & Architecture Check
Write-Host "[1/4] Running rule & architecture checks..." -ForegroundColor Yellow
node "$RepoRoot/scripts/check-rules.mjs"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Rule check failed. Fix violations before proceeding."
    exit 1
}

# Step 2: Vitest Suite
Write-Host "`n[2/4] Running full frontend test suite..." -ForegroundColor Yellow
Push-Location $FrontendDir
try {
    & pnpm.cmd test --watch=false
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Frontend tests failed."
        exit 1
    }

    # Step 3: Production Build
    Write-Host "`n[3/4] Running frontend production build..." -ForegroundColor Yellow
    & pnpm.cmd build
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Frontend build failed."
        exit 1
    }
}
finally {
    Pop-Location
}

# Step 4: Whitespace and Formatting Check
Write-Host "`n[4/4] Checking git diff for whitespace anomalies..." -ForegroundColor Yellow
& git diff --check
if ($LASTEXITCODE -ne 0) {
    Write-Error "Git diff has whitespace errors (trailing spaces or extra newlines at EOF)."
    exit 1
}

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host "✅ ALL FRONTEND VERIFICATION CHECKS PASSED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "========================================================`n" -ForegroundColor Green
exit 0
