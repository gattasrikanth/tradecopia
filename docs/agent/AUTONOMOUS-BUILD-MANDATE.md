# Autonomous Build Mandate

The executing agent is the coordinating Build Agent for the open-source NinjaTrader trade-copier project.

Read the attached `OPEN-TRADE-COPIER-FULL-SYSTEM-DESIGN-AUTONOMOUS-SDLC-2026-08-15.md` completely before acting. If the PRD is also attached, treat the PRD as product source of truth and the Full System Design as the implementation/SDLC source of truth.

Execute the design end-to-end without waiting for routine approvals.

Start by creating a **brand-new public GitHub repository** using the currently authenticated GitHub account. Use `open-trade-copier` as the working repository name unless a final repo name has already been supplied; if that name is unavailable/unrelated, use `open-trade-copier-nt8`. Do not create a GitHub Organization automatically. The project can be transferred to an organization later after branding is finalized.

Then proceed phase-by-phase through the full design.

Non-negotiable operating rules:

- Work autonomously and keep moving through every feasible phase.
- Use a Coordinator Agent plus isolated subagents/worktrees when parallelism is safe and materially useful.
- Stabilize shared contracts before parallelizing dependent modules.
- Make small coherent commits.
- Test before committing.
- Push completed commits/checkpoints continuously.
- Keep `main` green.
- Keep the working tree clean at each checkpoint.
- Never overwrite or destroy unrelated repositories/files.
- Persist continuation state in `docs/agent/STATE.md`, `NEXT.md`, `TASKS.md`, `DECISIONS.md`, and `BLOCKERS.md` so a fresh context can resume without chat history.
- Treat context-window exhaustion, terminal loss, agent restart, or machine interruption as expected: checkpoint first, push often, and resume from repository state.
- If a dependency/environment blocker affects one area, document it and continue all other feasible work instead of stopping.
- Never place orders in a real/live account as part of development or automated testing.
- Native order-submission smoke tests are allowed only when the target is positively identified as a simulation account; otherwise prepare the test and leave it for manual certification.
- Never commit real account names/numbers, credentials, secrets, local databases, user diagnostic bundles, or NinjaTrader proprietary binaries.
- Implement the trade copier originally from repository requirements, official platform APIs, and our own tests. Do not copy, decompile, derive from, or imitate third-party trade-copier source, binaries, UI assets, trade dress, private implementation details, or product copy. Do not reuse third-party trade-copier repository code in V1.
- Do not weaken tests, security, or safety semantics merely to finish faster.
- Do not introduce cloud/SaaS/telemetry dependencies. Runtime must remain local-only.
- The browser/control plane must never sit in the follower-order hot path.
- There must be no arbitrary browser API for placing discretionary trades in V1.
- Copying starts disabled.
- Do not label the product Stable or live-ready until the manual NinjaTrader SIM certification matrix has actually passed. The autonomous goal is the most complete **Code-Complete Alpha / Release Candidate** possible.

Continue until all feasible source, automated tests, dashboard, local control plane, native AddOn, journaling/analytics, diagnostics, security controls, packaging/scripts, documentation, CI, benchmark tooling, and synthetic/demo assets are implemented and pushed.

If screenshots are feasible, generate them automatically from synthetic demo data only and add a small number of polished screenshots to the README. Never use real account data.

At the end:

1. Run the fullest feasible automated build/test/coverage/security/performance suite.
2. Fix failures rather than hiding them.
3. Ensure `git status` is clean and all commits are pushed.
4. Create the final implementation report under `docs/reports/` with exact commit SHA, feature status, test evidence, coverage, security results, performance baseline, environment blockers, known limitations, and manual SIM work remaining.
5. Produce the manual NinjaTrader SIM certification checklist for the product owner.
6. If packaging is sound, create a clearly marked Alpha/pre-release GitHub Release with checksums; otherwise document exact packaging steps/blockers and leave source fully buildable.

Do not stop to ask ordinary questions. Choose safe, production-grade defaults consistent with the design, document important decisions in ADRs, and keep going.
