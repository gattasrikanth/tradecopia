# ADR-0004 — IPC ownership

- Status: Accepted
- Date: 2026-08-16
- Phase: 1
- Design: SYSTEM-DESIGN §23

## Context

The browser must never reach the execution engine. The companion process may
restart independently of NinjaTrader.

## Decision

- Transport: Windows named pipe (not TCP).
- The **native engine hosts** the pipe server; the control plane is the client.
- Pipe name pattern: `TradeCopia.Engine.v1.<sid-hash>` (no raw SID/username
  in logs).
- ACL: current interactive Windows user only.
- Framing: length-prefixed UTF-8 JSON with a versioned envelope.
- There is no generic `ExecuteOrder` message.
- Telemetry is bounded and must not block follower submission.

## Consequences

- Companion restart cannot take the engine down.
- Protocol types live in `TradeCopia.Protocol` and are shared by both ends.
