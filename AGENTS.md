# TradeCopia — Agent Instructions

You are working in the public **TradeCopia** repository: a local-first,
open-source NinjaTrader 8 multi-account trade copier.

## Resume first

A new context must **not** restart the project plan. Always:

1. `git status` and inspect the working tree.
2. Read `docs/agent/STATE.md`.
3. Read `docs/agent/NEXT.md`.
4. Read `docs/agent/TASKS.md`, `docs/agent/DECISIONS.md`, and `docs/agent/BLOCKERS.md`.
5. Inspect recent `git log`.
6. Run `scripts/resume.ps1`.
7. Continue the first uncompleted task.

## Sources of truth

| Document | Role |
| --- | --- |
| `docs/product/PRD.md` | Product requirements |
| `docs/architecture/SYSTEM-DESIGN.md` | Architecture, implementation, SDLC |
| `docs/agent/AUTONOMOUS-BUILD-MANDATE.md` | Persistent operating contract |
| `docs/adr/` | Locked implementation decisions |
| `docs/agent/STATE.md` | Resume checkpoint |

The Grok/operator launch prompt is **not** part of this repository.

## Non-negotiable rules

- Work autonomously. Do not wait for routine approvals.
- Small coherent commits. Test before commit. Push completed work.
- Keep `main` green and the working tree clean at checkpoints.
- Never place orders in a real/live account.
- Native submit smoke tests require a positively identified SIM account.
- Never commit secrets, real account data, diagnostic bundles, or NinjaTrader proprietary binaries.
- Do not copy or derive from third-party trade-copier products.
- Runtime is local-only. No SaaS, telemetry, CDN, or license server.
- Browser/control plane is never in the follower-order hot path.
- No generic browser API for discretionary order entry.
- Copying starts disabled.
- Do not label the product Stable or live-ready until manual NT SIM certification passes.
- Persist state in `docs/agent/*` before context resets or long delegation.

## Naming

Product and repository name: **TradeCopia**.

C# namespaces and project names use `TradeCopia.*`. Design documents may still
say `OpenTradeCopier` as a historical working title; treat that as the same
product.

## Safety defaults

- Default HTTP bind: `127.0.0.1`.
- Default dashboard port: `17841` with documented fallbacks.
- Local data root: `%LOCALAPPDATA%\TradeCopia\`.
- Synthetic fixtures only: `SIM-LEADER-01`, `SIM-FOLLOWER-01`, etc.

## Coordination

The coordinating agent owns integration to `main`. Isolated subagents use
branches/worktrees and must not push conflicting changes directly to `main`.
Stabilize shared contracts before parallelizing dependent modules.
