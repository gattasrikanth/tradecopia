# TradeCopia implementation report — OneDrive, installer, SIM gate

**Status language:** Alpha / automated tests only. Manual NinjaTrader SIM certification required before live use. Not Stable. Not live-certified.

Repository: https://github.com/gattasrikanth/tradecopia

## Commits

- Plan committed: `ea8ded0`
- Installer / SIM gate: `4014a65`
- Parent before this report slice: `ea3dc62`
- Report + payload embed: `0c783de`
- Linux CI path-test fix / `origin/main` tip at verification: `727aaae`

## OneDrive / Documents

| Item | Result (redacted) |
| --- | --- |
| Documents known folder | `%USERPROFILE%\Documents` (not OneDrive) |
| NinjaTrader user-data | `%USERPROFILE%\Documents\NinjaTrader 8` |
| OneDrive copy | retained (not deleted) |
| Backup | local `TradeCopia-Backups\NinjaTrader8\<timestamp>` with manifest |
| NinjaTrader version | 8.1.8.2 |

## Installer

- Technology: testable `TradeCopia.Installer` engine + self-contained `TradeCopia-Setup-0.1.0-alpha.1.exe` ([ADR-0007](../adr/ADR-0007-windows-installer.md)).
- Artifact: `TradeCopia-Setup-0.1.0-alpha.1.exe` (single-file; payload zip embedded).
- SHA-256: `05D1087C2D158F537D0DB751E6255F5C531F2C7047151E480664E68CC252D5B3`
- No NinjaTrader proprietary DLLs in the package.
- Cloud-backed NT user-data is a blocking preflight. No Install Anyway.
- Companion: per-user mutex ([ADR-0008](../adr/ADR-0008-companion-lifecycle.md)).

## Dogfood (development PC)

- Setup `--silent` installed under `%LOCALAPPDATA%\TradeCopia`.
- `TradeCopia.*` assemblies copied to NT `bin\Custom` only.
- Start Menu `Open TradeCopia` shortcut created.
- Control plane status: bind `127.0.0.1`, `copyingEnabled=false`. When NT was already logged in via backbone, `engineConnected=true`.

## SIM executor

- Official `NinjaTrader.Cbi.Provider` Simulator/Playback → KnownTrue; Unknown and live brokers fail closed ([ADR-0009](../adr/ADR-0009-simulation-provider-gate.md)).
- Browser cannot bypass the gate. No generic order-entry API.

## Automated tests (two consecutive `pwsh ./scripts/test.ps1`)

| Assembly | Passed |
| --- | --- |
| Installer.UnitTests | 12 |
| Protocol.UnitTests | 18 |
| Domain.UnitTests | 108 |
| ArchitectureTests | 4 |
| ControlPlane.UnitTests | 13 |
| **Total** | **155** |

Both runs exit 0. `scan-secrets.ps1` OK (199 files). Domain coverlet last measured 95.52% line / 91.41% branch.

## Remaining owner-only

| Item | Class |
| --- | --- |
| Manual SIM trade matrix S1–S10 | Owner / manual |
| NinjaTrader Welcome login | Owner / Windows Trading Backbone helper |
| SmartScreen/UAC on unsigned setup | Owner |
| Public CI compile of NT-referenced AddOn | External blocker |

## Confirmation

- Copying starts disabled.
- Product is not live-certified.
- TradeCopia does not store or fill NinjaTrader passwords.
