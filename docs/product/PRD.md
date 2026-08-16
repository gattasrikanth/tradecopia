# Product Requirements Document (PRD)
# Open-Source Local-First NinjaTrader Trade Copier

**Status:** Product direction / source-of-truth PRD  
**Date:** 2026-08-15  
**Working title:** `OpenCopier` (placeholder only; final project name TBD)  
**Target platform:** NinjaTrader 8 on Windows  
**Distribution model:** Free forever, open source  
**Primary UX:** Modern local browser dashboard  
**Execution model:** Native leader → follower trade copying inside NinjaTrader  

---

## 1. Executive Summary

Build the most trustworthy, transparent, modern, local-first, open-source trade copier for NinjaTrader 8.

The product should combine:

1. A **small, deterministic, high-reliability native NinjaTrader copier engine** that observes one or more leader accounts and mirrors eligible order activity to configured follower accounts.
2. A **beautiful modern browser dashboard** running only on the user’s Windows machine, accessible through a loopback URL such as `http://127.0.0.1:<port>`.
3. **Deep observability**: copy health, account synchronization, event timelines, latency measurements, order mapping, divergence detection, and diagnostic history.
4. A **local trading journal and analytics experience** built from copier-observed events, with no cloud account and no remote data storage required.
5. Strong **safety, testing, documentation, packaging, release engineering, and open-source governance** standards.

The product must not depend on any cloud service to copy trades. Closing the browser must never stop trade copying. Failure of the dashboard or local journal must not become a dependency in the trade-execution hot path.

The project should be useful to a trader who manually trades through Chart Trader, SuperDOM, ATM strategies, NinjaScript strategies, or another NinjaTrader-integrated tool. The copier does not generate trade ideas and does not decide when to enter a trade. It mirrors leader-account activity according to explicit user configuration.

---

# 2. Product Vision

## 2.1 Vision Statement

> Make professional-grade NinjaTrader multi-account trade copying free, open, local, observable, and pleasant to use.

The product should feel like modern 2026 software even though it integrates with a desktop trading platform whose native AddOn UI ecosystem is visually dated.

The dashboard should be good enough that users naturally leave the copier’s NinjaTrader-native UI alone after installation and perform almost all routine management through the local web experience.

## 2.2 Product Positioning

The product is **not merely another free copier**.

Its differentiation should be:

- local-first rather than cloud-first;
- open-source rather than proprietary;
- modern browser UX rather than traditional WPF/table-heavy AddOn UX;
- observable and measurable rather than opaque;
- correctness-first rather than feature-count-first;
- security-conscious even though it runs on localhost;
- deeply documented and testable;
- free forever for core copying, dashboard, journaling, analytics, and diagnostics.

---

# 3. Problem Statement

NinjaTrader users who trade multiple accounts commonly need a leader/follower copier. Existing solutions generally fall into one or more of these categories:

- proprietary paid AddOns;
- free but visually dated NinjaTrader-native tools;
- open-source implementations with limited UX or limited operational visibility;
- cloud-oriented copier products with polished dashboards but recurring cost and remote dependencies;
- tools that copy signals rather than faithfully mirror arbitrary NinjaTrader leader-account activity.

The opportunity is to provide a single product that is:

- native enough to be fast and reliable;
- local enough to preserve privacy and eliminate cloud dependency;
- open enough to be independently auditable;
- modern enough to provide an excellent user experience;
- instrumented enough to explain what happened when something goes wrong.

---

# 4. Product Principles

These are non-negotiable unless the product owner explicitly revises this PRD.

## P1. Correctness before features

A copier that occasionally duplicates, misses, reverses, or leaves an orphan order is unacceptable regardless of UI quality.

## P2. The execution path must remain small and boring

The native NinjaTrader engine should perform only the minimum work required to:

- observe leader state;
- classify relevant events;
- resolve follower mappings;
- validate safety invariants;
- create/change/cancel follower orders;
- maintain authoritative in-memory mapping/state;
- emit asynchronous telemetry.

No dashboard rendering, database query, remote request, analytics calculation, or nonessential serialization may block the execution path.

## P3. Browser/dashboard is control plane, not execution plane

Closing Chrome/Edge must not stop copying.

The copier engine must continue operating if:

- the dashboard is closed;
- WebSocket clients disconnect;
- the journal database is temporarily unavailable;
- dashboard rendering fails;
- the companion local service restarts, subject to explicit configuration consistency rules.

## P4. Local-only by default

Default runtime must require no:

- SaaS account;
- product login;
- telemetry endpoint;
- cloud database;
- cloud message bus;
- subscription server;
- license server;
- remote configuration server.

## P5. Safe by default

Copying starts disabled after first installation and after any configuration condition where safe restoration cannot be proven.

Potentially dangerous actions must be explicit and auditable.

## P6. Observable by design

Every copy decision should be explainable after the fact.

For any leader order/event, the system should be able to answer:

- what was observed;
- when it was observed;
- what rule matched;
- which followers were eligible;
- what follower action was generated;
- when submission was attempted;
- what NinjaTrader reported afterward;
- whether followers converged with the intended state;
- whether any divergence occurred.

## P7. No fake performance claims

The project must distinguish:

- local copier dispatch latency;
- NinjaTrader event timing;
- broker acknowledgment latency;
- exchange/fill latency;
- leader/follower fill-price differences.

Published performance claims must be backed by repeatable benchmarks and clearly defined measurement boundaries.

## P8. Configuration must be understandable

A user should not need to edit code or configuration files for normal operation.

## P9. Open source should be real, not cosmetic

The source, build process, tests, documentation, and release artifacts should be auditable. Reproducible or at least verifiable release practices are strongly preferred.

## P10. Never silently “repair” a dangerous ambiguity

If the system cannot safely infer intended state, it should surface the divergence and require an explicit reconcile/repair action rather than guessing.

---

# 5. Goals

## 5.1 Primary Goals

### G1. Reliable leader/follower copying

Support one or more leader accounts, each with one or more follower accounts.

### G2. Modern localhost dashboard

Provide a polished responsive browser experience for configuration, monitoring, trade history, journaling, analytics, diagnostics, and safe control actions.

### G3. Strong order lifecycle coverage

Correctly handle relevant NinjaTrader order lifecycle activity including creation, changes, cancellations, fills, partial fills, rejects, and bracket/ATM-related behavior where supported.

### G4. Deterministic synchronization

Maintain explicit mappings between leader orders/trades and follower orders so the copier can reason about state rather than merely fire-and-forget commands.

### G5. Divergence detection and reconciliation

Identify when leader and follower state no longer matches intended configuration and provide controlled remediation workflows.

### G6. Measurable latency

Instrument the copier so users and maintainers can inspect leader-event → follower-dispatch timing and associated downstream acknowledgments.

### G7. Local journal and analytics

Provide useful trade history and account/copy analytics without uploading trading data.

### G8. High-quality open-source project

Use modern engineering practices: modular design, tests, CI, release automation, documentation, semantic versioning, issue templates, security guidance, architecture records, and contributor guidance.

---

# 6. Non-Goals

Unless added by a future PRD, the project is not intended to become:

1. A signal provider.
2. A Discord/Telegram trading bot.
3. An AI trade-decision system.
4. A cloud-hosted trade execution platform.
5. A brokerage.
6. A prop-firm account provider.
7. A trading strategy marketplace.
8. A general-purpose FIX/Tradovate/IBKR multi-broker execution platform.
9. A replacement for NinjaTrader itself.
10. A social copy-trading network where strangers subscribe to other traders.
11. A remote Internet-accessible copier in the initial product.
12. A mobile app in the initial product.
13. A portfolio optimizer or tax/accounting product.

The initial product should remain narrowly focused on **NinjaTrader local leader/follower copying and the experience around that workflow**.

---

# 7. Target Users

## Persona A — Multi-account discretionary futures trader

Trades manually using Chart Trader, SuperDOM, hotkeys, or ATM templates and wants one trade to be mirrored across several accounts.

Needs:

- fast copying;
- clear follower status;
- quantity control;
- bracket synchronization;
- immediate visibility into rejects/divergence;
- emergency controls.

## Persona B — Prop-firm multi-account trader

Manages several accounts with different contract constraints or risk preferences.

Needs:

- account grouping;
- per-follower sizing;
- mini/micro mappings;
- account-specific enable/disable;
- risk caps;
- proof that all intended accounts are synchronized.

## Persona C — NinjaScript/automation user

A strategy or third-party tool originates orders in a leader account and the user wants follower copying without integrating that tool with every account.

Needs:

- source-agnostic leader observation;
- predictable lifecycle semantics;
- strong event history;
- API-independent behavior.

## Persona D — Open-source contributor / advanced operator

Wants to inspect implementation details, reproduce a bug, add broker/provider compatibility, or contribute tests.

Needs:

- architecture documentation;
- test harnesses;
- deterministic reproduction workflows;
- structured logs;
- contributor documentation.

---

# 8. Product Boundary and High-Level Architecture Constraints

The exact implementation architecture belongs in a later System Design document, but this PRD establishes the required logical separation.

## 8.1 Native Copier Engine

Runs inside NinjaTrader and is the only component authorized to submit/change/cancel follower orders.

Responsibilities:

- account discovery and event subscriptions;
- leader event observation;
- leader/follower order mappings;
- quantity/instrument transformation;
- order submission/change/cancel actions;
- lifecycle tracking;
- safety state;
- divergence detection primitives;
- latency timestamps;
- non-blocking event emission to the local control plane.

## 8.2 Local Companion / Control Plane

Runs on the same Windows machine.

Responsibilities:

- local HTTP API;
- local real-time event stream (for example WebSocket/SSE, design TBD);
- browser SPA hosting;
- local persistent configuration storage;
- journal/history database;
- analytics aggregation;
- exports/imports;
- diagnostics bundles;
- update/version metadata;
- secure communication with the native engine.

The specific IPC technology between NinjaTrader and the companion process is deferred to System Design. It must be local, authenticated/validated where appropriate, low-overhead, and must not place the companion process in the order-execution critical path.

## 8.3 Browser Dashboard

Runs in a standard browser against a loopback-only local endpoint by default.

Example UX address:

`http://127.0.0.1:<configured-port>`

The actual port is TBD and must support collision handling.

---

# 9. Core User Experience

## 9.1 First-run experience

The first-run experience should:

1. Confirm NinjaTrader compatibility.
2. Confirm the native engine is loaded.
3. Start or validate the local companion service.
4. Launch the dashboard.
5. Detect connected NinjaTrader accounts.
6. Clearly distinguish simulation vs live-capable accounts where NinjaTrader exposes that information.
7. Explain that copying is disabled by default.
8. Guide the user through creating a first copy group.
9. Encourage initial SIM validation.
10. Require explicit enablement before any follower order can be generated.

## 9.2 Routine startup

A returning user should see within seconds:

- NinjaTrader connection status;
- engine status;
- current version;
- active copy groups;
- leader/follower connectivity;
- positions;
- working mapped orders;
- synchronization health;
- copying enabled/paused state;
- actionable warnings.

## 9.3 Main dashboard

The home page should prioritize operational truth, not decorative analytics.

Recommended hierarchy:

1. Global status.
2. Copy-group health.
3. Open leader/follower positions.
4. Active working-order mappings.
5. Current divergence/rejection warnings.
6. Live event stream.
7. Today/session summary.
8. Latency summary.

---

# 10. Copy Groups

## 10.1 Definition

A copy group consists of:

- one leader account;
- one or more follower accounts;
- group-level defaults;
- optional follower-specific overrides;
- instrument/sizing/risk rules;
- enablement state.

## 10.2 Requirements

The product must support:

- multiple independent copy groups;
- different leaders for different groups;
- followers with independent quantity settings;
- disabling a follower without deleting configuration;
- pausing an entire group;
- duplicate-account validation;
- loop prevention;
- prevention of impossible/circular follower relationships.

## 10.3 Loop prevention

The system must never allow a configuration such as:

- A copies to B while B copies to A;
- A → B → C → A;
- a follower copies an order generated by this copier back into another configured leader path.

Copied orders must be identifiable as copier-originated through supported metadata/state so recursive copying is prevented deterministically.

---

# 11. Order-Copying Semantics

This is a high-risk product area and must be specified in much greater depth in the later execution-engine design.

## 11.1 Required order types

Target support:

- Market
- Limit
- Stop Market
- Stop Limit
- MIT when supported by the originating/provider context

Unsupported or provider-specific behavior must be visible rather than silently transformed unless transformation is explicitly configured.

## 11.2 Required lifecycle actions

The engine must reason about:

- initial order creation/submission;
- accepted/working state;
- price changes;
- quantity changes;
- cancellation;
- partial fills;
- complete fills;
- rejects;
- execution amendments if surfaced by NinjaTrader;
- order replacement/change semantics;
- position updates;
- disconnect/reconnect transitions.

## 11.3 Source agnosticism

The copier should copy eligible leader activity regardless of whether the leader action originated from:

- Chart Trader;
- SuperDOM;
- NinjaTrader ATM;
- NinjaScript strategy;
- supported third-party AddOn;
- other account-level NinjaTrader order source.

The system should operate on the leader account’s observable NinjaTrader order/execution state rather than depend on a proprietary external signal format.

---

# 12. Sizing

## 12.1 Minimum sizing modes

Per follower:

1. **1:1** — follower quantity equals leader quantity.
2. **Multiplier** — follower quantity = leader quantity × configured multiplier, subject to deterministic rounding rules.
3. **Fixed quantity** — qualifying leader entry maps to configured follower quantity.
4. **Disabled** — follower remains configured but receives no new copy action.

## 12.2 Future/advanced sizing candidates

May be included after core correctness is proven:

- account-balance ratio;
- buying-power/equity ratio;
- risk-dollar sizing;
- per-instrument overrides.

Any advanced sizing feature must specify exact rounding and scale-out semantics before implementation.

## 12.3 Scale-out behavior

The product must define deterministic behavior when leader position/order quantity is reduced.

Follower reductions must never accidentally reverse a follower position due to rounding or stale state.

---

# 13. Instrument Mapping

## 13.1 Same-instrument copying

Default behavior is leader instrument → same follower instrument/expiry when available.

## 13.2 Mini/micro mapping

Target first-class mappings include common futures equivalents such as:

- NQ ↔ MNQ
- ES ↔ MES

Additional mappings should be configuration-driven and extensible rather than scattered through execution code.

## 13.3 Contract expiry

The system must not make dangerous assumptions about contract month mapping.

The later design must define:

- exact contract identity rules;
- expiry matching;
- rollover behavior;
- user overrides;
- validation when a mapped follower instrument does not exist or is unavailable.

---

# 14. ATM, Brackets, Stops, Targets, and OCO

This is a flagship reliability area.

## 14.1 Product requirement

Where NinjaTrader/provider APIs expose sufficient information, follower protection orders should maintain the intended relationship to the leader trade.

The system should support the behavior required for:

- stop-loss orders;
- profit targets;
- stop modifications;
- target modifications;
- quantity adjustments;
- cancellations;
- OCO behavior;
- leader ATM-managed changes.

## 14.2 Follower-side independence

Follower OCO identifiers and order identifiers must be follower-specific. The copier must map logical relationships rather than blindly reuse leader account identifiers.

## 14.3 Failure safety

A follower entry without expected protection must be surfaced immediately as a high-severity condition if the configured copy policy expected a stop/target.

The system must never hide bracket creation failure behind a generic “copied successfully” indicator.

---

# 15. State Model and Mapping

Every copied logical action must have durable correlation identifiers at the product level.

The exact schema is deferred to System Design, but the model must distinguish at least:

- logical copy group;
- leader account;
- follower account;
- leader order identity;
- follower order identity;
- instrument;
- logical trade/copy correlation;
- parent/child bracket relationship;
- OCO relationship;
- intended quantity;
- observed quantity;
- lifecycle state;
- reconciliation state;
- timestamps.

Mappings must not rely on fragile UI text parsing when NinjaTrader provides structured identity/state.

---

# 16. Enable, Pause, Disable, and Flatten Semantics

These words must have precise meanings throughout the UI.

## Enable copying

Allows configured eligible new leader activity to generate follower actions.

## Pause copying

Stops generating new entry copy actions while preserving existing follower positions/orders according to a clearly documented policy. Exit/protective behavior while paused must be explicitly defined in System Design and clearly shown in UI.

## Disable follower

Removes a follower from new copying without deleting its configuration.

## Flatten follower

Explicitly requests flattening of the specified follower/instrument or follower account subject to strong confirmation rules.

## Flatten group

Explicit emergency action affecting configured followers in that group.

## Global emergency stop

Must be intentionally designed. It must not ambiguously combine “disable future copying” with “flatten everything” behind a single unqualified control.

Separate controls are preferred unless the UI makes the combined semantics unmistakable.

---

# 17. Reconciliation and Divergence

## 17.1 Divergence definition

A divergence exists when the follower’s actual state does not match the state implied by the active copy policy and known leader state.

Examples:

- follower flat while leader is long;
- follower long 1 while target quantity is long 2;
- missing follower stop;
- extra unmapped follower order;
- follower order rejected;
- mapped order changed outside expected copier state;
- follower disconnected during a required update.

## 17.2 Required behavior

The system must:

- detect known divergence classes;
- show severity;
- identify impacted account/instrument/group;
- preserve an event trail;
- avoid automatic dangerous guessing;
- provide an explicit reconcile workflow when safe reconciliation can be specified.

## 17.3 Reconcile preview

Before a manual reconciliation that can submit real orders, the dashboard should show a preview such as:

- current state;
- target state;
- exact proposed actions;
- risk warnings;
- confirmation requirement.

---

# 18. Risk and Safety Controls

The copier is not a broker risk engine, but it should provide guardrails.

Target controls include:

- maximum follower quantity per instrument;
- maximum absolute follower position;
- permitted instruments/symbol filters;
- simulation-only mode;
- dry-run/observe-only mode;
- new-entry lockout;
- follower enable/disable;
- optional session/account loss limits after core engine stability is proven;
- optional profit lock after core engine stability is proven.

Risk controls must fail closed when their required data is unavailable and the product cannot safely evaluate the rule.

---

# 19. Local Browser Dashboard Requirements

## 19.1 Design goal

The dashboard must look and behave like a modern professional fintech/control-plane application, not like a browser recreation of a legacy NinjaTrader settings grid.

## 19.2 Visual principles

- clean typography;
- strong information hierarchy;
- dense data only where appropriate;
- responsive layouts;
- high-quality empty states;
- consistent severity/status language;
- light and dark mode eventually, with one highly polished mode acceptable for first release;
- no gratuitous animation in operational views;
- keyboard-friendly controls for power users;
- color must never be the sole indicator of status;
- accessibility considered from the beginning.

## 19.3 Required application areas

### A. Overview

- global engine status;
- NinjaTrader status;
- account count;
- copy groups;
- active positions;
- working orders;
- warnings/divergences;
- current copy latency summary;
- session summary.

### B. Copy Groups

- create/edit/delete groups;
- leader selection;
- follower selection;
- sizing;
- instrument mappings;
- filters;
- risk settings;
- enable/pause state;
- validation feedback.

### C. Live Trades

- leader positions/orders;
- follower positions/orders;
- side-by-side account matrix;
- synchronization status;
- fill prices;
- quantity;
- stop/target state;
- per-follower warnings.

### D. Event Timeline

For a logical copied trade/order:

- leader event observed;
- copy decision;
- follower dispatch times;
- follower order states;
- fills;
- modifications;
- cancels;
- rejects;
- divergence/reconciliation events.

### E. Journal

- trade list;
- date/session filters;
- instrument filters;
- copy-group filters;
- leader/follower drilldown;
- notes/tags eventually;
- export.

### F. Analytics

- copy success rate;
- reject rate;
- divergence count;
- latency distribution;
- follower fill-price delta/slippage relative to leader;
- per-account and per-instrument operational summaries;
- session/day/week views.

Analytics must distinguish operational copier metrics from financial performance metrics.

### G. Diagnostics

- component versions;
- engine uptime;
- local service uptime;
- NinjaTrader connection/account states;
- recent errors;
- queue/IPC health as applicable;
- database health;
- latency probe status;
- downloadable support bundle.

### H. Settings

- local web port;
- startup behavior;
- retention policy;
- privacy/telemetry status;
- logging level;
- export/import;
- update channel eventually;
- advanced/debug options separated from normal configuration.

---

# 20. Journal Requirements

The journal is an operational trading journal built from data the copier already observes.

## 20.1 V1 journal

For each logical trade/session, preserve where available:

- timestamp;
- group;
- leader;
- followers;
- instrument;
- side;
- leader entry/exit executions;
- follower executions;
- quantities;
- stop/target events;
- realized P/L if reliably available through supported NinjaTrader account data;
- follower-vs-leader fill difference;
- copier warnings;
- rejects;
- divergence events;
- latency measurements.

## 20.2 Data truth

The journal must not fabricate missing broker/execution data.

If a metric cannot be reliably calculated, show unavailable/partial rather than extrapolating.

## 20.3 Retention

Local data retention must be user-controlled.

Provide:

- reasonable default retention;
- manual delete;
- export before delete;
- database backup/restore guidance;
- schema migration support.

---

# 21. Analytics Requirements

Initial analytics should focus on copier quality:

- copy attempts;
- successful follower submissions;
- accepted/working/fill rates;
- rejects;
- divergence frequency;
- reconcile frequency;
- local dispatch latency p50/p95/p99/max;
- follower acknowledgment latency where measurable;
- follower fill-time delta where meaningful;
- leader/follower fill-price difference;
- per-account reliability;
- per-instrument reliability.

Possible later financial analytics:

- realized P/L;
- win/loss summary;
- average trade;
- drawdown summaries;
- session performance.

The project must clearly label financial analytics as derived from available account/execution data and not as tax/accounting records.

---

# 22. Latency and Performance Measurement

## 22.1 Required timestamps

The design must capture high-resolution timestamps around at least:

1. leader event callback/observation;
2. copy decision completion;
3. follower submission invocation;
4. follower order update/acceptance when observable;
5. follower execution/fill when observable.

## 22.2 Metrics

Per follower and aggregated:

- decision latency;
- dispatch latency;
- follower dispatch skew;
- acknowledgment latency;
- fill-time delta;
- p50/p95/p99/max;
- sample count.

## 22.3 Benchmark harness

Before a stable release, the project must include a repeatable benchmark procedure covering multiple follower counts such as:

- 1 follower;
- 5 followers;
- 10 followers;
- 20 followers where practical.

The benchmark must document:

- hardware;
- Windows version;
- NinjaTrader version;
- connection/simulation mode;
- order type;
- sample count;
- whether metrics are internal dispatch vs broker-dependent.

## 22.4 Initial engineering objective

Aim for very low single-digit-millisecond internal processing where technically achievable, but **do not establish public numerical latency guarantees until measurements exist**.

A proposed internal pre-release objective is:

- no avoidable blocking I/O in the hot path;
- follower submissions dispatched with minimal serialization;
- instrumentation overhead demonstrated not to materially degrade copy performance;
- benchmark results published with every performance-sensitive stable release.

---

# 23. Reliability Requirements

## 23.1 Fundamental invariants

The implementation must be designed to prove or enforce these invariants:

1. A copier-originated follower order cannot recursively become a leader action.
2. The same leader event cannot intentionally generate duplicate follower actions due solely to replay/re-entry within the copier.
3. A follower order mapping cannot silently change owners/correlation.
4. An unknown state cannot be presented as synchronized.
5. Configuration changes that would invalidate active mappings must be blocked, staged, or explicitly resolved.
6. An execution-path exception must be isolated and surfaced; it must not silently terminate all copier event handling.
7. Event subscriptions must have deterministic lifecycle/unsubscription behavior.
8. The system must handle NinjaTrader shutdown/reload without leaving an ambiguous enabled state after restart.

## 23.2 Restart behavior

Restart/recovery semantics must be explicit.

On NinjaTrader restart, the copier must not blindly assume previous in-memory mappings remain valid.

The future design must define:

- configuration restoration;
- active position/order discovery;
- mapping reconstruction capability;
- when copying is safe to resume automatically;
- when user confirmation is required;
- how existing leader/follower positions are classified.

## 23.3 Companion-service failure

The native engine must continue copying using its validated active configuration when the browser dashboard disappears.

The System Design must decide whether the engine may continue if the companion process itself dies. Preferred product behavior is continued safe copying if the engine already owns a valid immutable active configuration snapshot and does not require the companion for any execution decision.

---

# 24. Security Requirements

Even localhost trading software requires meaningful security controls.

## 24.1 Network exposure

Default browser service must bind only to loopback, not all interfaces.

Preferred default:

- `127.0.0.1`
- optionally `::1` after explicit IPv6 handling/testing

Never default to `0.0.0.0`.

## 24.2 Browser-to-local-service protections

The design must mitigate attacks from malicious websites attempting to reach localhost.

Requirements should include appropriate combinations of:

- strict Host validation / DNS-rebinding defense;
- Origin validation;
- CSRF protection for state-changing endpoints;
- local session or ephemeral token where appropriate;
- restrictive CORS;
- no unauthenticated arbitrary cross-origin write endpoints;
- secure headers suited to a local SPA;
- CSP where practical.

## 24.3 IPC security

Engine/control-plane IPC must validate message origin/shape and use versioned contracts.

The control plane must never accept an arbitrary browser payload and forward it unchecked into order submission.

## 24.4 Secrets

The core product should ideally require no third-party secret.

If future integrations introduce secrets, use Windows-appropriate protected secret storage and keep that functionality isolated from the copier core.

## 24.5 Telemetry

Default: **none**.

If anonymous optional telemetry is ever proposed, it requires a separate product decision and must be explicit opt-in.

---

# 25. Data Storage and Privacy

## 25.1 Local database

A local embedded database is appropriate for:

- configuration history;
- journal events;
- copier event records;
- analytics aggregates;
- migrations/versioning.

SQLite is a strong candidate but is not mandated by this PRD.

## 25.2 Data separation

Execution-critical state must not depend on successful analytical/journal persistence.

The system may asynchronously record events, but a slow journal must not delay follower order actions.

## 25.3 Privacy statement

The project documentation should clearly state:

- what data is stored;
- where it is stored;
- that no trade data is uploaded by default;
- how to delete/export it;
- whether logs may contain account names/order identifiers.

---

# 26. Configuration Management

## 26.1 Profiles

Users should be able to save named copier configurations.

A profile may contain:

- groups;
- leader/follower relationships;
- follower sizing;
- instrument mapping;
- filters;
- risk controls;
- enablement defaults.

## 26.2 Safe activation

Editing configuration should not necessarily mutate an active execution configuration one field at a time.

Preferred UX concept:

- edit draft;
- validate;
- show changes;
- activate atomically.

Exact implementation is deferred to System Design.

## 26.3 Export/import

Support human-portable configuration export/import with:

- schema version;
- validation;
- no secrets by default;
- safe preview before activation.

---

# 27. Diagnostics and Supportability

The project should be designed so a user can report a problem without recording their screen for ten minutes.

## 27.1 Diagnostic bundle

Provide an exportable support bundle containing configurable/redactable forms of:

- product versions;
- environment metadata;
- recent structured logs;
- recent copier events;
- group configuration snapshot;
- connection state history;
- mapping/divergence information;
- performance metrics;
- crash/error details.

Sensitive account identifiers should support redaction.

## 27.2 Structured logging

Logs should be machine-readable internally and rendered clearly in the dashboard.

Use stable event/error codes for significant states.

Example categories:

- ENGINE
- CONFIG
- LEADER_EVENT
- FOLLOWER_DISPATCH
- FOLLOWER_REJECT
- DIVERGENCE
- RECONCILE
- CONNECTION
- IPC
- JOURNAL
- SECURITY

---

# 28. Testing Requirements

Testing quality is part of the product.

## 28.1 Unit tests

Cover pure deterministic logic including:

- copy eligibility;
- sizing;
- rounding;
- mapping;
- loop prevention;
- order classification;
- divergence detection;
- configuration validation;
- state transitions;
- event deduplication/idempotency rules.

## 28.2 Integration tests

Validate:

- NinjaTrader account events to copier decisions where testable;
- follower submission/change/cancel pathways;
- IPC contracts;
- local API;
- database migrations;
- dashboard state synchronization.

## 28.3 Scenario/certification tests

A documented certification suite should include at minimum:

- market entry;
- limit entry;
- stop entry;
- cancel before fill;
- modify limit price;
- modify quantity;
- partial fill;
- full fill;
- stop-loss creation/modification/fill;
- target creation/modification/fill;
- OCO sibling cancellation;
- scale in;
- scale out;
- manual follower intervention;
- follower rejection;
- follower disconnect;
- leader disconnect;
- reconnect;
- NinjaTrader restart;
- dashboard closed;
- companion restart;
- high event volume;
- multiple groups simultaneously;
- loop-prevention attempts.

## 28.4 Simulation-first release gate

No release should be described as live-ready until the documented SIM certification suite passes on supported NinjaTrader versions.

## 28.5 Failure injection

The test strategy should deliberately inject:

- exceptions;
- delayed persistence;
- dropped IPC connection;
- malformed config messages;
- duplicate/replayed events;
- out-of-order nonauthoritative telemetry;
- follower rejects;
- connection flaps.

---

# 29. Performance and Resource Requirements

The product should remain lightweight relative to NinjaTrader.

Requirements:

- no polling loops at unnecessarily high frequency when event-driven state is available;
- no synchronous disk writes in the copy hot path;
- bounded queues with explicit backpressure/failure semantics;
- bounded in-memory history;
- no unbounded log growth;
- configurable journal retention;
- dashboard virtualization/pagination for large histories;
- measurable CPU/memory footprint.

Performance tests should include both idle and burst trading scenarios.

---

# 30. Dashboard Responsiveness and Accessibility

Initial dashboard should support common Windows desktop resolutions and remain usable on tablet-sized screens.

Phone-sized responsiveness is desirable for local viewing but must not drive V1 architecture.

Accessibility expectations:

- keyboard navigation for core controls;
- semantic labels;
- visible focus states;
- status not encoded only by color;
- sufficient contrast;
- readable typography;
- confirmation dialogs that clearly describe consequential actions.

---

# 31. Documentation Requirements

Documentation is a first-class deliverable.

Required documentation categories:

1. Product overview.
2. Installation guide.
3. First SIM copy tutorial.
4. Copy-group concepts.
5. Sizing rules.
6. Instrument mapping.
7. ATM/bracket behavior.
8. Pause/disable/flatten semantics.
9. Divergence/reconciliation guide.
10. Safety guide.
11. Performance/latency measurement guide.
12. Troubleshooting.
13. Upgrade/migration guide.
14. Architecture overview.
15. Developer setup.
16. Testing guide.
17. Contribution guide.
18. Security policy.
19. Release process.
20. FAQ.

Documentation should include diagrams and screenshots where they materially improve understanding.

---

# 32. Open-Source and Governance Requirements

## 32.1 License

The project should use a permissive open-source license unless the product owner later chooses otherwise.

Candidates:

- MIT;
- Apache-2.0.

License choice should be made deliberately before accepting meaningful external contributions.

## 32.2 Repository hygiene

Required:

- clear README;
- LICENSE;
- SECURITY.md;
- CONTRIBUTING.md;
- CODE_OF_CONDUCT.md if/when community grows;
- issue templates;
- pull-request template;
- architecture/design docs;
- changelog/release notes;
- semantic versioning policy;
- automated CI checks.

## 32.3 Dependency discipline

- minimize dependencies in the native NinjaTrader engine;
- pin/lock dependencies where applicable;
- scan dependencies for known vulnerabilities;
- document third-party licenses;
- avoid pulling large frameworks into the execution engine for convenience.

## 32.4 Code quality

The codebase should prefer:

- small cohesive modules;
- explicit interfaces/contracts;
- dependency inversion around platform-specific APIs where practical;
- testable pure domain logic;
- strong naming;
- nullable/reference safety appropriate to selected language/runtime;
- structured errors;
- no giant single-file implementation as the target architecture;
- comments explaining why rather than restating code;
- architecture decision records for consequential choices.

---

# 33. Packaging and Installation

A polished product should not require users to manually copy random source files and troubleshoot compilation as the only supported path.

Long-term target:

- signed or verifiable release artifacts where practical;
- versioned installer/package flow;
- guided NinjaTrader AddOn installation;
- local companion installation/startup;
- easy dashboard launch;
- clean uninstall;
- migration-safe upgrades;
- fallback/manual installation documentation for contributors.

Exact packaging constraints must be validated against NinjaTrader’s supported import/distribution mechanisms during System Design.

---

# 34. Release Channels

Recommended maturity stages:

## Dev

Internal/contributor builds. No live-use claim.

## Alpha

Feature-incomplete. SIM-only strongly enforced or prominently required.

## Beta

Core scenarios implemented and heavily tested. Still requires explicit caution and published limitations.

## Stable

Only after repeatable certification across supported NinjaTrader versions/providers and no unresolved known high-severity correctness defects.

---

# 35. Version Compatibility

The project must document supported NinjaTrader versions.

Compatibility should not be assumed across all NT8 releases.

CI/build verification should compile against supported local NinjaTrader assemblies where licensing/distribution constraints permit.

Runtime compatibility checks should warn clearly when the user runs an untested version.

---

# 36. Product Metrics

The project should measure its own quality using metrics such as:

## Reliability

- duplicate follower actions attributable to copier: target zero;
- missed eligible copy actions attributable to copier: target zero in certified scenarios;
- silent divergence: target zero;
- uncaught execution-engine exceptions: target zero;
- reconciliation-required events per test/session.

## Performance

- leader-event → follower-dispatch p50/p95/p99;
- dispatch skew across followers;
- CPU use;
- memory use;
- event processing throughput.

## UX

- successful first SIM copy completion rate;
- number of configuration validation failures resolved without logs/manual editing;
- time to identify which follower failed in a deliberately induced rejection scenario.

## Open source

Community popularity is not a release criterion. Stars/downloads/contributions may be tracked but must not override engineering quality.

---

# 37. Suggested Product Roadmap

This is a product roadmap, not an implementation task list.

## Phase 0 — Research and architecture

Deliverables:

- review NinjaTrader AddOn/account/order lifecycle APIs;
- review current open-source copiers for behavior and lessons;
- document clean-room/original architecture decisions;
- define authoritative event semantics;
- choose process/IPC architecture;
- create threat model;
- create testability strategy;
- establish repository/tooling conventions.

No live trading development should start before order-lifecycle semantics and safety invariants are documented.

## Phase 1 — Minimal deterministic copier engine

Scope:

- one leader;
- one/multiple followers;
- SIM accounts;
- 1:1 market order behavior initially;
- mapping/idempotency foundation;
- enable/disable;
- structured event output;
- automated tests.

Goal: prove the engine model, not UI.

## Phase 2 — Order lifecycle completeness

Add:

- limit;
- stop;
- modify;
- cancel;
- partial fills;
- scale in/out;
- rejects;
- connection transitions;
- restart/recovery rules;
- divergence detection.

## Phase 3 — Bracket / ATM / OCO certification

Add and certify:

- stop/target copying;
- OCO mapping;
- leader ATM modifications;
- quantity changes;
- protection-failure warnings;
- rigorous scenario testing.

## Phase 4 — Multi-group, sizing, mappings

Add:

- multiple leaders/groups;
- multiplier/fixed sizing;
- mini/micro mapping;
- filters;
- configuration validation;
- atomic configuration activation.

## Phase 5 — Local control plane and modern dashboard

Add:

- companion service;
- loopback HTTP;
- secured browser access;
- live dashboard;
- group management;
- diagnostics;
- real-time events.

Execution engine must remain independently operational with active configuration.

## Phase 6 — Journal and analytics

Add:

- local persistence;
- history;
- trade drilldown;
- event timeline;
- operational analytics;
- latency dashboard;
- exports.

## Phase 7 — Reliability hardening and packaging

Add:

- failure injection;
- load/latency benchmarks;
- installer/upgrader;
- support bundle;
- migration tests;
- security review;
- documentation completion;
- public beta release.

## Phase 8 — Stable public release

Requirements:

- complete certification matrix;
- published known limitations;
- benchmark report;
- documented recovery procedures;
- no unresolved critical/high correctness or security defects;
- versioned release artifacts;
- contributor/release governance ready.

---

# 38. V1 Release Definition

A V1 should be considered successful if a new user can:

1. Install the product using documented steps.
2. Open NinjaTrader.
3. Open a localhost dashboard.
4. See connected accounts.
5. Create a leader/follower group.
6. Configure follower quantities.
7. Enable copying after clear SIM warnings.
8. Place supported leader orders.
9. Observe followers mirror supported lifecycle changes.
10. See live mapped order/trade state.
11. See explicit reject/divergence conditions.
12. Review a complete event timeline afterward.
13. Inspect measured copy latency.
14. Review journal/history locally.
15. Export diagnostic/history data.
16. Close the browser without stopping copying.
17. Restart dashboard/control-plane components without corrupting engine state.
18. Understand exactly whether copying is enabled, paused, unhealthy, or divergent at all times.

---

# 39. V1 Non-Requirements / Deferred Features

Do not delay first stable release for:

- cloud sync;
- remote phone access;
- native mobile app;
- social copy trading;
- subscription billing;
- AI analysis;
- Discord/Telegram ingestion;
- brokerage integrations outside NinjaTrader;
- strategy generation;
- marketplace;
- multi-machine copying;
- Internet-facing API;
- team/user RBAC;
- sophisticated tax/accounting reports.

These require separate PRDs if ever pursued.

---

# 40. Official Platform / Research Baseline

This product is an original implementation. Product behavior and implementation decisions should be derived from this PRD, repository-approved design documents, official platform documentation, and our own simulation/certification evidence.

## NinjaTrader official API

Relevant documented capabilities include account discovery, account events, order/execution/position updates, and account-level CreateOrder/Submit/Change/Cancel/Flatten methods.

References:

- https://ninjatrader.com/support/helpguides/nt8/add_on.htm
- https://ninjatrader.com/support/helpguides/nt8/account_class.htm
- https://ninjatrader.com/support/helpguides/nt8/createorder.htm
- https://ninjatrader.com/support/helpguides/nt8/orderupdate.htm
- https://ninjatrader.com/support/helpguides/nt8/startatmstrategy.htm

## Original implementation requirement

For the initial product:

- do not copy, decompile, or derive implementation details from proprietary third-party trade-copier products;
- do not reuse source code from third-party trade-copier repositories;
- create original UI components, product copy, diagrams, icons, state machines, and execution logic;
- use general-purpose dependencies only after normal dependency, security, and license review;
- validate ambiguous NinjaTrader behavior through our own SIM experiments and automated regression tests.

Future introduction of third-party copier code requires an explicit architecture decision, provenance record, license review, and product-owner approval.

---

# 41. Legal, Risk, and Disclaimer Requirements

The project should prominently state that:

- it can submit real orders;
- it is not financial advice;
- users are responsible for broker/prop-firm rules and permissions;
- users must validate with simulation before live use;
- network/broker/exchange conditions can cause follower fill differences;
- software cannot guarantee identical fills;
- open-source availability does not eliminate trading risk.

The project must not claim compliance with any specific prop firm’s rules unless separately verified and documented.

The project must use an original implementation based on official platform APIs, repository-approved designs, and our own tests. Third-party trade-copier code must not be introduced in V1.

---

# 42. Product Decisions to Lock Early in the New Project

The new ChatGPT project should turn this PRD into explicit architecture/design decisions before implementation.

Priority decisions:

1. Exact leader event semantics: order update vs execution update vs hybrid state machine.
2. How partial fills propagate.
3. Whether pending leader limit/stop orders mirror immediately or only after execution for each configured mode.
4. Exact ATM/bracket correlation model.
5. OCO mapping rules.
6. Follower quantity rounding and scale-out rules.
7. Restart/mapping reconstruction behavior.
8. Configuration activation model.
9. Engine ↔ companion IPC.
10. Engine configuration ownership when companion is offline.
11. Persistence/event-journal schema.
12. Local web security model.
13. Supported NinjaTrader versions/providers for V1.
14. Packaging/install/update model.
15. License: MIT vs Apache-2.0.
16. Product name/branding.

None of these should be left as accidental implementation behavior.

---

# 43. Development Workflow Expectations

The project should follow a design-first agentic workflow:

1. Discuss product/architecture decisions before code.
2. Produce explicit design documents for implementation phases.
3. Use a build/coding agent to implement from those documents.
4. Require tests and evidence for each phase.
5. Commit and push completed work in small coherent increments.
6. Keep the repository clean.
7. Produce implementation reports containing:
   - scope completed;
   - files/modules changed;
   - tests run/results;
   - known limitations;
   - commit SHA;
   - deviations from design;
   - next recommended step.
8. Treat delivered implementation reports as the source of truth for subsequent planning.

The Build Agent should never silently expand scope beyond the approved design.

---

# 44. Recommended Documentation Set for Project Bootstrap

After creating the repository/new ChatGPT project, the first architecture phase should eventually create something similar to:

```text
docs/
  product/
    PRD.md
  architecture/
    system-overview.md
    execution-engine.md
    order-state-machine.md
    ipc-control-plane.md
    data-model.md
    security-threat-model.md
    dashboard-architecture.md
  adr/
    ADR-0001-...
  testing/
    test-strategy.md
    sim-certification-matrix.md
    performance-benchmark.md
  operations/
    installation.md
    recovery.md
    troubleshooting.md
    release-process.md
  reports/
    ...
```

This is illustrative, not an implementation mandate.

---

# 45. Final Product Standard

The project should not consider itself “done” merely because a leader order appears on followers.

The quality bar is:

> A trader can understand, configure, trust, inspect, troubleshoot, benchmark, and recover the copier without needing to inspect source code or decipher NinjaTrader logs — while advanced users can still audit every line of the open-source implementation.

The ideal user impression should be:

- **execution engine:** boring, deterministic, dependable;
- **dashboard:** modern, fast, beautiful, informative;
- **journal:** useful and local;
- **diagnostics:** unusually transparent;
- **documentation:** excellent;
- **security/privacy:** obvious and credible;
- **business model:** free forever, no cloud dependency, no hidden lock-in.

---

# 46. New-Project Handoff Prompt

When this document is added to a new ChatGPT project, use the following instruction as the initial project mandate:

> Treat this PRD as the product source of truth. Do not begin implementation immediately. First perform a structured architecture phase covering NinjaTrader event semantics, state machine design, reliability invariants, restart/reconciliation behavior, IPC/control-plane architecture, localhost security, data model, test strategy, packaging constraints, and open-source licensing/provenance. Clearly separate product requirements from implementation decisions. Any proposed change to a non-negotiable product principle must be surfaced explicitly for product-owner approval. Build execution correctness before dashboard features. All trade-copying functionality must be proven in simulation with automated and documented scenario tests before any live-use claim.

---

**End of PRD**
