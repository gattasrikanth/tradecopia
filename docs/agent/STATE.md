# Agent State

Last updated: 2026-08-16
Current branch: main
HEAD: see `git rev-parse HEAD` (do not add pin-only SHA commits)
Current phase: Installer / OneDrive / SIM-certification plan
Phase status: COMPLETE_ALPHA (independent work). Not Stable. Not live-certified.

## Completed

- Documents known folder is local (`%USERPROFILE%\Documents`), not OneDrive.
- NinjaTrader 8 user-data copied to the local Documents tree; OneDrive copy retained; backup under local `TradeCopia-Backups`.
- Known-folder resolver + cloud-path preflight; setup blocks OneDrive NT trees.
- Per-user installer engine, setup host, launcher, companion mutex.
- No-F5 native deploy copies `TradeCopia.*` only into `bin\Custom`.
- SIM native submit gated on official `Provider` Simulator/Playback; live/unknown fail closed.
- Dogfood install from setup on this machine; copying starts disabled.

## Current invariants / locked decisions

- Copying starts disabled. No generic order-entry API.
- Bind 127.0.0.1 only. CSRF required on POST.
- Simulation identity fail-closed at the native execution boundary.
- Cloud-backed NinjaTrader user-data is unsupported for normal install.
- NinjaTrader Welcome login is owned by Windows Trading Backbone, not TradeCopia.

## Tests last run

Recorded in the implementation report for this slice.

## Known blockers

- Public CI cannot compile NT-referenced AddOn (no proprietary assemblies).
- Manual owner SIM trade matrix (S1–S10) remains owner-only.
- Unsigned Alpha may show SmartScreen/UAC (owner).

## Active subagents/worktrees

- none

## Next exact action

Owner-only: backbone NT login after reboot if needed; run `docs/testing/manual-sim-certification.md` on SIM accounts. Do not label Stable.
