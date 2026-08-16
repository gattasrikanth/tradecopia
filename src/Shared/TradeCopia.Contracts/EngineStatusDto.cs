using System;

namespace TradeCopia.Contracts
{
    public sealed class EngineStatusDto
    {
        public EngineStatusDto(string engineState, long configVersion, string sessionId, DateTime utc)
        {
            EngineState = engineState ?? "Disabled";
            ConfigVersion = configVersion;
            SessionId = sessionId ?? string.Empty;
            ObservedAtUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        }

        public string EngineState { get; }
        public long ConfigVersion { get; }
        public string SessionId { get; }
        public DateTime ObservedAtUtc { get; }
    }
}
