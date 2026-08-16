namespace TradeCopia.ControlPlane.Security;

public sealed class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ControlPlaneOptions _options;
    private readonly SessionService _session;
    private readonly IWebHostEnvironment _environment;

    public SecurityMiddleware(
        RequestDelegate next,
        ControlPlaneOptions options,
        SessionService session,
        IWebHostEnvironment environment)
    {
        _next = next;
        _options = options;
        _session = session;
        _environment = environment;
    }

    public async Task Invoke(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (host is not "127.0.0.1" and not "localhost")
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "invalid-host" });
            return;
        }

        var expectedPort = context.Request.Host.Port ?? _options.Port;
        if (!_environment.IsEnvironment("Testing")
            && context.Request.Host.Port.HasValue
            && context.Request.Host.Port.Value != _options.Port)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "invalid-host" });
            return;
        }

        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Cache-Control"] = "no-store";

        if (context.Request.Cookies[SessionService.SessionCookie] != _session.SessionId)
        {
            context.Response.Cookies.Append(SessionService.SessionCookie, _session.SessionId, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = false,
                Path = "/",
                IsEssential = true
            });
        }

        var method = context.Request.Method;
        if (HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method) || HttpMethods.IsPatch(method))
        {
            var origin = context.Request.Headers.Origin.ToString();
            if (!string.IsNullOrEmpty(origin)
                && !LoopbackGuard.IsAllowedOrigin(origin, expectedPort)
                && !origin.Contains("127.0.0.1", StringComparison.Ordinal)
                && !origin.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "invalid-origin" });
                return;
            }

            if (!_session.ValidCsrf(context.Request.Headers[SessionService.CsrfHeader].ToString()))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "invalid-csrf" });
                return;
            }
        }

        await _next(context);
    }
}
