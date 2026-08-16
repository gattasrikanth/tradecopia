# Agent State

Last updated: 2026-08-16
Current branch: main
HEAD: see `git rev-parse HEAD` (do not add pin-only SHA commits)
Current phase: Real NT account discovery / dashboard Alpha.2
Phase status: REAL_ACCOUNTS_ALPHA. Not Stable. Not live-certified.

## Completed

- Customer install certified from GitHub Release `v0.1.0-alpha.1`.
- Real-account discovery, official-metadata classification (ADR-0010), draft/validate/activate, and no-fixture APIs shipped as `v0.1.0-alpha.4`.
- Published setup SHA-256: `CE88837513325FFE765CD50475E4DF1116E0832F937850BB94D98A5ED099951A`.
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
