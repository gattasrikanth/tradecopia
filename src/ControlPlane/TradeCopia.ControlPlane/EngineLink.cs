using TradeCopia.Protocol;

namespace TradeCopia.ControlPlane;

/// <summary>
/// Companion-side engine connection. Starts disconnected. Commands fail closed
/// until a named-pipe handshake succeeds.
/// </summary>
public sealed class EngineLink : IDisposable
{
    private readonly object _gate = new();
    private NamedPipeCompanionClient? _client;

    public bool IsConnected
    {
        get
        {
            lock (_gate)
            {
                return _client != null && _client.IsConnected;
            }
        }
    }

    public bool TryAttach(string pipeName, int timeoutMs)
    {
        lock (_gate)
        {
            _client?.Dispose();
            var client = new NamedPipeCompanionClient(pipeName);
            if (!client.TryConnect(timeoutMs))
            {
                client.Dispose();
                _client = null;
                return false;
            }

            var hello = new ProtocolEnvelope(
                ProtocolLimits.CurrentVersion,
                Guid.NewGuid().ToString("N"),
                ProtocolMessageTypes.Hello,
                DateTime.UtcNow,
                string.Empty,
                "{}");
            var result = client.Send(hello);
            if (!result.Accepted)
            {
                client.Dispose();
                _client = null;
                return false;
            }

            _client = client;
            return true;
        }
    }

    public ProtocolValidationResult Send(string messageType)
    {
        lock (_gate)
        {
            if (_client == null || !_client.IsConnected)
            {
                return new ProtocolValidationResult(
                    false,
                    "engine-disconnected",
                    new ProtocolEnvelope(
                        ProtocolLimits.CurrentVersion,
                        Guid.NewGuid().ToString("N"),
                        messageType,
                        DateTime.UtcNow,
                        string.Empty,
                        "{}"));
            }

            var envelope = new ProtocolEnvelope(
                ProtocolLimits.CurrentVersion,
                Guid.NewGuid().ToString("N"),
                messageType,
                DateTime.UtcNow,
                string.Empty,
                "{}");
            return _client.Send(envelope);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _client?.Dispose();
            _client = null;
        }
    }
}
