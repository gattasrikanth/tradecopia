using System.Runtime.Versioning;

namespace TradeCopia.Installer;

public static class StartMenuInstall
{
    public const string CommandShortcutName = "Open TradeCopia.cmd";
    public const string LinkShortcutName = "Open TradeCopia.lnk";
    public const string LegacyUrlName = "Open TradeCopia.url";

    public static string ProductFolder(string startMenuPrograms)
    {
        return Path.Combine(startMenuPrograms, ProductInfo.ProductName);
    }

    public static string WriteLauncherShortcut(string startMenuPrograms, string launcherFullPath)
    {
        if (string.IsNullOrWhiteSpace(startMenuPrograms))
        {
            throw new ArgumentException("Start Menu folder is required.", nameof(startMenuPrograms));
        }

        if (string.IsNullOrWhiteSpace(launcherFullPath))
        {
            throw new ArgumentException("Launcher path is required.", nameof(launcherFullPath));
        }

        var folder = ProductFolder(startMenuPrograms);
        Directory.CreateDirectory(folder);

        var staleUrl = Path.Combine(folder, LegacyUrlName);
        if (File.Exists(staleUrl))
        {
            File.Delete(staleUrl);
        }

        var cmd = Path.Combine(folder, CommandShortcutName);
        File.WriteAllText(cmd, "@echo off\r\nstart \"\" \"" + launcherFullPath + "\"\r\n");

        if (OperatingSystem.IsWindows())
        {
            TryWriteWindowsLink(Path.Combine(folder, LinkShortcutName), launcherFullPath);
        }

        return cmd;
    }

    public static void Remove(string startMenuPrograms)
    {
        if (string.IsNullOrWhiteSpace(startMenuPrograms))
        {
            return;
        }

        var folder = ProductFolder(startMenuPrograms);
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void TryWriteWindowsLink(string linkPath, string launcherFullPath)
    {
        try
        {
            var type = Type.GetTypeFromProgID("WScript.Shell");
            if (type == null)
            {
                return;
            }

            var shell = Activator.CreateInstance(type);
            if (shell == null)
            {
                return;
            }

            var create = type.GetMethod("CreateShortcut");
            if (create == null)
            {
                return;
            }

            var shortcut = create.Invoke(shell, new object[] { linkPath });
            if (shortcut == null)
            {
                return;
            }

            var shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { launcherFullPath });
            shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(launcherFullPath) ?? "" });
            shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { "Open TradeCopia" });
            shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, Array.Empty<object>());
        }
        catch (Exception)
        {
        }
    }
}
