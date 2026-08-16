# Agent State

Last updated: 2026-08-16
Current branch: main
HEAD: see `git rev-parse HEAD` (do not add pin-only SHA commits)
Current phase: Customer install / experience certification
Phase status: INSTALL_CERTIFIED_ALPHA. Not Stable. Not live-certified.

## Completed

- Customer install certified from GitHub Release `v0.1.0-alpha.1`.
- Published setup SHA-256: `E4BC287500F8198730A3BC815A0B50AE2A6C58BAA0A0657CBD27278CA6131F4E`.
- Start Menu launches installed single-file `TradeCopia.Launcher.exe`; companion starts; copying disabled; loopback only.
- Launcher apphost-without-DLL defect fixed in `package.ps1` (`4499ff6`) and the release asset was replaced.

## Current invariants / locked decisions

- Copying starts disabled. No generic order-entry API.
- Bind 127.0.0.1 only. CSRF required on POST.
- NinjaTrader Welcome login is owned by Windows Trading Backbone.

## Known blockers

- Manual first SIM trade (and full S1–S10) is owner-only.
- Public CI cannot compile NT-referenced AddOn.
- Unsigned Alpha may show SmartScreen/UAC.

## Next exact action

Owner: start NinjaTrader via Windows Trading Backbone, then one MNQ SIM leader → SIM follower market order. Do not label Stable.
