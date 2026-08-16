using TradeCopia.Analytics;

namespace TradeCopia.ControlPlane.Demo;

public sealed class DemoCatalog
{
    public object SystemStatus() => new
    {
        product = "TradeCopia",
        status = "Development",
        releaseLabel = "Alpha — SIM only recommended",
        privacy = "local-only",
        telemetry = "none"
    };

    public object Privacy() => new
    {
        dataStoredLocally = true,
        cloudUpload = false,
        telemetry = "none",
        dataRoot = "%LOCALAPPDATA%\\TradeCopia\\",
        realAccountDataForbidden = true
    };

    public object[] Accounts() => [];

    public object[] Groups() => [];

    public object[] LiveTrades() => [];

    public object[] Divergences() => [];

    public object[] Journal() => [];

    public object Analytics()
    {
        var stats = ReliabilityCalculator.FromSamples(0, 0, 0, 0, Array.Empty<double>());
        return new
        {
            reliability = stats,
            disclaimer = "Operational copier metrics only. Not financial performance or tax records."
        };
    }
}
