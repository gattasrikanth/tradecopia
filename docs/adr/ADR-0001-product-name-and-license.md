# ADR-0001 — Product name and license

- Status: Accepted
- Date: 2026-08-16
- Phase: 0

## Context

The Product Requirements Document and System Design use temporary working
titles (`OpenCopier`, `OpenTradeCopier`, repository `open-trade-copier`).
The operator bootstrap supplied the product and repository name **TradeCopia**.
The System Design defaults to Apache-2.0 unless a different license is chosen
before first external contribution.

## Decision

- Public product name: **TradeCopia**.
- GitHub repository: `tradecopia` under the authenticated owner.
- Code namespaces and project names: `TradeCopia.*`.
- Local application root: `%LOCALAPPDATA%\TradeCopia\`.
- License: **Apache-2.0**.
- Design documents retain their original working-title wording as historical
  source-of-truth text. New code and user-facing copy use TradeCopia.

## Consequences

- No GitHub organization is created.
- Future branding changes are documentation/packaging only; architecture
  boundaries do not depend on the marketing name.
- THIRD_PARTY_NOTICES.md is the provenance ledger for dependencies.
