using TradeCopia.Installer;
using TradeCopia.Platform;

namespace TradeCopia.Installer.UnitTests;

public class PathAndPreflightTests
{
    [Fact]
    public void OneDrive_documents_is_cloud_backed()
    {
        Assert.True(CloudPathDetector.IsCloudBacked(@"C:\Users\example\OneDrive\Documents\NinjaTrader 8"));
        Assert.True(CloudPathDetector.IsCloudBacked(@"C:\Users\example\OneDrive - Contoso\Documents"));
        Assert.False(CloudPathDetector.IsCloudBacked(@"C:\Users\example\Documents\NinjaTrader 8"));
    }

    [Fact]
    public void Resolver_uses_injected_documents_folder()
    {
        var docs = Path.Combine("C:", "Users", "example", "Documents");
        var location = NinjaTraderPaths.Resolve(new FixedDocumentsFolder(docs));
        Assert.Equal(Path.Combine(docs, "NinjaTrader 8"), location.UserDataPath);
        Assert.False(location.CloudBacked);
        Assert.Equal(Path.Combine(docs, "NinjaTrader 8", "bin", "Custom"), location.CustomPath);
    }

    [Fact]
    public void Resolver_flags_onedrive_documents()
    {
        var location = NinjaTraderPaths.Resolve(new FixedDocumentsFolder(@"D:\OneDrive\Documents"));
        Assert.True(location.CloudBacked);
    }

    [Fact]
    public void Preflight_blocks_cloud_backed_nt_data()
    {
        var result = Preflight.Evaluate(new InstallRequest
        {
            LocalAppData = @"C:\tmp\local",
            Documents = new FixedDocumentsFolder(@"C:\tmp\OneDrive\Documents"),
            PathExists = _ => true,
            DriveFreeBytes = _ => 1_000_000_000,
            NinjaTraderRunning = () => false,
            CompanionRunning = () => false,
            PayloadDirectory = @"C:\tmp\payload"
        });
        Assert.False(result.CanInstall);
        Assert.Contains(result.Checks, c => c.Name == "nt-userdata-not-cloud" && !c.Passed && c.Blocking);
    }

    [Fact]
    public void Preflight_blocks_when_ninjatrader_is_running()
    {
        var result = Preflight.Evaluate(new InstallRequest
        {
            LocalAppData = @"C:\tmp\local",
            Documents = new FixedDocumentsFolder(@"C:\tmp\Documents"),
            PathExists = _ => true,
            DriveFreeBytes = _ => 1_000_000_000,
            NinjaTraderRunning = () => true,
            CompanionRunning = () => false
        });
        Assert.False(result.CanInstall);
        Assert.Contains(result.Checks, c => c.Name == "ninjatrader-closed" && !c.Passed);
    }

    [Fact]
    public void Preflight_allows_local_closed_nt()
    {
        var result = Preflight.Evaluate(new InstallRequest
        {
            LocalAppData = @"C:\tmp\local",
            Documents = new FixedDocumentsFolder(@"C:\tmp\Documents"),
            PathExists = _ => true,
            DriveFreeBytes = _ => 1_000_000_000,
            NinjaTraderRunning = () => false,
            CompanionRunning = () => false,
            PayloadDirectory = @"C:\tmp\payload"
        });
        Assert.True(result.CanInstall);
    }
}
