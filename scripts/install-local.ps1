#requires -Version 7.0
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$NinjaTraderUserData
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
. "$PSScriptRoot\lib\DocumentsPath.ps1"

Write-Host "TradeCopia local install (development fallback)"
Write-Host "This script never copies NinjaTrader proprietary assemblies."
Write-Host "Normal customers should use TradeCopia-Setup-*.exe from GitHub Releases."

$documents = Get-TradeCopiaDocumentsPath
if (-not $NinjaTraderUserData) {
    $NinjaTraderUserData = Get-TradeCopiaNinjaTraderUserData -DocumentsPath $documents
}

Write-Host ("Documents known folder: " + $documents)
Write-Host ("NinjaTrader user-data: " + $NinjaTraderUserData)

if (Test-TradeCopiaCloudPath $NinjaTraderUserData) {
    Write-Host "BLOCKED: NinjaTrader user-data is cloud-synchronized (OneDrive or similar)."
    Write-Host "See docs/operations/onedrive-remediation.md. TradeCopia will not install into that tree."
    exit 2
}

if (-not (Test-Path $NinjaTraderUserData)) {
    Write-Host "BLOCKED: NinjaTrader user-data directory is not present."
    Write-Host "Launch NinjaTrader 8 once, then re-run this script or TradeCopia Setup."
    exit 2
}

$custom = Join-Path $NinjaTraderUserData 'bin\Custom'
if (-not (Test-Path $custom)) {
    Write-Host "NinjaTrader custom directory was not found under the user-data tree."
    exit 2
}

Write-Host "User-data directory is local and bin\Custom exists."
Write-Host "Prefer: pwsh ./scripts/package.ps1 then run the generated TradeCopia-Setup-*.exe"
exit 0
