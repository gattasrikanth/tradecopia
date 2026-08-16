# Agent State

Last updated: 2026-08-16T04:10:00Z
Current branch: main
HEAD: (update after push)
Current phase: Phase 2
Phase status: IN_PROGRESS

## Completed

- Phase 0 repository/governance (public `gattasrikanth/tradecopia`, Apache-2.0).
- Phase 1 ADRs 0001–0006 (name/license, events, TFM, IPC, packaging, control plane).
- Official NT 8 API notes; Desktop 8.1.8.2 detected locally.
- Shared domain: identifiers, config, sizing, topology, fingerprints, origin registry, copy coordinator.
- Protocol length-prefixed framing.
- Disabled native order executor + subscription registry (no submits).
- FakeNinjaTrader broker harness.
- Automated tests: 59 passing (domain 51, protocol 4, architecture 4).
- CI now restores/tests `TradeCopia.slnx`.

## Current invariants / locked decisions

- Product name TradeCopia; license Apache-2.0; namespaces `TradeCopia.*`.
- Copying starts disabled. Browser is not in the hot path.
- OrderUpdate = intent; ExecutionUpdate = fills; PositionUpdate = reconcile only.
- V1 topology is a strict star/forest (no leader also a follower).
- Risk caps block rather than clamp.
- No generic order-entry API.

## Tests last run

- `dotnet test TradeCopia.slnx` — 59 passed, 0 failed
- `pwsh ./scripts/scan-secrets.ps1` — OK (110 files)
- `pwsh ./scripts/verify-ninjatrader.ps1` — NT 8.1.8.2 present; user-data dir missing
- Domain coverage snapshot ~70% line / ~59% branch (below 95/90 gate; more tests required)

## Known blockers

- Native `net48` AddOn compile: targeting pack install attempted (4.8.1); reference assemblies path still not visible to this session. Public CI will not compile NT adapter (by design).
- NT user-data directory missing until NinjaTrader is launched once.

## Active subagents/worktrees

- none

## Next exact action

- Raise domain coverage toward 95/90 with more state-machine/scenario tests.
- Add remaining Phase 2 pieces: explicit transition matrix docs, instrument mapping tests, reconcile planner skeleton.
- When net48 targeting pack is visible, compile `src/Native/TradeCopia.Native` against local NT refs without copying DLLs.
- Do not start browser work until domain/protocol are stable.
