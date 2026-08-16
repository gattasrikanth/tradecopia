using System.Net;
using System.Text.Json;
using TradeCopia.ControlPlane;
using TradeCopia.ControlPlane.Commands;
using TradeCopia.ControlPlane.Demo;
using TradeCopia.ControlPlane.Groups;
using TradeCopia.ControlPlane.Presentation;
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
Mutex? singleInstance = null;
if (string.Equals(builder.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
{
    options = new ControlPlaneOptions
    {
        DemoMode = true,
        Port = ControlPlaneOptions.DefaultPort,
        DataDirectory = Path.Combine(Path.GetTempPath(), "tradecopia-tests", Guid.NewGuid().ToString("N")),
        PipeName = TradeCopia.Protocol.EnginePipeName.FromMaterial("testing-" + Guid.NewGuid().ToString("N"))
    };
}
else if (options.Port == ControlPlaneOptions.DefaultPort)
{
    singleInstance = new Mutex(true, @"Local\TradeCopia.ControlPlane.v1", out var createdNew);
    if (!createdNew)
    {
        singleInstance.Dispose();
        return;
    }
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
builder.Services.AddSingleton(_ => new GroupConfigStore(options.DataDirectory));

var app = builder.Build();
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var engineLink = app.Services.GetRequiredService<EngineLink>();
engineLink.StartRetryAttach(options.PipeName, lifetime.ApplicationStopping);
app.UseMiddleware<SecurityMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api/v1");
api.MapGet("/system/status", (ControlPlaneOptions opt, EngineLink engine, GroupConfigStore store) =>
{
    var active = store.List().FirstOrDefault(g => g.Status == "active");
    EngineAccountRecord? leader = null;
    var followers = new List<EngineAccountRecord>();
    if (active != null)
    {
        leader = engine.Accounts.FirstOrDefault(a => a.StableKey == active.LeaderKey);
        followers = engine.Accounts.Where(a => active.FollowerKeys.Contains(a.StableKey)).ToList();
    }

    var topologyOk = active != null && GroupConfigStore.ValidateCore(active, engine.Accounts) == null;
    var preflight = CustomerPresentation.Preflight(
        engine.IsConnected,
        engine.CopyingEnabled,
        leader,
        followers,
        active?.Sizing ?? CustomerPresentation.DefaultSizing(),
        topologyOk,
        blockingDivergence: false);
    var alerts = new List<CustomerAlert>();
    if (!engine.CopyingEnabled)
    {
        alerts.Add(new CustomerAlert
        {
            Severity = "warning",
            Title = "Copying starts disabled",
            Message = "This dashboard cannot place discretionary trades."
        });
    }

    return Results.Json(new
    {
        opt.Port,
        opt.BindAddress,
        opt.DemoMode,
        engineConnected = engine.IsConnected,
        engineState = engine.EngineState,
        copyingEnabled = engine.CopyingEnabled,
        presentation = new
        {
            engine = CustomerPresentation.EngineStateLabel(engine.IsConnected, engine.EngineState),
            copying = CustomerPresentation.CopyingLabel(engine.CopyingEnabled),
            environment = "SIM / DEMO ONLY",
            headline = CustomerPresentation.StatusHeadline(engine.IsConnected, engine.CopyingEnabled, preflight.Ready),
            alerts,
            alertHtml = CustomerPresentation.AlertHtml(alerts),
            preflight
        },
        details = new
        {
            product = "TradeCopia",
            status = "Development",
            releaseLabel = "Alpha — SIM only recommended",
            engineState = engine.EngineState,
            engineConnected = engine.IsConnected,
            ninjaTraderDetected = System.Diagnostics.Process.GetProcessesByName("NinjaTrader").Length > 0,
            demoMode = opt.DemoMode,
            copyingEnabled = engine.CopyingEnabled,
            bindAddress = opt.BindAddress,
            privacy = "local-only",
            telemetry = "none"
        }
    });
});
api.MapGet("/system/version", () => Results.Json(new { product = "TradeCopia", version = "0.1.0-alpha.6", commit = "local" }));
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

api.MapGet("/accounts", (EngineLink engine) =>
{
    if (!engine.IsConnected)
    {
        return Results.Json(new
        {
            source = "disconnected",
            accounts = Array.Empty<object>(),
            error = "engine-disconnected",
            message = CustomerPresentation.DisconnectedMessage()
        });
    }

    var choices = CustomerPresentation.Choices(engine.Accounts, null);
    return Results.Json(new
    {
        source = "engine",
        message = (string?)null,
        accounts = engine.Accounts.Select(a =>
        {
            var choice = choices.First(c => c.StableKey == a.StableKey);
            return new
            {
                a.StableKey,
                a.DisplayName,
                a.Provider,
                a.OfficialMode,
                a.IsDemo,
                safetyClass = a.SafetyClass.ToString(),
                safetyLabel = choice.SafetyLabel,
                eligibilityLabel = choice.EligibilityLabel,
                connectionLabel = choice.ConnectionLabel,
                lockReason = choice.LockReason,
                availableAsLeader = choice.AvailableAsLeader,
                availableAsFollower = choice.AvailableAsFollower,
                a.Selectable
            };
        })
    });
});
api.MapGet("/groups", (GroupConfigStore store, EngineLink engine) =>
{
    var accounts = engine.Accounts;
    return Results.Json(store.List().Select(g => new
    {
        g.Id,
        g.Name,
        g.LeaderKey,
        g.FollowerKeys,
        g.Sizing,
        sizingLabel = CustomerPresentation.SizingLabel(g.Sizing),
        g.Status,
        g.Version,
        leaderDisplayName = CustomerPresentation.DisplayNameFor(accounts, g.LeaderKey),
        followerDisplayNames = g.FollowerKeys.Select(k => CustomerPresentation.DisplayNameFor(accounts, k)).ToList(),
        pauseHelp = CustomerPresentation.PauseHelp(),
        disableHelp = CustomerPresentation.DisableHelp()
    }));
});
api.MapPost("/groups", async (HttpContext http, GroupConfigStore store) =>
{
    var body = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(http.Request.Body) ?? new();
    var name = body.TryGetValue("name", out var n) ? n.GetString() ?? "" : "";
    var leader = body.TryGetValue("leaderKey", out var l) ? l.GetString() ?? "" : "";
    var followers = new List<string>();
    if (body.TryGetValue("followerKeys", out var f) && f.ValueKind == JsonValueKind.Array)
    {
        foreach (var item in f.EnumerateArray())
        {
            var v = item.GetString();
            if (!string.IsNullOrWhiteSpace(v))
            {
                followers.Add(v);
            }
        }
    }

    var sizing = body.TryGetValue("sizing", out var sz) ? sz.GetString() ?? CustomerPresentation.DefaultSizing() : CustomerPresentation.DefaultSizing();
    var created = store.CreateDraft(name, leader, followers, sizing);
    return Results.Json(created);
});
api.MapPost("/groups/save-and-activate", async (HttpContext http, GroupConfigStore store, EngineLink engine) =>
{
    if (!engine.IsConnected)
    {
        return Results.Json(new { ok = false, error = "engine-disconnected" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var body = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(http.Request.Body) ?? new();
    var id = body.TryGetValue("id", out var idEl) ? idEl.GetString() : "";
    var name = body.TryGetValue("name", out var n) ? n.GetString() ?? "" : "";
    var leader = body.TryGetValue("leaderKey", out var l) ? l.GetString() ?? "" : "";
    var sizing = body.TryGetValue("sizing", out var sz) ? sz.GetString() ?? CustomerPresentation.DefaultSizing() : CustomerPresentation.DefaultSizing();
    var followers = new List<string>();
    if (body.TryGetValue("followerKeys", out var f) && f.ValueKind == JsonValueKind.Array)
    {
        foreach (var item in f.EnumerateArray())
        {
            var v = item.GetString();
            if (!string.IsNullOrWhiteSpace(v))
            {
                followers.Add(v);
            }
        }
    }

    var result = store.SaveAndActivate(id, name, leader, followers, sizing, engine.Accounts);
    if (!result.Ok || result.Group == null)
    {
        return Results.Json(new { ok = false, error = result.Reason, group = result.Group }, statusCode: StatusCodes.Status400BadRequest);
    }

    var payload = "{\"leader\":\"" + result.Group.LeaderKey + "\",\"followers\":[" +
        string.Join(",", result.Group.FollowerKeys.Select(k => "\"" + k + "\"")) + "]}";
    var pipe = engine.Send(ProtocolMessageTypes.ActivateConfig, payload);
    if (!pipe.Accepted)
    {
        return Results.Json(new { ok = false, error = pipe.Reason, group = result.Group }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Json(new { ok = true, group = result.Group });
});
api.MapPost("/groups/{groupId}/validate", (string groupId, GroupConfigStore store, EngineLink engine) =>
{
    if (!engine.IsConnected)
    {
        return Results.Json(new { ok = false, error = "engine-disconnected" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var result = store.Validate(groupId, engine.Accounts);
    return result.Ok
        ? Results.Json(new { ok = true, group = result.Group })
        : Results.Json(new { ok = false, error = result.Reason, group = result.Group }, statusCode: StatusCodes.Status400BadRequest);
});
api.MapPost("/groups/{groupId}/activate", async (string groupId, HttpContext http, GroupConfigStore store, EngineLink engine) =>
{
    if (!engine.IsConnected)
    {
        return Results.Json(new { ok = false, error = "engine-disconnected" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var body = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(http.Request.Body) ?? new();
    var expected = body.TryGetValue("expectedVersion", out var ev) && ev.TryGetInt32(out var v) ? v : -1;
    var result = store.Activate(groupId, expected, engine.Accounts);
    if (!result.Ok || result.Group == null)
    {
        return Results.Json(new { ok = false, error = result.Reason, group = result.Group }, statusCode: StatusCodes.Status409Conflict);
    }

    var payload = "{\"leader\":\"" + result.Group.LeaderKey + "\",\"followers\":[" +
        string.Join(",", result.Group.FollowerKeys.Select(k => "\"" + k + "\"")) + "]}";
    var pipe = engine.Send(ProtocolMessageTypes.ActivateConfig, payload);
    if (!pipe.Accepted)
    {
        return Results.Json(new { ok = false, error = pipe.Reason }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Json(new { ok = true, group = result.Group });
});
api.MapPost("/groups/{groupId}/enable", (string groupId, GroupConfigStore store, EngineLink engine) =>
{
    var group = store.Get(groupId);
    if (group == null || group.Status != "active")
    {
        return Results.Json(new { ok = false, error = "not-active" }, statusCode: StatusCodes.Status400BadRequest);
    }

    return FailClosedCommand(engine, ProtocolMessageTypes.EnableCopying, groupId);
});
api.MapGet("/live/trades", (EngineLink engine) => Results.Json(Array.Empty<object>()));
api.MapGet("/live/orders", (EngineLink engine) => Results.Json(Array.Empty<object>()));
api.MapGet("/live/divergences", (EngineLink engine) => Results.Json(Array.Empty<object>()));
api.MapGet("/live/health", (EngineLink engine, GroupConfigStore store) =>
{
    var active = store.List().FirstOrDefault(g => g.Status == "active");
    var ready = engine.IsConnected && active != null && GroupConfigStore.ValidateCore(active, engine.Accounts) == null;
    return Results.Json(new
    {
        groupHealth = engine.IsConnected
            ? (ready ? "Ready" : "Blocked")
            : "Engine Disconnected",
        reason = engine.IsConnected
            ? (ready
                ? "Active group is valid and the engine is connected."
                : "Engine connected. Group is not ready for non-live copying.")
            : "Engine disconnected."
    });
});
api.MapGet("/journal/trades", (DemoCatalog demo) => Results.Json(demo.Journal()));
api.MapGet("/analytics/overview", (DemoCatalog demo) => Results.Json(demo.Analytics()));
api.MapGet("/analytics/latency", (DemoCatalog demo) => Results.Json(demo.Analytics()));
api.MapGet("/analytics/reliability", (DemoCatalog demo) => Results.Json(demo.Analytics()));
api.MapGet("/diagnostics/status", (EngineLink engine, ControlPlaneOptions opt) => Results.Json(new
{
    controlPlane = "running",
    engine = engine.IsConnected ? "connected" : "disconnected",
    engineState = engine.EngineState,
    copyingEnabled = engine.CopyingEnabled,
    namedPipe = opt.PipeName,
    sqlite = "ok",
    lastError = engine.IsConnected
        ? "None."
        : "Engine not connected. Dashboard is control plane only.",
    orderSubmission = engine.CopyingEnabled ? "enabled" : "disabled"
}));
api.MapGet("/diagnostics/errors", () => Results.Json(Array.Empty<object>()));

api.MapPost("/groups/{groupId}/pause-new-entries", (string groupId, EngineLink engine) =>
    FailClosedCommand(engine, ProtocolMessageTypes.PauseNewEntries, groupId));
api.MapPost("/groups/{groupId}/disable", (string groupId, EngineLink engine) =>
    FailClosedCommand(engine, ProtocolMessageTypes.DisableGroup, groupId));
api.MapPost("/groups/{groupId}/resume-new-entries", (string groupId, EngineLink engine) =>
    FailClosedCommand(engine, ProtocolMessageTypes.ResumeNewEntries, groupId));

api.MapPost("/flatten/prepare", (ConfirmationStore confirmations) =>
{
    var preview = new
    {
        accounts = Array.Empty<string>(),
        instruments = Array.Empty<string>(),
        warning = "Flatten applies to follower exposure only after an active group exists."
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

api.MapGet("/events/stream", async (HttpContext http, CancellationToken token) =>
{
    http.Response.Headers.ContentType = "text/event-stream";
    var payload = JsonSerializer.Serialize(new { type = "snapshot", trades = Array.Empty<object>() });
    await http.Response.WriteAsync("event: snapshot\ndata: " + payload + "\n\n", token);
    await http.Response.Body.FlushAsync(token);
});

app.MapFallbackToFile("index.html");

try
{
    app.Run();
}
finally
{
    singleInstance?.Dispose();
}

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
