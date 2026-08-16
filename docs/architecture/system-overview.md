# System overview

TradeCopia is a hybrid three-layer local application.

```text
NinjaTrader 8
  TradeCopia.Native  (execution engine, named-pipe server)
        |
        | Windows named pipe (current-user ACL)
        v
TradeCopia.ControlPlane  (.NET 10, loopback HTTP + SSE + SQLite)
        |
        | http://127.0.0.1:17841
        v
Browser SPA  (React + TypeScript)
```

## Responsibility split

| Layer | May submit/change/cancel follower orders | Owns durable config | Owns journal |
| --- | --- | --- | --- |
| Native engine | Yes | Active immutable snapshot only | No |
| Control plane | No | Yes (drafts + history) | Yes |
| Browser | No | No | No |

Closing the browser must not stop copying. The engine continues from its last
accepted `ActiveConfigSnapshot` if the companion disappears.

## Copying

Default mode is **Order Mirror**. Execution Mirror is deferred.

Leader `OrderUpdate` drives intent. `ExecutionUpdate` drives fills.
`PositionUpdate` is reconciliation evidence only.

Copying starts **disabled**.

## Identity

See `docs/adr/` and `docs/architecture/SYSTEM-DESIGN.md`.
