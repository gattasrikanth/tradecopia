# Task board

Status values: `TODO` | `IN_PROGRESS` | `DONE` | `BLOCKED`.

## Phase 0 — Repository, governance, and agent continuity

| ID | Task | Status |
| --- | --- | --- |
| P0-01 | Public GitHub repository `tradecopia` | DONE |
| P0-02 | `main` default branch pushed | DONE |
| P0-03 | Apache-2.0 LICENSE | DONE |
| P0-04 | `.gitignore`, `.editorconfig`, `.gitattributes` | DONE |
| P0-05 | README skeleton + warning language | DONE |
| P0-06 | SECURITY / CONTRIBUTING / CODE_OF_CONDUCT / THIRD_PARTY_NOTICES | DONE |
| P0-07 | PRD + System Design + mandate in `docs/` | DONE |
| P0-08 | `AGENTS.md` and `docs/agent/*` continuity files | DONE |
| P0-09 | `docs/reports/` and directory skeleton | DONE |
| P0-10 | GitHub Actions / Dependabot / CodeQL | DONE |
| P0-11 | Secret/account-data scan script | DONE |
| P0-12 | `scripts/resume.ps1` | DONE |

## Phase 1 — Architecture spikes and API verification

| ID | Task | Status |
| --- | --- | --- |
| P1-01 | Locate NT install and record version (no sensitive paths committed) | DONE |
| P1-02 | Native AddOn project targeting .NET Framework 4.8 | BLOCKED |
| P1-03 | No-order-submit adapter facade | DONE |
| P1-04 | ADRs: events, shared TFM, IPC, packaging, control-plane runtime | DONE |

## Phase 2 — Shared domain, state machines, fake engine

| ID | Task | Status |
| --- | --- | --- |
| P2-01 | Domain / contracts / protocol projects | DONE |
| P2-02 | Identifiers, events, config, sizing, topology | DONE |
| P2-03 | State machines and execution intents | IN_PROGRESS |
| P2-04 | Fake NinjaTrader adapter + fixtures | DONE |
| P2-05 | Unit / property / architecture tests | IN_PROGRESS |

## Later phases

See `docs/architecture/SYSTEM-DESIGN.md` §55 Phases 3–14. Do not start them until Phase 2 acceptance is met, except for independent docs/CI that do not invert the dependency order.
