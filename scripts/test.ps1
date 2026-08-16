#requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

& "$PSScriptRoot\scan-secrets.ps1"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

dotnet test "$root\TradeCopia.slnx" --nologo --no-restore
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
