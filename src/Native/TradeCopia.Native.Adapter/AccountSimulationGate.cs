using System;
using TradeCopia.Domain;

namespace TradeCopia.Native.Adapter
{
    /// <summary>
    /// Classifies NinjaTrader account simulation using official
    /// <c>NinjaTrader.Cbi.Provider</c> names, not account-name substrings.
    /// Simulator and Playback are KnownTrue. Unknown fails closed.
    /// Any other provider is treated as live-capable (KnownFalse).
    /// </summary>
    public static class AccountSimulationGate
    {
        public static TriState ClassifyProvider(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return TriState.Unknown;
            }

            if (string.Equals(provider, "Simulator", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "Playback", StringComparison.OrdinalIgnoreCase))
            {
                return TriState.KnownTrue;
            }

            if (string.Equals(provider, "Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return TriState.Unknown;
            }

            return TriState.KnownFalse;
        }

        public static bool AllowsNativeSubmit(TriState classification)
        {
            return SimulationIdentity.IsPositiveSimulation(classification);
        }
    }
}
