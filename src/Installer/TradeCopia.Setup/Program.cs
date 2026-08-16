using System.Diagnostics;
using TradeCopia.Installer;
using TradeCopia.Platform;

var silent = args.Any(a => string.Equals(a, "--silent", StringComparison.OrdinalIgnoreCase)
    || string.Equals(a, "/S", StringComparison.OrdinalIgnoreCase));
var uninstall = args.Any(a => string.Equals(a, "--uninstall", StringComparison.OrdinalIgnoreCase));
var payload = PayloadLocator.Find(AppContext.BaseDirectory);

WriteLine("TradeCopia Setup " + ProductInfo.Version);
WriteLine("Modern, local-first multi-account trade copying for NinjaTrader 8.");
WriteLine("Copying starts disabled. This installer never submits orders.");
WriteLine("");

var request = new InstallRequest
{
    PayloadDirectory = payload,
    LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    StartMenuPrograms = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
        "Programs"),
    Documents = new WindowsDocumentsFolder(),
    PathExists = Directory.Exists,
    DriveFreeBytes = path =>
    {
        var root = Path.GetPathRoot(path);
        if (string.IsNullOrEmpty(root))
        {
            return 0;
        }

        var drive = new DriveInfo(root);
        return drive.IsReady ? drive.AvailableFreeSpace : 0;
    },
    NinjaTraderRunning = () => Process.GetProcessesByName("NinjaTrader").Length > 0,
    CompanionRunning = () => Process.GetProcessesByName("TradeCopia.ControlPlane").Length > 0
        || Process.GetProcessesByName("TradeCopia.Launcher").Length > 0
};

var engine = new InstallerEngine();
if (uninstall)
{
    var removed = engine.Uninstall(request.LocalAppData, null, request.Documents);
    WriteLine(removed.Succeeded ? "Uninstalled TradeCopia product files." : removed.Reason);
    return removed.Succeeded ? 0 : 2;
}

var preflight = Preflight.Evaluate(request);
foreach (var check in preflight.Checks)
{
    WriteLine((check.Passed ? "[ok] " : "[!!] ") + check.Name + " — " + check.Detail);
}

if (!preflight.CanInstall)
{
    WriteLine("");
    WriteLine("Setup cannot continue. Close NinjaTrader if it is open.");
    WriteLine("If NinjaTrader user-data is on OneDrive, see docs/operations/onedrive-remediation.md.");
    WriteLine("There is no Install Anyway option.");
    return 2;
}

if (!silent)
{
    WriteLine("");
    WriteLine("Press Enter to install per-user under %LOCALAPPDATA%\\TradeCopia, or Ctrl+C to cancel.");
    Console.ReadLine();
}

var result = engine.Install(request);
if (!result.Succeeded)
{
    WriteLine("Install failed: " + result.Reason);
    return 2;
}

WriteLine("");
WriteLine("TradeCopia installed successfully.");
WriteLine("Next:");
WriteLine("1. Launch NinjaTrader 8.");
WriteLine("2. Open TradeCopia from the Start menu.");
WriteLine("3. Start with SIM accounts.");
WriteLine("Copying is disabled by default.");
return 0;

void WriteLine(string text) => Console.WriteLine(text);

internal static class PayloadLocator
{
    public static string Find(string baseDir)
    {
        var beside = Path.Combine(baseDir, "payload");
        if (Directory.Exists(beside))
        {
            return beside;
        }

        var parent = Path.Combine(baseDir, "..", "payload");
        return Path.GetFullPath(parent);
    }
}
