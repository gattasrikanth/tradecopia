#requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location (Join-Path $root 'tests\Web')
if (-not (Test-Path 'node_modules')) {
    npm install --no-fund --no-audit
}
npx playwright test --reporter=line
