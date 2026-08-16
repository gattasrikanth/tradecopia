#requires -Version 7.0
# Shared Documents known-folder helper. Mirrors TradeCopia.Platform:
# query Windows Documents, never assume %USERPROFILE%\Documents.

function Get-TradeCopiaDocumentsPath {
    [Environment]::GetFolderPath('MyDocuments')
}

function Test-TradeCopiaCloudPath {
    param([Parameter(Mandatory)][string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    if ($Path -match '(?i)OneDrive|Dropbox|Google Drive|iCloud') { return $true }
    if ($env:OneDrive -and $Path.StartsWith($env:OneDrive, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    if ($env:OneDriveCommercial -and $Path.StartsWith($env:OneDriveCommercial, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    return $false
}

function Get-TradeCopiaNinjaTraderUserData {
    param([string]$DocumentsPath)
    if (-not $DocumentsPath) {
        $DocumentsPath = Get-TradeCopiaDocumentsPath
    }
    Join-Path $DocumentsPath 'NinjaTrader 8'
}
