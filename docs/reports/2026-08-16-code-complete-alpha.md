# TradeCopia implementation report — 2026-08-16

**Status language:** Alpha / automated tests only. Manual NinjaTrader SIM certification required before live use. Not Stable. Not live-certified.

## Commits

- Starting `main` at goal resume: `eaa0ba190229f1e8ef38bf23860c39246ca8954c`
- Final `main` after this report: recorded in `docs/agent/STATE.md` after push

Repository: https://github.com/gattasrikanth/tradecopia

## Phases delivered

- Domain copier brain (Order Mirror): loop prevention, fingerprint idempotency, star/forest topology, sizing, mapping, scale-out never reverses, reject/divergence visibility, stale reconcile rejection, restart leaves **Disabled**
- SIM fail-closed executor: `SimulationGuardedExecutor` requires `TriState.KnownTrue`; name substrings are not used; default `DisabledOrderExecutor` never submits
- IPC: length-prefixed frames + `ProtocolSession` handshake/reconnect; `ExecuteOrder` rejected; incompatible version fail-closed
- Control plane: bind `127.0.0.1` only; Host/Origin/CSRF; no generic order-entry API
- Dashboard/journal/analytics/diagnostics on demo synthetic IDs (`SIM-LEADER-01` …)
- Playwright critical flow (load + CSRF + flatten prepare)
- Packaging script `scripts/package.ps1` (no NT binaries)

## Automated tests (local, two consecutive `pwsh ./scripts/test.ps1` runs)

| Assembly | Passed |
| --- | --- |
| Protocol.UnitTests | 11 |
| ArchitectureTests | 4 |
| Domain.UnitTests | 78 |
| ControlPlane.UnitTests | 8 |
| **Total** | **101** |

Both runs: exit 0. Playwright `tests/Web/dashboard.spec.ts`: 1 passed.

## Coverage

Coverlet on Domain.UnitTests: **line 87.7% / branch 75.8%**. Below System Design 95/90. Exception documented in `docs/development/coverage-exceptions.md` (identifier boilerplate and rare branches; NT wrappers excluded from public CI). Safety paths (loop, topology, sizing, SIM fail-closed, stale reconcile) have direct tests.

## Security checks

- Loopback bind only; `0.0.0.0` rejected by `LoopbackGuard`
- POST without CSRF → 403 (live probe + unit tests)
- POST `/api/v1/orders` with CSRF → 404 `no-generic-order-entry`
- `scan-secrets.ps1`: OK (156 files)
- No `NinjaTrader*.dll` in git

## Performance / latency

In-process `LatencySample` on coordinator finish. No disk/HTTP/DB on the domain decision path. No public numeric latency claims.

## CI

`ci` workflow on `82729ce` (and prior `eaa0ba1`): **success**. CodeQL on the newest SHA may still be in-flight at report time.

## Packaging

`scripts/package.ps1` publishes the control plane. Native AddOn compiles locally against NT 8.1.8.2 and is **not** redistributed.

## Screenshots

None. Playwright is the UI evidence. Optional polish screenshots deferred.

## Known limitations / classification

| Item | Class |
| --- | --- |
| Manual NT SIM certification | Manual SIM item |
| NT user-data directory missing | Genuine external blocker (install-local only) |
| Public CI cannot compile NT AddOn | Genuine external blocker (cannot commit proprietary DLLs) |
| Domain coverage < 95/90 | Approved documented exception (not a hidden defect) |
| OS named-pipe transport vs in-process `ProtocolSession` | Remaining independent work / next NEXT.md item |
| Live native Submit to NT SIM | Manual SIM item; implementation is fail-closed + disabled default |

## Confirmation

- Completed work is pushed to `origin/main`
- Product is **not** live-certified
- Copying starts **disabled**
