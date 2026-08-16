# Contributing to TradeCopia

Thank you for helping build a trustworthy local-first NinjaTrader 8 trade copier.

## Product and architecture sources of truth

1. `docs/product/PRD.md` — product requirements.
2. `docs/architecture/SYSTEM-DESIGN.md` — architecture, SDLC, and implementation mandate.
3. `docs/adr/` — locked decisions that refine the design.
4. `docs/agent/` — autonomous-agent continuity files. Humans may update these when handing off work.

Do not copy, decompile, or derive implementation from third-party trade-copier products.

## Development prerequisites

- Windows x64 (native AddOn and named-pipe work).
- Git, GitHub CLI optional.
- .NET 10 SDK for domain, protocol, and control plane.
- .NET Framework 4.8 targeting pack / Visual Studio Build Tools for the native AddOn.
- Node.js 20+ and pnpm for the dashboard.
- NinjaTrader 8 Desktop for native compile and SIM certification.

Run `scripts/resume.ps1` to print environment and agent-state status.

## Safety

- Never use real/live accounts in automated tests.
- Never commit account names/numbers, credentials, API keys, user diagnostic bundles, or NinjaTrader proprietary DLLs.
- Issues and fixtures must use synthetic names such as `SIM-LEADER-01`.
- Native order-submission smoke tests are allowed only when the target is positively identified as a simulation account.

## Architecture boundaries

- Shared domain/contracts/protocol: no NinjaTrader, web, or database references.
- Native engine: only component authorized to submit/change/cancel follower orders.
- Control plane: local persistence, REST/SSE, config drafts, journal, analytics.
- Browser: observe and configure; never sits in the follower-order hot path.

## Workflow

1. Create a focused branch for one coherent change.
2. Add or update tests with the change.
3. Run the relevant test suite (`scripts/test.ps1` when available, otherwise project-local test commands).
4. Keep diffs reviewable. Prefer small commits.
5. Update docs/ADRs when behavior or architecture changes.
6. Open a pull request using the template.

`main` must stay green. Do not weaken tests, security, or safety semantics to make CI pass.

## Commit messages

Use Conventional Commits, for example:

- `feat(domain): add scale-out sizing`
- `fix(native): unsubscribe account events on unload`
- `test(domain): cover cyclic topology rejection`
- `docs: add recovery procedure`
- `chore: bootstrap repository governance and docs`

## Security reports

See `SECURITY.md`. Do not file public issues for exploitable control-plane or order-submission defects.
