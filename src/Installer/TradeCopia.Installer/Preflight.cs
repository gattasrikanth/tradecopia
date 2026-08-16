using TradeCopia.Platform;

namespace TradeCopia.Installer;

public sealed class PreflightCheck
{
    public PreflightCheck(string name, bool passed, string detail, bool blocking)
    {
        Name = name;
        Passed = passed;
        Detail = detail;
        Blocking = blocking;
    }

    public string Name { get; }
    public bool Passed { get; }
    public string Detail { get; }
    public bool Blocking { get; }
}

public sealed class PreflightResult
{
    public PreflightResult(IReadOnlyList<PreflightCheck> checks)
    {
        Checks = checks;
        CanInstall = checks.All(c => c.Passed || !c.Blocking);
    }

    public IReadOnlyList<PreflightCheck> Checks { get; }
    public bool CanInstall { get; }
}

public sealed class InstallRequest
{
    public string PayloadDirectory { get; init; } = "";
    public string LocalAppData { get; init; } = "";
    public string StartMenuPrograms { get; init; } = "";
    public string? DocumentsOverride { get; init; }
    public bool AllowMissingNinjaTrader { get; init; }
    public long RequiredFreeBytes { get; init; } = 200L * 1024 * 1024;
    public Func<string, bool>? PathExists { get; init; }
    public Func<string, long>? DriveFreeBytes { get; init; }
    public Func<bool>? NinjaTraderRunning { get; init; }
    public Func<bool>? CompanionRunning { get; init; }
    public IDocumentsFolder? Documents { get; init; }
}

public static class Preflight
{
    public static PreflightResult Evaluate(InstallRequest request)
    {
        var exists = request.PathExists ?? Directory.Exists;
        var checks = new List<PreflightCheck>
        {
            new("os-windows", OperatingSystem.IsWindows() || !string.IsNullOrEmpty(request.LocalAppData),
                OperatingSystem.IsWindows() ? "Windows" : "synthetic/non-windows test host", false)
        };

        var documents = request.Documents ?? new WindowsDocumentsFolder();
        var location = NinjaTraderPaths.Resolve(documents);
        if (!string.IsNullOrEmpty(request.DocumentsOverride))
        {
            location = NinjaTraderPaths.Resolve(new FixedDocumentsFolder(request.DocumentsOverride));
        }

        checks.Add(new PreflightCheck(
            "documents-known-folder",
            !string.IsNullOrWhiteSpace(location.DocumentsPath),
            "Documents=" + Redact.UserPath(location.DocumentsPath),
            true));

        var cloud = location.CloudBacked;
        checks.Add(new PreflightCheck(
            "nt-userdata-not-cloud",
            !cloud,
            cloud
                ? "NinjaTrader user-data is cloud-synchronized. TradeCopia will not install into a OneDrive (or other cloud) tree."
                : "NinjaTrader user-data path is local.",
            true));

        var ntPresent = exists(location.UserDataPath) && exists(location.CustomPath);
        checks.Add(new PreflightCheck(
            "ninjatrader-userdata",
            ntPresent || request.AllowMissingNinjaTrader,
            ntPresent ? "NinjaTrader 8 user-data and bin\\Custom found." : "NinjaTrader 8 user-data or bin\\Custom is missing.",
            !request.AllowMissingNinjaTrader));

        var ntRunning = request.NinjaTraderRunning?.Invoke() ?? false;
        checks.Add(new PreflightCheck(
            "ninjatrader-closed",
            !ntRunning,
            ntRunning ? "NinjaTrader must be closed to install or update TradeCopia." : "NinjaTrader is not running.",
            true));

        var companionRunning = request.CompanionRunning?.Invoke() ?? false;
        checks.Add(new PreflightCheck(
            "companion-stopped",
            !companionRunning,
            companionRunning ? "TradeCopia companion is running and must be stopped." : "Companion is not running.",
            true));

        if (!string.IsNullOrWhiteSpace(request.PayloadDirectory))
        {
            checks.Add(new PreflightCheck(
                "payload",
                exists(request.PayloadDirectory),
                exists(request.PayloadDirectory) ? "Payload directory present." : "Payload directory missing.",
                true));
        }

        var free = request.DriveFreeBytes?.Invoke(request.LocalAppData) ?? request.RequiredFreeBytes;
        checks.Add(new PreflightCheck(
            "disk-space",
            free >= request.RequiredFreeBytes,
            "freeBytes=" + free,
            true));

        return new PreflightResult(checks);
    }
}

public static class Redact
{
    public static string UserPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "";
        }

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(profile) && path.StartsWith(profile, StringComparison.OrdinalIgnoreCase))
        {
            return "%USERPROFILE%" + path[profile.Length..];
        }

        return Path.GetFileName(path);
    }
}
