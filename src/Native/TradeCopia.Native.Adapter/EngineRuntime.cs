using System;
using System.Collections.Generic;
using System.IO;
using TradeCopia.Domain;
using TradeCopia.Domain.Config;
using TradeCopia.Domain.Engine;
using TradeCopia.Domain.Events;
using TradeCopia.Domain.Intents;
using TradeCopia.Domain.Model;
using TradeCopia.Domain.Origin;
using TradeCopia.Domain.Time;
using TradeCopia.Protocol;

namespace TradeCopia.Native.Adapter
{
    /// <summary>
    /// Shipped engine runtime used by the NinjaTrader AddOn host.
    /// Starts the named-pipe server. Copying remains disabled until a
    /// validated ResumeNewEntries after Pause (never auto-enabled on start).
    /// Normalized leader orders go through CopyCoordinator then the guarded dispatcher.
    /// </summary>
    public sealed class EngineRuntime : IDisposable
    {
        private readonly SubscriptionRegistry _subscriptions = new SubscriptionRegistry();
        private readonly INativeOrderExecutor _executor;
        private readonly GuardedNativeRuntime _guarded;
        private readonly CopyCoordinator _coordinator;
        private readonly LiveCopyBook _live = new LiveCopyBook();
        private readonly object _copyGate = new object();
        private string _appliedConfigToken = string.Empty;
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
            : this(pipeName, inner, classify, null)
        {
        }

        public EngineRuntime(
            string pipeName,
            INativeOrderExecutor inner,
            Func<AccountKey, TriState> classify,
            string? dataDirectory)
        {
            if (string.IsNullOrWhiteSpace(pipeName))
            {
                throw new ArgumentException("Pipe name is required.", nameof(pipeName));
            }

            PipeName = pipeName;
            Session = new ProtocolSession();
            _executor = inner ?? throw new ArgumentException("Executor is required.", nameof(inner));
            _guarded = new GuardedNativeRuntime(inner, classify, () => Session.CopyingEnabled);
            var ledger = string.IsNullOrWhiteSpace(dataDirectory)
                ? (ILeaderIdentityLedger)NullLedger.Instance
                : new FileLeaderIdentityLedger(Path.Combine(dataDirectory, "seen-leader-orders.txt"));

            _coordinator = new CopyCoordinator(DisabledEmptyConfig(), new OriginRegistry(), new SystemClock(), ledger);
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
        public CopyCoordinator Coordinator => _coordinator;
        public bool PipeStarted => _pipe != null;

        public void PublishAccounts(IEnumerable<EngineAccountRecord> accounts)
        {
            Session.ReplaceAccounts(accounts);
        }

        public IReadOnlyList<NativeExecutionResult> HandleOrder(NormalizedOrderEvent evt)
        {
            if (evt == null)
            {
                throw new ArgumentNullException(nameof(evt));
            }

            lock (_copyGate)
            {
                SyncCoordinatorFromSession();
                var result = _coordinator.ProcessOrder(evt);
                var dispatched = new List<NativeExecutionResult>(result.Intents.Count);
                for (var i = 0; i < result.Intents.Count; i++)
                {
                    dispatched.Add(_guarded.Dispatch(result.Intents[i]));
                }

                PublishLive(evt, result);
                return dispatched;
            }
        }

        private void PublishLive(NormalizedOrderEvent evt, CoordinatorResult result)
        {
            var copier = !string.IsNullOrEmpty(evt.OrderName)
                && evt.OrderName.StartsWith("TC:", StringComparison.Ordinal);
            _live.Observe(
                evt.OrderKey.Value,
                evt.AlternateOrderKey,
                evt.Account.Value,
                evt.Instrument.Value,
                evt.Action.ToString(),
                evt.OrderType.ToString(),
                evt.Quantity,
                evt.FilledQuantity,
                evt.State.ToString(),
                LiveCopyBook.FormatPrice(evt.LimitPrice),
                LiveCopyBook.FormatPrice(evt.StopPrice),
                evt.OrderName,
                copier,
                evt.ObservedAtUtc);

            for (var i = 0; i < result.Intents.Count; i++)
            {
                var intent = result.Intents[i];
                if (intent.Kind == IntentKind.RaiseDivergence)
                {
                    _live.AddDivergence(intent.ReasonCode, intent.ReasonCode);
                }
            }

            Session.ReplaceLiveActivity(_live.Snapshot(), _live.DivergenceSnapshot());
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

        private void SyncCoordinatorFromSession()
        {
            var token = Session.ActiveConfigVersion + "|" + Session.EngineState + "|" + Session.CopyingEnabled;
            if (string.Equals(token, _appliedConfigToken, StringComparison.Ordinal))
            {
                return;
            }

            var snapshot = BuildSnapshotFromSession();
            if (snapshot == null)
            {
                return;
            }

            _coordinator.ReplaceConfig(snapshot);
            _appliedConfigToken = token;
        }

        private ActiveConfigSnapshot? BuildSnapshotFromSession()
        {
            if (string.IsNullOrEmpty(Session.ActiveLeaderKey) || Session.ActiveFollowerKeys.Count == 0)
            {
                return null;
            }

            var accounts = new Dictionary<AccountKey, AccountDescriptor>();
            foreach (var record in Session.Accounts)
            {
                var key = new AccountKey(record.StableKey);
                var sim = AccountSimulationGate.ClassifySafety(record.SafetyClass);
                accounts[key] = new AccountDescriptor(
                    key,
                    record.DisplayName,
                    record.Provider,
                    AccountReadiness.Ready,
                    sim);
            }

            var leader = new AccountKey(Session.ActiveLeaderKey);
            if (!accounts.ContainsKey(leader))
            {
                return null;
            }

            var followers = new List<FollowerRule>();
            foreach (var followerKey in Session.ActiveFollowerKeys)
            {
                var key = new AccountKey(followerKey);
                if (!accounts.ContainsKey(key))
                {
                    return null;
                }

                followers.Add(new FollowerRule(key, true, SizingPolicy.OneToOne(), Array.Empty<InstrumentMapping>()));
            }

            if (followers.Count == 0)
            {
                return null;
            }

            var groupState = Session.CopyingEnabled
                ? GroupEnabledState.Enabled
                : (string.Equals(Session.EngineState, "PausedNewEntries", StringComparison.Ordinal)
                    ? GroupEnabledState.PausedNewEntries
                    : GroupEnabledState.Disabled);

            Guid groupGuid;
            var groupId = Guid.TryParse(Session.ActiveConfigVersion, out groupGuid) && groupGuid != Guid.Empty
                ? new CopyGroupId(groupGuid)
                : CopyGroupId.New();

            var group = new CopyGroup(
                groupId,
                "Active",
                leader,
                followers.ToArray(),
                CopyMode.OrderMirror,
                groupState);

            var engineState = Session.CopyingEnabled
                ? EngineSafetyState.Enabled
                : (groupState == GroupEnabledState.PausedNewEntries
                    ? EngineSafetyState.PausedNewEntries
                    : EngineSafetyState.Disabled);

            return new ActiveConfigSnapshot(
                new ConfigVersion(1),
                engineState,
                new RiskPolicy(simulationOnly: true, dryRun: false, blockWhenAnyFollowerDisconnected: true),
                new[] { group },
                accounts);
        }

        private static ActiveConfigSnapshot DisabledEmptyConfig()
        {
            return new ActiveConfigSnapshot(
                new ConfigVersion(0),
                EngineSafetyState.Disabled,
                new RiskPolicy(true, false, true),
                Array.Empty<CopyGroup>(),
                new Dictionary<AccountKey, AccountDescriptor>());
        }
    }
}
