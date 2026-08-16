# ADR-0003 — Shared target framework

- Status: Accepted
- Date: 2026-08-16
- Phase: 1
- Design: SYSTEM-DESIGN §5.2

## Context

The native AddOn must target **.NET Framework 4.8** (NinjaTrader 8 documented
environment). The control plane targets **.NET 10 LTS**. Shared copy logic
must be unit-testable without NinjaTrader assemblies.

## Decision

- `TradeCopia.Domain`, `TradeCopia.Contracts`, and `TradeCopia.Protocol`
  target `netstandard2.0`.
- Shared projects reference neither NinjaTrader nor ASP.NET / SQLite.
- Native AddOn: `net48` (machine-local NT references, `Private=false`).
- Control plane and tests: `net10.0` (or `net10.0-windows` where required).
- Language version is `latest` with nullable enabled. Prefer explicit types
  over runtime features that are unavailable on netstandard2.0.

## Consequences

- Public CI builds shared/domain/control-plane without proprietary NT DLLs.
- Native compile is a local Windows verification step
  (`scripts/verify-ninjatrader.ps1`).
