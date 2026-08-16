# First run

1. Install the .NET 10 SDK and (for native compile) the .NET Framework 4.8.1 targeting pack.
2. From the repository root:

   ```powershell
   pwsh ./scripts/test.ps1
   pwsh ./scripts/run-control-plane.ps1
   ```

3. Open `http://127.0.0.1:17841`.
4. Confirm the dashboard shows **copying disabled** and **UNKNOWN** health while the engine is disconnected.
5. Do **not** enable copying on a live account.

The dashboard is a control plane. Closing the browser does not start or stop order submission; only the native engine may submit follower orders, and it starts disabled.

NinjaTrader must be launched at least once before `scripts/install-local.ps1` can see a user-data directory.
