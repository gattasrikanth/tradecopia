using System.Net;
using System.Text.Json;
using TradeCopia.ControlPlane;
using TradeCopia.ControlPlane.Commands;
using TradeCopia.ControlPlane.Demo;
using TradeCopia.ControlPlane.Security;
using TradeCopia.Persistence;
using TradeCopia.Protocol;

var options = ControlPlaneOptions.FromArgs(args);
if (!LoopbackGuard.IsLoopbackBind(options.BindAddress))
{
    throw new InvalidOperationException("Control plane may bind only to loopback.");
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = options.WebRoot
});
if (string.Equals(builder.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
{
    options = new ControlPlaneOptions
    {
        DemoMode = true,
        Port = ControlPlaneOptions.DefaultPort,
        DataDirectory = Path.Combine(Path.GetTempPath(), "tradecopia-tests", Guid.NewGuid().ToString("N"))
    };
}

if (!string.Equals(builder.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
{
    builder.WebHost.ConfigureKestrel(kestrel =>
    {
        kestrel.Listen(IPAddress.Loopback, options.Port);
        kestrel.Limits.MaxRequestBodySize = 64 * 1024;
    });
}
builder.Services.AddSingleton(options);
builder.Services.AddSingleton<SessionService>();
builder.Services.AddSingleton<DemoCatalog>();
builder.Services.AddSingleton<ConfirmationStore>();
builder.Services.AddSingleton<EngineLink>();
builder.Services.AddSingleton(_ => new LocalDatabase(options.DataDirectory, "control.db"));

var app = builder.Build();
app.UseMiddleware<SecurityMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api/v1");
api.MapGet("/system/status", (ControlPlaneOptions opt, DemoCatalog demo, EngineLink engine) =>
    Results.Json(new
    {
        opt.Port,
        opt.BindAddress,
        opt.DemoMode,
        engineConnected = engine.IsConnected,
        details = demo.SystemStatus()
    }));
api.MapGet("/system/version", () => Results.Json(new { product = "TradeCopia", version = "0.1.0-alpha", commit = "local" }));
api.MapGet("/system/health", () => Results.Json(new { status = "ok", copying = "disabled" }));
api.MapGet("/system/capabilities", () => Results.Json(new
{
    orderEntryApi = false,
    remoteAccess = false,
    telemetry = false,
    copyModes = new[] { "OrderMirror" }
}));
api.MapGet("/system/privacy", (DemoCatalog demo) => Results.Json(demo.Privacy()));
api.MapGet("/system/bootstrap", (SessionService session) => Results.Json(new { csrfToken = session.CsrfToken }));

api.MapGet("/accounts", (DemoCatalog demo) => Results.Json(demo.Accounts()));
api.MapGet("/groups", (DemoCatalog demo) => Results.Json(demo.Groups()));
api.MapGet("/live/trades", (DemoCatalog demo) => Results.Json(demo.LiveTrades()));
api.MapGet("/live/orders", (DemoCatalog demo) => Results.Json(demo.LiveTrades()));
api.MapGet("/live/divergences", (DemoCatalog demo) => Results.Json(demo.Divergences()));
api.MapGet("/live/health", () => Results.Json(new { groupHealth = "UNKNOWN", reason = "Engine disconnected. Unknown is never healthy." }));
api.MapGet("/journal/trades", (DemoCatalog demo) => Results.Json(demo.Journal()));
api.MapGet("/analytics/overview", (DemoCatalog demo) => Results.Json(demo.Analytics()));
api.MapGet("/analytics/latency", (DemoCatalog demo) => Results.Json(demo.Analytics()));
api.MapGet("/analytics/reliability", (DemoCatalog demo) => Results.Json(demo.Analytics()));
api.MapGet("/diagnostics/status", (DemoCatalog demo) => Results.Json(demo.Diagnostics()));
api.MapGet("/diagnostics/errors", () => Results.Json(Array.Empty<object>()));

api.MapPost("/groups/{groupId}/pause-new-entries", (string groupId, EngineLink engine) =>
    FailClosedCommand(engine, ProtocolMessageTypes.PauseNewEntries, groupId));
api.MapPost("/groups/{groupId}/disable", (string groupId, EngineLink engine) =>
    FailClosedCommand(engine, ProtocolMessageTypes.DisableGroup, groupId));

api.MapPost("/flatten/prepare", (ConfirmationStore confirmations) =>
{
    var preview = new
    {
        accounts = new[] { "SIM-FOLLOWER-01", "SIM-FOLLOWER-02" },
        instruments = new[] { "NQ 06-26", "MNQ 06-26" },
        warning = "This would flatten follower exposure only. Engine is disconnected; execute will not place orders."
    };
    var record = confirmations.Prepare("flatten", preview, "flatten-followers-v1");
    return Results.Json(new { confirmationId = record.Id, expiresAt = record.ExpiresAt, preview });
});
api.MapPost("/flatten/execute", async (HttpContext http, ConfirmationStore confirmations) =>
{
    var body = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(http.Request.Body) ?? new();
    if (!body.TryGetValue("confirmationId", out var id)
        || !confirmations.TryConsume(id, "flatten", "flatten-followers-v1", out _))
    {
        return Results.Json(new { accepted = false, error = "stale-or-invalid-confirmation" }, statusCode: 409);
    }

    return Results.Json(new { accepted = false, error = "engine-disconnected", submitted = false });
});

api.MapPost("/reconcile/prepare", (ConfirmationStore confirmations) =>
{
    var preview = new { actions = Array.Empty<object>(), unresolvable = new[] { "Engine snapshot unavailable." } };
    var record = confirmations.Prepare("reconcile", preview, "reconcile-v1");
    return Results.Json(new { confirmationId = record.Id, preview });
});
api.MapPost("/reconcile/execute", () => Results.Json(new { accepted = false, submitted = false, error = "engine-disconnected" }));

api.MapMethods("/orders", new[] { "GET", "POST", "PUT", "PATCH", "DELETE" }, () =>
    Results.Json(new { error = "no-generic-order-entry" }, statusCode: StatusCodes.Status404NotFound));

api.MapGet("/events/stream", async (HttpContext http, DemoCatalog demo, CancellationToken token) =>
{
    http.Response.Headers.ContentType = "text/event-stream";
    var payload = JsonSerializer.Serialize(new { type = "snapshot", trades = demo.LiveTrades() });
    await http.Response.WriteAsync("event: snapshot\ndata: " + payload + "\n\n", token);
    await http.Response.Body.FlushAsync(token);
});

app.MapFallbackToFile("index.html");

app.Run();

static IResult FailClosedCommand(EngineLink engine, string messageType, string groupId)
{
    if (!engine.IsConnected)
    {
        return Results.Json(new
        {
            accepted = false,
            error = "engine-disconnected",
            submitted = false,
            groupId,
            command = messageType
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var result = engine.Send(messageType);
    if (!result.Accepted)
    {
        return Results.Json(new
        {
            accepted = false,
            error = result.Reason,
            submitted = false,
            groupId,
            command = messageType
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Json(new { accepted = true, command = messageType, groupId, submitted = false });
}

public partial class Program;
