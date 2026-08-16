using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TradeCopia.Analytics;
using TradeCopia.ControlPlane.Security;

namespace TradeCopia.ControlPlane.UnitTests;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}

public class ApiSecurityTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiSecurityTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Loopback_bind_rejects_unspecified()
    {
        Assert.False(LoopbackGuard.IsLoopbackBind("0.0.0.0"));
        Assert.True(LoopbackGuard.IsLoopbackBind("127.0.0.1"));
    }

    [Fact]
    public void Host_guard_rejects_dns_rebinding()
    {
        Assert.False(LoopbackGuard.IsAllowedHost("evil.example:17841", 17841));
        Assert.True(LoopbackGuard.IsAllowedHost("127.0.0.1:17841", 17841));
    }

    [Fact]
    public async Task Status_is_reachable_on_loopback()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/system/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Disabled", json);
        Assert.Contains("demoMode", json);
    }

    [Fact]
    public async Task Generic_order_entry_endpoint_does_not_exist()
    {
        var client = _factory.CreateClient();
        var bootstrap = await client.GetFromJsonAsync<Bootstrap>("/api/v1/system/bootstrap");
        Assert.NotNull(bootstrap);
        client.DefaultRequestHeaders.Add("X-CSRF-Token", bootstrap!.CsrfToken);
        var response = await client.PostAsJsonAsync("/api/v1/orders", new { instrument = "NQ 06-26", qty = 1 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task State_changing_request_requires_csrf()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/groups/demo/pause-new-entries", new { });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Evil_origin_is_rejected()
    {
        var client = _factory.CreateClient();
        var bootstrap = await client.GetFromJsonAsync<Bootstrap>("/api/v1/system/bootstrap");
        client.DefaultRequestHeaders.Add("X-CSRF-Token", bootstrap!.CsrfToken);
        client.DefaultRequestHeaders.Add("Origin", "https://evil.example");
        var response = await client.PostAsJsonAsync("/api/v1/groups/demo/disable", new { });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_is_served()
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/");
        Assert.Contains("TradeCopia", html);
        Assert.Contains("Alpha", html);
    }

    [Fact]
    public void Reliability_percentiles_are_defined()
    {
        var stats = ReliabilityCalculator.FromSamples(10, 9, 1, 0, new[] { 1d, 2d, 3d, 4d, 10d });
        Assert.Equal(10, stats.ActionsAttempted);
        Assert.NotNull(stats.DecisionLatencyP95Ms);
        Assert.True(stats.DecisionLatencyP95Ms >= stats.DecisionLatencyP50Ms);
    }

    private sealed class Bootstrap
    {
        public string CsrfToken { get; set; } = "";
    }
}
