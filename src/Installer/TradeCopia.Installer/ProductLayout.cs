namespace TradeCopia.Installer;

public static class ProductInfo
{
    public const string ProductName = "TradeCopia";
    public const string Version = "0.1.0-alpha.4";
    public const string SetupFileName = "TradeCopia-Setup-" + Version + ".exe";
    public const string CompanionExe = "TradeCopia.ControlPlane.exe";
    public const string LauncherExe = "TradeCopia.Launcher.exe";
    public const string DefaultPort = "17841";
    public const string LoopbackUrl = "http://127.0.0.1:" + DefaultPort;
}

public static class ProductLayout
{
    public static string PerUserRoot(string localAppData)
    {
        return Path.Combine(localAppData, ProductInfo.ProductName);
    }

    public static string AppDirectory(string root) => Path.Combine(root, "app");
    public static string ConfigDirectory(string root) => Path.Combine(root, "config");
    public static string LogsDirectory(string root) => Path.Combine(root, "logs");
    public static string DataDirectory(string root) => Path.Combine(root, "data");
    public static string VersionFile(string root) => Path.Combine(root, "version.json");
    public static string NativeStaging(string root) => Path.Combine(root, "native");
    public static string UninstallMarker(string root) => Path.Combine(root, "uninstall.json");
}
