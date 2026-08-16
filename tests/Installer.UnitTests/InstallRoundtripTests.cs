using TradeCopia.Installer;
using TradeCopia.Platform;

namespace TradeCopia.Installer.UnitTests;

public class InstallRoundtripTests
{
    [Fact]
    public void Install_deploys_owned_files_and_not_ninjatrader_dlls()
    {
        var root = Path.Combine(Path.GetTempPath(), "tradecopia-install-" + Guid.NewGuid().ToString("N"));
        var payload = Path.Combine(root, "payload");
        var local = Path.Combine(root, "local");
        var docs = Path.Combine(root, "Documents");
        var custom = Path.Combine(docs, "NinjaTrader 8", "bin", "Custom");
        Directory.CreateDirectory(Path.Combine(payload, "app"));
        Directory.CreateDirectory(Path.Combine(payload, "native"));
        Directory.CreateDirectory(custom);
        File.WriteAllText(Path.Combine(payload, "app", "TradeCopia.ControlPlane.exe"), "cp");
        File.WriteAllText(Path.Combine(payload, "native", "TradeCopia.Native.dll"), "addon");
        File.WriteAllText(Path.Combine(payload, "native", "NinjaTrader.Core.dll"), "forbidden");

        var engine = new InstallerEngine();
        var result = engine.Install(new InstallRequest
        {
            PayloadDirectory = payload,
            LocalAppData = local,
            StartMenuPrograms = Path.Combine(root, "StartMenu"),
            Documents = new FixedDocumentsFolder(docs),
            PathExists = Directory.Exists,
            DriveFreeBytes = _ => 1_000_000_000,
            NinjaTraderRunning = () => false,
            CompanionRunning = () => false
        });

        Assert.True(result.Succeeded);
        Assert.True(File.Exists(Path.Combine(custom, "TradeCopia.Native.dll")));
        Assert.False(File.Exists(Path.Combine(custom, "NinjaTrader.Core.dll")));
        Assert.True(File.Exists(Path.Combine(ProductLayout.PerUserRoot(local), "version.json")));
        var version = File.ReadAllText(Path.Combine(ProductLayout.PerUserRoot(local), "version.json"));
        Assert.Contains("disabled", version);

        var uninstall = engine.Uninstall(local, docs, new FixedDocumentsFolder(docs));
        Assert.True(uninstall.Succeeded);
        Assert.False(File.Exists(Path.Combine(custom, "TradeCopia.Native.dll")));
        Assert.True(Directory.Exists(ProductLayout.DataDirectory(ProductLayout.PerUserRoot(local))));
    }

    [Fact]
    public void Backup_aborts_when_source_missing()
    {
        var dest = Path.Combine(Path.GetTempPath(), "tradecopia-bak-" + Guid.NewGuid().ToString("N"));
        var manifest = UserDataBackup.CopyTree(Path.Combine(dest, "missing"), dest);
        Assert.Contains("source-missing", manifest.Errors);
    }

    [Fact]
    public void Backup_copies_tree_and_verifies()
    {
        var root = Path.Combine(Path.GetTempPath(), "tradecopia-baksrc-" + Guid.NewGuid().ToString("N"));
        var src = Path.Combine(root, "src");
        var dest = Path.Combine(root, "dest");
        Directory.CreateDirectory(Path.Combine(src, "bin", "Custom"));
        File.WriteAllText(Path.Combine(src, "Config.xml"), "<config/>");
        File.WriteAllText(Path.Combine(src, "bin", "Custom", "note.txt"), "ok");
        var manifest = UserDataBackup.CopyTree(src, dest);
        Assert.Empty(manifest.Errors);
        Assert.Equal(2, manifest.FileCount);
        Assert.True(UserDataBackup.VerifyReadable(dest));
    }

    [Fact]
    public void Remediation_refuses_cloud_target_and_running_nt()
    {
        var running = UserDataRemediator.RelocateToLocalDocuments(@"C:\src", @"C:\Users\example\Documents", @"C:\bak", ninjaTraderRunning: true);
        Assert.False(running.Succeeded);
        Assert.Equal("ninjatrader-running", running.Reason);

        var cloud = UserDataRemediator.RelocateToLocalDocuments(@"C:\src", @"C:\Users\example\OneDrive\Documents", @"C:\bak", ninjaTraderRunning: false);
        Assert.False(cloud.Succeeded);
        Assert.Equal("target-still-cloud", cloud.Reason);
    }

    [Fact]
    public void Payload_bundle_extracts_zip_to_usable_directory()
    {
        var root = Path.Combine(Path.GetTempPath(), "tradecopia-payload-" + Guid.NewGuid().ToString("N"));
        var payload = Path.Combine(root, "payload");
        Directory.CreateDirectory(Path.Combine(payload, "app"));
        File.WriteAllText(Path.Combine(payload, "app", "TradeCopia.ControlPlane.exe"), "cp");
        var zipDir = Path.Combine(root, "zip-only");
        Directory.CreateDirectory(zipDir);
        var zip = Path.Combine(zipDir, "payload.zip");
        System.IO.Compression.ZipFile.CreateFromDirectory(payload, zip);
        var extracted = PayloadBundle.ExtractZip(zip);
        Assert.True(File.Exists(Path.Combine(extracted, "app", "TradeCopia.ControlPlane.exe")));
        var resolved = PayloadBundle.Resolve(zipDir);
        Assert.True(File.Exists(Path.Combine(resolved, "app", "TradeCopia.ControlPlane.exe")));
    }

    [Fact]
    public void Upgrade_replaces_version_file()
    {
        var root = Path.Combine(Path.GetTempPath(), "tradecopia-up-" + Guid.NewGuid().ToString("N"));
        var payload = Path.Combine(root, "payload");
        var local = Path.Combine(root, "local");
        var docs = Path.Combine(root, "Documents");
        Directory.CreateDirectory(Path.Combine(payload, "app"));
        Directory.CreateDirectory(Path.Combine(docs, "NinjaTrader 8", "bin", "Custom"));
        File.WriteAllText(Path.Combine(payload, "app", "app.txt"), "v2");
        var engine = new InstallerEngine();
        var request = new InstallRequest
        {
            PayloadDirectory = payload,
            LocalAppData = local,
            Documents = new FixedDocumentsFolder(docs),
            PathExists = Directory.Exists,
            DriveFreeBytes = _ => 1_000_000_000,
            NinjaTraderRunning = () => false,
            CompanionRunning = () => false
        };
        Assert.True(engine.Install(request).Succeeded);
        Assert.True(engine.Upgrade(request).Succeeded);
        Assert.Contains(ProductInfo.Version, File.ReadAllText(ProductLayout.VersionFile(ProductLayout.PerUserRoot(local))));
    }
}
