using System;
using System.Collections.Generic;
using TradeCopia.Domain.Model;

namespace TradeCopia.Domain.Config
{
    public sealed class ActiveConfigSnapshot
    {
        public ActiveConfigSnapshot(
            ConfigVersion version,
            EngineSafetyState engineState,
            RiskPolicy risk,
            IReadOnlyList<CopyGroup> groups,
            IReadOnlyDictionary<AccountKey, AccountDescriptor> accounts)
        {
            Version = version;
            EngineState = engineState;
            Risk = risk ?? throw new ArgumentNullException(nameof(risk));
            Groups = groups ?? throw new ArgumentNullException(nameof(groups));
            Accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        }

        public ConfigVersion Version { get; }
        public EngineSafetyState EngineState { get; }
        public RiskPolicy Risk { get; }
        public IReadOnlyList<CopyGroup> Groups { get; }
        public IReadOnlyDictionary<AccountKey, AccountDescriptor> Accounts { get; }

        public bool AllowsNewEntries => EngineState == EngineSafetyState.Enabled;
        public bool AllowsRiskReducingActions => EngineState != EngineSafetyState.Disabled;

        public AccountDescriptor GetAccount(AccountKey key)
        {
            AccountDescriptor account;
            if (!Accounts.TryGetValue(key, out account))
            {
                return new AccountDescriptor(key, key.Value, string.Empty, AccountReadiness.Unknown, TriState.Unknown);
            }

            return account;
        }
    }
}
