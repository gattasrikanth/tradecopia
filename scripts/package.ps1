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

$version = '0.1.0-alpha.2'
$payload = Join-Path $OutputDir 'payload'
$appOut = Join-Path $payload 'app'
$nativeOut = Join-Path $payload 'native'
New-Item -ItemType Directory -Force -Path $appOut, $nativeOut | Out-Null
Set-Location $root

Write-Host "Publishing self-contained control plane (win-x64)"
dotnet publish "$root\src\ControlPlane\TradeCopia.ControlPlane\TradeCopia.ControlPlane.csproj" `
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false `
    -o $appOut

dotnet publish "$root\src\Installer\TradeCopia.Launcher\TradeCopia.Launcher.csproj" `
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true `
    -o (Join-Path $OutputDir 'launcher')

Get-ChildItem (Join-Path $OutputDir 'launcher') -Filter 'TradeCopia.Launcher.*' | ForEach-Object {
    Copy-Item -Force $_.FullName $appOut
}

$ntCore = Join-Path ${env:ProgramFiles} 'NinjaTrader 8\bin\NinjaTrader.Core.dll'
if (Test-Path $ntCore) {
    Write-Host "Building native AddOn (local NT references, Private=false)"
    dotnet build "$root\src\Native\TradeCopia.Native\TradeCopia.Native.csproj" -c Release
    $nativeBin = Join-Path $root 'src\Native\TradeCopia.Native\bin\Release\net481'
    Get-ChildItem $nativeBin -Filter 'TradeCopia.*.dll' | ForEach-Object {
        if ($_.Name -notlike 'NinjaTrader*') {
            Copy-Item $_.FullName $nativeOut -Force
        }
    }
}
else {
    Write-Host "NinjaTrader assemblies not present; setup payload ships without native AddOn DLLs."
}

$embeddedZip = Join-Path $root 'src\Installer\TradeCopia.Setup\payload.zip'
if (Test-Path $embeddedZip) { Remove-Item $embeddedZip -Force }
Compress-Archive -Path (Join-Path $payload '*') -DestinationPath $embeddedZip -Force

dotnet publish "$root\src\Installer\TradeCopia.Setup\TradeCopia.Setup.csproj" `
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true `
    -o (Join-Path $OutputDir 'setup')
Remove-Item $embeddedZip -Force -ErrorAction SilentlyContinue

$setupName = "TradeCopia-Setup-$version.exe"
Copy-Item -Recurse -Force $payload (Join-Path $OutputDir 'setup\payload')
Copy-Item -Force (Join-Path $OutputDir "setup\TradeCopia-Setup-$version.exe") (Join-Path $OutputDir $setupName)

$hash = Get-FileHash -Algorithm SHA256 (Join-Path $OutputDir $setupName)
Set-Content -Path (Join-Path $OutputDir "$setupName.sha256") -Value ($hash.Hash + '  ' + $setupName) -Encoding ascii

$note = @"
TradeCopia $version
Status: Alpha / automated tests only. Manual NinjaTrader SIM certification required before live use.
Copying starts disabled.
This package does not include NinjaTrader proprietary assemblies.
Unsigned Alpha may show a Windows SmartScreen unknown-publisher warning.
"@
Set-Content -Path (Join-Path $OutputDir 'README.txt') -Value $note -Encoding utf8
Write-Host "Packaged $setupName sha256=$($hash.Hash)"
