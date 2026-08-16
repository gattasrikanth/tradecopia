#requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$failed = $false

Get-ChildItem -Path (Join-Path $root 'scripts') -Filter '*.ps1' | ForEach-Object {
    $tokens = $null
    $parseErrors = $null
    $contents = Get-Content -LiteralPath $_.FullName -Raw
    $null = [System.Management.Automation.Language.Parser]::ParseInput(
        $contents,
        $_.FullName,
        [ref]$tokens,
        [ref]$parseErrors)

    if ($null -ne $parseErrors -and $parseErrors.Count -gt 0) {
        Write-Host "PARSE FAIL $($_.Name)"
        $parseErrors | ForEach-Object { Write-Host $_ }
        $failed = $true
    }
    else {
        Write-Host "PARSE OK $($_.Name)"
    }
}

if ($failed) {
    exit 1
}

exit 0
