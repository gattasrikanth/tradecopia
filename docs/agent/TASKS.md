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
| P1-02 | Native AddOn project targeting .NET Framework 4.8 | DONE |
| P1-03 | No-order-submit adapter facade | DONE |
| P1-04 | ADRs: events, shared TFM, IPC, packaging, control-plane runtime | DONE |

## Phase 2 — Shared domain, state machines, fake engine

| ID | Task | Status |
| --- | --- | --- |
| P2-01 | Domain / contracts / protocol projects | DONE |
| P2-02 | Identifiers, events, config, sizing, topology | DONE |
| P2-03 | State machines and execution intents | DONE |
| P2-04 | Fake NinjaTrader adapter + fixtures | DONE |
| P2-05 | Unit / property / architecture tests | DONE |

## Phase 7–9 — Control plane and dashboard

| ID | Task | Status |
| --- | --- | --- |
| P7-01 | Named-pipe protocol types | DONE |
| P7-02 | OS named-pipe engine server + companion client | DONE |
| P7-03 | Shipped AddOn/runtime host the pipe; control plane retries attach; session snapshot mutates | DONE |
| P8-01 | Loopback control plane + security | DONE |
| P8-02 | Demo API and journal/analytics read models | DONE |
| P9-01 | Local dashboard SPA | DONE |
| P9-02 | Playwright E2E | DONE |

## Installer / OneDrive / SIM certification (2026-08-16 plan)

| ID | Task | Status |
| --- | --- | --- |
| I-A1 | Commit plan + agent state | DONE |
| I-A2 | Machine inventory (redacted) | DONE |
| I-B | Backup + migrate NT user-data off OneDrive | DONE |
| I-C | Known-folder resolver + cloud-path policy | DONE |
| I-D | Customer installer + companion lifecycle | DONE |
| I-E | No-F5 native AddOn deployment | DONE |
| I-H | Self-contained publish + GitHub pre-release | DONE |
| I-I | Automated installer tests | DONE |
| I-K | SIM-only native executor at execution boundary | DONE |
| I-J | Dogfood real TradeCopia-Setup-*.exe | DONE |
| I-L | Docs + implementation report | DONE |

See `docs/architecture/SYSTEM-DESIGN.md` §55 Phases 3–14 and `docs/architecture/ONEDRIVE-INSTALLER-RELEASE-PLAN.md`.
