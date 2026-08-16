using System;
using TradeCopia.Domain;
using TradeCopia.Domain.Safety;

namespace TradeCopia.Native.Adapter
{
    /// <summary>
    /// Classifies whether an account may receive a native submit.
    /// Provider-only Simulator/Playback stay KnownTrue. Full ADR-0010
    /// safety (including official Tradovate AccountType) is used when
    /// Mode/IsDemo/AccountType are available. Display names are ignored.
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

        public static TriState ClassifyOfficial(string provider, string officialMode, bool isDemo, string officialAccountType)
        {
            return ClassifySafety(AccountSafetyClassifier.Classify(provider, officialMode, isDemo, officialAccountType));
        }

        public static TriState ClassifySafety(AccountSafetyClass safety)
        {
            if (AccountSafetyClassifier.AlphaMayEnable(safety))
            {
                return TriState.KnownTrue;
            }

            if (safety == AccountSafetyClass.Live)
            {
                return TriState.KnownFalse;
            }

            return TriState.Unknown;
        }

        public static bool AllowsNativeSubmit(TriState classification)
        {
            return SimulationIdentity.IsPositiveSimulation(classification);
        }

        public static bool AllowsNativeSubmit(AccountSafetyClass safety)
        {
            return AllowsNativeSubmit(ClassifySafety(safety));
        }
    }

    public static class NativeAccountIdentity
    {
        public static string StableKey(string provider, string id)
        {
            return (provider ?? string.Empty) + "|" + (id ?? string.Empty);
        }

        public static bool Matches(string requested, string provider, string id, string name, string displayName)
        {
            if (string.IsNullOrWhiteSpace(requested))
            {
                return false;
            }

            return string.Equals(requested, StableKey(provider, id), StringComparison.Ordinal)
                || string.Equals(requested, id, StringComparison.Ordinal)
                || string.Equals(requested, name, StringComparison.Ordinal)
                || string.Equals(requested, displayName, StringComparison.Ordinal);
        }
    }
}
