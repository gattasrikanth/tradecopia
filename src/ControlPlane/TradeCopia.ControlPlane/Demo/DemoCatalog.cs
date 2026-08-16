using TradeCopia.Analytics;

namespace TradeCopia.ControlPlane.Demo;

public sealed class DemoCatalog
{
    public object SystemStatus() => new
    {
        product = "TradeCopia",
        status = "Development",
        releaseLabel = "Alpha — SIM only recommended",
        engineState = "Disabled",
        engineConnected = false,
        ninjaTraderDetected = true,
        ninjaTraderVersion = "8.1.8.2",
        demoMode = true,
        copyingEnabled = false,
        bindAddress = "127.0.0.1",
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

    public object[] Accounts() =>
    [
        Account("SIM-LEADER-01", "Leader", "Ready", "long", 2, "NQ 06-26"),
        Account("SIM-FOLLOWER-01", "Follower", "Ready", "long", 2, "NQ 06-26"),
        Account("SIM-FOLLOWER-02", "Follower", "Ready", "long", 1, "MNQ 06-26"),
        Account("SIM-FOLLOWER-03", "Follower", "Disconnected", "flat", 0, "NQ 06-26")
    ];

    public object[] Groups() =>
    [
        new
        {
            id = "11111111-1111-1111-1111-111111111111",
            name = "SIM Primary",
            leader = "SIM-LEADER-01",
            followers = new[] { "SIM-FOLLOWER-01", "SIM-FOLLOWER-02" },
            copyMode = "OrderMirror",
            enabledState = "Disabled",
            health = "UNKNOWN",
            note = "Copying starts disabled."
        }
    ];

    public object[] LiveTrades() =>
    [
        new
        {
            logicalTradeId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            instrument = "NQ 06-26",
            side = "Buy",
            leaderQty = 2,
            followers = new[]
            {
                new { account = "SIM-FOLLOWER-01", qty = 2, fill = 18102.50m, health = "Working" },
                new { account = "SIM-FOLLOWER-02", qty = 1, fill = 18102.75m, health = "Working" }
            }
        }
    ];

    public object[] Divergences() =>
    [
        new
        {
            id = "dddddddd-dddd-dddd-dddd-dddddddddddd",
            className = "FollowerDisconnected",
            severity = "Critical",
            account = "SIM-FOLLOWER-03",
            instrument = "NQ 06-26",
            detail = "Follower is disconnected. Unknown is never healthy."
        }
    ];

    public object[] Journal() =>
    [
        new
        {
            id = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            openedAtUtc = "2026-08-16T14:02:11Z",
            group = "SIM Primary",
            instrument = "NQ 06-26",
            side = "Buy",
            leaderFills = 2,
            followerFills = 3,
            rejects = 0,
            divergences = 1,
            decisionLatencyMs = 1.8
        }
    ];

    public object Analytics()
    {
        var stats = ReliabilityCalculator.FromSamples(48, 46, 1, 2, new[] { 0.8, 1.1, 1.4, 1.9, 2.2, 3.4, 4.1 });
        return new
        {
            reliability = stats,
            disclaimer = "Operational copier metrics only. Not financial performance or tax records."
        };
    }

    public object Diagnostics() => new
    {
        controlPlane = "running",
        engine = "disconnected",
        namedPipe = "TradeCopia.Engine.v1.<sid-hash>",
        sqlite = "ok",
        lastError = "Engine not connected. Dashboard is control plane only.",
        orderSubmission = "disabled"
    };

    private static object Account(string key, string role, string readiness, string position, int qty, string instrument) => new
    {
        accountKey = key,
        displayName = key,
        role,
        readiness,
        isSimulation = "KnownTrue",
        position,
        qty,
        instrument
    };
}
