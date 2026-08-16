using System;
using System.Collections.Generic;
using TradeCopia.Domain;
using TradeCopia.Protocol;

namespace TradeCopia.Native
{
    /// <summary>
    /// Shipped engine host used by the NinjaTrader AddOn. Starts the named-pipe
    /// server through <see cref="Adapter.EngineRuntime"/>. Copying stays disabled.
    /// </summary>
    public sealed class TradeCopiaEngineHost
    {
        public const string ProductName = "TradeCopia";
        public static readonly EngineSafetyState DefaultState = EngineSafetyState.Disabled;

        private readonly Adapter.EngineRuntime _runtime;

        public TradeCopiaEngineHost()
            : this(new Adapter.EngineRuntime())
        {
        }

        public TradeCopiaEngineHost(Adapter.EngineRuntime runtime)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public TradeCopiaEngineHost(Adapter.INativeOrderExecutor inner, Func<AccountKey, TriState> classify)
            : this(new Adapter.EngineRuntime(EnginePipeName.ForCurrentUser(), inner, classify))
        {
        }

        public EngineSafetyState State => _runtime.State;
        public Adapter.SubscriptionRegistry Subscriptions => _runtime.Subscriptions;
        public Adapter.INativeOrderExecutor Executor => _runtime.Executor;
        public string PipeName => _runtime.PipeName;
        public Adapter.EngineRuntime Runtime => _runtime;
        public bool PipeStarted => _runtime.PipeStarted;

        public void PublishAccounts(IEnumerable<EngineAccountRecord> accounts)
        {
            _runtime.PublishAccounts(accounts);
        }

        public IReadOnlyList<Adapter.NativeExecutionResult> HandleOrder(TradeCopia.Domain.Events.NormalizedOrderEvent evt)
        {
            return _runtime.HandleOrder(evt);
        }

        public void Start()
        {
            _runtime.Start();
        }

        public void Stop()
        {
            _runtime.Stop();
        }
    }
}
