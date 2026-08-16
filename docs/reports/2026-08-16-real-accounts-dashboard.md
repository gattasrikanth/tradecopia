# TradeCopia real NT account discovery and dashboard (Alpha.2)

**Status language:** Alpha. Not Stable. Not live-certified. No live order and no autonomous SIM/demo trade was submitted.

Repository: https://github.com/gattasrikanth/tradecopia  
Release: https://github.com/gattasrikanth/tradecopia/releases/tag/v0.1.0-alpha.4

## Artifact

- File: `TradeCopia-Setup-0.1.0-alpha.4.exe`
- SHA-256: `CE88837513325FFE765CD50475E4DF1116E0832F937850BB94D98A5ED099951A`
- Source: GitHub Release `v0.1.0-alpha.4` (that exact setup EXE is installed)
- Alpha.2 kept the first empty snapshot. Alpha.3 refreshes snapshots. Alpha.4 classifies Tradovate demo from official `AccountType` (`Mode=Live` + `IsDemo=false` is not sufficient).
- Native payload ships `TradeCopia.Native`, `TradeCopia.Native.Adapter`, `TradeCopia.Protocol`, `TradeCopia.Domain` (no NinjaTrader proprietary DLLs)

## What shipped

- Official-metadata classification (`Account.Provider`, `ConnectOptions.Mode`, `ConnectOptions.IsDemo`). Display-name substring is not the safety signal. ADR-0010.
- Engine snapshot includes discovered accounts. `GET /api/v1/accounts` is empty + `engine-disconnected` when the pipe is down; engine accounts when connected. No `SIM-LEADER-01` / `SIM-FOLLOWER-*` fixtures.
- Dashboard draft → validate → activate → persist → enable non-live. Native `EnableCopying` rejects Live/Unknown and missing groups.
- Copying still starts disabled. Loopback + CSRF + no generic order-entry API remain.

## Verification

| Check | Result |
| --- | --- |
| `pwsh ./scripts/test.ps1` ×2 | exit 0 (171 tests) |
| scan-secrets | OK |
| Installed bind | `127.0.0.1:17841` |
| Disconnected accounts/status | no fixture account keys; `copyingEnabled=false` |
| Connected accounts/status | recorded after NT + engine (identifiers redacted) |
| CSRF POST without token | not 2xx |
| POST `/api/v1/orders` | 404 `no-generic-order-entry` |

## Owner-only next step

1. Confirm the two non-live accounts on the dashboard.
2. Create / validate / activate a SIM/Demo group.
3. Enable non-live copying.
4. Place **one** 1-MNQ SIM/demo leader market order and confirm the follower mirrors it.

Do not label Stable. Do not enable Live/Unknown.

## Confirmation

- Copying starts disabled.
- Product is not live-certified.
- Real account identifiers are not committed.
