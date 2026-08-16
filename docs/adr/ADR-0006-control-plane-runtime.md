# ADR-0006 — Control-plane runtime

- Status: Accepted
- Date: 2026-08-16
- Phase: 1
- Design: SYSTEM-DESIGN §5.3, §24, §25

## Context

The control plane hosts the loopback API, config drafts, journal, and SPA
files. It must not sit in the order hot path.

## Decision

- Runtime: .NET 10 LTS, ASP.NET Core, self-contained win-x64 for release.
- Bind: `127.0.0.1` only. Default port `17841` with documented fallbacks.
- API: versioned REST under `/api/v1/`. Telemetry: SSE.
- Persistence: SQLite (`control.db`, `journal.db`) under
  `%LOCALAPPDATA%\TradeCopia\`.
- Dashboard: React + TypeScript + Vite + pnpm, local static assets only.
- Destructive commands use a two-step confirmation token.
- No generic order-entry HTTP endpoint.

## Consequences

- Closing the browser cannot stop copying.
- Control-plane failure after a valid config snapshot is accepted does not
  disable the native engine.
