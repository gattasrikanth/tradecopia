# Next actions

1. Add more domain scenario tests (stop/limit/MIT, multi-group, instrument mapping, dry-run) to raise coverage toward the 95/90 gate.
2. Extract a documented transition-matrix table into `docs/architecture/order-state-machine.md`.
3. Add a reconcile-planner skeleton that never auto-repairs.
4. Compile native AddOn when the net48 targeting pack is visible; keep NT binaries uncommitted.
5. After Phase 2 acceptance, start Phase 3 SIM market-order adapter behind the disabled executor + SIM guard.
6. Keep pushing each completed slice to `main`.
