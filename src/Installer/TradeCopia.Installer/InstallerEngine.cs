using System.Text.Json;
using TradeCopia.Platform;

namespace TradeCopia.Installer;

public sealed class InstallResult
{
    public InstallResult(bool succeeded, string reason, string productRoot, IReadOnlyList<string> writtenFiles)
    {
        Succeeded = succeeded;
        Reason = reason;
        ProductRoot = productRoot;
        WrittenFiles = writtenFiles;
    }

    public bool Succeeded { get; }
    public string Reason { get; }
    public string ProductRoot { get; }
    public IReadOnlyList<string> WrittenFiles { get; }
}

public sealed class InstallerEngine
{
    public const string NativePrefix = "TradeCopia.";

    public InstallResult Install(InstallRequest request)
    {
        var preflight = Preflight.Evaluate(request);
        if (!preflight.CanInstall)
        {
            var first = preflight.Checks.First(c => !c.Passed && c.Blocking);
            return new InstallResult(false, first.Name, "", Array.Empty<string>());
        }

        var root = ProductLayout.PerUserRoot(request.LocalAppData);
        var written = new List<string>();
        Directory.CreateDirectory(ProductLayout.AppDirectory(root));
        Directory.CreateDirectory(ProductLayout.ConfigDirectory(root));
        Directory.CreateDirectory(ProductLayout.LogsDirectory(root));
        Directory.CreateDirectory(ProductLayout.DataDirectory(root));
        Directory.CreateDirectory(ProductLayout.NativeStaging(root));

        if (!string.IsNullOrWhiteSpace(request.PayloadDirectory) && Directory.Exists(request.PayloadDirectory))
        {
            CopyOwnedFiles(Path.Combine(request.PayloadDirectory, "app"), ProductLayout.AppDirectory(root), written);
            CopyOwnedFiles(Path.Combine(request.PayloadDirectory, "native"), ProductLayout.NativeStaging(root), written);
        }

        var documents = request.Documents ?? new WindowsDocumentsFolder();
        var location = string.IsNullOrEmpty(request.DocumentsOverride)
            ? NinjaTraderPaths.Resolve(documents)
            : NinjaTraderPaths.Resolve(new FixedDocumentsFolder(request.DocumentsOverride));

        if (Directory.Exists(location.CustomPath) && Directory.Exists(ProductLayout.NativeStaging(root)))
        {
            foreach (var file in Directory.GetFiles(ProductLayout.NativeStaging(root)))
            {
                var name = Path.GetFileName(file);
                if (!name.StartsWith(NativePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (name.StartsWith("NinjaTrader", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dest = Path.Combine(location.CustomPath, name);
                File.Copy(file, dest, overwrite: true);
                written.Add(dest);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.StartMenuPrograms))
        {
            var menu = Path.Combine(request.StartMenuPrograms, ProductInfo.ProductName);
            Directory.CreateDirectory(menu);
            var shortcut = Path.Combine(menu, "Open TradeCopia.url");
            File.WriteAllText(shortcut, "[InternetShortcut]\r\nURL=" + ProductInfo.LoopbackUrl + "\r\n");
            written.Add(shortcut);
        }

        var versionJson = JsonSerializer.Serialize(new
        {
            product = ProductInfo.ProductName,
            version = ProductInfo.Version,
            copyingStarts = "disabled",
            bind = "127.0.0.1"
        });
        File.WriteAllText(ProductLayout.VersionFile(root), versionJson);
        written.Add(ProductLayout.VersionFile(root));
        File.WriteAllText(ProductLayout.UninstallMarker(root), JsonSerializer.Serialize(written));
        return new InstallResult(true, "installed", root, written);
    }

    public InstallResult Uninstall(string localAppData, string? documentsOverride, IDocumentsFolder? documents, Func<string, bool>? pathExists = null)
    {
        var root = ProductLayout.PerUserRoot(localAppData);
        if (!Directory.Exists(root))
        {
            return new InstallResult(false, "not-installed", root, Array.Empty<string>());
        }

        var location = NinjaTraderPaths.Resolve(documents ?? new WindowsDocumentsFolder());
        if (!string.IsNullOrEmpty(documentsOverride))
        {
            location = NinjaTraderPaths.Resolve(new FixedDocumentsFolder(documentsOverride));
        }

        if (Directory.Exists(location.CustomPath))
        {
            foreach (var file in Directory.GetFiles(location.CustomPath, NativePrefix + "*"))
            {
                if (Path.GetFileName(file).StartsWith("NinjaTrader", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                File.Delete(file);
            }
        }

        var data = ProductLayout.DataDirectory(root);
        var preserveData = Directory.Exists(data);
        foreach (var dir in new[]
                 {
                     ProductLayout.AppDirectory(root),
                     ProductLayout.ConfigDirectory(root),
                     ProductLayout.LogsDirectory(root),
                     ProductLayout.NativeStaging(root)
                 })
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        foreach (var file in new[] { ProductLayout.VersionFile(root), ProductLayout.UninstallMarker(root) })
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        _ = pathExists;
        _ = preserveData;
        return new InstallResult(true, "uninstalled", root, Array.Empty<string>());
    }

    public InstallResult Upgrade(InstallRequest request)
    {
        var root = ProductLayout.PerUserRoot(request.LocalAppData);
        if (Directory.Exists(root))
        {
            Uninstall(request.LocalAppData, request.DocumentsOverride, request.Documents, request.PathExists);
        }

        return Install(request);
    }

    private static void CopyOwnedFiles(string source, string dest, List<string> written)
    {
        if (!Directory.Exists(source))
        {
            return;
        }

        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith("NinjaTrader", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
            written.Add(target);
        }
    }
}
