# ADR-0007: Customer Windows installer

- Status: Accepted
- Date: 2026-08-16

## Decision

TradeCopia ships a versioned per-user `TradeCopia-Setup-<semver>.exe` whose install/uninstall/preflight logic lives in a testable `TradeCopia.Installer` library. The setup host is a .NET console/WinExe wrapper. WiX/Burn was considered; a custom engine was chosen so OneDrive/NT preflight, owned-file deploy, and installer tests run in public CI without MSI tooling.

## Consequences

- End users do not run PowerShell or compile source.
- Setup blocks cloud-backed NinjaTrader user-data. There is no Install Anyway in V1.
- Artifacts are published, not committed (`*.exe` remains gitignored).
