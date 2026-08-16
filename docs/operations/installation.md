# Installation

## End users (Alpha)

1. Download `TradeCopia-Setup-*.exe` and the `.sha256` file from GitHub Releases.
2. Close NinjaTrader.
3. Confirm NinjaTrader user-data is **not** under OneDrive (`docs/operations/onedrive-remediation.md`).
4. Run the setup executable (per-user, no Administrator required).
5. Launch NinjaTrader 8. TradeCopia should load without NinjaScript Editor / F5.
6. Open TradeCopia from the Start menu. Copying starts **disabled**.

Do not enable copying on a live account. Unsigned Alpha may show SmartScreen; that is expected until signing is configured.

## Developers

```powershell
pwsh ./scripts/bootstrap.ps1
pwsh ./scripts/test.ps1
pwsh ./scripts/package.ps1
```

`scripts/install-local.ps1` is a fallback diagnostic. It uses the Windows Documents known folder and blocks cloud-backed NinjaTrader trees.
