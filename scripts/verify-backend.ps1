<#
.SYNOPSIS
    Automated Agent Verification Harness for QuizApp Backend (.NET 10).
.DESCRIPTION
    Restores, compiles, and tests the backend solution according to AGENTS.md rules.
#>

$ErrorActionPreference = 'Stop'
$RepoRoot = Resolve-Path "$PSScriptRoot/.."
$Solution = Join-Path $RepoRoot "backend/Quizapp.sln"
$Tests = Join-Path $RepoRoot "backend/tests/Quizapp.Tests/Quizapp.Tests.csproj"

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "🛡️  QuizApp Backend Agent Verification Harness" -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Cyan

# Step 1: Restore
Write-Host "[1/3] Restoring .NET solution packages..." -ForegroundColor Yellow
dotnet restore "$Solution"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Dotnet restore failed."
    exit 1
}

# Step 2: Build
Write-Host "`n[2/3] Building solution without restore..." -ForegroundColor Yellow
dotnet build "$Solution" --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Dotnet build failed."
    exit 1
}

# Step 3: Run Tests
Write-Host "`n[3/3] Running backend tests..." -ForegroundColor Yellow
if (-not $env:QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING) {
    Write-Warning "QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING is not set in environment. SQL Server tests may be skipped."
}

dotnet test "$Tests" --no-build --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Dotnet test failed."
    exit 1
}

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host "✅ ALL BACKEND VERIFICATION CHECKS PASSED!" -ForegroundColor Green
Write-Host "========================================================`n" -ForegroundColor Green
exit 0
