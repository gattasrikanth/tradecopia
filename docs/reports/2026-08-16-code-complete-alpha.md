# TradeCopia implementation report — 2026-08-16

**Status language:** Alpha / automated tests only. Manual NinjaTrader SIM certification required before live use. Not Stable. Not live-certified.

## Commits

- Starting `main` at goal resume: `eaa0ba190229f1e8ef38bf23860c39246ca8954c`
- Feature tip (named-pipe + fail-closed pause/disable + domain coverage gate): `8b1d2938a86128bf1a99f2ca6f150eccce561f26`
- Agent-state sync commit on `main`: `494005bbecc1727d0dbf0b54d6faff6378ba6c9b` (and any subsequent docs-only STATE HEAD pin).

Repository: https://github.com/gattasrikanth/tradecopia

## Phases delivered

- Domain copier brain (Order Mirror) with loop prevention, fingerprint idempotency, topology, sizing, mapping, non-reversing scale-out, visible rejects, stale reconcile rejection, restart/disable
- Domain coverlet **line 95.52% / branch 91.41%** (gates met). NT AddOn wrappers remain out of public CI only (`docs/development/coverage-exceptions.md`).
- OS named-pipe transport: engine `NamedPipeEngineHost` (server), companion `NamedPipeCompanionClient` / `EngineLink` (client). Handshake + `ExecuteOrder` rejected.
- Pause/disable **fail closed** with `503 engine-disconnected` when no pipe (does not return `accepted:true`).
- SIM fail-closed executor; default `DisabledOrderExecutor`
- Loopback control plane + dashboard; CSRF; no generic order-entry API
- Playwright dashboard flow (synthetic)

## Automated tests (two consecutive `pwsh ./scripts/test.ps1`)

| Assembly | Passed |
| --- | --- |
| Protocol.UnitTests | 15 |
| ArchitectureTests | 4 |
| Domain.UnitTests | 98 |
| ControlPlane.UnitTests | 11 |
| **Total** | **128** |

Both runs exit 0.

## Live control-plane probe (twice)

- bind `127.0.0.1`, `copyingEnabled: false`, `engineConnected: false`
- POST pause without CSRF → 403
- POST pause with CSRF, no engine → 503
- POST `/api/v1/orders` with CSRF → 404

## Security

- `scan-secrets.ps1` OK (164 files)
- No NinjaTrader DLLs in git

## CI

`ci` on `8b1d293`: success (see scratch `ci.txt`).

## Remaining (classified)

| Item | Class |
| --- | --- |
| Manual NT SIM certification | Manual SIM item |
| NT user-data directory / public CI NT compile | Genuine external blocker |
| Live NT Submit on SIM | Manual SIM item (implementation fail-closed + disabled default) |

## Confirmation

- Completed work pushed to `origin/main`
- Product is not live-certified
- Copying starts disabled
