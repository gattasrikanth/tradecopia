#requires -Version 7.0
[CmdletBinding()]
param(
    [int]$Port = 17841
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "Starting TradeCopia control plane on http://127.0.0.1:$Port"
Write-Host "Copying starts disabled. This process does not submit orders."
dotnet run --project "$root\src\ControlPlane\TradeCopia.ControlPlane\TradeCopia.ControlPlane.csproj" -- --port=$Port
