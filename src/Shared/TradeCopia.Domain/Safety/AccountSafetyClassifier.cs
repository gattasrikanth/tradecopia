using System;

namespace TradeCopia.Domain.Safety
{
    public enum AccountSafetyClass
    {
        Unknown = 0,
        Simulation = 1,
        DemoPaper = 2,
        Live = 3
    }

    /// <summary>
    /// Classifies accounts from official NinjaTrader connection metadata:
    /// Provider, ConnectOptions.Mode (Live/Simulation), and ConnectOptions.IsDemo.
    /// Display-name substrings are not used.
    /// </summary>
    public static class AccountSafetyClassifier
    {
        public static AccountSafetyClass Classify(string provider, string officialMode, bool isDemo)
        {
            if (string.Equals(officialMode, "Simulation", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "Simulator", StringComparison.OrdinalIgnoreCase))
            {
                return AccountSafetyClass.Simulation;
            }

            if (isDemo || string.Equals(provider, "Playback", StringComparison.OrdinalIgnoreCase))
            {
                return AccountSafetyClass.DemoPaper;
            }

            if (string.IsNullOrWhiteSpace(provider)
                || string.Equals(provider, "Unknown", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(officialMode))
            {
                return AccountSafetyClass.Unknown;
            }

            if (string.Equals(officialMode, "Live", StringComparison.OrdinalIgnoreCase))
            {
                return AccountSafetyClass.Live;
            }

            return AccountSafetyClass.Unknown;
        }

        public static bool AlphaMaySelect(AccountSafetyClass safety)
        {
            return safety == AccountSafetyClass.Simulation || safety == AccountSafetyClass.DemoPaper;
        }

        public static bool AlphaMayEnable(AccountSafetyClass safety)
        {
            return AlphaMaySelect(safety);
        }
    }
}
