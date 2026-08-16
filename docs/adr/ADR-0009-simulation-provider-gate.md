# ADR-0009: SIM gate uses official NinjaTrader Provider

- Status: Accepted
- Date: 2026-08-16

## Decision

Native submit is allowed only when `NinjaTrader.Cbi.Account.Provider` is `Simulator` or `Playback` (`TriState.KnownTrue`). `Unknown` and all other providers (including Interactive Brokers, Trading Technologies, and custom live adapters) fail closed. Account display names are not used.

Verified against local NinjaTrader 8.1.8.2 `NinjaTrader.Core.dll` (`Provider` enum includes `Simulator`, `Playback`, `Unknown`, and live brokers).

## Consequences

- Browser/control plane cannot bypass the gate.
- A spoofed "SIM-*" account name on a live provider cannot submit.
