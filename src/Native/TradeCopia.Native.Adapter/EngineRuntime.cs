using System;
using System.Collections.Generic;
using TradeCopia.Domain;
using TradeCopia.Protocol;

namespace TradeCopia.Native.Adapter
{
    /// <summary>
    /// Shipped engine runtime used by the NinjaTrader AddOn host.
    /// Starts the named-pipe server. Copying remains disabled until a
    /// validated ResumeNewEntries after Pause (never auto-enabled on start).
    /// </summary>
    public sealed class EngineRuntime : IDisposable
    {
        private readonly SubscriptionRegistry _subscriptions = new SubscriptionRegistry();
        private readonly INativeOrderExecutor _executor;
        private readonly GuardedNativeRuntime _guarded;
        private NamedPipeEngineHost? _pipe;

        public EngineRuntime()
            : this(EnginePipeName.ForCurrentUser())
        {
        }

        public EngineRuntime(string pipeName)
            : this(pipeName, new DisabledOrderExecutor(), key => TriState.Unknown)
        {
        }

        public EngineRuntime(string pipeName, INativeOrderExecutor inner, Func<AccountKey, TriState> classify)
        {
            if (string.IsNullOrWhiteSpace(pipeName))
            {
                throw new ArgumentException("Pipe name is required.", nameof(pipeName));
            }

            PipeName = pipeName;
            Session = new ProtocolSession();
            _executor = inner ?? throw new ArgumentException("Executor is required.", nameof(inner));
            _guarded = new GuardedNativeRuntime(inner, classify, () => Session.CopyingEnabled);
        }

        public string PipeName { get; }
        public ProtocolSession Session { get; }
        public EngineSafetyState State => Session.CopyingEnabled
            ? EngineSafetyState.Enabled
            : (string.Equals(Session.EngineState, "PausedNewEntries", StringComparison.Ordinal)
                ? EngineSafetyState.PausedNewEntries
                : EngineSafetyState.Disabled);

        public SubscriptionRegistry Subscriptions => _subscriptions;
        public INativeOrderExecutor Executor => _executor;
        public GuardedNativeRuntime Guarded => _guarded;
        public bool PipeStarted => _pipe != null;

        public void PublishAccounts(IEnumerable<EngineAccountRecord> accounts)
        {
            Session.ReplaceAccounts(accounts);
        }

        public void Start()
        {
            if (_pipe != null)
            {
                return;
            }

            _subscriptions.Register("engine:status");
            _pipe = new NamedPipeEngineHost(PipeName, Session);
            _pipe.Start();
        }

        public void Stop()
        {
            if (_pipe != null)
            {
                _pipe.Dispose();
                _pipe = null;
            }

            _subscriptions.UnregisterAll();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
