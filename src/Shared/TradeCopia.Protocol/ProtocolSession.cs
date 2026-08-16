using System;
using System.Collections.Generic;

namespace TradeCopia.Protocol
{
    public sealed class ProtocolValidationResult
    {
        public ProtocolValidationResult(bool accepted, string reason, ProtocolEnvelope reply)
        {
            Accepted = accepted;
            Reason = reason ?? string.Empty;
            Reply = reply;
        }

        public bool Accepted { get; }
        public string Reason { get; }
        public ProtocolEnvelope Reply { get; }
    }

    public static class ProtocolCatalog
    {
        private static readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            ProtocolMessageTypes.Hello,
            ProtocolMessageTypes.EngineHello,
            ProtocolMessageTypes.RequestSnapshot,
            ProtocolMessageTypes.EngineStateSnapshot,
            ProtocolMessageTypes.ActivateConfig,
            ProtocolMessageTypes.PauseNewEntries,
            ProtocolMessageTypes.ResumeNewEntries,
            ProtocolMessageTypes.DisableGroup,
            ProtocolMessageTypes.EnableCopying,
            ProtocolMessageTypes.PrepareFlatten,
            ProtocolMessageTypes.ExecuteFlatten,
            ProtocolMessageTypes.Heartbeat
        };

        public static bool IsAllowed(string messageType)
        {
            return !string.IsNullOrEmpty(messageType) && Allowed.Contains(messageType);
        }

        public static bool IsForbiddenDiscretionaryOrder(string messageType)
        {
            if (string.IsNullOrEmpty(messageType))
            {
                return false;
            }

            return string.Equals(messageType, "ExecuteOrder", StringComparison.Ordinal)
                || string.Equals(messageType, "PlaceOrder", StringComparison.Ordinal)
                || string.Equals(messageType, "SubmitOrder", StringComparison.Ordinal);
        }
    }

    public static class ProtocolValidator
    {
        public static string? RejectReason(ProtocolEnvelope envelope)
        {
            if (envelope == null)
            {
                return "null-envelope";
            }

            if (!ProtocolFraming.IsCompatible(envelope.ProtocolVersion))
            {
                return "incompatible-version";
            }

            if (string.IsNullOrWhiteSpace(envelope.MessageId) || envelope.MessageId.Length > ProtocolLimits.MaxStringChars)
            {
                return "invalid-message-id";
            }

            if (string.IsNullOrWhiteSpace(envelope.MessageType) || envelope.MessageType.Length > ProtocolLimits.MaxStringChars)
            {
                return "invalid-message-type";
            }

            if (envelope.SessionId != null && envelope.SessionId.Length > ProtocolLimits.MaxStringChars)
            {
                return "invalid-session-id";
            }

            if (envelope.PayloadJson != null && envelope.PayloadJson.Length > ProtocolLimits.MaxMessageBytes)
            {
                return "payload-too-large";
            }

            if (ProtocolCatalog.IsForbiddenDiscretionaryOrder(envelope.MessageType))
            {
                return "forbidden-execute-order";
            }

            if (!ProtocolCatalog.IsAllowed(envelope.MessageType))
            {
                return "unknown-message-type";
            }

            return null;
        }
    }

    public sealed class ProtocolSession
    {
        private bool _handshook;
        private string _sessionId;
        private bool _connected = true;
        private string _engineState = "Disabled";
        private bool _copyingEnabled;
        private readonly List<EngineAccountRecord> _accounts = new List<EngineAccountRecord>();
        private readonly HashSet<string> _activeAccountKeys = new HashSet<string>(StringComparer.Ordinal);
        private string _leaderKey = string.Empty;
        private readonly List<string> _followerKeys = new List<string>();
        private string _activeConfigVersion = string.Empty;

        public ProtocolSession()
        {
            _sessionId = Guid.NewGuid().ToString("N");
        }

        public bool IsHandshook => _handshook;
        public bool IsConnected => _connected;
        public string SessionId => _sessionId;
        public string EngineState => _engineState;
        public bool CopyingEnabled => _copyingEnabled;
        public IReadOnlyList<EngineAccountRecord> Accounts => _accounts;
        public string ActiveConfigVersion => _activeConfigVersion;
        public string ActiveLeaderKey => _leaderKey;
        public IReadOnlyList<string> ActiveFollowerKeys => _followerKeys;

        public void ReplaceAccounts(IEnumerable<EngineAccountRecord> accounts)
        {
            _accounts.Clear();
            if (accounts == null)
            {
                return;
            }

            foreach (var account in accounts)
            {
                if (account != null && !string.IsNullOrEmpty(account.StableKey))
                {
                    _accounts.Add(account);
                }
            }
        }

        public void Disconnect()
        {
            _connected = false;
            _handshook = false;
        }

        public void Reconnect()
        {
            _connected = true;
            _handshook = false;
            _sessionId = Guid.NewGuid().ToString("N");
        }

        public string SnapshotJson()
        {
            var accounts = new System.Text.StringBuilder();
            accounts.Append('[');
            for (var i = 0; i < _accounts.Count; i++)
            {
                if (i > 0)
                {
                    accounts.Append(',');
                }

                accounts.Append(_accounts[i].ToJson());
            }

            accounts.Append(']');
            return "{\"engineState\":\"" + _engineState
                + "\",\"copyingEnabled\":" + (_copyingEnabled ? "true" : "false")
                + ",\"activeConfigVersion\":\"" + _activeConfigVersion
                + "\",\"accounts\":" + accounts + "}";
        }

        public ProtocolValidationResult Handle(ProtocolEnvelope incoming)
        {
            if (!_connected)
            {
                return Reject(incoming, "disconnected");
            }

            var reason = ProtocolValidator.RejectReason(incoming);
            if (reason != null)
            {
                return Reject(incoming, reason);
            }

            if (string.Equals(incoming.MessageType, ProtocolMessageTypes.Hello, StringComparison.Ordinal))
            {
                _handshook = true;
                return new ProtocolValidationResult(
                    true,
                    "handshake-ok",
                    new ProtocolEnvelope(
                        ProtocolLimits.CurrentVersion,
                        Guid.NewGuid().ToString("N"),
                        ProtocolMessageTypes.EngineHello,
                        DateTime.UtcNow,
                        _sessionId,
                        "{\"capabilities\":[\"OrderMirror\"],\"engineState\":\"" + _engineState + "\",\"copyingEnabled\":" + (_copyingEnabled ? "true" : "false") + "}"));
            }

            if (!_handshook)
            {
                return Reject(incoming, "handshake-required");
            }

            if (string.Equals(incoming.MessageType, ProtocolMessageTypes.RequestSnapshot, StringComparison.Ordinal))
            {
                return SnapshotReply("snapshot");
            }

            if (string.Equals(incoming.MessageType, ProtocolMessageTypes.PauseNewEntries, StringComparison.Ordinal))
            {
                _engineState = "PausedNewEntries";
                _copyingEnabled = false;
                return SnapshotReply("paused");
            }

            if (string.Equals(incoming.MessageType, ProtocolMessageTypes.DisableGroup, StringComparison.Ordinal))
            {
                _engineState = "Disabled";
                _copyingEnabled = false;
                return SnapshotReply("disabled");
            }

            if (string.Equals(incoming.MessageType, ProtocolMessageTypes.ResumeNewEntries, StringComparison.Ordinal))
            {
                if (!string.Equals(_engineState, "PausedNewEntries", StringComparison.Ordinal))
                {
                    return Reject(incoming, "resume-only-from-pause");
                }

                _engineState = "Enabled";
                _copyingEnabled = true;
                return SnapshotReply("resumed");
            }

            if (string.Equals(incoming.MessageType, ProtocolMessageTypes.EnableCopying, StringComparison.Ordinal))
            {
                var enableBlock = RejectEnableReason();
                if (enableBlock != null)
                {
                    return Reject(incoming, enableBlock);
                }

                _engineState = "Enabled";
                _copyingEnabled = true;
                return SnapshotReply("enabled");
            }

            if (string.Equals(incoming.MessageType, ProtocolMessageTypes.ActivateConfig, StringComparison.Ordinal))
            {
                _activeAccountKeys.Clear();
                _leaderKey = string.Empty;
                _followerKeys.Clear();
                var ordered = new List<string>();
                foreach (var key in ExtractActiveKeys(incoming.PayloadJson))
                {
                    if (_activeAccountKeys.Add(key))
                    {
                        ordered.Add(key);
                    }
                }

                if (ordered.Count < 2)
                {
                    _activeAccountKeys.Clear();
                    return Reject(incoming, "leader-and-follower-required");
                }

                _leaderKey = ordered[0];
                for (var i = 1; i < ordered.Count; i++)
                {
                    _followerKeys.Add(ordered[i]);
                }

                var activateBlock = RejectEnableReason();
                if (activateBlock != null)
                {
                    _activeAccountKeys.Clear();
                    _leaderKey = string.Empty;
                    _followerKeys.Clear();
                    return Reject(incoming, activateBlock);
                }

                _activeConfigVersion = Guid.NewGuid().ToString("N");
                _engineState = "Disabled";
                _copyingEnabled = false;
                return SnapshotReply("activated");
            }

            return new ProtocolValidationResult(
                true,
                "accepted",
                new ProtocolEnvelope(
                    ProtocolLimits.CurrentVersion,
                    Guid.NewGuid().ToString("N"),
                    ProtocolMessageTypes.Heartbeat,
                    DateTime.UtcNow,
                    _sessionId,
                    SnapshotJson()));
        }

        private string? RejectEnableReason()
        {
            if (_activeAccountKeys.Count == 0)
            {
                return "no-active-group";
            }

            foreach (var key in _activeAccountKeys)
            {
                EngineAccountRecord? found = null;
                for (var i = 0; i < _accounts.Count; i++)
                {
                    if (string.Equals(_accounts[i].StableKey, key, StringComparison.Ordinal))
                    {
                        found = _accounts[i];
                        break;
                    }
                }

                if (found == null)
                {
                    return "unknown-account";
                }

                if (!TradeCopia.Domain.Safety.AccountSafetyClassifier.AlphaMayEnable(found.SafetyClass))
                {
                    return "non-live-required:" + found.SafetyClass;
                }
            }

            return null;
        }

        private static IEnumerable<string> ExtractActiveKeys(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                yield break;
            }

            var leader = ReadQuoted(payload, "leader");
            if (!string.IsNullOrEmpty(leader) && !leader.StartsWith("[", StringComparison.Ordinal))
            {
                yield return leader;
            }

            var followers = ReadQuoted(payload, "followers");
            if (followers.StartsWith("[", StringComparison.Ordinal))
            {
                var inner = followers.Trim('[', ']');
                foreach (var part in inner.Split(','))
                {
                    var key = part.Trim().Trim('"');
                    if (!string.IsNullOrEmpty(key))
                    {
                        yield return key;
                    }
                }
            }
        }

        private static string ReadQuoted(string json, string name)
        {
            var key = "\"" + name + "\":";
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0)
            {
                return string.Empty;
            }

            i += key.Length;
            if (i < json.Length && json[i] == '"')
            {
                var end = json.IndexOf('"', i + 1);
                return end < 0 ? string.Empty : json.Substring(i + 1, end - i - 1);
            }

            if (i < json.Length && json[i] == '[')
            {
                var end = json.IndexOf(']', i);
                return end < 0 ? string.Empty : json.Substring(i, end - i + 1);
            }

            return string.Empty;
        }

        private ProtocolValidationResult SnapshotReply(string reason)
        {
            return new ProtocolValidationResult(
                true,
                reason,
                new ProtocolEnvelope(
                    ProtocolLimits.CurrentVersion,
                    Guid.NewGuid().ToString("N"),
                    ProtocolMessageTypes.EngineStateSnapshot,
                    DateTime.UtcNow,
                    _sessionId,
                    SnapshotJson()));
        }

        private static ProtocolValidationResult Reject(ProtocolEnvelope incoming, string reason)
        {
            var id = incoming != null ? incoming.MessageId : "none";
            return new ProtocolValidationResult(
                false,
                reason,
                new ProtocolEnvelope(
                    ProtocolLimits.CurrentVersion,
                    id,
                    "Reject",
                    DateTime.UtcNow,
                    string.Empty,
                    "{\"error\":\"" + reason + "\"}"));
        }
    }
}
