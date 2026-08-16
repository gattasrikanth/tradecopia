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

        public ProtocolSession()
        {
            _sessionId = Guid.NewGuid().ToString("N");
        }

        public bool IsHandshook => _handshook;
        public bool IsConnected => _connected;
        public string SessionId => _sessionId;

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
                        "{\"capabilities\":[\"OrderMirror\"],\"copying\":\"Disabled\"}"));
            }

            if (!_handshook)
            {
                return Reject(incoming, "handshake-required");
            }

            if (string.Equals(incoming.MessageType, ProtocolMessageTypes.RequestSnapshot, StringComparison.Ordinal))
            {
                return new ProtocolValidationResult(
                    true,
                    "snapshot",
                    new ProtocolEnvelope(
                        ProtocolLimits.CurrentVersion,
                        Guid.NewGuid().ToString("N"),
                        ProtocolMessageTypes.EngineStateSnapshot,
                        DateTime.UtcNow,
                        _sessionId,
                        "{\"engineState\":\"Disabled\",\"copyingEnabled\":false}"));
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
                    "{}"));
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
