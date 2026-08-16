using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TradeCopia.ControlPlane.Groups;
using TradeCopia.Domain.Safety;
using TradeCopia.Native.Adapter;
using TradeCopia.Protocol;

namespace TradeCopia.ControlPlane.UnitTests;

public class AccountAndGroupApiTests
{
    [Fact]
    public async Task Accounts_when_disconnected_are_empty_not_fixture()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var json = await client.GetStringAsync("/api/v1/accounts");
        Assert.Contains("engine-disconnected", json);
        Assert.DoesNotContain("SIM-LEADER-01", json);
        Assert.DoesNotContain("SIM-FOLLOWER-03", json);
        var groups = await client.GetStringAsync("/api/v1/groups");
        Assert.DoesNotContain("SIM-LEADER-01", groups);
        var status = await client.GetStringAsync("/api/v1/system/status");
        Assert.Contains("\"copyingEnabled\":false", status);
        Assert.DoesNotContain("SIM-LEADER-01", status);
        Assert.DoesNotContain("SIM-FOLLOWER-03", status);
        var journal = await client.GetStringAsync("/api/v1/journal/trades");
        Assert.DoesNotContain("SIM-LEADER-01", journal);
    }

    [Fact]
    public async Task Enable_fails_closed_when_engine_disconnected()
    {
        await using var factory = new ApiFactory();
        var store = factory.Services.GetRequiredService<GroupConfigStore>();
        var draft = store.CreateDraft("g", "sim-1", new[] { "demo-1" });
        var client = factory.CreateClient();
        var boot = await client.GetFromJsonAsync<Bootstrap>("/api/v1/system/bootstrap");
        client.DefaultRequestHeaders.Add("X-CSRF-Token", boot!.CsrfToken);
        var validate = await client.PostAsJsonAsync("/api/v1/groups/" + draft.Id + "/validate", new { });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, validate.StatusCode);
        var enable = await client.PostAsJsonAsync("/api/v1/groups/" + draft.Id + "/enable", new { });
        Assert.True(enable.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public void Stale_activate_is_rejected()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tc-groups-" + Guid.NewGuid().ToString("N"));
        var store = new GroupConfigStore(dir);
        var accounts = new[]
        {
            new EngineAccountRecord("sim-1", "Sim", "Simulator", "Simulation", false, AccountSafetyClass.Simulation),
            new EngineAccountRecord("demo-1", "Demo", "Provider31", "Live", true, AccountSafetyClass.DemoPaper)
        };
        var draft = store.CreateDraft("g", "sim-1", new[] { "demo-1" });
        Assert.Equal("draft", draft.Status);
        var validated = store.Validate(draft.Id, accounts);
        Assert.True(validated.Ok);
        Assert.Equal("validated", validated.Group!.Status);
        Assert.NotEqual("active", validated.Group.Status);
        var liveOnly = store.CreateDraft("live", "live-1", new[] { "sim-1" });
        var liveAccounts = new[]
        {
            new EngineAccountRecord("sim-1", "Sim", "Simulator", "Simulation", false, AccountSafetyClass.Simulation),
            new EngineAccountRecord("live-1", "Live", "InteractiveBrokers", "Live", false, AccountSafetyClass.Live)
        };
        var liveValidate = store.Validate(liveOnly.Id, liveAccounts);
        Assert.False(liveValidate.Ok);
        Assert.Contains("leader-not-selectable", liveValidate.Reason);
        var stale = store.Activate(draft.Id, validated.Group!.Version - 1, accounts);
        Assert.False(stale.Ok);
        Assert.Equal("stale-activate", stale.Reason);
        var ok = store.Activate(draft.Id, validated.Group.Version, accounts);
        Assert.True(ok.Ok);
        Assert.Equal("active", ok.Group!.Status);
        var reloaded = new GroupConfigStore(dir);
        var persisted = reloaded.Get(draft.Id);
        Assert.NotNull(persisted);
        Assert.Equal("active", persisted!.Status);
        Assert.Equal("sim-1", persisted.LeaderKey);
    }

    [Fact]
    public async Task Connected_snapshot_accounts_are_served()
    {
        await using var factory = new ApiFactory();
        var options = factory.Services.GetRequiredService<TradeCopia.ControlPlane.ControlPlaneOptions>();
        using var runtime = new EngineRuntime(options.PipeName);
        runtime.PublishAccounts(new[]
        {
            new EngineAccountRecord("sim-1", "Sim", "Simulator", "Simulation", false, AccountSafetyClass.Simulation)
        });
        runtime.Start();
        var client = factory.CreateClient();
        string body = "";
        for (var i = 0; i < 40; i++)
        {
            body = await client.GetStringAsync("/api/v1/accounts");
            if (body.Contains("sim-1", StringComparison.Ordinal))
            {
                break;
            }

            await Task.Delay(50);
        }

        Assert.Contains("sim-1", body);
        Assert.Contains("Simulation", body);
        Assert.DoesNotContain("SIM-LEADER-01", body);
    }

    [Fact]
    public async Task Late_published_accounts_appear_without_fixture_fallback()
    {
        await using var factory = new ApiFactory();
        var options = factory.Services.GetRequiredService<TradeCopia.ControlPlane.ControlPlaneOptions>();
        using var runtime = new EngineRuntime(options.PipeName);
        runtime.Start();
        var client = factory.CreateClient();
        var connected = false;
        for (var i = 0; i < 40; i++)
        {
            var status = await client.GetStringAsync("/api/v1/system/status");
            if (status.Contains("\"engineConnected\":true", StringComparison.Ordinal))
            {
                connected = true;
                break;
            }

            await Task.Delay(50);
        }

        Assert.True(connected);
        var empty = await client.GetStringAsync("/api/v1/accounts");
        Assert.DoesNotContain("SIM-LEADER-01", empty);
        runtime.PublishAccounts(new[]
        {
            new EngineAccountRecord("sim-1", "Sim", "Simulator", "Simulation", false, AccountSafetyClass.Simulation),
            new EngineAccountRecord("demo-1", "Demo", "Provider31", "Live", true, AccountSafetyClass.DemoPaper)
        });
        string body = "";
        for (var i = 0; i < 40; i++)
        {
            body = await client.GetStringAsync("/api/v1/accounts");
            if (body.Contains("sim-1", StringComparison.Ordinal) && body.Contains("demo-1", StringComparison.Ordinal))
            {
                break;
            }

            await Task.Delay(50);
        }

        Assert.Contains("sim-1", body);
        Assert.Contains("demo-1", body);
        Assert.Contains("DemoPaper", body);
        Assert.DoesNotContain("SIM-LEADER-01", body);
    }

    private sealed class Bootstrap
    {
        public string CsrfToken { get; set; } = "";
    }
}
