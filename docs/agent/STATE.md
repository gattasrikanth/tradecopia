# Agent State

Last updated: 2026-08-16
Current branch: main
HEAD: see `git rev-parse HEAD` (do not add pin-only SHA commits)
Current phase: Installer / OneDrive / SIM-certification plan
Phase status: IN_PROGRESS. Not Stable. Not live-certified.

## Completed

- Domain coverage gate met (coverlet line>=95, branch>=90).
- OS named-pipe engine host + companion client; handshake; ExecuteOrder rejected.
- Pause/disable fail closed when engine disconnected (503 engine-disconnected).
- Shipped `TradeCopiaEngineHost.Start()` / `EngineRuntime.Start()` host `NamedPipeEngineHost`.
- Control plane retries attach; session snapshot mutates on pause/disable/resume.
- SIM fail-closed executor wrapper exists; copying starts disabled.

## Current invariants / locked decisions

- Copying starts disabled. No generic order-entry API.
- Bind 127.0.0.1 only. CSRF required on POST.
- Simulation identity fail-closed.
- Pause/disable require a connected engine pipe and mutate the session snapshot.
- Cloud-backed NinjaTrader user-data is unsupported for normal install.

## Tests last run

Prior Alpha slice: 135 automated tests; Domain coverlet 95.52 / 91.41; `ci` green on `0c6b111`.

## Known blockers

- Windows Documents known folder currently resolves under OneDrive. Local `%USERPROFILE%\Documents` exists but does not contain NinjaTrader 8.
- Public CI cannot compile NT-referenced AddOn.
- Manual owner SIM trades remain owner-only.

## Active subagents/worktrees

- none

## Next exact action

Execute `docs/architecture/ONEDRIVE-INSTALLER-RELEASE-PLAN.md`: path resolver, NT user-data backup/migration off OneDrive, customer installer, no-F5 AddOn deploy, SIM native executor, release artifact, dogfood setup EXE.
