# Agent State

Last updated: 2026-08-16
Current branch: main
HEAD: see `git rev-parse HEAD` (do not add pin-only SHA commits)
Current phase: First-trade UX / Alpha.7
Phase status: FIRST_TRADE_UX_ALPHA. Not Stable. Not live-certified.

## Completed

- Customer install certified from GitHub Release `v0.1.0-alpha.1`.
- Real-account discovery and native copy path shipped as `v0.1.0-alpha.5`.
- First-trade UX (customer labels, Save & Activate, preflight, Pause/Disable states) shipped as `v0.1.0-alpha.6`.
- Published setup SHA-256: `76252F5555709FC791EF765EC126E0017C3E208BD34BF6E88310F5D7A6A7CBA7`.
- Start Menu launches installed single-file `TradeCopia.Launcher.exe`; companion starts; copying disabled; loopback only.

## Current invariants / locked decisions

- Copying starts disabled. No generic order-entry API.
- Bind 127.0.0.1 only. CSRF required on POST.
- Account safety uses official NT Mode / IsDemo / Provider only.
- Alpha may enable only Simulation and Demo/Paper.
- NinjaTrader Welcome login is owned by Windows Trading Backbone.

## Known blockers

- Manual first SIM/demo trade (and full S1–S10) is owner-only.
- Public CI cannot compile NT-referenced AddOn.
- Unsigned Alpha may show SmartScreen/UAC.

## Next exact action

Owner: create/activate a non-live group from the dashboard, enable non-live copying, then one MNQ SIM/demo leader → follower market order. Do not label Stable.
