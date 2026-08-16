# Coverage exceptions

System Design asks for domain line >= 95% and branch >= 90%.

## Shared domain (in gate)

Latest coverlet snapshot from `TradeCopia.Domain.UnitTests`:

- line >= 95%
- branch >= 90%

Recorded in `docs/reports/` with each Alpha report.

## NT-only surfaces (out of public CI)

Public CI cannot load NinjaTrader proprietary assemblies, so these are excluded
from the public coverage gate:

- `src/Native/TradeCopia.Native/**` — NT AddOn wrappers (`AddOnBase`, `Account` events).
  Compiled only on a machine with local NT references; not in the public solution test graph.

Safety logic (sizing, topology, loop prevention, SIM fail-closed, stale reconcile,
named-pipe protocol) is tested without those assemblies.
