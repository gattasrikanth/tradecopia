#requires -Version 7.0
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$NinjaTraderUserData
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

if (-not $NinjaTraderUserData) {
    $NinjaTraderUserData = Join-Path $env:USERPROFILE 'Documents\NinjaTrader 8'
}

Write-Host "TradeCopia local install (development)"
Write-Host "This script never copies NinjaTrader proprietary assemblies."

if (-not (Test-Path $NinjaTraderUserData)) {
    Write-Host "BLOCKED: NinjaTrader user-data directory is not present."
    Write-Host "Launch NinjaTrader 8 once, then re-run this script."
    Write-Host "The control plane can still run: pwsh ./scripts/run-control-plane.ps1"
    exit 2
}

$custom = Join-Path $NinjaTraderUserData 'bin\Custom'
if (-not (Test-Path $custom)) {
    Write-Host "NinjaTrader custom directory was not found under the user-data tree."
    exit 2
}

Write-Host "User-data directory exists. Native AddOn copy is deferred until a documented NT import package is produced."
Write-Host "Until then: open the dashboard from the control plane and keep copying disabled."
exit 0
