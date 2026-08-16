# Next actions

1. Write Phase 1 ADRs: event semantics, shared TFM, IPC ownership, native packaging, control-plane runtime.
2. Create `src/Native` projects targeting .NET Framework 4.8 with a no-order-submit adapter facade.
3. Run `scripts/verify-ninjatrader.ps1` and attempt a local native compile without committing NT binaries.
4. If native compile is blocked, mark it in BLOCKERS and start Phase 2 (shared domain + FakeNinjaTrader).
5. Implement domain identifiers, sizing, topology validation, and state machines with tests.
6. Push each completed slice; keep `main` green.
