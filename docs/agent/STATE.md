# Agent State

Last updated: 2026-08-16T12:30:00Z
Current branch: main
HEAD: b638b8f
Current phase: Phase 8
Phase status: IN_PROGRESS

## Completed

- Phase 0–1 governance and ADRs.
- Phase 2 domain engine (coordinator, sizing, topology, mapping, fingerprints, origin registry, reconcile planner). Coverage still below 95/90.
- Native AddOn compiles locally (`net481`) against NinjaTrader 8.1.8.2. Inherits `AddOnBase`. **Does not submit orders.**
- Control plane: loopback bind, host/origin/CSRF, demo API, dashboard SPA, SQLite store with patched native SQLite.
- Docs: first-run, install, localhost security, SIM certification checklist.

## Current invariants / locked decisions

- TradeCopia / Apache-2.0 / `TradeCopia.*`
- Copying starts disabled
- No generic browser order-entry API
- Bind `127.0.0.1` only
- Unknown is never healthy

## Tests last run

- `dotnet test TradeCopia.slnx` — domain 61, protocol 4, architecture 4, control plane 8 (verify again before commit)
- Native `dotnet build src/Native/TradeCopia.Native` — succeeded locally
- `pwsh ./scripts/parse-scripts.ps1` — OK

## Known blockers

- NT user-data directory missing until the owner launches NinjaTrader once (`install-local` and SIM cert)
- Named-pipe live connection and SIM submit executor not yet wired
- Domain coverage gate not met
- Playwright/screenshots not done

## Active subagents/worktrees

- none

## Next exact action

- Push this checkpoint.
- Add named-pipe client/server framing integration.
- Continue SIM executor behind DisabledOrderExecutor + positive SIM detection.
- Raise domain coverage.
- Do not label Stable.
