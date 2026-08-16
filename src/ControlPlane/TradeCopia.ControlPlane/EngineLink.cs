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
    private string _engineState = "Unknown";
    private bool _copyingEnabled;
    private IReadOnlyList<EngineAccountRecord> _accounts = Array.Empty<EngineAccountRecord>();
    private IReadOnlyList<LiveCopyRecord> _liveTrades = Array.Empty<LiveCopyRecord>();
    private IReadOnlyList<LiveDivergenceRecord> _liveDivergences = Array.Empty<LiveDivergenceRecord>();

    public string EngineState
    {
        get { lock (_gate) { return _engineState; } }
    }

    public bool CopyingEnabled
    {
        get { lock (_gate) { return _copyingEnabled; } }
    }

    public IReadOnlyList<EngineAccountRecord> Accounts
    {
        get { lock (_gate) { return _accounts; } }
    }

    public IReadOnlyList<LiveCopyRecord> LiveTrades
    {
        get { lock (_gate) { return _liveTrades; } }
    }

    public IReadOnlyList<LiveDivergenceRecord> LiveDivergences
    {
        get { lock (_gate) { return _liveDivergences; } }
    }

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
                _accounts = Array.Empty<EngineAccountRecord>();
                _liveTrades = Array.Empty<LiveCopyRecord>();
                _liveDivergences = Array.Empty<LiveDivergenceRecord>();
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
            RememberSnapshot(result.Reply.PayloadJson);
            var snap = client.Send(new ProtocolEnvelope(
                ProtocolLimits.CurrentVersion,
                Guid.NewGuid().ToString("N"),
                ProtocolMessageTypes.RequestSnapshot,
                DateTime.UtcNow,
                string.Empty,
                "{}"));
            if (snap.Accepted)
            {
                RememberSnapshot(snap.Reply.PayloadJson);
            }

            return true;
        }
    }

    public ProtocolValidationResult Send(string messageType, string payloadJson = "{}")
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
                        payloadJson ?? "{}"));
            }

            var envelope = new ProtocolEnvelope(
                ProtocolLimits.CurrentVersion,
                Guid.NewGuid().ToString("N"),
                messageType,
                DateTime.UtcNow,
                string.Empty,
                payloadJson ?? "{}");
            var result = _client.Send(envelope);
            if (result.Accepted)
            {
                RememberSnapshot(result.Reply.PayloadJson);
            }

            return result;
        }
    }

    public void StartRetryAttach(string pipeName, CancellationToken token)
    {
        Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                if (!IsConnected)
                {
                    TryAttach(pipeName, 250);
                }
                else
                {
                    Send(ProtocolMessageTypes.RequestSnapshot);
                }

                token.WaitHandle.WaitOne(1000);
            }
        }, token);
    }

    private void RememberSnapshot(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return;
        }

        if (payload.Contains("\"engineState\":\"PausedNewEntries\"", StringComparison.Ordinal))
        {
            _engineState = "PausedNewEntries";
        }
        else if (payload.Contains("\"engineState\":\"Enabled\"", StringComparison.Ordinal))
        {
            _engineState = "Enabled";
        }
        else if (payload.Contains("\"engineState\":\"Disabled\"", StringComparison.Ordinal))
        {
            _engineState = "Disabled";
        }

        if (payload.Contains("\"copyingEnabled\":true", StringComparison.Ordinal))
        {
            _copyingEnabled = true;
        }
        else if (payload.Contains("\"copyingEnabled\":false", StringComparison.Ordinal))
        {
            _copyingEnabled = false;
        }

        if (payload.Contains("\"accounts\":", StringComparison.Ordinal))
        {
            _accounts = EngineAccountRecord.ParseArray(payload);
        }

        if (payload.Contains("\"liveTrades\":", StringComparison.Ordinal))
        {
            _liveTrades = LiveCopyRecord.ParseArray(payload);
        }

        if (payload.Contains("\"liveDivergences\":", StringComparison.Ordinal))
        {
            _liveDivergences = LiveCopyRecord.ParseDivergences(payload);
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
