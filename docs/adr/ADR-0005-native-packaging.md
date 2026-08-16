# ADR-0005 — Native component packaging

- Status: Accepted
- Date: 2026-08-16
- Phase: 1
- Design: SYSTEM-DESIGN §50

## Context

NinjaTrader AddOns integrate through the official AddOnBase / NTWindow flow.
Proprietary NinjaTrader assemblies must not be committed or redistributed.

## Decision

- Native code lives in `src/Native/TradeCopia.Native` (AddOn entry) and
  `src/Native/TradeCopia.Native.Adapter` (platform mapping, no NT reference
  in the adapter **contracts**).
- Project HintPaths resolve
  `$(ProgramW6432)\NinjaTrader 8\bin\NinjaTrader.*.dll` or
  `NinjaTraderBin` override. `Private=false`.
- Public CI never compiles the NT-referenced project unless those assemblies
  are present (they will not be on GitHub runners).
- Installation copies only TradeCopia artifacts into the NinjaTrader user
  custom/AddOn location via `scripts/install-local.ps1` (later phase).
- Updates require a NinjaTrader restart; never replace a loaded DLL silently.

## Consequences

- Missing .NET Framework 4.8 targeting pack or NT user-data directory is an
  environment blocker, not a product-architecture change.
