# TradeCopia first-trade UX and safety hardening (Alpha.6)

**Status language:** Alpha. Not Stable. Not live-certified. No order was submitted.

Repository: https://github.com/gattasrikanth/tradecopia  
Release: https://github.com/gattasrikanth/tradecopia/releases/tag/v0.1.0-alpha.7

## Artifact

- File: `TradeCopia-Setup-0.1.0-alpha.7.exe`
- SHA-256: `C38E61AA16305D533D05B5D409D88FE95151725E4197427F54E486B29F01C5AF`
- Does not overwrite `v0.1.0-alpha.5` or `v0.1.0-alpha.6`.
- Alpha.7: Save & Activate updates one logical group; customer list is one card; preferred pairing is Simulation leader → Demo/Paper follower.

## What shipped

- Customer labels: `Simulation`, `Demo / Paper`, `Live — Locked`, `Unknown — Blocked`.
- Overview status chips replace unexplained `UNKNOWN`. Blank critical banners do not render.
- Normal UI shows NinjaTrader display names, never `Provider|Id` or `Selectable = true`.
- One group card. `Save & Activate` = draft → validate → native activate; failure does not activate.
- Leader cannot follow itself. Live/Unknown locked with a reason. Default sizing `1 : 1`.
- Enable confirmation; enabled state shows Pause New Entries and Disable Copying only.
- First-trade preflight is Ready only when every blocking check passes.
- Native hot path has no new HTTP/DB/UI dependency.

## Verification

| Check | Result |
| --- | --- |
| `test.ps1` ×2 | exit 0 |
| Playwright isolated :17842 | 2 passed |
| CSRF / `/orders` | 403 / 404 `no-generic-order-entry` |
| Native adapter | no AspNetCore/Sqlite/wwwroot refs |

## Next human action

Verify the displayed SIM leader and Demo follower, click **Enable Non-Live Copying**, then place **one 1-MNQ market order on the leader** in NinjaTrader.
