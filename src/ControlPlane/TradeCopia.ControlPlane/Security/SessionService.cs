using System.Security.Cryptography;

namespace TradeCopia.ControlPlane.Security;

public sealed class SessionService
{
    public const string SessionCookie = "tc.sid";
    public const string CsrfHeader = "X-CSRF-Token";

    public SessionService()
    {
        SessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        CsrfToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    public string SessionId { get; }
    public string CsrfToken { get; }

    public bool ValidCsrf(string? presented)
    {
        return !string.IsNullOrEmpty(presented)
            && CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(presented),
                System.Text.Encoding.UTF8.GetBytes(CsrfToken));
    }
}
