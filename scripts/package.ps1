#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$OutputDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $OutputDir) {
    $OutputDir = Join-Path $root 'artifacts\package'
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
Set-Location $root

dotnet publish "$root\src\ControlPlane\TradeCopia.ControlPlane\TradeCopia.ControlPlane.csproj" `
    -c Release -r win-x64 --self-contained false -o (Join-Path $OutputDir 'control-plane')

$note = @"
TradeCopia Alpha package
Status: Alpha / automated tests only. Manual NinjaTrader SIM certification required before live use.
Copying starts disabled.
This package does not include NinjaTrader proprietary assemblies.
"@
Set-Content -Path (Join-Path $OutputDir 'README.txt') -Value $note -Encoding utf8
Write-Host "Packaged to $OutputDir"
