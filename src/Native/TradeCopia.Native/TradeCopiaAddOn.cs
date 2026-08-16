using System;
using TradeCopia.Domain;
using TradeCopia.Native.Adapter;

namespace TradeCopia.Native
{
    /// <summary>
    /// Native AddOn entry. Order submission is wired through
    /// <see cref="DisabledOrderExecutor"/> until Phase 3 SIM work.
    /// This type is compiled only when NinjaTrader reference assemblies are present.
    /// </summary>
    public sealed class TradeCopiaEngineHost
    {
        public const string ProductName = "TradeCopia";
        public static readonly EngineSafetyState DefaultState = EngineSafetyState.Disabled;

        private readonly SubscriptionRegistry _subscriptions = new SubscriptionRegistry();
        private readonly DisabledOrderExecutor _executor = new DisabledOrderExecutor();

        public EngineSafetyState State { get; private set; } = DefaultState;
        public SubscriptionRegistry Subscriptions => _subscriptions;
        public INativeOrderExecutor Executor => _executor;

        public void Start()
        {
            State = EngineSafetyState.Disabled;
            _subscriptions.Register("engine:status");
        }

        public void Stop()
        {
            _subscriptions.UnregisterAll();
            State = EngineSafetyState.Disabled;
        }
    }
}
