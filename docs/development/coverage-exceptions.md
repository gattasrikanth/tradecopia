# Coverage exceptions

System Design asks for domain line >= 95% and branch >= 90%.

Public CI cannot load NinjaTrader proprietary assemblies, so these surfaces are
excluded from the gate:

- `src/Native/TradeCopia.Native/**` — NT AddOn wrappers (`AddOnBase`, `Account` events).
  Compiled only on a machine with local NT references; not in the public solution test graph.
- Direct NinjaTrader SDK method thunks (CreateOrder/Submit/Change/Cancel) — not present
  in the Alpha submit path. Production submit remains `DisabledOrderExecutor` plus
  `SimulationGuardedExecutor` (unit-tested).

Shared domain latest measured snapshot (coverlet, Domain.UnitTests):

- line ≈ 87.7%
- branch ≈ 75.8%

Remaining uncovered domain lines are identifier `Equals(object)`/operator
boilerplate, rare coordinator branches, and unused descriptor getters — not
NinjaTrader wrappers. Raising that to 95/90 without gaming exclusions is still
open work; it is **not** a silent gate waiver for safety logic (sizing,
topology, loop prevention, SIM fail-closed, stale reconcile) which is
directly tested.

A coverage snapshot is captured in `docs/reports/` with each Alpha report.
