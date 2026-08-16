# Third-Party Notices

TradeCopia is original software licensed under Apache-2.0.

This file records third-party components distributed with or used by the
project. It is updated when a dependency is introduced.

No third-party trade-copier source, binaries, UI assets, or trade dress are
permitted in this repository.

## Current notices

### Microsoft.Data.Sqlite / SQLitePCLRaw

Used by the local control-plane persistence project. Native SQLite is pulled
through `SQLitePCLRaw.lib.e_sqlite3` 3.53.3 to avoid the unpatched 2.1.11
advisory (GHSA-2m69-gcr7-jv3q). License: Apache-2.0 / public domain SQLite.

### xUnit / coverlet / .NET test SDK

Test-only dependencies. Licenses: Apache-2.0.

NinjaTrader is a trademark of its owner. This project is not affiliated with
or endorsed by NinjaTrader, LLC. NinjaTrader proprietary assemblies must never
be committed or redistributed.
