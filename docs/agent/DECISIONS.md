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
| D-006 | Named-pipe IPC; engine hosts the server | Locked | [ADR-0004](../adr/ADR-0004-ipc-ownership.md) |
| D-007 | Shared domain targets `netstandard2.0` | Locked | [ADR-0003](../adr/ADR-0003-shared-target-framework.md) |
| D-008 | Control plane is .NET 10 LTS ASP.NET Core | Locked | [ADR-0006](../adr/ADR-0006-control-plane-runtime.md) |
| D-009 | Dashboard is React + TypeScript + Vite + pnpm | Locked | [ADR-0006](../adr/ADR-0006-control-plane-runtime.md) |
| D-010 | Event semantics: OrderUpdate intent, ExecutionUpdate fills | Locked | [ADR-0002](../adr/ADR-0002-event-semantics.md) |
| D-011 | Native packaging uses local NT HintPaths, Private=false | Locked | [ADR-0005](../adr/ADR-0005-native-packaging.md) |
