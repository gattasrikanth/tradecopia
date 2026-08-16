# OpenTradeCopier — Full System Design, SDLC, and Autonomous Build Specification

**Status:** Execution-grade design / autonomous Build Agent mandate  
**Date:** 2026-08-15  
**Working product name:** `OpenTradeCopier` (temporary; branding can change without architectural impact)  
**Default working GitHub repository:** `open-trade-copier`  
**Target platform:** NinjaTrader 8 Desktop on Windows  
**Distribution:** Public, free forever, open source  
**Default license:** Apache-2.0 unless a final product-owner branding/license decision is supplied before repository bootstrap  
**Primary experience:** Modern localhost browser dashboard  
**Execution model:** Native NinjaTrader leader → follower order-copying engine  
**Operating model:** Local-only; no SaaS account, license server, telemetry server, or cloud runtime required  

> **This document is both the system design and the autonomous implementation mandate.** A capable coding agent may execute it end-to-end without waiting for routine approvals. The agent must preserve the safety boundaries, test gates, documentation requirements, commit discipline, and source-control checkpoints defined here.

---

# 0. How the Build Agent Must Use This Document

This file is the authoritative implementation specification for the initial product build. The Product Requirements Document remains the product source of truth; this document resolves the major implementation decisions and turns them into a buildable architecture and SDLC.

The Build Agent must:

1. Read this document completely before changing code.
2. Create the public GitHub repository immediately if it does not exist.
3. Bootstrap source control, governance, CI, documentation, and project structure before feature coding.
4. Implement in the phase order defined here unless a dependency requires a justified reorder.
5. Work autonomously. Do **not** pause for ordinary product-owner approvals.
6. Use subagents/worktrees when parallel work is safe and materially faster.
7. Commit small coherent slices and push each completed slice.
8. Keep `main` green and the working tree clean at every checkpoint.
9. Persist agent state in the repository so a context reset, agent restart, terminal loss, or machine reboot does not destroy project continuity.
10. Never place real trades as part of automated development or testing.
11. Never commit credentials, real account names/numbers, API keys, local secrets, user paths containing personal information, screenshots with account data, or NinjaTrader proprietary binaries.
12. Continue through non-blocking failures. Record blockers and complete everything else that remains feasible.
13. Finish by producing a code-complete release-candidate repository, automated test evidence, documentation, installation tooling, and a final implementation report.

## 0.1 What “autonomous” means

Autonomy means the Build Agent should decide routine implementation details within the boundaries of this design, fix its own build/test/lint failures, refactor when needed, create documentation, run tests, commit, push, and move to the next task without asking the user to approve every step.

Autonomy does **not** permit:

- live trading;
- use of real brokerage credentials;
- destructive changes to unrelated repositories or system configuration;
- copying, decompiling, or deriving implementation details, assets, UI trade dress, or source from any third-party proprietary trade-copier product;
- weakening tests to make CI pass;
- silently changing product safety semantics;
- publishing claims such as “live-ready” before manual NinjaTrader simulation certification is completed.

## 0.2 Unattended-run reality

The repository must be made **resumable**, but no coding-agent prompt can guarantee that a third-party agent process will automatically relaunch after a host OS reboot unless that agent platform itself provides task persistence/restart. Therefore:

- persist all progress frequently;
- push every completed unit of work;
- maintain `docs/agent/STATE.md` as a machine-readable/human-readable resume checkpoint;
- maintain `docs/agent/NEXT.md` with the exact next action;
- create `scripts/resume.ps1` that performs environment checks and prints the resume state;
- if the agent platform supports persistent tasks/subagent continuation, enable those capabilities;
- if execution is interrupted, a new agent context must be able to resume by reading repository state without relying on chat history.

---

# 1. Product Mission

Build the best free/open-source NinjaTrader 8 trade copier: highly reliable, low-latency, local-first, modern, observable, testable, and pleasant enough to use that it feels like a contemporary fintech application rather than a legacy trading-platform plugin.

The user trades once in a **leader** NinjaTrader account. The product mirrors eligible lifecycle activity into configured **follower** accounts according to explicit per-follower rules.

The copier itself does not originate trade ideas and does not require the trader to use a particular strategy, Chart Trader, SuperDOM, ATM template, or external signal source. It operates from observable NinjaTrader account/order/execution events.

The product has two equally important layers:

1. **Execution core:** intentionally small, deterministic, defensive, and boring.
2. **Experience/control plane:** modern local browser UX for configuration, status, journal, analytics, latency, diagnostics, reconciliation, and safe controls.

---

# 2. Independent Product Direction

This product is an original, requirements-driven implementation. Its architecture and behavior must be derived from:

- this repository's Product Requirements Document;
- this System Design and approved Architecture Decision Records;
- official NinjaTrader platform APIs and documentation;
- official Microsoft/.NET platform documentation;
- first-principles reliability, security, usability, and performance engineering;
- empirical behavior observed in our own simulation and certification tests.

The implementation must not depend on, clone, imitate, or derive from the source code, decompiled binaries, private implementation details, UI assets, trade dress, or internal behavior of third-party trade-copier products.

For V1, the Build Agent must not import or reuse source code from third-party trade-copier repositories. If future maintainers propose a third-party dependency or code reuse, it requires an explicit architecture decision, license review, provenance record, and product-owner approval before introduction.

## 2.1 Product differentiation

The product should distinguish itself through the combination of:

- native account-event-driven NinjaTrader copying;
- an original correctness-first execution state machine;
- open-source implementation;
- free-forever core product;
- no cloud dependency;
- modern localhost browser UX;
- deep journal and execution timelines;
- explicit divergence state;
- deterministic reconciliation;
- meaningful latency instrumentation;
- crash/restart clarity;
- exceptional automated tests and documentation;
- privacy by default;
- auditable releases.

## 2.2 Clean implementation provenance

Repository history should make implementation provenance easy to understand:

- requirements and design decisions are documented before implementation;
- platform behavior is validated against official APIs and our own tests;
- no competitor source code is copied into the repository;
- no proprietary assets or screenshots are used;
- original UI components, copy, diagrams, icons, and interaction patterns are created for this product;
- dependencies are general-purpose engineering dependencies rather than trade-copier implementations unless explicitly approved later.

---

# 3. Non-Negotiable Engineering Principles

## 3.1 Correctness outranks feature velocity

A missed, duplicated, reversed, oversized, orphaned, or incorrectly canceled follower order is a critical defect. A visually beautiful dashboard never compensates for execution uncertainty.

## 3.2 No blocking I/O in the hot path

The NinjaTrader event callback path must not synchronously perform:

- SQLite operations;
- HTTP requests;
- WebSocket sends;
- file writes;
- analytics aggregation;
- screenshot generation;
- update checks;
- dependency calls outside NinjaTrader required for the copy decision;
- long-running locks.

## 3.3 Browser is not the execution engine

The browser may configure and observe. The local service may persist and aggregate. Only the NinjaTrader execution engine may submit/change/cancel follower orders.

Closing the browser must have zero impact on active copying.

## 3.4 Companion failure must not strand valid active copying

Once the native engine has atomically accepted a valid active configuration snapshot, that snapshot is sufficient for continued copying if the companion process or browser disappears.

A companion reconnect performs state synchronization; it does not become a prerequisite for each order.

## 3.5 Unknown is never healthy

When the product cannot prove synchronization, the UI must show `UNKNOWN` or `DIVERGENT`, never `HEALTHY`.

## 3.6 Safety semantics are explicit

`Pause`, `Disable`, `Stop New Entries`, `Flatten`, and `Emergency Flatten` are distinct commands with distinct behavior.

## 3.7 No silent dangerous repair

Reconciliation that can create/cancel/modify orders must have an exact proposed action plan. Auto-repair is allowed only for narrowly defined, deterministic, pre-approved classes in later versions. V1 defaults to explicit reconciliation.

## 3.8 Local-only really means local-only

The shipped app must make no external network request during normal operation other than whatever NinjaTrader/broker connectivity the user already has. No analytics, fonts, icons, JS CDNs, telemetry, error reporting, license checks, or update pings may occur unless a future explicit opt-in feature is added.

## 3.9 Public repository contains no private trading data

Examples, fixtures, screenshots, docs, and demos use synthetic accounts such as `SIM-LEADER-01` and `SIM-FOLLOWER-01` only.

---

# 4. Locked High-Level Architecture

The production architecture is a **hybrid three-layer local application**.

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Windows Trading Machine                            │
│                                                                             │
│  ┌──────────────────────────── NinjaTrader 8 ────────────────────────────┐   │
│  │                                                                       │   │
│  │  OpenTradeCopier.Native                                               │   │
│  │  ┌─────────────────────────────────────────────────────────────────┐  │   │
│  │  │ Account subscriptions                                           │  │   │
│  │  │ Leader classifier / state machine                               │  │   │
│  │  │ Copy policy evaluator                                           │  │   │
│  │  │ Follower order mapper/executor                                  │  │   │
│  │  │ OCO/bracket coordinator                                         │  │   │
│  │  │ Idempotency / loop prevention                                   │  │   │
│  │  │ Divergence primitives                                           │  │   │
│  │  │ Active immutable config snapshot                                │  │   │
│  │  │ High-resolution timestamps                                      │  │   │
│  │  └───────────────────────┬─────────────────────────────────────────┘  │   │
│  │                          │ async bounded telemetry / commands         │   │
│  └──────────────────────────┼─────────────────────────────────────────────┘   │
│                             │ Windows Named Pipe (current-user ACL)           │
│  ┌──────────────────────────▼─────────────────────────────────────────────┐   │
│  │ OpenTradeCopier.ControlPlane (.NET 10 LTS self-contained process)     │   │
│  │                                                                        │   │
│  │ REST API / SSE or WebSocket                                            │   │
│  │ Config drafts + validation                                             │   │
│  │ SQLite config + journal                                                │   │
│  │ Event persistence                                                      │   │
│  │ Analytics                                                              │   │
│  │ Diagnostics / support bundle                                           │   │
│  │ Local SPA static file host                                              │   │
│  │ Loopback security                                                       │   │
│  └──────────────────────────┬─────────────────────────────────────────────┘   │
│                             │ http://127.0.0.1:<port>                         │
│  ┌──────────────────────────▼─────────────────────────────────────────────┐   │
│  │ Browser — React + TypeScript                                           │   │
│  │ Overview · Groups · Live Trades · Journal · Analytics · Diagnostics    │   │
│  └────────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 4.1 Why not put the entire web server inside NinjaTrader?

It is technically possible to host more functionality in-process, but production-grade risk isolation favors a minimal NinjaTrader component. A browser/service defect, database migration issue, analytics spike, UI dependency problem, or web-server bug must not destabilize the NinjaTrader process or add latency to order callbacks.

## 4.2 Why not use only a native WPF AddOn window?

A small native status window is useful as a fallback, but the primary goal is a modern web-class UX with flexible layout, charts, journal tables, responsive design, automated UI testing, and independent iteration. The browser SPA is a better fit.

## 4.3 Minimal native fallback UI

The NinjaTrader AddOn should expose only a compact native window/menu with:

- engine version;
- engine state;
- current config version;
- `Open Dashboard`;
- `Pause New Entries`;
- `Disable Copying`;
- connection/companion status;
- last critical error;
- emergency control entry point with confirmation.

It must not try to recreate the entire browser dashboard in WPF.

---

# 5. Technology Stack

## 5.1 NinjaTrader native component

- Language: C#
- Target framework: **.NET Framework 4.8**, aligned with NinjaTrader’s documented AddOn development environment.
- Build environment: Visual Studio/MSBuild on Windows.
- UI: minimal WPF only where required by NinjaTrader AddOn integration.
- Dependencies: near-zero production third-party dependencies.
- Logging: internal structured event model, asynchronously forwarded; fallback NinjaTrader trace for critical bootstrap failures.

## 5.2 Shared domain/contracts

Create dependency-light shared projects that can be consumed by both .NET Framework 4.8 and modern .NET.

Preferred target:

- `netstandard2.0` for DTOs/contracts/domain primitives where feasible;
- no NinjaTrader references in shared domain;
- no web/database dependencies;
- deterministic, highly unit-testable pure logic.

If a feature cannot be safely represented in `netstandard2.0`, use carefully isolated multi-targeting rather than dragging NinjaTrader dependencies into the control plane.

## 5.3 Local control plane

- Runtime: **.NET 10 LTS**.
- Hosting: ASP.NET Core, self-contained Windows x64 release.
- API: versioned REST (`/api/v1/...`).
- Real-time: prefer Server-Sent Events for one-way dashboard telemetry unless bidirectional WebSocket requirements materially justify WebSockets. State-changing browser actions still use authenticated REST commands.
- Persistence: SQLite.
- Database access: minimal explicit repository layer; avoid heavyweight abstractions in execution semantics.
- Background work: bounded `Channel<T>`/hosted services.
- Structured logs: JSON-capable local logs with rotation.

## 5.4 Browser application

- React + TypeScript.
- Vite or equivalent minimal modern bundler.
- TypeScript strict mode.
- Local-only static assets; zero CDN dependencies.
- CSS system: Tailwind CSS or an equally maintainable token-based system.
- Accessible primitives: Radix or equivalent permissively licensed primitives where useful.
- Icons: a permissively licensed local icon set such as Lucide.
- Charts: lightweight permissively licensed chart library; choose based on bundle/maintenance health at implementation time.
- Testing: Vitest + Testing Library + Playwright.
- Package manager: pnpm with frozen lockfile.

## 5.5 Scripting/tooling

- PowerShell 7 preferred for Windows install/build/release scripts; Windows PowerShell compatibility only where needed.
- No hardcoded user names or machine-specific absolute paths.
- Scripts discover NinjaTrader directories and allow explicit overrides.

---

# 6. Repository Bootstrap and GitHub Requirements

## 6.1 Repository creation

At the beginning of execution:

1. Verify GitHub CLI authentication with `gh auth status`.
2. Determine repository owner from the currently authenticated GitHub account.
3. Do **not** create a GitHub Organization automatically unless one is already explicitly configured in environment/input; organization creation is a separate account-governance decision.
4. Attempt to create public repository `open-trade-copier`.
5. If that exact repo already exists under the owner and is unrelated, use `open-trade-copier-nt8` rather than overwriting anything.
6. Initialize `main`.
7. Enable GitHub Issues.
8. Enable Discussions only if easily available; it is not a build blocker.
9. Add repository topics when supported: `ninjatrader`, `ninjatrader-8`, `trade-copier`, `futures`, `csharp`, `open-source`, `local-first`.
10. Push the bootstrap commit immediately.

## 6.2 First commit

First commit should contain only foundational project material:

- README skeleton;
- LICENSE;
- `.gitignore`;
- `.gitattributes`;
- `.editorconfig`;
- SECURITY.md;
- CONTRIBUTING.md;
- CODE_OF_CONDUCT.md;
- THIRD_PARTY_NOTICES.md;
- PRD copied into `docs/product/PRD.md` if available locally;
- this design in `docs/architecture/FULL-SYSTEM-DESIGN.md`;
- agent continuity files;
- initial directory skeleton.

Suggested commit:

`chore: bootstrap open trade copier repository`

## 6.3 Branch policy during autonomous initial build

The coordinating agent may commit directly to `main` for sequential slices, provided every commit passes its required local checks before push.

Subagents must use isolated branches/worktrees and must **not** independently push conflicting code directly to `main`.

After public contributors exist, branch protections/PR review can be strengthened.

---

# 7. Required Repository Layout

```text
open-trade-copier/
├─ .github/
│  ├─ workflows/
│  │  ├─ ci.yml
│  │  ├─ codeql.yml
│  │  ├─ dependency-review.yml
│  │  └─ release.yml
│  ├─ ISSUE_TEMPLATE/
│  ├─ pull_request_template.md
│  └─ dependabot.yml
│
├─ src/
│  ├─ Native/
│  │  ├─ OpenTradeCopier.Native.sln
│  │  ├─ OpenTradeCopier.Native/
│  │  └─ OpenTradeCopier.Native.Adapter/
│  │
│  ├─ Shared/
│  │  ├─ OpenTradeCopier.Domain/
│  │  ├─ OpenTradeCopier.Contracts/
│  │  └─ OpenTradeCopier.Protocol/
│  │
│  ├─ ControlPlane/
│  │  ├─ OpenTradeCopier.ControlPlane/
│  │  ├─ OpenTradeCopier.Persistence/
│  │  └─ OpenTradeCopier.Analytics/
│  │
│  └─ Web/
│     ├─ package.json
│     ├─ pnpm-lock.yaml
│     └─ src/
│
├─ tests/
│  ├─ Domain.UnitTests/
│  ├─ Protocol.UnitTests/
│  ├─ Native.UnitTests/
│  ├─ Native.ContractTests/
│  ├─ ControlPlane.UnitTests/
│  ├─ ControlPlane.IntegrationTests/
│  ├─ Persistence.IntegrationTests/
│  ├─ ArchitectureTests/
│  ├─ Performance/
│  ├─ FaultInjection/
│  ├─ Web/
│  └─ fixtures/
│
├─ tools/
│  ├─ FakeNinjaTrader/
│  ├─ EventReplay/
│  ├─ BenchmarkRunner/
│  └─ DiagnosticsInspector/
│
├─ scripts/
│  ├─ bootstrap.ps1
│  ├─ build.ps1
│  ├─ test.ps1
│  ├─ verify-ninjatrader.ps1
│  ├─ install-local.ps1
│  ├─ uninstall-local.ps1
│  ├─ package.ps1
│  ├─ security-scan.ps1
│  ├─ generate-demo-data.ps1
│  └─ resume.ps1
│
├─ docs/
│  ├─ product/
│  ├─ architecture/
│  ├─ adr/
│  ├─ testing/
│  ├─ operations/
│  ├─ security/
│  ├─ development/
│  ├─ images/
│  ├─ reports/
│  └─ agent/
│
├─ installer/
│  ├─ packaging/
│  └─ assets/
│
├─ demo/
│  └─ synthetic-data/
│
├─ AGENTS.md
├─ CHANGELOG.md
├─ CONTRIBUTING.md
├─ CODE_OF_CONDUCT.md
├─ LICENSE
├─ README.md
├─ SECURITY.md
├─ THIRD_PARTY_NOTICES.md
├─ Directory.Build.props
├─ global.json
└─ .editorconfig
```

Exact project names may be refined, but responsibility boundaries must remain.

---

# 8. Domain Model

The domain model must be explicit. Do not let NinjaTrader object references become the business model.

## 8.1 Core identifiers

Use strongly typed identifiers rather than unstructured strings internally where practical.

Suggested primitives:

- `CopyGroupId`
- `ConfigVersion`
- `AccountKey`
- `InstrumentKey`
- `LeaderOrderKey`
- `FollowerOrderKey`
- `LogicalOrderId`
- `LogicalTradeId`
- `OcoGroupId`
- `ExecutionKey`
- `DivergenceId`
- `CommandId`
- `EventId`
- `SessionId`

Never use database auto-increment IDs as the only cross-process correlation identity. Prefer GUID/ULID-style application IDs generated before persistence.

## 8.2 Account model

```text
AccountDescriptor
- AccountKey
- DisplayName
- ConnectionName
- ConnectionState
- IsSimulation (KnownTrue / KnownFalse / Unknown)
- ProviderKind (when determinable)
- Currency
- Capabilities
```

`IsSimulation=Unknown` must not be treated as simulation.

## 8.3 Instrument model

```text
InstrumentDescriptor
- InstrumentKey
- FullName
- RootSymbol
- Expiry
- InstrumentType
- TickSize
- PointValue (when reliable)
- NativeIdentity
```

Instrument mapping is explicit and versioned.

## 8.4 Copy group model

```text
CopyGroup
- CopyGroupId
- Name
- LeaderAccountKey
- Followers[]
- CopyMode
- InstrumentRules
- EntryPolicy
- ExitPolicy
- RiskPolicy
- EnabledState
- ConfigVersion
```

## 8.5 Follower rule

```text
FollowerRule
- AccountKey
- Enabled
- SizingPolicy
- MaxQuantity
- MaxAbsolutePosition
- InstrumentMappings
- SymbolAllowList / DenyList
- RiskGuards
- BehaviorOverrides
```

## 8.6 Logical order

A `LogicalOrder` represents what the copier believes the leader is asking the group to do. It is not the same thing as one native NinjaTrader Order object.

```text
LogicalOrder
- LogicalOrderId
- CopyGroupId
- LeaderOrderKey
- LogicalTradeId? 
- InstrumentKey
- Side / OrderAction
- OrderType
- RequestedQuantity
- LimitPrice?
- StopPrice?
- TimeInForce
- LeaderOcoIdentity?
- ParentRelationship?
- IntentClassification
- State
- SemanticRevision
- FirstObservedAt
- LastObservedAt
```

## 8.7 Follower order link

```text
FollowerOrderLink
- LogicalOrderId
- FollowerAccountKey
- FollowerOrderKey?
- IntendedQuantity
- SubmittedQuantity
- FilledQuantity
- IntendedPrices
- LastObservedNativeState
- FollowerOcoGroupId?
- DispatchTimestamp
- AckTimestamp?
- TerminalTimestamp?
- Health
- LastError?
```

## 8.8 Logical trade

A logical trade groups related entry/protection/exit activity for journaling and reconciliation.

```text
LogicalTrade
- LogicalTradeId
- CopyGroupId
- LeaderAccountKey
- InstrumentKey
- Direction
- OpenedAt
- ClosedAt?
- LeaderExecutions[]
- ChildLogicalOrders[]
- FollowerStates[]
- LifecycleState
```

The precise rule that creates/ends a logical trade must be deterministic and documented; it must not be based merely on UI grouping.

---

# 9. Authoritative Event Semantics

The engine uses a **hybrid event model**.

## 9.1 OrderUpdate is authoritative for order-intent lifecycle

`OrderUpdate` drives:

- detection of a new working/submitted leader order;
- price modifications;
- quantity modifications;
- cancellations;
- terminal order-state transitions;
- observation of protective child orders when represented as account orders.

## 9.2 ExecutionUpdate is authoritative for fills

`ExecutionUpdate` drives:

- leader fill correlation;
- partial-fill accounting;
- logical trade position deltas;
- execution-based copy mode;
- follower fill tracking;
- fill-price analytics;
- bracket activation quantity decisions where follower fill quantity matters.

## 9.3 PositionUpdate is reconciliation evidence, not primary copy trigger

`PositionUpdate` is used to:

- verify net positions;
- detect unexplained mismatch;
- recover from missed/non-correlatable events;
- support reconcile previews;
- detect manual follower intervention.

Do not primarily copy position deltas because they lose order intent and can create ambiguity.

## 9.4 AccountStatusUpdate is a gating signal

Account/connection status drives readiness and safety state.

A disconnected follower must not be presented as successfully synchronized.

## 9.5 Event ordering assumptions

Never assume order/execution/position events arrive in a simplistic globally ordered sequence.

Implementation rules:

- process each account event through a serialized per-account/per-order domain pipeline where needed;
- permit concurrent processing across unrelated accounts/groups when safe;
- tolerate duplicate semantic states;
- tolerate an execution being observed near order-state updates;
- maintain idempotent transition functions;
- preserve raw observation timestamps and normalized processing timestamps.

---

# 10. Copier Modes

The architecture supports two explicit modes. Do not mix their semantics invisibly.

## 10.1 Order Mirror Mode — primary/default

Purpose: mirror pending and working leader orders and their lifecycle.

Behavior:

1. A qualifying new leader order becomes a `LogicalOrder`.
2. Follower quantity/instrument is computed immediately.
3. Equivalent follower orders are submitted as soon as the leader reaches the configured eligible submission state.
4. Leader price/quantity changes update follower orders when safe and supported.
5. Leader cancel triggers follower cancel.
6. Leader/follower executions are tracked independently.
7. Divergence is surfaced when follower state cannot follow the intended lifecycle.

Best suited to:

- discretionary limit/stop orders;
- order-based ATM/bracket mirroring;
- users who expect followers to have working orders before leader fill.

## 10.2 Execution Mirror Mode — advanced

Purpose: replicate leader **filled quantity deltas** rather than pending order intent.

Behavior:

1. Pending leader orders are observed but do not create follower entry orders.
2. A new leader execution delta triggers follower market/appropriate configured execution orders.
3. Each leader execution ID/delta is idempotently tracked.
4. Follower protective behavior must be explicitly configured.

Best suited to:

- traders who only want confirmed leader fills copied;
- simple scalping workflows;
- cases where follower pending-order rejection/divergence is undesirable.

Tradeoffs must be clearly explained in the UI: execution mode necessarily waits for leader fill and may increase follower fill-price difference.

## 10.3 V1 sequencing

Build Order Mirror Mode first. Build Execution Mirror Mode only after the common mapping/idempotency foundation is stable.

---

# 11. Leader Event Eligibility and Loop Prevention

## 11.1 Never copy our own follower order back into a leader path

Every copier-originated order must be correlated in an in-memory `OriginRegistry` before or atomically with submission.

Preferred markers, in order:

1. Supported native order metadata/tag/name fields that can safely contain a compact copier correlation marker.
2. Direct object/order-key registry maintained before submission.
3. Persistent follower-order mapping once native order ID is assigned.

Never rely solely on human-readable order names if NinjaTrader/provider can mutate them.

## 11.2 Recursive topology validation

Configuration validation constructs a directed graph of leader → follower account relationships.

Reject any active configuration containing:

- self-edge A → A;
- two-node cycle A → B → A;
- longer cycle A → B → C → A;
- a follower that is simultaneously a leader in a way that can receive copier-originated flow and retransmit it, unless a future explicitly designed cascading mode exists.

V1 should favor a strict forest/star topology.

## 11.3 Semantic event fingerprinting

NinjaTrader may emit repeated updates. Compute a semantic fingerprint containing fields relevant to copier action, for example:

```text
LeaderOrderKey
OrderState
OrderType
Action
Quantity
Filled
LimitPrice
StopPrice
TimeInForce
OCO identity
```

Only a meaningful semantic revision should create a new copy action.

Never dedupe merely by timestamp.

---

# 12. Order State Machine

The implementation must formalize the state machine in code and documentation. No sprawling event-handler `if/else` logic without a transition model.

## 12.1 Normalized leader order states

Map NinjaTrader native states into stable domain states such as:

```text
Observed
PendingSubmission
Working
PartiallyFilled
Filled
CancelPending
Canceled
ChangePending
Rejected
UnknownTerminal
```

Native state mapping belongs in the NinjaTrader adapter; domain logic does not depend on undocumented numeric enum values.

## 12.2 Logical copy states

```text
Discovered
Validated
Dispatching
Active
PartiallySatisfied
Satisfied
Canceling
Canceled
Failed
Divergent
Terminal
```

## 12.3 Follower link health

Each follower link has independent state:

```text
NotApplicable
Pending
Dispatched
Acknowledged
Working
PartiallyFilled
Filled
Canceled
Rejected
Disconnected
Divergent
Unknown
```

Group-level health is an aggregation and must not hide a single unhealthy follower.

## 12.4 Transition function style

Prefer pure functions:

```text
(previousState, normalizedEvent, activePolicy) -> TransitionDecision
```

`TransitionDecision` contains:

- new domain state;
- zero or more `ExecutionIntent`s;
- warnings;
- telemetry events;
- invariant violations.

The adapter executes `ExecutionIntent`s and returns results as subsequent events.

This pattern makes the state machine testable without NinjaTrader.

---

# 13. Follower Execution Intents

The domain layer never directly calls NinjaTrader.

It emits explicit intents such as:

```text
SubmitFollowerOrder
ChangeFollowerOrder
CancelFollowerOrder
FlattenFollowerInstrument
NoOp
RaiseDivergence
StageProtectionOrder
ActivateProtectionOrder
```

Every intent has:

- `CommandId`;
- source `EventId`;
- copy group;
- follower account;
- logical order/trade correlation;
- expected preconditions;
- exact order parameters;
- creation timestamp;
- reason code.

Before executing an intent, the adapter re-validates safety-critical preconditions that could have changed since decision creation.

---

# 14. Sizing Engine

Sizing is pure, deterministic logic.

## 14.1 V1 modes

### 1:1

`followerQty = leaderQty`

### Multiplier

`followerQty = roundAccordingToPolicy(leaderQty * multiplier)`

Default multiplier rounding: **floor toward zero for entry exposure**, with minimum 1 only if explicit `minimumOne=true`. Never silently round 0.4 to 1 unless configured.

### Fixed

New qualifying entry intent uses configured fixed quantity. Scale-outs use the deterministic proportional algorithm below rather than blindly reapplying fixed quantity.

### Disabled

No new entry copy action. Exit/protection behavior is governed separately by `ExitPolicy` to avoid stranding an already-copied position.

## 14.2 Scale-out algorithm

Maintain leader initial/copied basis and follower actual filled basis.

When a leader reduces position/order quantity, derive a reduction fraction and target follower remaining quantity rather than simply multiplying the reduction in isolation when that can produce rounding drift.

Example:

```text
Leader initial position: 3
Follower target initial: 2
Leader reduces 1 -> leader remains 2 (66.67% remains)
Follower target remaining = floor/definedRound(2 * 2/3) = 1
Follower reduction = actualFollowerPosition - 1
```

Define rounding centrally and test all small integer combinations.

Never let a reduce-only copy operation reverse a follower position.

## 14.3 Hard caps

Before submission:

- apply per-order maximum;
- apply max absolute position;
- account for working entry orders when computing projected exposure where possible;
- if projected state exceeds cap, block or clamp according to explicit policy;
- default V1 policy is **block and alert**, not silently clamp.

---

# 15. Instrument Mapping

## 15.1 Same-instrument default

Copy exact normalized contract identity where supported.

## 15.2 Mini/micro mapping

Provide a registry with explicit ratio metadata, initially including common CME mappings after verification, e.g. NQ ↔ MNQ and ES ↔ MES.

Do not infer quantity ratio solely from symbol names. Mapping entries specify:

```text
SourceRoot
TargetRoot
ContractValueRatio
DefaultQuantityRatio
ExpiryMappingPolicy
Enabled
```

## 15.3 Contract month

Default: target the corresponding expiry month only when the mapped instrument exists and is verified.

If target contract resolution fails:

- do not fall back to another expiry silently;
- block that follower action;
- raise high-severity configuration/execution error.

## 15.4 Rollover

V1 does not automatically rewrite active group mappings during rollover.

Dashboard should detect when configured leader/follower contract mappings differ from currently traded contract and warn.

---

# 16. Brackets, ATM, OCO, Stops, and Targets

This is a critical reliability subsystem.

## 16.1 Core principle

Mirror **observable order intent**, not the proprietary internals of the leader ATM template.

If the leader ATM generates native account orders for stop/target management, the copier observes those orders and maps their logical relationships.

Do not require access to the original ATM template name/configuration to copy resulting protection.

## 16.2 Follower-specific OCO identities

Never reuse a leader OCO value on followers.

For each logical leader OCO group:

```text
LeaderOcoIdentity
  -> Follower A OcoId A
  -> Follower B OcoId B
  -> Follower C OcoId C
```

Follower OCO IDs should be collision-resistant and generated locally.

## 16.3 Protection staging

A leader stop/target may become visible before a follower has sufficient filled position.

Use a `ProtectionIntent` model:

1. Observe/correlate leader protection order.
2. Compute follower target protection quantity and price.
3. If follower has adequate fill/position, submit protection immediately.
4. If not, stage the intent.
5. As follower execution arrives, activate protection for the newly protectable filled quantity.
6. Update quantity as additional fills arrive.

The exact provider behavior must be validated against NinjaTrader simulation.

## 16.4 Protection health invariant

If policy expects protection and a follower has nonzero copied exposure, the system must know whether expected stop/target protection exists.

States:

```text
Protected
PartiallyProtected
ProtectionPending
Unprotected
Unknown
```

`Unprotected` is critical severity.

## 16.5 OCO sibling completion

When one follower OCO child fills/cancels its sibling at provider/broker level, observe resulting native state and update domain mappings. Do not issue unnecessary duplicate cancellation unless policy/state requires it.

## 16.6 Leader stop/target modifications

Price and quantity modifications generate follower change intents only for active mapped child orders.

If follower order is already terminal, do not recreate automatically without a defined reconciliation path.

## 16.7 Manual follower intervention

If the user manually moves/cancels a follower stop, classify this as follower intervention and divergence unless a future policy explicitly allows independent follower management.

V1 default: warn and do not silently fight the user in an infinite change loop.

---

# 17. Partial Fills

Partial fills must be first-class state, not an edge case.

## 17.1 Order Mirror Mode

If a leader submits quantity 5 and follower submits mapped quantity 5, follower execution is allowed to differ in timing from leader execution. We do not repeatedly submit follower orders for each leader partial fill because the follower working order already represents the intended quantity.

Track:

- leader filled quantity;
- follower filled quantity;
- working remainder;
- protection quantity available;
- fill-price delta.

## 17.2 Execution Mirror Mode

Each unique leader execution delta maps to an independent follower execution intent.

Deduplicate by stable execution identity plus account/session context.

## 17.3 Cancel after partial fill

When leader cancels remaining quantity after a partial fill:

- cancel corresponding follower remainder if still working;
- retain filled portion as position state;
- retain/adjust protection for actual follower filled quantity;
- do not flatten the filled amount unless leader subsequently exits.

---

# 18. Rejections and Failure Semantics

## 18.1 Never hide a rejection

Follower rejection immediately records:

- follower account;
- native error/code/message;
- logical order;
- intended action;
- leader current state;
- resulting follower position/order state;
- whether protection is affected.

## 18.2 Automatic resubmission

V1 default: **no generic automatic resubmission as a different order type**.

Automatically converting rejected stop/limit orders to market orders can materially change risk. Such behavior may be added later only as an explicit opt-in policy with strong documentation.

## 18.3 Retry policy

Retry only failures proven to be transient and idempotently safe. Do not retry a submission merely because an acknowledgment is delayed; that can duplicate orders.

## 18.4 Ambiguous submission outcome

If the native API call outcome is ambiguous:

1. inspect account orders/mappings;
2. transition to `UNKNOWN`/`DIVERGENT`;
3. do not blindly resubmit;
4. surface a reconciliation action.

---

# 19. Connection State and Readiness

Each account has a readiness state:

```text
Unknown
Disconnected
Connecting
ConnectedButUnverified
Ready
BlockedByRisk
BlockedByConfig
```

A group is entry-ready only if:

- leader is observable/ready;
- every enabled follower required by policy is ready, or the group’s explicit partial-availability policy allows copying to remaining followers;
- active config is valid;
- engine is enabled;
- no global safety lock exists.

Default V1 policy when any enabled follower is disconnected: **block new group entries**, while permitting safety-reducing exit/protection actions for reachable accounts. The user may later configure `continue-with-ready-followers`, but that must be explicit.

---

# 20. Pause, Disable, Exit Safety, and Flatten Semantics

## 20.1 `PAUSE_NEW_ENTRIES`

- Stops new exposure-increasing copied actions.
- Continues reducing exits, stop/target changes, cancellations that reduce risk, and protective-order handling for existing copied exposure.
- Does not flatten anything.

This is the preferred “pause” semantic.

## 20.2 `DISABLE_GROUP`

- Prevents any new entry copying.
- Existing mapped trades remain tracked.
- Risk-reducing lifecycle activity continues by default until positions are flat, unless the user explicitly detaches them.

## 20.3 `DETACH_FOLLOWER`

Dangerous if active exposure exists.

When follower has active copied position/orders, require an explicit choice:

- `keep managing until flat, then detach`; or
- `detach now and leave account unmanaged` with strong warning.

## 20.4 `FLATTEN_FOLLOWER`

Two-step command:

1. Preview exact account/instruments/orders affected.
2. Confirmation command with short-lived confirmation token.

Flatten action itself is performed by the native engine using supported NinjaTrader account APIs.

## 20.5 `FLATTEN_GROUP`

Same two-step semantics; only follower accounts by default. Leader flatten is a separate explicit option.

## 20.6 Global emergency controls

Expose two distinct controls:

- `STOP NEW COPY ACTIVITY` — risk-neutral control.
- `EMERGENCY FLATTEN FOLLOWERS` — destructive trading action with confirmation.

Never combine them into an ambiguous single red button.

---

# 21. Configuration Ownership and Atomic Activation

## 21.1 Companion owns durable configuration

The control plane stores editable configuration and history.

## 21.2 Engine owns active runtime snapshot

The engine receives a complete validated `ActiveConfigSnapshot` with a monotonically increasing `ConfigVersion`.

The engine never consults SQLite in the hot path.

## 21.3 Draft → validate → activate

Dashboard changes create a draft.

Activation flow:

```text
Edit Draft
   ↓
Server schema/domain validation
   ↓
Topology + safety validation
   ↓
Engine preflight validation
   ↓
Show change summary
   ↓
Activate ConfigVersion N atomically
```

The native engine either accepts the complete snapshot or rejects it. No partial mutation.

## 21.4 Active-order configuration locks

Changes that could invalidate active mappings—leader change, follower route change, contract mapping, copy mode—must be blocked/staged while affected copied orders/positions exist unless a deterministic migration plan is implemented.

Safe fields such as display name may change immediately.

---

# 22. Restart and Recovery Model

## 22.1 Companion restart while NinjaTrader remains running

Expected behavior:

- native copier continues using its active immutable config;
- telemetry queue may retain a bounded recent window;
- browser becomes unavailable temporarily;
- when companion reconnects, perform protocol handshake and full state snapshot synchronization;
- companion reconciles its view with engine truth;
- journal indicates telemetry gap if any events exceeded the bounded buffer.

## 22.2 Browser restart

No effect on engine/control plane.

## 22.3 NinjaTrader restart

V1 safety default:

1. Engine starts **copying disabled**.
2. Companion sends latest valid config snapshot.
3. Engine enumerates current account orders/positions/executions available through NinjaTrader.
4. Recovery classifier determines whether prior logical mappings can be proven/reconstructed.
5. Dashboard shows a recovery report.
6. If no active positions/orders exist, enabling is straightforward.
7. If active state exists and mapping cannot be proven, remain disabled and mark `RECOVERY_REVIEW_REQUIRED`.

Do not blindly auto-resume after a NinjaTrader process restart in V1.

## 22.4 Future optional safe auto-resume

May be added after certification if all of the following are provable:

- previous shutdown was clean;
- active config hash matches;
- account identities match;
- active orders/positions match durable mapping snapshot;
- no ambiguous terminal state exists;
- provider reconnection has completed.

---

# 23. Engine ↔ Control Plane IPC Design

Use Windows Named Pipes rather than exposing an execution API on a TCP port.

## 23.1 Goals

- same-machine only;
- current-user access only;
- reconnectable;
- low overhead;
- versioned;
- bounded;
- no browser directly reaches the engine;
- companion failure cannot block execution callback;
- all messages validated.

## 23.2 Pipe ownership

Preferred design: the NinjaTrader engine hosts the named-pipe server; the companion connects as client.

Benefits:

- companion can restart/reconnect without engine dependency;
- engine lifecycle naturally defines availability;
- no companion process can impersonate engine unless it can access current-user pipe and pass protocol handshake.

If implementation evidence strongly favors inverse ownership, record an ADR before changing.

## 23.3 Pipe name

Use a versioned, user-scoped name, for example:

`OpenTradeCopier.Engine.v1.<sid-hash>`

Do not put full Windows SID or user name into logs unnecessarily.

## 23.4 ACL

Restrict pipe access to:

- current Windows user;
- LocalSystem only if installer/service architecture genuinely requires it.

Prefer companion running as the current interactive user, avoiding elevation and LocalSystem complexity.

## 23.5 Protocol framing

Use length-prefixed UTF-8 JSON or a similarly simple auditable framing protocol.

Do not use newline-delimited unbounded payloads.

Envelope:

```json
{
  "protocolVersion": 1,
  "messageId": "...",
  "messageType": "EngineEvent",
  "sentAtUtc": "...",
  "sessionId": "...",
  "payload": { }
}
```

Hard limits:

- maximum message length;
- maximum string lengths;
- maximum collection counts;
- schema/version validation.

## 23.6 Handshake

On connection:

1. companion sends `Hello` with supported protocol range and build version;
2. engine responds with chosen version, engine build, session ID, capabilities, and current state;
3. incompatible major protocol fails closed with clear diagnostics;
4. companion requests full snapshot;
5. engine sends current active config metadata, accounts, mapped orders, positions, divergences, and engine status.

## 23.7 Message classes

Engine → companion:

- `EngineHello`
- `EngineStateSnapshot`
- `AccountObserved`
- `AccountStateChanged`
- `LeaderEventObserved`
- `CopyDecisionMade`
- `FollowerIntentDispatched`
- `FollowerOrderObserved`
- `ExecutionObserved`
- `PositionObserved`
- `DivergenceRaised`
- `DivergenceResolved`
- `LatencySample`
- `ConfigActivated`
- `ConfigRejected`
- `CriticalError`
- `Heartbeat`

Companion → engine:

- `Hello`
- `RequestSnapshot`
- `ValidateConfig`
- `ActivateConfig`
- `PauseNewEntries`
- `ResumeNewEntries`
- `DisableGroup`
- `PrepareReconcile`
- `ExecuteReconcile`
- `PrepareFlatten`
- `ExecuteFlatten`
- `SetDiagnosticMode`

No generic `ExecuteOrder` message exists in V1.

## 23.8 Bounded queues

Native telemetry queue must be bounded.

Priority classes:

- P0 critical engine/divergence/config safety event — never intentionally dropped; maintain reserved capacity/fallback log.
- P1 order/execution mapping event — retain preferentially.
- P2 latency/verbose diagnostic sample — may be sampled/dropped under pressure.

Queue pressure must never block order execution indefinitely.

Record a `TelemetryGap` event when noncritical events are dropped.

---

# 24. Browser / Local HTTP Security Model

A localhost-bound web server is reachable by the local browser and can be targeted by malicious web pages. Treat it as a security-sensitive control surface.

## 24.1 Binding

Default:

- bind only `127.0.0.1`;
- do not bind `0.0.0.0`;
- do not expose LAN mode in V1;
- IPv6 `::1` may be enabled only after Host/origin tests cover it.

## 24.2 Port

Use a configurable port with a stable default and collision fallback.

Example default: `17841`.

If occupied:

- verify whether the process is already our healthy instance;
- otherwise select from a documented limited fallback range;
- persist the chosen port;
- native `Open Dashboard` action discovers the current endpoint from authenticated local state rather than assuming the default.

## 24.3 Host validation

Accept only expected loopback hosts/ports:

- `127.0.0.1:<port>`
- `localhost:<port>` only if explicitly supported and DNS resolution cannot be abused.

Reject arbitrary Host headers to mitigate DNS rebinding.

## 24.4 Origin validation

State-changing API requests require an exact local application Origin.

Reject `Origin: https://evil.example` even though destination is localhost.

## 24.5 Session token

On companion startup:

- generate cryptographically random high-entropy session token;
- make it available to the locally served SPA through a secure bootstrap mechanism;
- do not expose it in logs;
- rotate on process restart;
- use HttpOnly/SameSite cookie or equivalent design that minimizes script exposure.

## 24.6 CSRF

Use same-site cookies plus explicit anti-forgery token/header for state-changing actions.

GET endpoints are read-only.

## 24.7 CORS

Disable broad CORS. No `*` origin.

## 24.8 Content Security Policy

Ship a restrictive CSP:

- scripts/styles from self only;
- no remote fonts;
- no arbitrary frames;
- no mixed content;
- no eval unless tooling absolutely requires it in development only.

## 24.9 Command authorization

The browser can request only predefined high-level commands. It cannot specify arbitrary native order fields and have them forwarded to NinjaTrader.

This preserves the product boundary: browser manages copying; it is not a general-purpose local trading API.

## 24.10 Confirmation protocol for destructive commands

For flatten/reconcile actions that may submit orders:

1. `POST /prepare` returns a preview and a short-lived one-time `confirmationId` tied to exact action hash.
2. UI presents the preview.
3. `POST /execute` sends the confirmation ID.
4. Server verifies action hash/config version/expiry.
5. Engine independently rechecks preconditions.

Prevents stale UI from executing an operation against a changed account state.

## 24.11 Rate limits and body limits

Apply conservative local limits to:

- request body size;
- command rate;
- failed session validation;
- SSE/WebSocket connection count.

---

# 25. Local API Surface

All APIs are versioned under `/api/v1`.

The exact OpenAPI document is generated from code and committed/generated in CI.

## 25.1 System

```text
GET  /api/v1/system/status
GET  /api/v1/system/version
GET  /api/v1/system/health
GET  /api/v1/system/capabilities
GET  /api/v1/system/privacy
```

## 25.2 Accounts

```text
GET  /api/v1/accounts
GET  /api/v1/accounts/{accountKey}
GET  /api/v1/accounts/{accountKey}/positions
GET  /api/v1/accounts/{accountKey}/orders
```

## 25.3 Groups/configuration

```text
GET    /api/v1/groups
GET    /api/v1/groups/{groupId}
POST   /api/v1/groups/drafts
PUT    /api/v1/groups/drafts/{draftId}
POST   /api/v1/groups/drafts/{draftId}/validate
POST   /api/v1/groups/drafts/{draftId}/activate
DELETE /api/v1/groups/drafts/{draftId}
POST   /api/v1/groups/{groupId}/pause-new-entries
POST   /api/v1/groups/{groupId}/resume
POST   /api/v1/groups/{groupId}/disable
```

## 25.4 Live activity

```text
GET /api/v1/live/trades
GET /api/v1/live/orders
GET /api/v1/live/divergences
GET /api/v1/live/health
GET /api/v1/events/stream   # SSE preferred
```

## 25.5 Trade/journal

```text
GET /api/v1/journal/trades
GET /api/v1/journal/trades/{logicalTradeId}
GET /api/v1/journal/trades/{logicalTradeId}/timeline
PUT /api/v1/journal/trades/{logicalTradeId}/notes
PUT /api/v1/journal/trades/{logicalTradeId}/tags
GET /api/v1/journal/export
```

Notes/tags are local metadata and never affect execution.

## 25.6 Analytics

```text
GET /api/v1/analytics/overview
GET /api/v1/analytics/latency
GET /api/v1/analytics/reliability
GET /api/v1/analytics/fill-delta
GET /api/v1/analytics/accounts
```

## 25.7 Reconcile

```text
POST /api/v1/reconcile/prepare
POST /api/v1/reconcile/execute
GET  /api/v1/reconcile/{commandId}
```

## 25.8 Flatten

```text
POST /api/v1/flatten/prepare
POST /api/v1/flatten/execute
GET  /api/v1/flatten/{commandId}
```

## 25.9 Diagnostics

```text
GET  /api/v1/diagnostics/status
GET  /api/v1/diagnostics/errors
GET  /api/v1/diagnostics/performance
POST /api/v1/diagnostics/bundle
GET  /api/v1/diagnostics/bundle/{id}
```

## 25.10 No generic order-entry endpoint

There must be **no** V1 endpoint such as:

`POST /api/v1/orders` with arbitrary side/quantity/instrument.

That feature is outside the trade-copier product boundary and substantially expands attack surface.

---

# 26. Persistence Design

Separate durable configuration from high-volume journal/event data.

## 26.1 Databases

Preferred:

```text
%LOCALAPPDATA%\OpenTradeCopier\data\control.db
%LOCALAPPDATA%\OpenTradeCopier\data\journal.db
```

Do not store under the Git repository.

## 26.2 `control.db`

Contains:

- schema migrations;
- app settings;
- copy-group configurations;
- followers/rules;
- instrument mappings;
- config versions;
- config activation audit history;
- non-secret UI preferences;
- command audit metadata.

Use stronger durability settings appropriate to low-volume configuration changes.

## 26.3 `journal.db`

Contains:

- normalized engine events;
- logical trades;
- logical orders;
- follower links;
- executions;
- divergences;
- latency samples/aggregates;
- session summaries;
- user journal notes/tags.

Use WAL mode and asynchronous ingestion.

## 26.4 Suggested schema tables

### control.db

```text
schema_migrations
app_settings
config_versions
copy_groups
follower_rules
instrument_mappings
risk_policies
activation_history
command_audit
```

### journal.db

```text
schema_migrations
sessions
engine_events
logical_trades
logical_orders
follower_order_links
executions
position_snapshots
divergences
latency_samples
latency_rollups
journal_notes
journal_tags
journal_trade_tags
telemetry_gaps
```

## 26.5 Event retention

Default detailed-event retention: e.g. 90 days, configurable.

Long-term aggregated journal records may remain until user deletes them.

Never implement unbounded logs/database growth.

## 26.6 Backups

Provide:

- manual `Export/Backup` from dashboard;
- safe SQLite backup while service is running;
- schema version metadata;
- restore validation before replacing current data.

## 26.7 Migrations

- every schema change has forward migration;
- migrations tested from every supported previous stable version once stable releases exist;
- migration failure does not start web command surface in partially migrated state;
- backup before destructive migration.

---

# 27. Local Files and Paths

Use a single application root:

```text
%LOCALAPPDATA%\OpenTradeCopier\
  config\
  data\
  logs\
  diagnostics\
  cache\
  run\
```

Native engine may require a minimal bridge/bootstrap file under NinjaTrader's custom directory, but operational data stays outside `Documents\NinjaTrader 8` unless NinjaTrader packaging rules require otherwise.

Never write secrets/config into source-controlled directories.

---

# 28. Observability Model

Observability is a product feature, not merely developer logging.

## 28.1 Structured event envelope

Every significant event includes:

```text
EventId
EventCode
Severity
OccurredAtUtc
ObservedHighResTicks (when same-process timing is meaningful)
EngineSessionId
CopyGroupId?
LogicalTradeId?
LogicalOrderId?
LeaderAccountKey?
FollowerAccountKey?
InstrumentKey?
CorrelationId
Payload
```

## 28.2 Stable event codes

Examples:

```text
ENG-STARTED
ENG-STOPPED
ENG-QUEUE-PRESSURE
CFG-VALIDATED
CFG-ACTIVATED
CFG-REJECTED
LEAD-ORDER-OBSERVED
LEAD-EXECUTION-OBSERVED
COPY-DECISION
FOL-SUBMIT-START
FOL-SUBMIT-RETURN
FOL-ORDER-ACK
FOL-EXECUTION
FOL-REJECT
DIV-POSITION-MISMATCH
DIV-MISSING-PROTECTION
DIV-UNKNOWN-ORDER
REC-PREVIEW-CREATED
REC-EXECUTED
IPC-CONNECTED
IPC-DISCONNECTED
WEB-SECURITY-REJECT
JRN-WRITE-LAG
```

## 28.3 Severity

```text
TRACE
DEBUG
INFO
NOTICE
WARNING
ERROR
CRITICAL
```

User-facing severity names should be simpler:

- Healthy
- Info
- Warning
- Action Required
- Critical

## 28.4 Redaction

Logs/support bundles can hash or alias account names.

Default public-support bundle uses stable aliases:

```text
Account-01
Account-02
```

Allow user opt-in to include raw local account names when sharing privately.

---

# 29. Latency Measurement Design

Do not conflate local dispatch speed with broker/exchange fills.

## 29.1 Time sources

Within one process use `Stopwatch.GetTimestamp()`/high-resolution monotonic clock for durations.

Use UTC wall clock only for cross-process/event timeline display.

Do not subtract wall-clock timestamps to claim sub-millisecond performance.

## 29.2 Core measurements

For each follower action:

- `T0` leader event callback entered;
- `T1` semantic normalization complete;
- `T2` copy decision complete;
- `T3` follower native submit/change/cancel call invoked;
- `T4` native call returned;
- `T5` follower OrderUpdate acknowledgement observed;
- `T6` follower ExecutionUpdate observed if filled.

Metrics:

```text
DecisionLatency = T2 - T0
DispatchLatency = T3 - T0
NativeCallDuration = T4 - T3
AckLatency = T5 - T3
FillLatency = T6 - T3
FollowerDispatchSkew = max(T3 followers) - min(T3 followers)
```

## 29.3 What can be published

Publish only clearly labeled metrics:

- local decision/dispatch;
- NinjaTrader acknowledgement;
- execution/fill observations.

Never market fill latency as copier-only latency.

## 29.4 Benchmark scenarios

Synthetic/fake-adapter benchmark:

- 1 follower;
- 5 followers;
- 10 followers;
- 20 followers;
- market order creation;
- limit modify burst;
- cancel burst;
- partial-fill event burst;
- multiple independent groups.

NinjaTrader SIM benchmark is separate and requires manual/controlled environment.

## 29.5 Performance regression gate

Before a stable baseline exists, optimize for architecture: no blocking I/O, bounded allocations, low lock contention.

Once baseline exists:

- fail CI/perf gate on >20% statistically significant p95 regression in deterministic local benchmark unless an ADR explains the tradeoff;
- track allocation counts and queue pressure;
- record build SHA and machine profile in benchmark result.

## 29.6 Aspirational pre-beta budget

On a modern desktop under synthetic benchmark, target:

- decision + dispatch overhead in low single-digit milliseconds at p99 for 10 followers;
- follower dispatch skew low enough to avoid serial multi-millisecond delays;
- zero duplicate/missed execution intents;
- no unbounded allocations.

These are engineering targets, not public guarantees until measured on real NT8 environments.

---

# 30. Divergence Detection

Divergence is continuously evaluated from known leader intent, follower mappings, native orders, executions, and positions.

## 30.1 Divergence classes

```text
FollowerDisconnected
MissingFollowerOrder
UnexpectedFollowerOrder
FollowerRejected
PositionQuantityMismatch
PositionDirectionMismatch
FollowerFlatWhileLeaderExposed
UnexpectedFollowerExposure
MissingStop
MissingTarget
ProtectionQuantityMismatch
ProtectionPriceMismatch
OrphanMappedOrder
UnknownNativeOrderState
ConfigMismatch
RecoveryAmbiguity
```

## 30.2 Severity rules

Critical examples:

- copied exposure exists without expected stop protection;
- follower position direction opposite expected state;
- unknown submission outcome could represent duplicate exposure;
- engine loses ability to observe account while exposure is active.

Warning examples:

- fill price differs from leader;
- target differs by one tick due explicitly documented mapping/rounding;
- journal persistence temporarily behind.

## 30.3 Divergence lifecycle

```text
Raised -> Acknowledged? -> Resolving -> Resolved
                         -> Persisting
```

Do not delete divergence history after resolution.

---

# 31. Reconciliation Engine

Reconciliation is a planner before it is an executor.

## 31.1 Inputs

- active config version;
- current leader positions/orders;
- current follower positions/orders;
- durable/in-memory mappings;
- sizing/instrument rules;
- existing divergences.

## 31.2 Output

`ReconcilePlan`:

```text
PlanId
GeneratedAt
ConfigVersion
ObservedStateHash
Actions[]
Warnings[]
UnresolvableAmbiguities[]
RiskLevel
ExpiresAt
```

Each action is explicit:

- cancel order X;
- submit reduce order quantity Y;
- submit missing stop at Z;
- no action possible because identity is ambiguous.

## 31.3 Default V1 reconcile policy

Prefer **reducing risk** and returning followers to intended net exposure. Never increase exposure when state is ambiguous.

Any reconcile that increases exposure requires explicit confirmation and exact preview.

## 31.4 Stale-plan protection

Before executing:

- verify `ConfigVersion` unchanged;
- verify `ObservedStateHash` unchanged or within defined safe tolerance;
- otherwise reject and require a fresh plan.

---

# 32. Risk Controls

Risk controls are secondary safety guards; they must not pretend to replace broker/prop-firm risk systems.

## 32.1 V1 hard controls

Per follower/group:

- allowed instruments;
- maximum order quantity;
- maximum absolute position;
- entry enable/disable;
- simulation-only mode;
- dry-run mode;
- optional daily loss/profit guards only after account-value semantics are proven for supported providers.

## 32.2 Risk action hierarchy

When risk blocks a new entry, exits/protective actions must still be allowed where possible.

Never let an entry lockout strand a position by blocking its stop or exit.

## 32.3 P&L guards

If implemented:

- define session boundary/time zone;
- define realized vs unrealized basis;
- account for provider data availability;
- mark guard `UNAVAILABLE` when required P&L data cannot be trusted;
- fail closed for new entries if user configured guard as mandatory.

---

# 33. Dashboard Product Design

The browser experience is a major product differentiator. It should feel like a premium local fintech control plane, not a settings form.

## 33.1 Design principles

- operational truth first;
- beautiful but restrained;
- dense when useful, spacious when making decisions;
- near-zero decorative animation in critical trading state;
- statuses always include text/icon, never color alone;
- desktop-first, tablet-usable, phone-readable;
- light/dark architecture from beginning; dark may be default;
- keyboard navigation for high-frequency controls;
- no remote fonts/assets;
- never show a success toast for an action until engine acknowledgement exists.

## 33.2 Visual identity

Until final brand is chosen:

- neutral product name `OpenTradeCopier`;
- modern dark surface with restrained accent color configured as design token;
- system typography stack (`Segoe UI`, `Inter` when locally available, sans-serif fallback);
- rounded but not cartoonish cards;
- monospace only for IDs/timestamps/latency;
- status icons + labels;
- charts emphasize clarity over gradients/3D effects.

The UI must be an original product design and must not imitate any third-party product's trade dress.

## 33.3 Navigation

Primary left/compact navigation:

```text
Overview
Copy Groups
Live Trades
Journal
Analytics
Divergences
Diagnostics
Settings
About
```

A persistent top status strip shows:

```text
Engine ● | NT ● | Copying ENABLED/PAUSED | Groups 2 | Followers 8/8 | Critical 0
```

## 33.4 Overview page

Sections:

### Global status

- engine status;
- NinjaTrader session;
- companion status;
- copying state;
- active config version;
- uptime;
- last event.

### Copy group cards

Each card:

- group name;
- leader alias;
- follower count / ready count;
- active position summary;
- health;
- p50/p95 local dispatch latency;
- enabled/paused state;
- immediate warning count.

### Active trades

Compact grid:

```text
Instrument | Leader | Side | Qty | Followers OK/Total | Protection | Age | P/L*
```

P/L shown only if data is reliable.

### Critical alerts

Critical divergences are never buried below charts.

### Session operational summary

- copy attempts;
- successful mappings;
- rejects;
- divergences;
- latency sample count.

## 33.5 Copy Groups page

Support:

- group list;
- create group;
- clone group;
- edit draft;
- activate;
- archive/delete only when safe;
- pause/resume;
- follower enable/disable;
- quick status matrix.

### Group editor sections

1. Identity/name.
2. Leader.
3. Followers.
4. Copy mode.
5. Sizing.
6. Instruments/mappings.
7. Order types.
8. Risk guards.
9. Connection policy.
10. Advanced behavior.
11. Validation summary.
12. Change preview.

Use clear explanations rather than exposing raw enum/property names.

## 33.6 Follower matrix

Recommended layout:

```text
Follower      Ready  Size       Instrument    Position     Protection   Sync
SIM-FOL-01      ●    1.0x       Same          +1 NQ        Protected      ✓
SIM-FOL-02      ●    2.0x       Same          +2 NQ        Protected      ✓
SIM-FOL-03      ●    10x mapped NQ→MNQ       +10 MNQ       Protected      ✓
```

Rows expand for:

- working orders;
- fill differences;
- risk limits;
- last action;
- latency;
- divergence details.

## 33.7 Live Trades page

Focus on current logical trades.

Trade detail drawer/page:

- leader summary;
- each follower side-by-side;
- actual vs target quantity;
- entry fills;
- working stop/target;
- protection status;
- fill delta;
- current divergence state;
- event timeline.

## 33.8 Event timeline

A premium diagnostic feature.

Example:

```text
10:32:04.182341  Leader order observed       LEAD-ORDER-OBSERVED
10:32:04.182912  Copy decision completed     4 followers eligible
10:32:04.183027  Follower 1 submit invoked   +0.686 ms
10:32:04.183155  Follower 2 submit invoked   +0.814 ms
10:32:04.184220  Follower 1 working           ack +1.193 ms
10:32:04.190511  Follower 1 execution         1 @ 24821.25
```

Support filters:

- all;
- leader;
- specific follower;
- errors only;
- execution only;
- protection only.

## 33.9 Divergences page

Each divergence card/table row shows:

- severity;
- group;
- follower;
- instrument;
- expected state;
- actual state;
- detected time;
- why it matters;
- current safe options.

Actions:

- inspect;
- acknowledge;
- prepare reconcile;
- ignore only if explicitly safe and documented.

## 33.10 Diagnostics page

Show:

- versions;
- component uptime;
- engine session ID;
- IPC state;
- queue utilization;
- database health;
- event persistence lag;
- current memory/CPU estimate;
- current NinjaTrader version;
- supported/tested status;
- recent error codes;
- security posture (`Loopback only`, `Telemetry off`);
- create redacted support bundle.

## 33.11 Settings page

Sections:

- General;
- Startup;
- Journal retention;
- Privacy;
- Diagnostics logging;
- Backup/restore;
- Appearance;
- Advanced.

Dangerous debug controls require an “Advanced” disclosure and never default on.

## 33.12 About page

Clearly state:

- open source;
- license;
- build/version/commit;
- GitHub link;
- no telemetry;
- local storage location;
- trading-risk disclaimer;
- supported NinjaTrader versions.

---

# 34. Demo Mode and README Screenshots

A deterministic synthetic demo mode is required for development, UI tests, documentation, and screenshots.

## 34.1 Demo data

Use fake accounts only:

```text
SIM-LEADER-01
SIM-FOLLOWER-01
SIM-FOLLOWER-02
SIM-FOLLOWER-03
```

Fake instruments/prices may resemble real futures symbols but contain no user activity.

## 34.2 Demo scenarios

- all healthy;
- partial fill;
- one rejected follower;
- missing stop critical divergence;
- disconnected follower;
- journal history;
- latency distribution.

## 34.3 Screenshot automation

If feasible during unattended build:

1. launch control plane in demo mode;
2. launch SPA;
3. Playwright opens fixed viewport(s);
4. capture Overview, Group, Live Trade, Journal screenshots;
5. store under `docs/images/`;
6. reference best 1–3 in README.

Before commit, automated check/manual script verifies screenshot fixture names do not contain known real-account patterns.

Screenshot generation is a nice-to-have; lack of it must not block core execution work.

---

# 35. Journal Design

The journal is derived from copier-observed truth.

## 35.1 Journal trade record

Display:

- logical trade ID;
- group;
- instrument;
- direction;
- start/end time;
- leader executions;
- follower executions;
- total follower count;
- fill differences;
- protection lifecycle;
- rejects/divergences;
- copier latency stats;
- realized P/L only when source data is reliable;
- user notes/tags.

## 35.2 Search/filter

- date range;
- group;
- instrument/root;
- leader/follower alias;
- healthy/divergent;
- rejected;
- copy mode;
- tag.

## 35.3 Journal truth rules

- never fabricate missing executions;
- label incomplete history;
- distinguish “copier observed” from broker statement/accounting truth;
- no tax claims;
- no performance metrics from data that cannot be confidently correlated.

## 35.4 Export

CSV and JSON export initially.

Export includes schema version and clear timestamp timezone/UTC fields.

---

# 36. Analytics Design

Prioritize **copier quality analytics** over trading-strategy analytics.

## 36.1 Reliability dashboard

Metrics:

- follower actions attempted;
- success/acknowledgement ratio;
- reject count/rate;
- divergence count/rate;
- protection failures;
- connection interruptions;
- reconcile count;
- telemetry gaps.

## 36.2 Latency dashboard

- p50/p95/p99/max local decision latency;
- p50/p95/p99/max dispatch latency;
- dispatch skew by follower count;
- ack latency;
- fill observation delta;
- time series;
- histogram/distribution;
- sample count.

Filters:

- group;
- follower;
- instrument;
- action type;
- date/session.

## 36.3 Fill-delta analytics

For comparable executions:

- follower price minus leader price in ticks;
- direction-normalized slippage;
- average/median/p95;
- do not imply that difference was caused only by copier latency.

## 36.4 Financial metrics

Deferred until execution/account data semantics are validated. If implemented, label clearly as convenience analytics.

---

# 37. Native AddOn Lifecycle

## 37.1 Initialization

On NinjaTrader AddOn load:

1. create engine session ID;
2. initialize internal dispatcher;
3. enumerate accounts under appropriate NinjaTrader locking semantics;
4. subscribe to account status/global events;
5. subscribe to per-account order/execution/position events;
6. start named-pipe server on background thread/task;
7. set engine state `DISABLED`;
8. expose Tools menu / minimal status window;
9. emit startup status.

## 37.2 Shutdown

On termination:

1. transition engine to shutting down;
2. stop accepting new config/commands;
3. preserve explicit shutdown audit event when companion connected;
4. unsubscribe every event handler deterministically;
5. stop pipe listener;
6. dispose bounded queues/timers;
7. never submit surprise flatten orders merely because NinjaTrader is exiting.

## 37.3 Subscription hygiene

Event subscriptions/unsubscriptions must be centralized and tested. Duplicate subscription can duplicate orders and is therefore critical severity.

Create an internal `SubscriptionRegistry` and assertions preventing multiple active subscription sets per account/session.

---

# 38. Threading and Concurrency Design

NinjaTrader is multi-threaded. Assume callbacks may arrive on threads not owned by our UI and do not share a simplistic global lock.

## 38.1 Rules

- no UI work from account event callback;
- no blocking on companion/database;
- avoid holding locks while invoking NinjaTrader order methods;
- do not hold one global lock across all groups;
- use narrow per-logical-order/per-group synchronization where needed;
- immutable active config snapshot eliminates config mutation locks in hot path;
- shared registries use thread-safe collections or tightly controlled synchronization.

## 38.2 Processing model

Preferred:

1. callback captures high-resolution timestamp;
2. normalize minimal native event to immutable domain event;
3. route to a deterministic domain coordinator;
4. compute execution intents;
5. dispatch follower intents promptly;
6. enqueue telemetry asynchronously.

Do not introduce an async worker hop before every follower submission unless benchmark/reentrancy constraints require it; unnecessary queue hops can increase latency. Prove callback/thread-safety behavior with NinjaTrader docs and tests.

## 38.3 Per-follower parallelism

Do not blindly spawn one thread/task per follower event.

Benchmark:

- sequential native calls;
- small bounded worker per follower/account;
- parallel dispatch where NinjaTrader API/thread rules permit.

Choose the safest lowest-skew design via ADR and measured evidence.

---

# 39. Memory and Backpressure

## 39.1 Bounded state

- active mappings remain while trade/order active plus short terminal grace period;
- journal persistence carries long-term history;
- telemetry ring buffer bounded;
- event queue bounded;
- diagnostic recent-events buffer bounded;
- UI event stream clients have bounded send buffers.

## 39.2 Slow dashboard client

A slow SSE/WebSocket browser must not backpressure the journal ingestor or native engine.

Drop/compact noncritical live UI updates and force client snapshot refresh if it falls too far behind.

## 39.3 Resource targets

Initial goals on idle modern Windows PC:

- native add-on incremental memory modest (<50 MB target; measure, don't guarantee);
- companion idle memory reasonable (<150 MB target self-contained runtime may vary);
- idle CPU near 0%;
- no high-frequency polling where event model exists.

---

# 40. Test Architecture — Quality Is a Product Feature

No phase is complete because it “works once.”

## 40.1 Testing pyramid

```text
             Manual NT8 SIM certification
                End-to-end UI tests
            Integration / contract tests
          State-machine / property tests
               Unit tests
```

Manual SIM is required eventually, but automation should prove as much behavior as possible before the user opens a market session.

## 40.2 Fake NinjaTrader adapter

Build a high-fidelity test adapter implementing our internal native interface without referencing proprietary NinjaTrader assemblies.

Capabilities:

- create fake accounts;
- inject order updates;
- inject executions;
- inject positions;
- simulate asynchronous acknowledgements;
- partial fills;
- rejects;
- disconnects;
- reorder/duplicate events;
- delayed acknowledgements;
- ambiguous submission outcomes;
- OCO sibling cancellation;
- deterministic virtual time where useful.

This adapter is central to unattended development.

## 40.3 Event replay tool

Persist synthetic event streams in JSON fixtures and replay them through domain engine.

Every production defect that can be represented by events should gain a regression fixture before the fix is merged.

## 40.4 Unit coverage targets

These are minimum gates, not objectives to game.

### Shared domain/state machine

- line coverage >= 95%;
- branch coverage >= 90%;
- 100% defined transition-matrix scenario coverage;
- all safety invariants directly tested.

### Native adapter logic excluding direct NinjaTrader SDK wrappers

- line >= 90%;
- branch >= 85%.

### Control plane

- line >= 90%;
- branch >= 85%.

### Web

- statements/lines >= 85%;
- branches >= 80%;
- all destructive-command flows have component + Playwright coverage.

Coverage exclusions must be explicit and justified; do not exclude hard code simply to raise percentage.

## 40.5 Mutation testing

Run mutation testing on the core sizing/state-machine/idempotency projects periodically (not necessarily every fast CI commit).

Target strong mutation score; surviving mutants in safety logic must be reviewed.

## 40.6 Property/randomized tests

Generate combinations for:

- leader/follower quantities 1–100;
- multipliers/fixed sizing;
- scale-out sequences;
- event duplication;
- event ordering variations;
- multiple partial fills;
- cancel/change races;
- topology graphs.

Properties include:

- reducing action never reverses follower;
- dedupe never emits two equivalent submit intents;
- cyclic topology never validates;
- active exposure never exceeds configured hard cap due solely to sizing calculation;
- terminal logical order never returns to active without explicit new semantic identity.

---

# 41. Required Scenario Test Matrix

Every scenario records expected domain state, follower intents, divergence state, and telemetry.

## 41.1 Basic entries

- market buy;
- market sell;
- limit buy working then fill;
- limit sell working then fill;
- stop-market buy/sell;
- stop-limit buy/sell;
- MIT if supported/implemented;
- unsupported order type visible rejection.

## 41.2 Changes

- modify limit price once;
- rapid multiple price changes;
- modify stop price;
- quantity increase;
- quantity decrease;
- change during partial fill;
- change after terminal state rejected safely.

## 41.3 Cancel

- cancel unfilled;
- cancel after partial fill;
- follower cancel acknowledgement delayed;
- follower cancel rejected/too-late because fill occurred.

## 41.4 Fills

- full fill leader before follower;
- follower before leader when pending order mirrors;
- leader partial fills 1+1+rest;
- follower partial fills with different partitioning;
- multiple executions same price/time;
- duplicate execution event;
- execution event near terminal order update.

## 41.5 Scale

- scale in separate leader order;
- scale out 1 of N;
- multiple scale-outs;
- rounding with mapped micros;
- fixed follower sizing then proportional reduction.

## 41.6 Brackets/OCO

- entry then stop+target appear;
- follower fills after protection observed;
- stop modified;
- target modified;
- target fills and stop cancels;
- stop fills and target cancels;
- partial target fill;
- bracket quantity changes;
- missing follower stop;
- follower manually cancels protection;
- OCO IDs unique by follower.

## 41.7 Reversals

- leader long -> flat -> short through separate lifecycle;
- native reverse action sequence;
- follower partially filled during reversal;
- ensure no transient accidental double exposure caused by duplicate logic.

## 41.8 Connections

- follower disconnected before leader entry;
- disconnect after working order;
- disconnect while position open;
- reconnect with state matching;
- reconnect with missing order;
- leader disconnect;
- companion disconnect;
- browser disconnect.

## 41.9 Configuration

- invalid same leader/follower;
- cycle;
- duplicate follower;
- mapping target unavailable;
- edit safe field during trade;
- edit route during active trade blocked;
- stale config activation rejected;
- companion sends lower config version rejected.

## 41.10 Risk

- max quantity block;
- projected position cap block;
- risk lock does not block exit;
- unknown P&L guard data blocks entry when mandatory;
- dry-run generates telemetry but no native intent.

## 41.11 Restart/recovery

- clean companion restart;
- telemetry buffer resync;
- native engine session restart with no active state;
- NT restart with active position and reconstructable mapping;
- NT restart with ambiguous mapping -> review required;
- stale journal database vs engine snapshot.

## 41.12 Security/API

- foreign Origin rejected;
- Host header rebinding rejected;
- no CSRF token rejected;
- expired destructive confirmation rejected;
- stale observed-state hash rejected;
- oversized request rejected;
- malformed JSON rejected;
- unknown protocol version rejected;
- unauthorized pipe client denied by ACL where testable.

---

# 42. Integration Tests

## 42.1 IPC integration

Run real named-pipe server/client processes in tests where Windows CI permits.

Verify:

- handshake;
- version negotiation;
- reconnect;
- message limits;
- snapshot resync;
- companion restart;
- queue pressure behavior;
- malformed message rejection.

## 42.2 Persistence integration

Run SQLite against temporary real files.

Verify:

- migration from empty DB;
- CRUD/config versioning;
- journal ingestion;
- transaction rollback;
- WAL behavior;
- backup/restore;
- corrupt/locked DB handling;
- retention pruning;
- crash-safe config activation audit.

## 42.3 HTTP integration

Use ASP.NET test server plus real Kestrel loopback smoke tests.

Verify:

- route contracts;
- validation;
- antiforgery;
- Host/Origin rules;
- confirmation workflows;
- SSE reconnect/snapshot behavior;
- OpenAPI consistency.

## 42.4 Architecture tests

Automated dependency rules:

- Domain may not reference NinjaTrader.
- Domain may not reference ControlPlane/Web/Persistence.
- Native may reference Domain/Contracts/Protocol but not ASP.NET/SQLite/Web.
- Web never references NinjaTrader binaries.
- Persistence never contains order-decision logic.
- No project in execution path references telemetry/network client packages.

---

# 43. Web UI Testing

## 43.1 Component tests

Test:

- status indicators;
- group editor validation;
- follower matrix;
- destructive confirmation modal;
- divergence presentation;
- event timeline filtering;
- journal filtering;
- loading/empty/error states;
- accessibility roles/labels.

## 43.2 Playwright E2E

Required synthetic flows:

1. first-run demo dashboard;
2. create group draft;
3. validation error;
4. activate valid group against fake engine;
5. simulated leader order appears in live trades;
6. follower reject surfaces prominently;
7. divergence drilldown;
8. prepare/cancel reconcile;
9. prepare/confirm synthetic flatten command against fake engine only;
10. journal detail/timeline;
11. dark/light appearance if both implemented;
12. browser refresh restores state from server snapshot.

## 43.3 Visual regression

Use targeted screenshot snapshots for major pages if stable enough. Avoid brittle pixel perfection across OS font rendering; focus on key fixed demo environments.

---

# 44. Performance / Soak / Fault Injection

## 44.1 Synthetic throughput

Benchmark event bursts substantially above normal retail trading volume to find race conditions.

Examples:

- 100,000 normalized order events;
- 20 followers;
- repeated modify bursts;
- multiple groups;
- telemetry persistence slowed intentionally.

## 44.2 Soak test

Run a multi-hour synthetic soak when environment permits.

Assertions:

- no duplicate intent;
- no memory growth trend beyond bounded caches;
- no unbounded database growth outside configured retention;
- no deadlock;
- queue remains bounded;
- telemetry gap is explicit if induced.

## 44.3 Fault injection

Inject:

- exception during telemetry serialization;
- SQLite busy/locked;
- disk write failure;
- pipe disconnect mid-message;
- malformed companion message;
- native adapter rejection;
- delayed order acknowledgement;
- duplicate callback;
- event reordering;
- process shutdown during config activation;
- browser stream disconnect storm.

The execution engine must continue safe native operation when noncritical supporting components fail.

---

# 45. Manual NinjaTrader SIM Certification Matrix

Automated development may prepare this suite, but a human/operator eventually performs it using simulation/demo accounts. Until this matrix is completed, release status remains **UNVERIFIED FOR LIVE TRADING**.

Document each result with:

- date/time;
- NT version;
- connection/provider or simulator/playback;
- product commit SHA/version;
- scenario;
- expected result;
- actual result;
- logs/diagnostics ID;
- pass/fail.

Required initial manual cases:

1. 1 leader + 1 follower market entry/exit.
2. 1 leader + 5 followers market entry/exit.
3. limit order mirrors before fill.
4. limit modify.
5. cancel before fill.
6. stop-market entry if supported.
7. partial fill if reproducible/playback.
8. ATM stop/target after entry.
9. move stop.
10. move target.
11. target fill cancels stop follower-side.
12. stop fill cancels target follower-side.
13. partial close.
14. scale in.
15. reversal.
16. follower reject (use safe SIM constraint if feasible).
17. follower disconnect/reconnect.
18. pause new entries while existing stop/exit continues.
19. dashboard closed while copying.
20. companion killed/restarted while copying.
21. NinjaTrader restart recovery review.
22. multiple independent groups.
23. mini → micro mapping.
24. latency probe capture.
25. diagnostics bundle redaction.

Do not mark stable/live-ready until all critical cases pass and known limitations are documented.

---

# 46. Security Engineering Plan

## 46.1 Threat model assets

Protect:

- ability to change copy configuration;
- ability to flatten/reconcile accounts;
- account/order data privacy;
- integrity of active config;
- integrity of IPC commands;
- release/update integrity;
- user local files.

## 46.2 Threat actors

- malicious website opened in user browser;
- other local process running as different Windows user;
- compromised dependency;
- malicious imported configuration;
- accidental user action;
- corrupted/stale local database;
- untrusted diagnostic bundle recipient;
- malicious pull request/dependency supply chain.

## 46.3 Mandatory controls

- loopback bind;
- Host validation;
- Origin validation;
- anti-CSRF;
- ephemeral local session token;
- current-user named-pipe ACL;
- strict DTO validation;
- body/message limits;
- command allowlist, no arbitrary order API;
- one-time destructive confirmations;
- config version/state hash checks;
- local-only assets;
- dependency pinning;
- secret scanning;
- CodeQL/static analysis;
- signed/checksummed release artifacts when feasible;
- no automatic code execution from imported profiles;
- safe ZIP path handling in diagnostics/import/export.

## 46.4 Dependency security

CI:

- Dependabot weekly updates;
- GitHub dependency review on PR;
- CodeQL C#/JavaScript/TypeScript;
- `dotnet list package --vulnerable` or current supported equivalent;
- `pnpm audit` used as signal, with reviewed exceptions;
- license inventory;
- gitleaks or equivalent secret scan over repository history/current tree.

## 46.5 Security documentation

`SECURITY.md` includes:

- supported versions;
- private vulnerability-reporting channel if available;
- response expectations without unrealistic SLA promises;
- no public posting of real account data;
- how to generate redacted diagnostic bundles.

## 46.6 No custom updater in initial build

A self-updater expands supply-chain attack surface. Initial release should use GitHub Releases/manual installer update. Future updater requires signed artifact verification and separate threat review.

---

# 47. Code Quality Rules

## 47.1 C# rules

- nullable analysis enabled where target supports it;
- warnings as errors for owned code, with narrowly documented exceptions for NinjaTrader interop;
- analyzers enabled;
- no `async void` except UI/event signatures requiring it and wrapped safely;
- `ConfigureAwait` strategy appropriate to framework/library context;
- cancellation tokens for background operations;
- no fire-and-forget tasks without supervised error handling;
- immutable records/value objects where compatible;
- no static mutable global domain state;
- explicit clock/random abstractions in deterministic tests;
- culture-invariant serialization/numeric handling;
- all decimal/price rounding explicit using instrument tick rules.

## 47.2 TypeScript rules

- `strict: true`;
- no implicit `any`;
- API client generated or strongly typed from contract where practical;
- exhaustive discriminated unions for status states;
- errors handled; no ignored promise rejections;
- state management kept simple; prefer server state + focused local state;
- no secret/config embedded in frontend bundle;
- accessible controls.

## 47.3 Functions/classes

Prefer small cohesive modules. A 7,000-line single copier file is explicitly **not** an acceptable target architecture.

## 47.4 Comments

Document:

- why a safety rule exists;
- NinjaTrader/provider lifecycle nuance;
- concurrency invariants;
- non-obvious performance decisions.

Do not clutter with comments that restate obvious code.

## 47.5 TODO policy

No anonymous `TODO` in production paths.

Use:

`TODO(#issue): reason` where deferred intentionally.

Critical safety TODOs block release.

---

# 48. API / Protocol Versioning

## 48.1 Semantic versioning

Product version: SemVer.

Examples:

- `0.1.0-dev`
- `0.2.0-alpha.1`
- `0.5.0-beta.1`
- `1.0.0`

## 48.2 IPC protocol version

Separate integer major/minor compatibility from product version.

Breaking protocol change requires major bump and clear mismatch behavior.

## 48.3 Database schema version

Independent monotonic migration sequence.

## 48.4 Config export schema

Include:

```json
{
  "schemaVersion": 1,
  "product": "OpenTradeCopier",
  "exportedAtUtc": "...",
  "groups": []
}
```

No executable code, secrets, or absolute local paths by default.

---

# 49. CI/CD Design

Use GitHub Actions.

## 49.1 Fast CI on every push/PR

Jobs:

### Shared/.NET

- restore locked dependencies;
- build Domain/Contracts/Protocol;
- unit tests;
- coverage threshold;
- formatting/analyzers.

### Control plane

- build .NET 10;
- unit/integration tests;
- persistence tests;
- API contract tests.

### Web

- pnpm frozen install;
- typecheck;
- lint;
- unit/component tests;
- build.

### Architecture

- dependency architecture tests;
- prohibited file scan;
- secret scan.

## 49.2 Windows-specific CI

Use Windows runner for:

- PowerShell scripts;
- named-pipe integration;
- .NET Framework-compatible shared project build where possible;
- installer packaging smoke test.

## 49.3 NinjaTrader native compilation constraint

Do **not** commit or redistribute NinjaTrader proprietary assemblies merely to make public CI compile the adapter.

Use two verification layers:

1. Public CI builds/tests the domain and adapter abstractions without proprietary binaries.
2. `scripts/verify-ninjatrader.ps1` on a Windows machine with NinjaTrader installed locates the local assemblies and compiles/verifies the real native adapter.

The Build Agent should run local NT compilation if NinjaTrader is installed on its machine. Record exact version and result.

## 49.4 Nightly/deep workflow

May run:

- mutation tests;
- extended fault injection;
- long synthetic benchmark;
- dependency/security scans;
- Playwright screenshots;
- packaging.

## 49.5 Release workflow

On version tag:

- verify clean tag on `main`;
- rerun full automated suite;
- build web static bundle;
- build control-plane self-contained package;
- build/package native component on authorized Windows environment where NT references are locally available;
- generate checksums;
- generate SBOM where practical;
- attach release notes;
- mark prerelease until manual certification gate is satisfied.

No automatic publishing to external package stores in V1.

---

# 50. NinjaTrader Packaging / Installation

NinjaTrader documents source/compiled NinjaScript export and Visual Studio AddOn development. Our packaging must comply with official supported mechanisms and must not redistribute NinjaTrader libraries.

## 50.1 Development install

`scripts/install-local.ps1`:

- discovers `%USERPROFILE%\Documents\NinjaTrader 8` or configurable user data dir;
- discovers NinjaTrader binary path;
- copies/builds only our native artifact/source as appropriate;
- installs companion into a local development directory;
- starts companion optionally;
- never edits unrelated NinjaTrader files;
- supports `-WhatIf`.

## 50.2 End-user release target

Preferred eventual installer:

- installs companion self-contained binaries under `%LOCALAPPDATA%\Programs\OpenTradeCopier` or appropriate per-user path;
- installs native AddOn using a documented NinjaTrader-compatible package/import process or carefully documented DLL method;
- creates Start Menu shortcut to dashboard/control plane if useful;
- configures per-user startup of companion;
- does not require Administrator if avoidable;
- clean uninstall leaves user journal/data only by explicit choice.

## 50.3 Startup

Companion should start automatically with user session **or** be launched by native AddOn if safely implemented. Choose one with ADR after testing.

Preferred production behavior:

- per-user background companion starts with login;
- native engine connects whenever NinjaTrader starts;
- dashboard opens on demand.

## 50.4 Native AddOn update

Because loaded DLLs may require NinjaTrader restart, update flow must say so explicitly.

Do not silently replace loaded native binaries.

---

# 51. Open-Source Governance

## 51.1 License

Default Apache-2.0 because it is permissive and includes an explicit patent grant. If existing design inputs require MIT for simpler alignment, switching before first external contribution is acceptable; document ADR.

## 51.2 Third-party code

Every nontrivial copied/adapted code segment:

- license checked;
- origin recorded;
- copyright notice preserved when required;
- `THIRD_PARTY_NOTICES.md` updated.

Do not ingest code from proprietary copiers.

## 51.3 Contributions

`CONTRIBUTING.md` defines:

- dev prerequisites;
- no real account data in issues/tests;
- tests required;
- architecture boundaries;
- sign-off/DCO only if chosen intentionally;
- security issues reported privately.

## 51.4 Issue labels

Suggested:

- `critical-safety`
- `execution-engine`
- `ninjatrader-adapter`
- `dashboard`
- `journal`
- `analytics`
- `security`
- `performance`
- `documentation`
- `good-first-issue`
- `needs-sim-repro`

---

# 52. Documentation Deliverables

The Build Agent must write docs as code is implemented, not defer them all to the end.

Required:

```text
docs/product/PRD.md
docs/architecture/system-overview.md
docs/architecture/execution-engine.md
docs/architecture/order-state-machine.md
docs/architecture/brackets-oco.md
docs/architecture/reconciliation.md
docs/architecture/ipc-control-plane.md
docs/architecture/data-model.md
docs/architecture/dashboard.md
docs/security/threat-model.md
docs/security/localhost-security.md
docs/testing/test-strategy.md
docs/testing/scenario-matrix.md
docs/testing/manual-sim-certification.md
docs/testing/performance-benchmark.md
docs/operations/installation.md
docs/operations/first-run.md
docs/operations/recovery.md
docs/operations/troubleshooting.md
docs/operations/backup-restore.md
docs/development/setup.md
docs/development/ninjatrader-local-build.md
docs/development/release-process.md
```

Include Mermaid diagrams where GitHub renders them effectively.

---

# 53. README Requirements

README is a product landing page and technical orientation.

Minimum structure:

1. Product name/logo text treatment.
2. One-sentence value proposition.
3. Strong warning: simulation first / real orders possible.
4. Feature bullets.
5. Screenshot(s) using synthetic demo data if available.
6. Architecture diagram.
7. Why local/open source.
8. Current development status badge (`Alpha`, `Not live certified`).
9. Install instructions or link.
10. Quick-start demo/SIM flow.
11. Privacy statement.
12. Latency measurement explanation.
13. Supported versions.
14. Roadmap.
15. Contributing/security.
16. License.

Never claim “zero latency,” “guaranteed identical fills,” or “safe for all prop firms.”

---

# 54. Release Status Language

Use precise status:

## `Development`

Code under construction.

## `Alpha — SIM only recommended`

Core functionality exists; not manually certified sufficiently.

## `Beta — manual SIM certified`

Critical scenario matrix passes on documented environments; limitations remain.

## `Stable`

Requires:

- complete automated gates;
- manual SIM certification on supported NT versions/providers;
- no unresolved critical/high safety defects;
- recovery docs;
- security review;
- benchmark report;
- installer/upgrade validation.

The autonomous overnight build may reach **code-complete Alpha/Release Candidate**, but must not label itself `Stable` merely because all unit tests pass.

---

# 55. Full SDLC Roadmap

The Build Agent executes phases in order. Each phase has entry criteria, implementation tasks, automated gates, artifacts, and a mandatory checkpoint commit/push.

The agent may overlap independent work through subagents but must preserve dependency order and phase acceptance criteria.

---

## Phase 0 — Repository, Governance, and Agent Continuity

### Objective

Create a professional public repository that is safe to work in autonomously and easy to resume.

### Tasks

- create public GitHub repo;
- initialize `main`;
- add Apache-2.0 license;
- add `.gitignore`, `.editorconfig`, `.gitattributes`;
- add README skeleton;
- add security/contribution/code-of-conduct docs;
- add PRD and full design docs;
- create `AGENTS.md`;
- create `docs/agent/STATE.md`;
- create `docs/agent/NEXT.md`;
- create `docs/agent/TASKS.md`;
- create `docs/agent/DECISIONS.md`;
- create `docs/agent/BLOCKERS.md`;
- create `docs/reports/`;
- add initial GitHub Actions skeleton;
- enable Dependabot/CodeQL config;
- add prohibited-secret/account-data patterns script;
- create `scripts/resume.ps1`.

### Acceptance

- repository public;
- `main` exists remotely;
- clean working tree;
- initial CI workflow syntax valid;
- no secret findings;
- state file identifies Phase 1 as next.

### Commit examples

- `chore: bootstrap repository governance and docs`
- `ci: add baseline quality and security workflows`

---

## Phase 1 — Architecture Spikes and API Verification

### Objective

Prove assumptions against installed NinjaTrader and official APIs before writing large execution logic.

### Tasks

- locate NinjaTrader installation and user data directory;
- record detected NT version without committing sensitive user paths;
- create Visual Studio native AddOn project targeting .NET Framework 4.8;
- verify references to local NinjaTrader assemblies;
- build minimal AddOn that loads/unloads;
- subscribe/unsubscribe to account status/order/execution/position events;
- enumerate SIM accounts in a non-trading diagnostics mode;
- verify minimal Tools menu/status window;
- create a no-order-submit adapter facade;
- validate native event callback threading assumptions through docs/controlled logs;
- investigate CreateOrder/Submit/Change/Cancel method requirements;
- do **not** submit live orders;
- write ADRs:
  - event semantics;
  - shared target framework;
  - IPC ownership;
  - native component packaging approach;
  - control-plane runtime.

### Acceptance

- native project compiles locally against installed NT if available;
- AddOn load/unload lifecycle is structurally correct;
- all event subscriptions have deterministic unsubscription;
- no order submission occurs;
- official API references documented;
- no proprietary NT binaries committed.

### If NinjaTrader is unavailable

Do not block the whole build. Mark local-native compile as `BLOCKED_ENVIRONMENT`, build adapter abstraction/fake environment, and continue Phases 2+ that do not require local proprietary assemblies.

---

## Phase 2 — Shared Domain, State Machines, and Fake Engine

### Objective

Build the deterministic copier brain independently of NinjaTrader.

### Tasks

- create domain/contracts/protocol projects;
- strongly typed identifiers;
- normalized account/instrument/order/execution events;
- copy group configuration;
- sizing policies;
- topology validation;
- semantic fingerprints;
- logical order state machine;
- follower link state;
- logical trade model;
- execution intent model;
- divergence model;
- fake NinjaTrader adapter;
- deterministic clock/test helpers;
- synthetic fixtures.

### Tests

- state transition matrix;
- sizing/rounding property tests;
- topology/cycle tests;
- duplicate-event tests;
- reduce-never-reverses tests;
- config version tests;
- invariant tests.

### Acceptance

- domain line coverage >=95%; branch >=90%;
- all documented invariants tested;
- no NinjaTrader references in domain;
- architecture tests enforce dependency boundaries;
- clean/pushed.

---

## Phase 3 — Minimal Native SIM Copier: Market Orders

### Objective

Connect real NinjaTrader leader observations to safe follower market-order intents, initially SIM only.

### Tasks

- implement account registry adapter;
- map native events to normalized domain events;
- implement origin registry/loop prevention;
- implement follower order factory/executor for market orders;
- configuration injection via hard-coded test snapshot only inside dev harness or companion stub (never production hard-coded accounts);
- simulation-account guard;
- copy enabled state default false;
- structured telemetry queue;
- latency T0-T4 capture;
- native critical error isolation.

### Safety

Automated smoke tests may only invoke native submit when target account is positively recognized as simulation and the agent environment explicitly runs the SIM test harness. If positive simulation detection is uncertain, skip native submission and leave manual certification step.

### Acceptance

- fake-adapter tests pass;
- local native compile passes if available;
- no recursive copying;
- one semantic leader event produces at most one follower submit intent;
- engine remains disabled by default;
- manual SIM test instructions documented.

---

## Phase 4 — Full Order Mirror Lifecycle

### Objective

Support working orders and complete standard lifecycle behavior.

### Tasks

- limit orders;
- stop-market;
- stop-limit;
- MIT only if official API/provider behavior supports it;
- price changes;
- quantity changes;
- cancel;
- rejection handling;
- partial fills;
- terminal mapping cleanup;
- multiple followers;
- multiple groups in domain and native orchestration;
- follower-specific sizing;
- instrument filters;
- account connection gates;
- risk caps.

### Tests

Full scenarios in Sections 41.1–41.7.

### Acceptance

- no known duplicate submission path;
- all standard order lifecycle fixtures pass;
- rejected follower becomes visible divergence;
- disconnect blocks entries per default policy;
- latency capture works for each action class.

---

## Phase 5 — Brackets, OCO, ATM-Generated Order Mirroring

### Objective

Correctly manage protective orders and follower-specific OCO relationships.

### Tasks

- identify leader OCO/protection relationships using structured native data;
- generate follower-specific OCO IDs;
- stage protection until follower fill when required;
- adjust protection quantity with partial fills;
- copy stop/target price modifications;
- observe OCO sibling terminal behavior;
- detect missing/unprotected follower;
- handle manual follower protection intervention as divergence;
- write `brackets-oco.md`.

### Acceptance

- OCO IDs never reused cross-follower;
- protection staging unit/scenario tests complete;
- missing expected stop raises critical divergence;
- no accidental recreation after terminal child without reconcile;
- manual SIM certification checklist ready.

---

## Phase 6 — Reconciliation, Recovery, and Failure Hardening

### Objective

Make the system understandable and safe when reality diverges from ideal event flow.

### Tasks

- continuous divergence evaluator;
- current-state snapshot;
- reconcile planner;
- state-hash/stale-plan protection;
- companion-independent active config snapshot semantics;
- NinjaTrader restart recovery classifier;
- follower disconnect/reconnect handling;
- ambiguous submission handling;
- event replay tool;
- fault injection harness;
- terminal mapping retention/grace rules.

### Acceptance

- every divergence class has scenario coverage;
- reconcile planner never increases exposure under ambiguity without explicit flag/confirmation;
- native restart defaults disabled;
- companion failure cannot terminate engine copying;
- failure injection produces explicit errors rather than silent state loss.

---

## Phase 7 — Named-Pipe Protocol and Companion Skeleton

### Objective

Build a robust control plane without inserting it in the execution path.

### Tasks

- named-pipe server in engine;
- current-user ACL;
- protocol envelope/versioning;
- handshake/capabilities;
- snapshot sync;
- bounded telemetry channel;
- companion reconnect/backoff;
- config validation/activation protocol;
- command audit;
- companion Windows process host;
- `/health` internal bootstrap.

### Acceptance

- named-pipe integration tests pass on Windows;
- malformed/oversized messages rejected;
- companion can be killed/restarted and resync;
- engine remains operational while disconnected;
- incompatible protocol clearly fails.

---

## Phase 8 — Persistence and Local API

### Objective

Create durable config/journal foundation and secure local control API.

### Tasks

- `control.db` migrations;
- `journal.db` migrations;
- config versioning;
- event ingestor;
- journal projection;
- HTTP API;
- SSE stream;
- loopback bind;
- Host/Origin/CSRF/session-token security;
- API validation/body limits;
- OpenAPI output;
- destructive command prepare/execute flow;
- retention/backup infrastructure.

### Acceptance

- persistence and API integration tests pass;
- foreign origins/hosts rejected;
- no generic order-entry endpoint;
- config activates atomically through engine acknowledgement;
- slow journal does not block native engine;
- control-plane coverage targets met.

---

## Phase 9 — Modern Dashboard Foundation

### Objective

Deliver a premium local web experience for status and configuration.

### Tasks

- React/TS workspace;
- design tokens;
- app shell/navigation;
- global status strip;
- overview;
- copy groups list/editor;
- follower matrix;
- activation validation/change summary;
- live event stream integration;
- responsive layout;
- accessibility baseline;
- synthetic demo mode.

### Acceptance

- no external asset/network dependency;
- group can be configured end-to-end against fake/demo engine;
- status truth preserved after refresh;
- Web component coverage thresholds pass;
- Playwright baseline flows pass.

---

## Phase 10 — Live Trades, Divergences, Reconciliation UX

### Objective

Make operational state exceptionally clear.

### Tasks

- live logical trades;
- follower side-by-side detail;
- working order/protection cards;
- event timeline;
- divergence page;
- reconcile preview/execute UX;
- pause/disable semantics;
- flatten prepare/confirmation UX;
- critical notification treatment.

### Acceptance

- critical divergence visible without navigating to logs;
- stale destructive confirmation fails safely;
- browser never claims success before engine acknowledgement;
- Playwright covers reject/divergence/reconcile/flatten fake scenarios.

---

## Phase 11 — Journal and Analytics

### Objective

Turn captured execution data into an excellent local history and quality dashboard.

### Tasks

- journal list/detail;
- filters;
- notes/tags;
- CSV/JSON export;
- latency distribution;
- reliability analytics;
- fill delta analytics;
- follower/account summaries;
- retention settings;
- database backup/restore UI.

### Acceptance

- analytics trace back to persisted source events;
- partial/unavailable metrics labeled;
- exports schema-versioned;
- no financial claim from missing data.

---

## Phase 12 — Diagnostics, Security Hardening, and Performance

### Objective

Prepare a code-complete public Alpha/RC.

### Tasks

- diagnostics dashboard;
- redacted support bundle;
- queue/DB/IPC diagnostics;
- CodeQL clean or reviewed findings;
- secret scan;
- dependency vulnerability review;
- license audit/SBOM;
- synthetic performance benchmark;
- soak/fault injection;
- mutation tests;
- logging retention;
- security threat model final review;
- benchmark docs.

### Acceptance

- no unresolved critical/high security finding;
- no unresolved critical execution correctness defect;
- performance baseline published as development benchmark, not marketing guarantee;
- diagnostic bundle contains no secrets/real account fixtures by default.

---

## Phase 13 — Packaging, Installer, Docs, Screenshots

### Objective

Make the project usable by another technically competent NinjaTrader user.

### Tasks

- build/package scripts;
- local installer or documented install bundle;
- clean uninstall;
- upgrade path;
- first-run docs;
- troubleshooting;
- recovery docs;
- README polish;
- architecture diagrams;
- demo screenshots via Playwright if feasible;
- checksums;
- release notes;
- create pre-release GitHub Release if artifact pipeline is sound.

### Acceptance

- clean machine/install procedure documented and preferably tested in VM/sandbox;
- no proprietary NinjaTrader binaries in release;
- synthetic screenshots only;
- README identifies Alpha/Not live certified;
- final automated CI green.

---

## Phase 14 — Manual SIM Certification Handoff

### Objective

Prepare the exact checklist for the product owner to validate when markets/demo environment are available.

### Build Agent deliverables

- `docs/testing/MANUAL-SIM-CERTIFICATION.md`;
- current build SHA/tag;
- install command;
- recommended synthetic/SIM group setup;
- every scenario with pass/fail boxes;
- diagnostic export instructions;
- known limitations;
- expected logs/event codes.

The autonomous Build Agent does not wait for manual testing to finish. It publishes the code-complete Alpha and clearly marks certification pending.

---

# 56. Autonomous Agent Operating Model

The build should be run by one **Coordinator Agent** plus optional specialized subagents.

## 56.1 Coordinator responsibilities

Only the Coordinator:

- owns roadmap state;
- edits `docs/agent/STATE.md` and `NEXT.md` as authoritative continuation state;
- integrates branches;
- resolves cross-module conflicts;
- runs phase gates;
- commits/merges to `main`;
- pushes checkpoints;
- changes architectural decisions through ADRs;
- produces implementation reports.

## 56.2 When to spawn subagents

Spawn subagents when tasks are independent and have clear file/module boundaries.

Good parallel work:

- Domain model + tests;
- web design system/components against mocked API;
- control-plane persistence/API after contracts are stable;
- CI/security workflows;
- documentation/diagrams;
- benchmark harness;
- installer scripts.

Bad parallel work:

- two agents editing the same state machine;
- two agents independently changing IPC schema;
- one agent changing config semantics while another implements UI against old semantics;
- multiple agents pushing directly to `main`.

## 56.3 Suggested specialized subagents

### `native-engine-agent`

Owns:

- NT adapter;
- event subscriptions;
- order executor;
- native lifecycle;
- named-pipe engine endpoint.

### `domain-reliability-agent`

Owns:

- state machines;
- sizing;
- idempotency;
- divergence;
- reconcile planner;
- core tests.

### `control-plane-agent`

Owns:

- .NET 10 host;
- IPC client;
- SQLite;
- REST/SSE;
- security middleware;
- diagnostics.

### `dashboard-agent`

Owns:

- React UI;
- demo mode;
- component/Playwright tests;
- README screenshots.

### `quality-security-agent`

Owns:

- CI;
- static/security scans;
- dependency review;
- coverage reports;
- benchmarks/fault injection support;
- packaging quality checks.

Do not spawn all subagents immediately before contracts exist. Bootstrap shared contracts first, then parallelize.

## 56.4 Worktree protocol

For each parallel task:

```text
branch: agent/<area>/<short-task>
worktree: ../worktrees/<area>-<task>
```

Subagent receives:

- exact objective;
- allowed directories;
- forbidden directories;
- relevant ADRs/contracts;
- acceptance tests;
- expected commit format.

Subagent returns:

- commit SHA(s);
- files changed;
- tests executed;
- result;
- assumptions;
- unresolved issues.

Coordinator reviews diff and tests before integrating.

## 56.5 Merge discipline

Preferred:

- rebase/cherry-pick coherent commits;
- run affected tests;
- run full fast suite at phase checkpoint;
- push `main`.

Never merge a failing branch simply to “save progress.” The subagent branch itself preserves progress.

---

# 57. Commit and Push Discipline

## 57.1 Commit size

Commits should represent one coherent outcome, typically:

- domain type + tests;
- one state-machine feature + tests;
- one API endpoint group + tests;
- one UI feature + tests;
- one documentation/CI concern.

Avoid huge “implement everything” commits.

## 57.2 Commit format

Conventional style:

```text
feat(engine): mirror limit order changes
fix(domain): prevent scale-out rounding reversal
test(reconcile): cover stale state hash rejection
feat(web): add copy group follower matrix
security(web): reject foreign localhost origins
docs: document recovery state machine
ci: add codeql and dependency review
```

## 57.3 Before each commit

- format;
- build affected projects;
- run affected tests;
- secret/account-data scan changed files;
- inspect `git diff`;
- ensure no generated junk.

## 57.4 After each commit

Push unless work is intentionally on a subagent branch awaiting coordinator integration.

## 57.5 Clean tree invariant

At end of every checkpoint:

```text
git status --short
```

must be empty.

If generated artifacts are intentionally not committed, `.gitignore` them before moving on.

---

# 58. Agent Context/Restart Continuity Protocol

The agent must assume its chat/context may disappear at any time.

## 58.1 `docs/agent/STATE.md`

Update after every meaningful checkpoint.

Required format:

```markdown
# Agent State

Last updated: <UTC>
Current branch: main
HEAD: <sha>
Current phase: Phase N
Phase status: IN_PROGRESS | COMPLETE | BLOCKED_PARTIAL

## Completed
- ...

## Current invariants / locked decisions
- ...

## Tests last run
- command — result

## Known blockers
- ...

## Active subagents/worktrees
- ...

## Next exact action
- ...
```

## 58.2 `docs/agent/NEXT.md`

Contains only the next 3–10 executable tasks in priority order. Keep concise.

## 58.3 `docs/agent/DECISIONS.md`

Index of ADRs and important implementation constraints.

## 58.4 `docs/agent/BLOCKERS.md`

A blocker does not automatically stop the whole run.

Format:

```text
BLOCKER-ID
Scope affected
Why blocked
Evidence
Workaround attempted
What can continue
What a human eventually must provide
```

## 58.5 Resume algorithm

At every new agent context or restart:

1. `git status`;
2. verify remote/fetch;
3. read `AGENTS.md`;
4. read `docs/agent/STATE.md`;
5. read `docs/agent/NEXT.md`;
6. read ADR index/relevant ADRs;
7. inspect last 10 commits;
8. run `scripts/resume.ps1`;
9. if tree dirty, determine whether it is a recoverable interrupted task; do not discard work blindly;
10. resume the first uncompleted task.

Chat history is never the sole source of project state.

---

# 59. Autonomous Decision Policy

The user has explicitly authorized autonomous development without routine approvals.

## 59.1 Agent may decide autonomously

- internal class/file names;
- test fixture organization;
- minor UI layout;
- dependency selection among well-maintained permissive options;
- refactoring;
- code formatting;
- CI implementation details;
- non-breaking API internals;
- error wording;
- database indexes;
- local performance optimizations preserving semantics.

## 59.2 Agent must not silently change

- local-only product boundary;
- leader/follower model;
- no arbitrary browser order-entry API;
- default disabled/safety behavior;
- live-trading certification requirement;
- execution correctness invariants;
- open-source/free model;
- telemetry default none;
- public repo privacy rules;
- destructive command confirmation semantics.

If an implementation constraint makes one impossible, record an ADR and choose the safest product-preserving alternative. Do not stop unless truly blocked.

---

# 60. Blocker Policy

Do not ask the user questions overnight for things that can be sensibly defaulted.

## 60.1 Non-blocking examples

- final product branding unresolved → use working name;
- screenshot optional → proceed without if UI runner unavailable;
- no NinjaTrader installed → build/test all independent layers, mark native local compile pending;
- no market open → do not wait; prepare manual certification;
- domain collision for marketing → irrelevant to source implementation;
- optional chart library issue → choose another permissive library.

## 60.2 Hard external blockers

Examples:

- GitHub authentication absent and repo cannot be created/pushed;
- filesystem permissions prevent writing project;
- required SDK cannot be installed and no compatible toolchain exists;
- corrupted environment prevents all builds.

Even then:

- complete local source/docs as far as possible;
- record blocker;
- do not destroy work;
- produce final report with exact command/error.

---

# 61. Production Safety Rules for the Build Agent

These rules override convenience.

1. **Never select/use a real account for automated tests.**
2. Native submit smoke tests require positive simulation-account identification.
3. If simulation identity cannot be proven, skip submission.
4. Never scrape/commit broker credentials.
5. Never enable automated live trading as part of install.
6. First install starts disabled.
7. Existing live positions are never modified automatically on installation/startup.
8. No automated test flattens an account outside fake/SIM environment.
9. Do not “test rejection” by intentionally violating a live account rule.
10. No screenshot/log/report includes real account IDs.
11. No source includes the user’s existing project secrets.
12. Do not weaken Windows/browser security for convenience.

---

# 62. Definition of Code-Complete Alpha

The autonomous coding run may declare **Code-Complete Alpha** only if all of the following are true or explicitly blocked by unavailable NinjaTrader/manual environment:

## Repository

- public repo created;
- source pushed;
- clean tree;
- license/security/contributing docs;
- CI present;
- no secret scan findings.

## Architecture

- modular shared/domain/native/control-plane/web structure;
- ADRs committed;
- no giant monolithic implementation;
- no proprietary NT binaries committed.

## Copier engine

- leader/follower grouping;
- multiple followers;
- market/limit/stop lifecycle implemented in domain/native adapter where API verified;
- modify/cancel;
- partial fills;
- idempotency/loop prevention;
- sizing;
- mapping;
- connection gating;
- divergence;
- bracket/OCO architecture implemented to the degree possible with verified API;
- reconcile planner;
- restart-safe disabled state.

## Control plane

- named-pipe protocol;
- secure loopback HTTP;
- config persistence/atomic activation;
- event journal;
- diagnostics;
- backup basics.

## Web

- overview;
- copy groups;
- live trades;
- event timeline;
- divergences/reconcile;
- journal;
- latency/reliability analytics;
- settings/diagnostics;
- synthetic demo data.

## Quality

- coverage thresholds met or explicitly documented exception;
- scenario suite green;
- integration tests green;
- Playwright critical flows green;
- security scans green/reviewed;
- benchmark baseline captured;
- no critical/high known correctness defect.

## Distribution

- build/install scripts;
- release artifacts where environment supports;
- README/operations docs;
- manual SIM certification handoff.

## Status label

README must still say:

> **Alpha / automated test complete; manual NinjaTrader SIM certification required before live use.**

---

# 63. Final Definition of Done for Stable 1.0

Stable 1.0 is a later milestone and requires product-owner/manual test evidence.

- all Alpha criteria;
- full manual SIM certification matrix passed;
- tested supported NinjaTrader versions documented;
- real installed package upgrade/uninstall tested;
- no critical/high security or execution defect;
- restart/recovery cases manually validated;
- bracket/OCO scenarios manually validated;
- performance benchmark report published with clear methodology;
- release artifacts checksum/SBOM;
- troubleshooting/recovery guides validated;
- known limitations clearly documented;
- version tagged `v1.0.0` only after those gates.

---

# 64. Core Interfaces and Module Responsibilities

These names are illustrative but the boundaries are required.

## 64.1 Domain interfaces

```csharp
public interface ICopyDecisionEngine
{
    CopyDecision Handle(NormalizedEngineEvent @event, EngineStateSnapshot state, ActiveConfigSnapshot config);
}

public interface ISizingEngine
{
    SizingDecision Calculate(SizingRequest request);
}

public interface ITopologyValidator
{
    ValidationResult Validate(CopyConfiguration configuration);
}

public interface IDivergenceDetector
{
    IReadOnlyList<DivergenceCandidate> Evaluate(ReconciliationSnapshot snapshot);
}

public interface IReconciliationPlanner
{
    ReconcilePlan Prepare(ReconcileRequest request, ReconciliationSnapshot snapshot);
}
```

Pure domain interfaces must not expose NinjaTrader types.

## 64.2 Native adapter interfaces

```csharp
public interface ITradingAccountGateway
{
    IReadOnlyList<AccountSnapshot> GetAccounts();
    NativeSubmitResult Submit(FollowerOrderRequest request);
    NativeChangeResult Change(FollowerOrderChange request);
    NativeCancelResult Cancel(FollowerOrderCancel request);
    NativeFlattenResult Flatten(FlattenRequest request);
}

public interface INativeEventSource
{
    event EventHandler<NormalizedOrderEvent> OrderChanged;
    event EventHandler<NormalizedExecutionEvent> ExecutionChanged;
    event EventHandler<NormalizedPositionEvent> PositionChanged;
    event EventHandler<NormalizedAccountStatusEvent> AccountStatusChanged;
}

public interface IOriginRegistry
{
    void RegisterPending(CommandId commandId, AccountKey follower, LogicalOrderId logicalOrderId);
    void BindNativeOrder(CommandId commandId, FollowerOrderKey nativeOrderKey);
    bool IsCopierOriginated(AccountKey account, NativeOrderIdentity order);
}
```

## 64.3 Engine orchestration

Suggested components:

```text
NativeAddOnHost
AccountSubscriptionManager
NativeEventNormalizer
CopierCoordinator
CopyDecisionEngine
OriginRegistry
FollowerExecutionDispatcher
OrderMappingRegistry
ProtectionCoordinator
DivergenceCoordinator
ActiveConfigManager
EngineStateSnapshotProvider
LatencyRecorder
TelemetryPublisher
NamedPipeServer
```

## 64.4 Control-plane services

```text
EngineConnectionService
EngineSnapshotCache
ConfigService
ConfigValidationService
JournalIngestionService
TradeProjectionService
AnalyticsService
DivergenceReadService
CommandService
ReconcileCommandService
FlattenCommandService
DiagnosticBundleService
RetentionService
BackupService
```

## 64.5 Web boundaries

Frontend must use an explicit API layer and stable DTO types rather than components calling `fetch` ad hoc throughout the tree.

Suggested:

```text
src/api/
src/features/overview/
src/features/groups/
src/features/live-trades/
src/features/divergences/
src/features/journal/
src/features/analytics/
src/features/diagnostics/
src/features/settings/
src/components/
src/design-system/
src/lib/
```

---

# 65. Engine State Model

Global engine state should be explicit:

```text
Starting
Disabled
Enabled
PausedNewEntries
Degraded
RecoveryReviewRequired
CriticalLock
ShuttingDown
```

## 65.1 `Disabled`

No new copy entries. Existing mapped safety actions may continue only if product semantics explicitly say existing positions remain managed.

## 65.2 `Enabled`

Normal copy behavior.

## 65.3 `PausedNewEntries`

No exposure-increasing copy actions. Existing exits/protection continue.

## 65.4 `Degraded`

System can still safely execute a subset of required behavior but one noncritical component/follower is unhealthy. Dashboard must explain.

## 65.5 `RecoveryReviewRequired`

After restart/reconnect, active native state is ambiguous enough that new copying remains blocked.

## 65.6 `CriticalLock`

A safety invariant was violated or engine can no longer trust state. No new entries. Risk-reducing actions may be allowed if deterministically safe.

State transitions are audited.

---

# 66. Error Taxonomy

Create typed domain errors rather than arbitrary exception text.

## 66.1 Classes

```text
ConfigurationError
TopologyError
UnsupportedOrderError
SizingError
RiskLimitError
NativeSubmissionError
NativeChangeError
NativeCancelError
ConnectionError
StateAmbiguityError
MappingError
ProtectionError
ProtocolError
PersistenceError
SecurityError
InvariantViolation
```

## 66.2 Exception policy

Expected business failures are result types, not exceptions.

Exceptions indicate unexpected technical failure and are caught at boundaries.

Never swallow exception and continue as if synchronized.

## 66.3 Critical invariant handling

If a core invariant fails:

- emit CRITICAL event;
- lock affected group or engine as narrowly as safe;
- preserve diagnostic context;
- avoid speculative recovery;
- continue unrelated groups only if isolation is proven.

---

# 67. Data and Privacy Classification

Classify fields to support redaction.

## Public/non-sensitive

- product version;
- event code;
- generic instrument root;
- latency distribution;
- synthetic fixtures.

## User-local operational

- account display names;
- connection names;
- native order IDs;
- execution IDs;
- positions;
- P/L;
- journal notes.

## Secret

Core product should have none. If future secrets exist, they are never in DB/log/support bundle unencrypted.

## Redaction rules

Support bundle aliases:

- account names;
- connection-specific identifiers;
- native order/execution IDs optionally hashed;
- local filesystem user path normalized to `%USERPROFILE%`.

---

# 68. Compatibility Strategy

## 68.1 Supported platform scope

Initial:

- Windows 11 strongly preferred/tested;
- Windows 10 only if NinjaTrader still supports the installed version and build evidence is available;
- NinjaTrader 8 Desktop only.

Do not claim support for NinjaTrader Web/Mobile because AddOn architecture is desktop-specific.

## 68.2 NinjaTrader version handling

At runtime:

- detect version;
- compare with tested compatibility table shipped in product;
- show:
  - `Tested`;
  - `Compatible but unverified`;
  - `Unsupported/unknown`.

Unknown version may run in SIM/dev mode, but UI warns before enabling live-capable accounts.

## 68.3 Provider differences

NinjaTrader connection providers can have order/OCO/reconnect differences. Domain must remain provider-neutral; compatibility docs record provider-specific observations.

Do not hardcode prop-firm rules into core copier.

---

# 69. Build Reproducibility

## 69.1 Version stamping

Every component reports:

- SemVer;
- Git SHA;
- build UTC time;
- build configuration;
- protocol version.

Dashboard and support bundle display them.

## 69.2 Locked dependencies

- `packages.lock.json` or equivalent for .NET where appropriate;
- `pnpm-lock.yaml` committed;
- `global.json` pins supported .NET SDK feature band where appropriate;
- CI uses reproducible restore modes.

## 69.3 Artifacts

Generate SHA-256 checksums.

Where practical generate SBOM for release bundle.

---

# 70. Quality Gates by Severity

## Critical

Blocks any release:

- duplicate order possibility with known reproduction;
- wrong-side order;
- unexpected position reversal;
- missing stop silently shown healthy;
- unauthorized browser command execution;
- secret/account leak into public repo;
- topology recursion;
- unsafe auto-resubmit creating exposure;
- corrupted config partially activated.

## High

Blocks Beta/Stable, usually Alpha release too:

- restart produces incorrect state;
- follower reject not surfaced;
- destructive command stale-state race;
- IPC command unauthenticated/invalidly authorized;
- persistent config loss;
- severe memory/queue leak.

## Medium

May ship in Alpha with known limitation:

- noncritical chart rendering bug;
- analytics metric unavailable for one provider;
- optional screenshot generation failure;
- cosmetic responsive issue.

## Low

Normal backlog.

---

# 71. Build Agent Test Command Contract

Create top-level scripts so a new agent does not memorize dozens of commands.

## `scripts/build.ps1`

Builds all components possible on current environment.

Flags:

```text
-Fast
-Full
-IncludeNative
-Configuration Release
```

## `scripts/test.ps1`

Modes:

```text
-Fast          unit + essential integration
-Full          all automated non-soak tests
-Coverage      enforce thresholds
-Mutation      deep domain mutation
-Performance   deterministic benchmark
-Soak          extended synthetic
-WebE2E        Playwright
```

## `scripts/verify-ninjatrader.ps1`

- discovers local NT assemblies;
- compiles real adapter;
- prints version;
- never copies proprietary DLLs into repo;
- optionally installs dev build only with explicit flag.

## `scripts/security-scan.ps1`

- secret scan;
- dependency vulnerabilities;
- license inventory;
- account-data pattern scan;
- prohibited binary check.

## `scripts/resume.ps1`

Prints:

- repo status;
- current SHA;
- remote sync;
- state/next files;
- toolchain versions;
- NinjaTrader detected yes/no;
- last test summary if stored.

---

# 72. GitHub Actions Quality Gate Example

Fast CI should conceptually enforce:

```text
checkout
  ↓
secret/prohibited file scan
  ↓
.NET restore/build/test ───────────────┐
Web install/typecheck/lint/test/build ├──> aggregate status
Windows IPC/integration tests ────────┤
CodeQL/dependency review ─────────────┘
  ↓
coverage gates
  ↓
artifact/test report upload
```

Do not make public CI depend on a secret/proprietary NinjaTrader SDK artifact.

---

# 73. First Autonomous Run — Exact Execution Order

When the Build Agent receives this design and the instruction “execute it,” it should proceed approximately as follows without waiting for approval:

## Step A — Environment inventory

- GitHub CLI/auth;
- git;
- Visual Studio/MSBuild;
- .NET SDKs;
- PowerShell;
- Node/pnpm;
- NinjaTrader install/user dir;
- available disk space;
- current working directory.

Record sanitized inventory in implementation report, not personal paths.

## Step B — Create public repository

- use authenticated GitHub user;
- `open-trade-copier` or fallback name;
- clone/create workspace;
- bootstrap governance/docs/agent-state;
- first commit and push.

## Step C — Establish CI/toolchains

- .NET shared/control-plane skeleton;
- web skeleton;
- initial tests;
- GitHub Actions;
- second/third commits and push.

## Step D — Architecture verification

- native NT project/spike;
- official APIs/local assemblies;
- ADRs;
- no orders.

## Step E — Build domain core test-first

This is the first substantial code phase. Coordinator may spawn a test/domain subagent and a UI shell subagent after contracts stabilize.

## Step F — Build native adapter + lifecycle

Implement in bounded slices and run local compile often.

## Step G — Parallelize supporting layers

Once contracts are stable:

- control plane subagent;
- dashboard subagent;
- quality/security subagent.

Coordinator continues execution semantics.

## Step H — Integrate continuously

Do not wait until all subagents finish weeks of independent work. Integrate small stable checkpoints.

## Step I — Full quality pass

- full tests;
- coverage;
- Playwright;
- security;
- performance;
- packaging;
- docs;
- demo screenshot if feasible.

## Step J — Publish code-complete Alpha

- clean `main`;
- final push;
- optional prerelease tag/release if packaging works;
- final implementation report;
- manual SIM checklist pending.

---

# 74. Implementation Report Template

Every major phase gets a report under `docs/reports/`.

Filename:

`IMPLEMENTATION-REPORT-PHASE-XX-<slug>-YYYY-MM-DD.md`

Template:

```markdown
# Implementation Report — Phase XX

## Status
COMPLETE | PARTIAL | BLOCKED_ENVIRONMENT

## Scope implemented
- ...

## Architecture decisions
- ADR-...

## Source changes
- modules/files summary

## Tests executed
| Command | Result | Coverage/Notes |
|---|---|---|

## Security checks
- ...

## Performance checks
- ...

## NinjaTrader local verification
- version:
- compile:
- SIM execution: NOT RUN / PASS / FAIL

## Known limitations
- ...

## Blockers
- ...

## Git
- starting SHA:
- ending SHA:
- commits:
- remote pushed: yes/no
- working tree clean: yes/no

## Next recommended task
- ...
```

Final report additionally includes full feature matrix and manual certification handoff.

---

# 75. ADR Template

`docs/adr/ADR-XXXX-title.md`

```markdown
# ADR-XXXX: Title

Status: Accepted
Date:

## Context

## Decision

## Alternatives considered

## Safety/reliability impact

## Performance impact

## Compatibility impact

## Consequences

## Validation evidence
```

Important decisions need evidence, not preference alone.

---

# 76. Prohibited Repository Content

CI/local scan must reject accidental inclusion of:

- NinjaTrader proprietary DLLs/EXEs;
- broker/prop credentials;
- `.env` secrets;
- Windows credential exports;
- real account screenshots;
- account numbers matching known local fixtures/user data;
- local SQLite operational databases;
- diagnostic bundles containing raw user data;
- Visual Studio user settings (`.suo`, user files);
- package caches/build outputs;
- certificates/private signing keys.

Large binaries require explicit review.

---

# 77. Dependency Selection Rules for Autonomous Agent

Before adding a production dependency, check:

- active maintenance;
- compatible permissive license;
- no known critical vulnerabilities;
- actual need vs standard library;
- package size/runtime impact;
- transitive dependency burden;
- Windows/.NET Framework compatibility where native.

Record material dependencies in `docs/development/dependencies.md` or generated inventory.

Native engine should prefer BCL/NinjaTrader APIs over convenience packages.

---

# 78. UI Design QA Checklist

Before dashboard phase completion:

- loading state on every async page;
- empty state;
- error state;
- disconnected engine state;
- stale data indication;
- keyboard focus visible;
- no icon-only dangerous button without accessible label;
- confirmation text names affected accounts using aliases;
- long account/group names truncate safely with tooltip;
- large follower counts virtualize/scroll;
- status chips consistent;
- latency units consistent;
- timestamps offer local display + exact UTC/detail;
- no fake real-time success animation;
- no sensitive data in document title/browser history beyond generic product page;
- page refresh does not lose server truth.

---

# 79. Operational Edge Cases Checklist

The Build Agent must explicitly consider and test/document:

- two leader orders same instrument simultaneously;
- simultaneous long/short intent depending account provider/netting behavior;
- manual leader cancel before follower acknowledgement;
- follower fill while leader cancels;
- leader modification while follower modification pending;
- follower manual order unrelated to copier;
- follower manual order on same instrument;
- multiple OCO groups same instrument;
- same account connected with unusual display naming;
- connection reconnect returning historical order updates;
- NinjaTrader simulation reset/playback rewind events;
- daylight-saving/session boundary for daily risk limits;
- locale decimal separators;
- tick-size price normalization;
- zero/negative/NaN/infinite invalid config values;
- huge multipliers;
- unsupported time-in-force;
- order state not recognized by current domain enum;
- app upgraded with active persisted journal;
- disk full;
- read-only data directory;
- clock adjustment while running;
- companion starts before NinjaTrader;
- NinjaTrader starts before companion;
- two companion processes accidentally launched;
- two NinjaTrader instances if possible;
- port already occupied;
- stale browser tab after companion restart.

---

# 80. Multiple Instance Policy

Initial design assumes one active NinjaTrader engine instance per Windows user/product instance.

Companion must detect engine `SessionId`.

If multiple NinjaTrader processes expose engines:

- do not guess which one should receive config;
- list instances and require explicit local selection before enabling;
- V1 may instead support only one and show `MULTIPLE_ENGINE_INSTANCES_UNSUPPORTED`.

A stale engine session must not receive commands intended for a new one.

---

# 81. Single Companion Instance Policy

Use per-user mutex/lock file plus port/process validation to prevent two companions mutating the same DB concurrently.

Second launch should:

- detect healthy existing instance;
- open its dashboard or exit cleanly;
- never start another journal writer.

Stale lock recovery must verify process liveness.

---

# 82. Configuration Backup / Portability

Profile export should be safe for GitHub/support sharing only if the user chooses redaction.

Default export can preserve account display names because it is local user data, but provide `Redact account identifiers` option.

Imported config:

- parse strict schema;
- reject unknown executable fields;
- validate accounts against current environment;
- leave missing accounts unresolved/disabled;
- never auto-enable immediately after import.

---

# 83. No Telemetry Guarantee

The codebase should make the claim auditable.

Automated test/build scan should flag unexpected outbound HTTP client usage in production projects where practical.

Permitted network behavior:

- browser loopback to companion;
- companion loopback only;
- engine named pipe;
- NinjaTrader itself independently connects to broker/provider.

The product does not proxy broker traffic.

GitHub update links in About page are ordinary hyperlinks opened by user, not background update calls.

---

# 84. Public Product Disclaimer

README/install UI should state plainly:

- software can submit, modify, cancel, and flatten real orders;
- use simulation first;
- identical fills are not guaranteed;
- network/broker/exchange/provider behavior can differ;
- user is responsible for account/prop-firm/broker permissions/rules;
- project is not financial advice;
- open source does not eliminate trading risk.

Avoid alarmist legalese dominating UX, but do not hide risk.

---

# 85. Authoritative Technical Basis for This Design

The product architecture is based on the repository PRD, first-principles engineering decisions, and official platform documentation.

## NinjaTrader official documentation

- AddOn Development Overview — documents AddOn capabilities, Visual Studio advanced development, multi-class AddOns, and the supported NinjaTrader desktop development model.  
  https://ninjatrader.com/support/helpguides/nt8/addon_development_overview.htm
- Account class — documents Account events and CreateOrder/Submit/Change/Cancel/Flatten capabilities.  
  https://ninjatrader.com/support/helpguides/nt8/account_class.htm
- Other Uses for an AddOn — documents AccountItemUpdate, ExecutionUpdate, OrderUpdate, PositionUpdate subscriptions.  
  https://ninjatrader.com/support/helpguides/nt8/other_uses_for_an_addon.htm
- Export / Considerations for Compiled Assemblies — documents NinjaScript distribution and compiled-assembly considerations.  
  https://ninjatrader.com/support/helpguides/nt8/export.htm  
  https://ninjatrader.com/support/helpguides/nt8/considerations_for_compiled_assemblies.htm

## Microsoft platform documentation

Use official Microsoft documentation for the selected companion-service runtime, Windows IPC/security mechanisms, packaging, cryptography, and lifecycle/support requirements.

Official platform documentation and repository-approved ADRs take precedence over assumptions. When platform behavior remains ambiguous, the Build Agent must create a minimal reproducible SIM experiment, record the observation, and turn the result into an ADR or test rather than infer behavior from third-party trade-copier implementations.

---

# 86. Final Autonomous Build Directive

When this file is supplied to a capable Build Agent, the product owner may simply say:

> **Execute the attached design end-to-end. Work fully autonomously. Create the new public GitHub repository first, then implement phase by phase. Use subagents/worktrees where safe and useful. Make small coherent commits, push continuously, keep `main` green and the tree clean, persist context/resume state in the repository, and continue through all non-blocking issues without asking for routine approvals. Never trade a live account. Deliver the most complete production-grade source, tests, documentation, packaging, dashboard, journal/analytics, diagnostics, security controls, and code-complete Alpha possible in the available environment. At the end, push everything and write the final implementation report plus manual NinjaTrader SIM certification checklist.**

The Build Agent must then:

1. Read the PRD and this full design.
2. Execute Phase 0 immediately.
3. Keep `docs/agent/STATE.md` current.
4. Spawn subagents only after shared contracts are stable enough to prevent divergent implementations.
5. Never wait for branding, screenshots, market hours, or manual SIM testing when other development can proceed.
6. Never hide environment blockers.
7. Never claim live certification it has not earned.
8. Stop only when all feasible phases are complete, all source is pushed, the tree is clean, and the final report accurately distinguishes complete, tested, environment-blocked, and manually-pending work.

---

# 87. Final Delivery Checklist for the Autonomous Run

Before the Build Agent concludes, it must check every item and include the result in the final report.

## Git/GitHub

- [ ] Public repository created.
- [ ] Remote URL recorded in report.
- [ ] `main` pushed.
- [ ] All agent branches integrated or documented.
- [ ] Working tree clean.
- [ ] No unpushed commits.
- [ ] No secrets/account data detected.

## Source

- [ ] Shared domain/contracts.
- [ ] Native NinjaTrader AddOn/adapter source.
- [ ] Control-plane source.
- [ ] Web dashboard source.
- [ ] Test tools/fake adapter.
- [ ] Installer/build scripts.

## Execution engine

- [ ] Default disabled.
- [ ] Loop prevention.
- [ ] Idempotency.
- [ ] Market order path.
- [ ] Limit order path.
- [ ] Stop order path.
- [ ] Change/cancel.
- [ ] Partial fill handling.
- [ ] Follower sizing.
- [ ] Instrument mapping.
- [ ] OCO/protection behavior implemented/tested or exact environment blocker documented.
- [ ] Divergence detection.
- [ ] Reconcile planner.
- [ ] Restart/recovery state.

## Local control plane

- [ ] Named-pipe protocol.
- [ ] Current-user/validation security.
- [ ] Config version/activation.
- [ ] SQLite migrations.
- [ ] Journal ingestor.
- [ ] Loopback HTTP.
- [ ] Host/Origin/CSRF/session security.
- [ ] SSE/live state.
- [ ] Diagnostics.

## Dashboard

- [ ] Overview.
- [ ] Copy groups.
- [ ] Live trades.
- [ ] Timeline.
- [ ] Divergences.
- [ ] Reconcile UX.
- [ ] Journal.
- [ ] Analytics.
- [ ] Diagnostics/settings.
- [ ] Demo mode.
- [ ] Screenshots if feasible.

## Tests

- [ ] Unit suite.
- [ ] State-machine transition suite.
- [ ] Property/randomized sizing/idempotency tests.
- [ ] Integration tests.
- [ ] IPC tests.
- [ ] SQLite tests.
- [ ] API security tests.
- [ ] Web component tests.
- [ ] Playwright critical flows.
- [ ] Fault injection.
- [ ] Performance baseline.
- [ ] Coverage thresholds.
- [ ] Mutation test result where feasible.

## Security/OSS

- [ ] SECURITY.md.
- [ ] Threat model.
- [ ] CodeQL workflow.
- [ ] Dependency scanning.
- [ ] Secret scanning.
- [ ] License review.
- [ ] THIRD_PARTY_NOTICES.
- [ ] No proprietary binaries.
- [ ] No outbound telemetry.

## Docs/operations

- [ ] README.
- [ ] Architecture docs.
- [ ] Install.
- [ ] First run.
- [ ] Recovery.
- [ ] Troubleshooting.
- [ ] Backup/restore.
- [ ] Benchmark methodology.
- [ ] Manual SIM certification matrix.
- [ ] Final implementation report.

## Release truth

- [ ] Status says Alpha/RC unless manual certification completed.
- [ ] No live-ready marketing claim.
- [ ] Known limitations listed.
- [ ] Exact HEAD SHA listed.

---

# 88. Product Success Standard

The project is successful when a trader can install it, choose a leader, select followers, configure sizing and mappings, enable copying deliberately, and then understand **exactly** what the copier believes happened across every account—without needing to decipher NinjaTrader logs.

The native execution engine should feel boring and trustworthy.

The browser dashboard should feel modern enough that users are surprised it is a free local NinjaTrader tool.

The open-source repository should feel professional enough that another experienced engineer can audit it, run the tests, reproduce a failure, contribute a fix, and understand why safety decisions were made.

The product should earn trust through **determinism, visibility, tests, and evidence**, not marketing claims.

---

**End of Full System Design / Autonomous SDLC Specification**
