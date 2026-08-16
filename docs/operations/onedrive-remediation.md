# OneDrive / NinjaTrader user-data remediation

NinjaTrader documents that OneDrive synchronization of its Documents folder can cause file-access failures. TradeCopia treats cloud-backed NinjaTrader user-data as **unsupported**.

## Desired state

Windows Documents known folder:

```text
%USERPROFILE%\Documents
```

NinjaTrader user-data:

```text
%USERPROFILE%\Documents\NinjaTrader 8
```

That path must not resolve under OneDrive.

## What TradeCopia does

1. Query the real Documents known folder (never assume `%USERPROFILE%\Documents`).
2. Block setup if that path is cloud-backed.
3. Provide backup + copy tooling. The OneDrive copy is not deleted.

## Owner confirmation (only if Windows asks)

If OneDrive shows **Stop backup** / **Keep files on this PC**, choose keep-local. Do not delete cloud files.

See also: [NinjaTrader OneDrive article](https://support.ninjatrader.com/s/article/Unhandled-exception-Access-to-the-path-is-denied-OneDrive-Error).
