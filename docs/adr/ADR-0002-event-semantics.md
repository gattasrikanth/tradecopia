# ADR-0002 — Authoritative event semantics

- Status: Accepted
- Date: 2026-08-16
- Phase: 1
- Design: SYSTEM-DESIGN §9

## Context

NinjaTrader emits order, execution, position, and account-status updates.
Copying from position deltas alone loses order intent and creates ambiguity.

Official NT 8 APIs used as the baseline:

- `Account.OrderUpdate` / `Account.ExecutionUpdate` / `Account.PositionUpdate`
- `Account.AccountStatusUpdate` (static)
- `Account.CreateOrder` + `Account.Submit` / `Change` / `Cancel` / `Flatten`

Observed on NinjaTrader Desktop **8.1.8.2** via official docs plus public-surface
reflection (no third-party copier code). `Order` exposes `Id`/`OrderId`,
`OrderState`, `OrderType`, `Quantity`, `Filled`, `LimitPrice`, `StopPrice`,
`Oco`, `Name`, and `OrderAction`. `CreateOrder` accepts an OCO string and an
order name (max 50 characters). `OrderType.MIT` is documented.

## Decision

- `OrderUpdate` is authoritative for order-intent lifecycle.
- `ExecutionUpdate` is authoritative for fills.
- `PositionUpdate` is reconciliation evidence, not a copy trigger.
- `AccountStatusUpdate` is a readiness gate.
- Domain logic consumes **normalized** events only. Native enum numeric values
  stay in the NinjaTrader adapter.
- Duplicate semantic states are ignored via fingerprints, never by timestamp.

## Consequences

- FakeNinjaTrader can drive the same coordinator as the real adapter.
- Execution Mirror Mode is deferred until Order Mirror Mode is stable.
