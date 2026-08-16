using System.Net;

namespace TradeCopia.ControlPlane.Security;

public static class LoopbackGuard
{
    public static bool IsAllowedHost(string? hostHeader, int expectedPort)
    {
        if (string.IsNullOrWhiteSpace(hostHeader))
        {
            return false;
        }

        if (!Uri.TryCreate("http://" + hostHeader, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Port != expectedPort && !(uri.IsDefaultPort && expectedPort == 80))
        {
            return false;
        }

        return uri.Host is "127.0.0.1" or "localhost";
    }

    public static bool IsAllowedOrigin(string? origin, int expectedPort)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (uri.Port != expectedPort)
        {
            return false;
        }

        return uri.Host is "127.0.0.1" or "localhost";
    }

    public static bool IsLoopbackBind(string address)
    {
        return address is "127.0.0.1" or "::1"
            || IPAddress.TryParse(address, out var ip) && IPAddress.IsLoopback(ip);
    }
}
