# Agent State

Last updated: 2026-08-16
Current branch: main
HEAD: see `git rev-parse HEAD` after this commit (pipe-host wiring). Do not add a follow-up pin-only SHA commit.
Current phase: Phase 12
Phase status: COMPLETE_ALPHA (automated). Not Stable. Not live-certified.

## Completed

- Domain coverage gate met (coverlet line>=95, branch>=90).
- OS named-pipe engine host + companion client; handshake; ExecuteOrder rejected.
- Pause/disable fail closed when engine disconnected (503 engine-disconnected).
- Shipped `TradeCopiaEngineHost.Start()` / `EngineRuntime.Start()` host `NamedPipeEngineHost`.
- Control plane `Program` calls `EngineLink.StartRetryAttach` and status/diagnostics expose live snapshot fields.
- `ProtocolSession` applies pause/disable/resume to observable `engineState` / `copyingEnabled`.
- SIM fail-closed executor; copying starts disabled.

## Current invariants / locked decisions

- Copying starts disabled. No generic order-entry API.
- Bind 127.0.0.1 only. CSRF required on POST.
- Simulation identity fail-closed.
- Pause/disable require a connected engine pipe and mutate the session snapshot.

## Tests last run

Recorded in the Alpha report after the verification pass that includes this commit.

## Known blockers

- NT user-data dir missing (install-local / manual SIM).
- Public CI cannot compile NT-referenced AddOn.

## Active subagents/worktrees

- none

## Next exact action

- External only: owner launches NinjaTrader once, then `scripts/install-local.ps1` and manual SIM certification.
- Independent source work for this Alpha slice is the pipe-host wiring in this commit. Do not label Stable.
