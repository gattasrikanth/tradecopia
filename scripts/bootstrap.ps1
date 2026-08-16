#requires -Version 7.0
<#
.SYNOPSIS
    Print first-time contributor environment checks. Does not mutate the system.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

Write-Host "TradeCopia bootstrap checks (read-only)"
Write-Host "Repository: $(Split-Path -Parent $PSScriptRoot)"
Write-Host ""

$required = @('git', 'dotnet', 'node', 'pnpm')
foreach ($cmd in $required) {
    if (Get-Command $cmd -ErrorAction SilentlyContinue) {
        Write-Host "OK   $cmd"
    }
    else {
        Write-Host "NEED $cmd" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Next: read AGENTS.md and run scripts/resume.ps1"
