#requires -Version 7.0
<#
.SYNOPSIS
    Scan the working tree for secrets, real-looking account identifiers, and
    prohibited NinjaTrader proprietary binaries.
#>
[CmdletBinding()]
param(
    [string]$Root
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $Root) {
    $Root = Split-Path -Parent $PSScriptRoot
}

$excludeDirNames = @(
    '.git', '.vs', 'bin', 'obj', 'node_modules', 'dist', 'build', '.turbo', 'coverage',
    'artifacts', 'TestResults', 'test-results', 'playwright-report'
)

$binaryExtensions = @(
    '.dll', '.exe', '.pdb', '.nupkg', '.pfx', '.pem', '.key', '.p12'
)

# Patterns that should never appear in a public TradeCopia commit.
$patterns = @(
    @{ Name = 'AWS access key'; Regex = 'AKIA[0-9A-Z]{16}' },
    @{ Name = 'Generic private key header'; Regex = '-----BEGIN (RSA |OPENSSH |EC )?PRIVATE KEY-----' },
    @{ Name = 'GitHub PAT'; Regex = 'ghp_[A-Za-z0-9]{20,}' },
    @{ Name = 'Generic password assignment'; Regex = '(?i)(password|passwd|pwd)\s*[:=]\s*[''"][^''"]{8,}' },
    @{ Name = 'Likely live brokerage account id'; Regex = '(?i)\b(live|real)-account-[0-9]{4,}\b' }
)

$failures = New-Object System.Collections.Generic.List[string]

function Test-ExcludedDirectory([string]$Path) {
    $parts = $Path.Split([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    foreach ($part in $parts) {
        if ($excludeDirNames -contains $part) {
            return $true
        }
    }
    return $false
}

$files = Get-ChildItem -Path $Root -Recurse -File -Force | Where-Object {
    -not (Test-ExcludedDirectory $_.DirectoryName)
}

foreach ($file in $files) {
    $rel = Resolve-Path -LiteralPath $file.FullName -Relative
    $ext = $file.Extension.ToLowerInvariant()

    if ($binaryExtensions -contains $ext) {
        $failures.Add("prohibited binary: $rel")
        continue
    }

    if ($file.Length -gt 2MB) {
        continue
    }

    $text = $null
    try {
        $text = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction Stop
    }
    catch {
        continue
    }

    if ([string]::IsNullOrEmpty($text)) {
        continue
    }

    foreach ($pattern in $patterns) {
        if ([regex]::IsMatch($text, $pattern.Regex)) {
            $failures.Add("$($pattern.Name): $rel")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "scan-secrets: FAILED" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host "scan-secrets: OK ($($files.Count) files scanned)"
exit 0
