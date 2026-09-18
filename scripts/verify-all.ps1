<#
.SYNOPSIS
    Unified Agent Verification Harness for QuizApp (Frontend + Backend).
.DESCRIPTION
    Runs complete verification across both frontend and backend subtrees.
#>

$ErrorActionPreference = 'Stop'
$PSScriptRoot_Local = Split-Path -Parent $MyInvocation.MyCommand.Definition

Write-Host "`n========================================================" -ForegroundColor Magenta
Write-Host "🚀  QuizApp Full Monorepo Agent Verification Harness" -ForegroundColor Magenta
Write-Host "========================================================`n" -ForegroundColor Magenta

# 1. Frontend
& "$PSScriptRoot_Local/verify-frontend.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Frontend verification failed."
    exit 1
}

# 2. Backend
& "$PSScriptRoot_Local/verify-backend.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Backend verification failed."
    exit 1
}

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host "🎉 ALL MONOREPO VERIFICATION CHECKS PASSED!" -ForegroundColor Green
Write-Host "========================================================`n" -ForegroundColor Green
exit 0
