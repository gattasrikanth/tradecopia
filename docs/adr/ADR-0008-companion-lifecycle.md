# ADR-0008: Per-user companion lifecycle

- Status: Accepted
- Date: 2026-08-16

## Decision

The control plane runs as a per-user process (not LocalSystem). A named mutex enforces one instance. Start Menu "Open TradeCopia" launches `TradeCopia.Launcher`, which starts the companion if needed and opens `http://127.0.0.1:17841`. Closing the browser does not stop the companion. Restart never enables copying.

## Consequences

- No Windows service, no firewall rule, no LAN bind.
- Tests use `ASPNETCORE_ENVIRONMENT=Testing` and skip the mutex.
