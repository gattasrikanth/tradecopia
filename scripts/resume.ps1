#requires -Version 7.0
<#
.SYNOPSIS
    Print TradeCopia environment and agent-state so a new context can resume.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

function Write-Section([string]$Title) {
    Write-Host ""
    Write-Host "=== $Title ===" -ForegroundColor Cyan
}

Write-Section "Repository"
Write-Host "Root: $repoRoot"
git rev-parse --abbrev-ref HEAD 2>$null | ForEach-Object { Write-Host "Branch: $_" }
git rev-parse HEAD 2>$null | ForEach-Object { Write-Host "HEAD: $_" }
Write-Host "Status:"
git status --short --branch

Write-Section "Agent state"
foreach ($name in @('STATE.md', 'NEXT.md', 'TASKS.md', 'DECISIONS.md', 'BLOCKERS.md')) {
    $path = Join-Path $repoRoot "docs\agent\$name"
    if (Test-Path $path) {
        Write-Host "present  docs/agent/$name"
    }
    else {
        Write-Host "MISSING  docs/agent/$name" -ForegroundColor Yellow
    }
}

$statePath = Join-Path $repoRoot 'docs\agent\STATE.md'
if (Test-Path $statePath) {
    Write-Host ""
    Write-Host "--- docs/agent/STATE.md ---"
    Get-Content -Path $statePath -Raw
}

Write-Section "Tooling"
foreach ($cmd in @('git', 'gh', 'dotnet', 'node', 'pnpm', 'pwsh')) {
    $found = Get-Command $cmd -ErrorAction SilentlyContinue
    if ($found) {
        Write-Host ("{0,-8} {1}" -f $cmd, $found.Source)
    }
    else {
        Write-Host ("{0,-8} MISSING" -f $cmd) -ForegroundColor Yellow
    }
}

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    Write-Host ""
    Write-Host "dotnet SDKs:"
    dotnet --list-sdks
    Write-Host "dotnet runtimes:"
    dotnet --list-runtimes
}

Write-Section "NinjaTrader (presence only; paths not required for resume)"
$ntExe = Join-Path ${env:ProgramFiles} 'NinjaTrader 8\bin\NinjaTrader.exe'
if (Test-Path $ntExe) {
    $ver = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($ntExe).FileVersion
    Write-Host "NinjaTrader Desktop detected. FileVersion=$ver"
}
else {
    Write-Host "NinjaTrader Desktop executable not found in the default Program Files location."
}

$userData = Join-Path $env:USERPROFILE 'Documents\NinjaTrader 8'
if (Test-Path $userData) {
    Write-Host "NinjaTrader user-data directory exists."
}
else {
    Write-Host "NinjaTrader user-data directory not present."
}

Write-Section "Resume algorithm"
Write-Host "1. Read docs/agent/STATE.md and NEXT.md"
Write-Host "2. If the tree is dirty, recover the interrupted task; do not discard work."
Write-Host "3. Execute the first uncompleted task. Do not restart the project plan."
