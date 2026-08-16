#requires -Version 7.0
<#
.SYNOPSIS
    Locate a local NinjaTrader 8 installation and report compile readiness.
    Does not copy or commit proprietary assemblies.
#>
[CmdletBinding()]
param(
    [string]$InstallRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

if (-not $InstallRoot) {
    $InstallRoot = Join-Path ${env:ProgramFiles} 'NinjaTrader 8'
}

$exe = Join-Path $InstallRoot 'bin\NinjaTrader.exe'
$core = Join-Path $InstallRoot 'bin\NinjaTrader.Core.dll'

Write-Host "TradeCopia NinjaTrader verification"
Write-Host "This script never copies proprietary assemblies into the repository."
Write-Host ""

if (-not (Test-Path $exe)) {
    Write-Host "BLOCKED_ENVIRONMENT: NinjaTrader.exe not found under the default install root."
    Write-Host "Native compile is skipped. Domain/control-plane work can continue."
    exit 2
}

$info = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
Write-Host "Detected FileVersion=$($info.FileVersion) ProductVersion=$($info.ProductVersion)"

if (Test-Path $core) {
    Write-Host "NinjaTrader.Core.dll is present locally (not committed)."
}
else {
    Write-Host "NinjaTrader.Core.dll was not found next to NinjaTrader.exe."
}

$userData = Join-Path $env:USERPROFILE 'Documents\NinjaTrader 8'
if (Test-Path $userData) {
    Write-Host "User-data directory exists."
}
else {
    Write-Host "User-data directory is not present. install-local cannot complete until NinjaTrader has been launched once."
}

Write-Host ""
Write-Host "OK: local NinjaTrader presence check finished."
exit 0
