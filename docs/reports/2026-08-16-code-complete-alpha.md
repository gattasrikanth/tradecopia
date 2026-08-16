# TradeCopia implementation report — 2026-08-16

**Status language:** Alpha / automated tests only. Manual NinjaTrader SIM certification required before live use. Not Stable. Not live-certified.

## Commits

- Parent `origin/main` before this wiring: `a0468ac92f7813d0e95d794efaf58eb56959cedd`
- This report describes the pipe-host + session-state commit on `main`. Read the SHA from git; do not add a pin-only follow-up commit.

Repository: https://github.com/gattasrikanth/tradecopia

## Phases delivered

- Domain copier brain (Order Mirror) with loop prevention, fingerprint idempotency, topology, sizing, mapping, non-reversing scale-out, visible rejects, stale reconcile rejection, restart/disable
- Domain coverlet **line 95.52% / branch 91.41%** (gates met). NT AddOn wrappers remain out of public CI only (`docs/development/coverage-exceptions.md`).
- OS named-pipe transport: engine `NamedPipeEngineHost` (server), companion `NamedPipeCompanionClient` / `EngineLink` (client). Handshake + `ExecuteOrder` rejected.
- **Shipped hosts use the pipe:** `EngineRuntime.Start()` / `TradeCopiaEngineHost.Start()` construct and start `NamedPipeEngineHost`. The NT AddOn calls that `Start()`. Control-plane `Program` calls `EngineLink.StartRetryAttach`.
- `ProtocolSession` applies pause / disable / resume to observable `engineState` / `copyingEnabled` and replies with `EngineStateSnapshot`.
- Pause/disable **fail closed** with `503 engine-disconnected` when no pipe (does not return `accepted:true`).
- SIM fail-closed executor; default `DisabledOrderExecutor`
- Loopback control plane + dashboard; CSRF; no generic order-entry API; dashboard shows live engine snapshot fields
- Playwright dashboard flow (synthetic)

## Automated tests (two consecutive `pwsh ./scripts/test.ps1`)

| Assembly | Passed |
| --- | --- |
| Protocol.UnitTests | 18 |
| ArchitectureTests | 4 |
| Domain.UnitTests | 100 |
| ControlPlane.UnitTests | 13 |
| **Total** | **135** |

Both runs exit 0.

## Live control-plane probe (twice)

- bind `127.0.0.1`, `copyingEnabled: false`, `engineConnected: false`, `engineState: Unknown`
- POST pause without CSRF → 403
- POST pause with CSRF, no engine → 503 `engine-disconnected` (`accepted:false`)
- POST `/api/v1/orders` with CSRF → 404 `no-generic-order-entry`

## Security

- `scan-secrets.ps1` OK (167 files)
- No NinjaTrader DLLs in git

## Remaining (classified)

| Item | Class |
| --- | --- |
| Manual NT SIM certification | Manual SIM item |
| NT user-data directory / public CI NT compile | Genuine external blocker |
| Live NT Submit on SIM | Manual SIM item (implementation fail-closed + disabled default) |

## Confirmation

- Completed work is intended for `origin/main`
- Product is not live-certified
- Copying starts disabled
