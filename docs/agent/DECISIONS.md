# Decision index

Formal ADRs live in `docs/adr/`. This file is the resume-friendly index.

| ID | Decision | Status | Notes |
| --- | --- | --- | --- |
| D-000 | Product name is **TradeCopia** | Locked | [ADR-0001](../adr/ADR-0001-product-name-and-license.md) |
| D-001 | Repository is `gattasrikanth/tradecopia`, public, default branch `main` | Locked | Created 2026-08-15. |
| D-002 | License is Apache-2.0 | Locked | [ADR-0001](../adr/ADR-0001-product-name-and-license.md) |
| D-003 | C# namespaces and project names use `TradeCopia.*` | Locked | Avoids shipping the temporary OpenTradeCopier name in code. |
| D-004 | Local data root is `%LOCALAPPDATA%\TradeCopia\` | Locked | Same layout as design §27 with product rename. |
| D-005 | Default loopback port `17841`, bind `127.0.0.1` | Locked | Design §24. |
| D-006 | Named-pipe IPC; engine hosts the server | Pending ADR | Design §23; formalize in Phase 1. |
| D-007 | Shared domain targets `netstandard2.0` where feasible | Pending ADR | Design §5.2. |
| D-008 | Control plane is .NET 10 LTS ASP.NET Core | Pending ADR | Design §5.3. |
| D-009 | Dashboard is React + TypeScript + Vite + pnpm | Pending ADR | Design §5.4. |
