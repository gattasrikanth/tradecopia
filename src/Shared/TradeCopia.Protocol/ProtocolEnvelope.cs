using System;
using System.IO;
using System.Text;

namespace TradeCopia.Protocol
{
    public static class ProtocolLimits
    {
        public const int CurrentVersion = 1;
        public const int MaxMessageBytes = 256 * 1024;
        public const int MaxStringChars = 4096;
        public const int HeaderBytes = 4;
    }

    public sealed class ProtocolEnvelope
    {
        public ProtocolEnvelope(
            int protocolVersion,
            string messageId,
            string messageType,
            DateTime sentAtUtc,
            string sessionId,
            string payloadJson)
        {
            ProtocolVersion = protocolVersion;
            MessageId = messageId ?? string.Empty;
            MessageType = messageType ?? string.Empty;
            SentAtUtc = DateTime.SpecifyKind(sentAtUtc, DateTimeKind.Utc);
            SessionId = sessionId ?? string.Empty;
            PayloadJson = payloadJson ?? "{}";
        }

        public int ProtocolVersion { get; }
        public string MessageId { get; }
        public string MessageType { get; }
        public DateTime SentAtUtc { get; }
        public string SessionId { get; }
        public string PayloadJson { get; }
    }

    public static class ProtocolFraming
    {
        public static byte[] Encode(string utf8Json)
        {
            if (utf8Json == null)
            {
                throw new ArgumentNullException(nameof(utf8Json));
            }

            var payload = Encoding.UTF8.GetBytes(utf8Json);
            if (payload.Length > ProtocolLimits.MaxMessageBytes)
            {
                throw new InvalidOperationException("Protocol message exceeds MaxMessageBytes.");
            }

            var frame = new byte[ProtocolLimits.HeaderBytes + payload.Length];
            frame[0] = (byte)((payload.Length >> 24) & 0xFF);
            frame[1] = (byte)((payload.Length >> 16) & 0xFF);
            frame[2] = (byte)((payload.Length >> 8) & 0xFF);
            frame[3] = (byte)(payload.Length & 0xFF);
            Buffer.BlockCopy(payload, 0, frame, ProtocolLimits.HeaderBytes, payload.Length);
            return frame;
        }

        public static string Decode(byte[] frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            if (frame.Length < ProtocolLimits.HeaderBytes)
            {
                throw new InvalidDataException("Frame is shorter than the length prefix.");
            }

            var length = (frame[0] << 24) | (frame[1] << 16) | (frame[2] << 8) | frame[3];
            if (length < 0 || length > ProtocolLimits.MaxMessageBytes)
            {
                throw new InvalidDataException("Frame length is outside allowed bounds.");
            }

            if (frame.Length != ProtocolLimits.HeaderBytes + length)
            {
                throw new InvalidDataException("Frame length does not match payload.");
            }

            return Encoding.UTF8.GetString(frame, ProtocolLimits.HeaderBytes, length);
        }

        public static bool IsCompatible(int remoteVersion)
        {
            return remoteVersion == ProtocolLimits.CurrentVersion;
        }
    }

    public static class ProtocolMessageTypes
    {
        public const string Hello = "Hello";
        public const string EngineHello = "EngineHello";
        public const string RequestSnapshot = "RequestSnapshot";
        public const string EngineStateSnapshot = "EngineStateSnapshot";
        public const string ActivateConfig = "ActivateConfig";
        public const string PauseNewEntries = "PauseNewEntries";
        public const string ResumeNewEntries = "ResumeNewEntries";
        public const string DisableGroup = "DisableGroup";
        public const string EnableCopying = "EnableCopying";
        public const string PrepareFlatten = "PrepareFlatten";
        public const string ExecuteFlatten = "ExecuteFlatten";
        public const string Heartbeat = "Heartbeat";
    }
}
