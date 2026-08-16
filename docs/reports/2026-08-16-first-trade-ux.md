# TradeCopia first-trade UX and safety hardening (Alpha.6)

**Status language:** Alpha. Not Stable. Not live-certified. No order was submitted.

Repository: https://github.com/gattasrikanth/tradecopia  
Release: https://github.com/gattasrikanth/tradecopia/releases/tag/v0.1.0-alpha.6

## Artifact

- File: `TradeCopia-Setup-0.1.0-alpha.6.exe`
- SHA-256: `76252F5555709FC791EF765EC126E0017C3E208BD34BF6E88310F5D7A6A7CBA7`
- Does not overwrite `v0.1.0-alpha.5`.

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
