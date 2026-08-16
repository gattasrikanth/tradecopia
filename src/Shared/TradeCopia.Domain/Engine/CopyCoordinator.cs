using System;
using System.Collections.Generic;
using TradeCopia.Domain.Config;
using TradeCopia.Domain.Events;
using TradeCopia.Domain.Fingerprints;
using TradeCopia.Domain.Intents;
using TradeCopia.Domain.Model;
using TradeCopia.Domain.Origin;
using TradeCopia.Domain.Mapping;
using TradeCopia.Domain.Sizing;
using TradeCopia.Domain.Telemetry;
using TradeCopia.Domain.Time;

namespace TradeCopia.Domain.Engine
{
    public sealed class CoordinatorResult
    {
        public CoordinatorResult(
            IReadOnlyList<ExecutionIntent> intents,
            IReadOnlyList<LogicalOrder> orders,
            IReadOnlyList<string> warnings)
        {
            Intents = intents;
            Orders = orders;
            Warnings = warnings;
        }

        public IReadOnlyList<ExecutionIntent> Intents { get; }
        public IReadOnlyList<LogicalOrder> Orders { get; }
        public IReadOnlyList<string> Warnings { get; }
        public LatencySample? Latency { get; set; }

        public int SubmitCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < Intents.Count; i++)
                {
                    if (Intents[i].Kind == IntentKind.SubmitFollowerOrder)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }

    public sealed class CopyCoordinator
    {
        private readonly IOriginRegistry _origins;
        private readonly IClock _clock;
        private readonly Dictionary<string, LogicalOrder> _orders = new Dictionary<string, LogicalOrder>(StringComparer.Ordinal);
        private readonly HashSet<string> _seenExecutions = new HashSet<string>(StringComparer.Ordinal);
        private ActiveConfigSnapshot _config;

        public CopyCoordinator(ActiveConfigSnapshot config, IOriginRegistry origins, IClock clock)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _origins = origins ?? throw new ArgumentNullException(nameof(origins));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public ActiveConfigSnapshot Config => _config;

        public void ReplaceConfig(ActiveConfigSnapshot config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var validation = ConfigValidator.Validate(config);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException("Rejected config: " + string.Join("; ", validation.Errors));
            }

            _config = config;
        }

        public void ResetAfterEngineRestart()
        {
            _orders.Clear();
            _seenExecutions.Clear();
            _config = new ActiveConfigSnapshot(
                _config.Version,
                EngineSafetyState.Disabled,
                _config.Risk,
                _config.Groups,
                _config.Accounts);
        }

        public CoordinatorResult ProcessOrder(NormalizedOrderEvent evt)
        {
            if (evt == null)
            {
                throw new ArgumentNullException(nameof(evt));
            }

            var intents = new List<ExecutionIntent>();
            var warnings = new List<string>();

            if (_origins.IsCopierOriginated(evt.Account, evt.OrderKey.Value)
                || LooksLikeCopierName(evt.OrderName))
            {
                return Finish(evt, new CoordinatorResult(
                    new[] { ExecutionIntent.NoOp(evt.EventId, _clock.UtcNow, "loop-prevention") },
                    SnapshotOrders(),
                    warnings));
            }

            var fingerprint = SemanticFingerprint.Compute(evt);
            LogicalOrder existing;
            if (_orders.TryGetValue(evt.OrderKey.Value, out existing)
                && string.Equals(existing.LastFingerprint, fingerprint, StringComparison.Ordinal))
            {
                return new CoordinatorResult(
                    new[] { ExecutionIntent.NoOp(evt.EventId, _clock.UtcNow, "duplicate-fingerprint") },
                    SnapshotOrders(),
                    warnings);
            }

            var group = FindGroupForLeader(evt.Account);
            if (group == null)
            {
                return new CoordinatorResult(
                    new[] { ExecutionIntent.NoOp(evt.EventId, _clock.UtcNow, "no-group") },
                    SnapshotOrders(),
                    warnings);
            }

            if (group.CopyMode != CopyMode.OrderMirror)
            {
                return new CoordinatorResult(
                    new[] { ExecutionIntent.NoOp(evt.EventId, _clock.UtcNow, "mode-not-order-mirror") },
                    SnapshotOrders(),
                    warnings);
            }

            if (existing == null)
            {
                existing = new LogicalOrder(LogicalOrderId.New(), group.Id, evt);
                existing.Classification = Classify(evt);
                _orders[evt.OrderKey.Value] = existing;
            }

            existing.LastFingerprint = fingerprint;
            existing.LastObservedAtUtc = evt.ObservedAtUtc;

            if (evt.OrderType == DomainOrderType.Unsupported)
            {
                existing.State = LogicalCopyState.Failed;
                intents.Add(CreateDivergence(evt, group, existing, evt.Account, DivergenceClass.UnsupportedOrderType, "unsupported-order-type"));
                ApplyObservedMutation(existing, evt);
                return new CoordinatorResult(intents, SnapshotOrders(), warnings);
            }

            if (IsNewWorkingOrPending(evt) && existing.Links.Count == 0)
            {
                TryDispatchNew(evt, group, existing, intents, warnings);
            }
            else if (existing.Links.Count > 0)
            {
                HandleLifecycle(evt, group, existing, intents, warnings);
            }

            ApplyObservedMutation(existing, evt);
            return Finish(evt, new CoordinatorResult(intents, SnapshotOrders(), warnings));
        }

        public CoordinatorResult ProcessExecution(NormalizedExecutionEvent evt)
        {
            if (evt == null)
            {
                throw new ArgumentNullException(nameof(evt));
            }

            var key = evt.Account.Value + "|" + evt.ExecutionKey.Value;
            if (!_seenExecutions.Add(key))
            {
                return new CoordinatorResult(
                    new[] { ExecutionIntent.NoOp(evt.EventId, _clock.UtcNow, "duplicate-execution") },
                    SnapshotOrders(),
                    Array.Empty<string>());
            }

            LogicalOrder order;
            if (_orders.TryGetValue(evt.OrderKey.Value, out order))
            {
                order.FilledQuantity += evt.Quantity;
                if (order.FilledQuantity > 0 && order.FilledQuantity < order.RequestedQuantity)
                {
                    order.State = LogicalCopyState.PartiallySatisfied;
                }
                else if (order.FilledQuantity >= order.RequestedQuantity)
                {
                    order.State = LogicalCopyState.Satisfied;
                }
            }

            return new CoordinatorResult(Array.Empty<ExecutionIntent>(), SnapshotOrders(), Array.Empty<string>());
        }

        private void TryDispatchNew(
            NormalizedOrderEvent evt,
            CopyGroup group,
            LogicalOrder logical,
            List<ExecutionIntent> intents,
            List<string> warnings)
        {
            if (!CanOpenNewExposure(group))
            {
                logical.State = LogicalCopyState.Discovered;
                warnings.Add("new-entries-blocked");
                return;
            }

            if (evt.State == LeaderOrderState.Rejected || evt.State == LeaderOrderState.Canceled)
            {
                logical.State = evt.State == LeaderOrderState.Rejected ? LogicalCopyState.Failed : LogicalCopyState.Canceled;
                return;
            }

            if (!IsEligibleSubmissionState(evt.State))
            {
                logical.State = LogicalCopyState.Discovered;
                return;
            }

            var leaderAccount = _config.GetAccount(evt.Account);
            if (_config.Risk.SimulationOnly && !leaderAccount.IsPositivelySimulation)
            {
                logical.State = LogicalCopyState.Failed;
                intents.Add(CreateDivergence(evt, group, logical, evt.Account, DivergenceClass.SimulationRequired, "leader-not-sim"));
                return;
            }

            if (_config.Risk.BlockWhenAnyFollowerDisconnected && AnyEnabledFollowerNotReady(group))
            {
                logical.State = LogicalCopyState.Divergent;
                intents.Add(CreateDivergence(evt, group, logical, evt.Account, DivergenceClass.FollowerDisconnected, "follower-not-ready"));
                return;
            }

            logical.State = LogicalCopyState.Validated;

            foreach (var follower in group.Followers)
            {
                if (!follower.Enabled || follower.Sizing.Mode == SizingMode.Disabled)
                {
                    continue;
                }

                var followerAccount = _config.GetAccount(follower.Account);
                if (_config.Risk.SimulationOnly && !followerAccount.IsPositivelySimulation)
                {
                    intents.Add(CreateDivergence(evt, group, logical, follower.Account, DivergenceClass.SimulationRequired, "follower-not-sim"));
                    continue;
                }

                if (!followerAccount.IsReadyForNewEntries)
                {
                    intents.Add(CreateDivergence(evt, group, logical, follower.Account, DivergenceClass.FollowerDisconnected, "follower-not-ready"));
                    continue;
                }

                var mapped = InstrumentMapper.Map(follower, evt.Instrument);
                if (!mapped.Succeeded || !mapped.Instrument.HasValue)
                {
                    intents.Add(CreateDivergence(evt, group, logical, follower.Account, DivergenceClass.ConfigMismatch, mapped.Reason));
                    continue;
                }

                var sizing = SizingEngine.ComputeEntryQuantity(evt.Quantity, follower.Sizing, 0);
                if (sizing.BlockedByCap)
                {
                    intents.Add(CreateDivergence(evt, group, logical, follower.Account, DivergenceClass.RiskCapBlocked, sizing.Reason));
                    continue;
                }

                if (!sizing.HasQuantity)
                {
                    warnings.Add("follower-qty-zero:" + follower.Account.Value);
                    continue;
                }

                if (_config.Risk.DryRun)
                {
                    warnings.Add("dry-run:" + follower.Account.Value);
                    continue;
                }

                var commandId = CommandId.New();
                var intent = new ExecutionIntent(
                    commandId,
                    evt.EventId,
                    IntentKind.SubmitFollowerOrder,
                    group.Id,
                    follower.Account,
                    logical.Id,
                    mapped.Instrument,
                    evt.OrderType,
                    evt.Action,
                    sizing.Quantity,
                    evt.LimitPrice,
                    evt.StopPrice,
                    string.Empty,
                    "order-mirror-submit",
                    _clock.UtcNow);

                _origins.RegisterPending(commandId, follower.Account, mapped.Instrument.Value);
                var link = new FollowerLink(follower.Account, sizing.Quantity, mapped.Instrument.Value)
                {
                    SubmitCommand = commandId,
                    SubmittedQuantity = sizing.Quantity,
                    Health = FollowerLinkHealth.Dispatched
                };
                logical.Links.Add(link);
                intents.Add(intent);
            }

            logical.State = logical.Links.Count > 0 ? LogicalCopyState.Dispatching : LogicalCopyState.Failed;
        }

        private void HandleLifecycle(
            NormalizedOrderEvent evt,
            CopyGroup group,
            LogicalOrder logical,
            List<ExecutionIntent> intents,
            List<string> warnings)
        {
            if (evt.State == LeaderOrderState.Canceled || evt.State == LeaderOrderState.CancelPending)
            {
                if (!_config.AllowsRiskReducingActions || !group.AllowsRiskReducingActions)
                {
                    warnings.Add("cancel-blocked-engine-disabled");
                    return;
                }

                logical.State = LogicalCopyState.Canceling;
                foreach (var link in logical.Links)
                {
                    if (link.Health == FollowerLinkHealth.Filled
                        || link.Health == FollowerLinkHealth.Canceled
                        || link.Health == FollowerLinkHealth.Rejected)
                    {
                        continue;
                    }

                    intents.Add(new ExecutionIntent(
                        CommandId.New(),
                        evt.EventId,
                        IntentKind.CancelFollowerOrder,
                        group.Id,
                        link.Follower,
                        logical.Id,
                        link.Instrument,
                        logical.OrderType,
                        logical.Action,
                        0,
                        null,
                        null,
                        string.Empty,
                        "leader-cancel",
                        _clock.UtcNow));
                    link.Health = FollowerLinkHealth.Canceled;
                }

                logical.State = LogicalCopyState.Canceled;
                return;
            }

            if (evt.State == LeaderOrderState.Rejected)
            {
                logical.State = LogicalCopyState.Failed;
                return;
            }

            if (evt.Quantity < logical.RequestedQuantity && evt.Quantity >= 0)
            {
                HandleQuantityDecrease(evt, group, logical, intents);
                return;
            }

            if ((evt.LimitPrice != logical.LimitPrice || evt.StopPrice != logical.StopPrice)
                && !evt.IsTerminal)
            {
                if (!CanOpenNewExposure(group) && IsExposureIncreasingChange(evt, logical))
                {
                    warnings.Add("change-blocked");
                    return;
                }

                logical.LimitPrice = evt.LimitPrice;
                logical.StopPrice = evt.StopPrice;
                foreach (var link in logical.Links)
                {
                    if (link.Health == FollowerLinkHealth.Filled || link.Health == FollowerLinkHealth.Canceled)
                    {
                        continue;
                    }

                    intents.Add(new ExecutionIntent(
                        CommandId.New(),
                        evt.EventId,
                        IntentKind.ChangeFollowerOrder,
                        group.Id,
                        link.Follower,
                        logical.Id,
                        link.Instrument,
                        logical.OrderType,
                        logical.Action,
                        link.IntendedQuantity,
                        evt.LimitPrice,
                        evt.StopPrice,
                        string.Empty,
                        "leader-price-change",
                        _clock.UtcNow));
                }

                logical.State = LogicalCopyState.Active;
            }
        }

        private void HandleQuantityDecrease(
            NormalizedOrderEvent evt,
            CopyGroup group,
            LogicalOrder logical,
            List<ExecutionIntent> intents)
        {
            if (!_config.AllowsRiskReducingActions)
            {
                return;
            }

            foreach (var link in logical.Links)
            {
                var remaining = SizingEngine.ComputeScaleOutRemaining(
                    logical.InitialQuantity,
                    evt.Quantity,
                    link.IntendedQuantity,
                    link.IntendedQuantity);
                var reduction = link.IntendedQuantity - remaining;
                if (reduction <= 0)
                {
                    continue;
                }

                if (remaining == 0)
                {
                    intents.Add(new ExecutionIntent(
                        CommandId.New(),
                        evt.EventId,
                        IntentKind.CancelFollowerOrder,
                        group.Id,
                        link.Follower,
                        logical.Id,
                        link.Instrument,
                        logical.OrderType,
                        logical.Action,
                        0,
                        null,
                        null,
                        string.Empty,
                        "scale-out-cancel",
                        _clock.UtcNow));
                    link.IntendedQuantity = 0;
                    continue;
                }

                intents.Add(new ExecutionIntent(
                    CommandId.New(),
                    evt.EventId,
                    IntentKind.ChangeFollowerOrder,
                    group.Id,
                    link.Follower,
                    logical.Id,
                    link.Instrument,
                    logical.OrderType,
                    logical.Action,
                    remaining,
                    evt.LimitPrice,
                    evt.StopPrice,
                    string.Empty,
                    "scale-out-change",
                    _clock.UtcNow));
                link.IntendedQuantity = remaining;
            }

            logical.RequestedQuantity = evt.Quantity;
        }

        private bool CanOpenNewExposure(CopyGroup group)
        {
            return _config.AllowsNewEntries && group.AllowsNewEntries;
        }

        private bool AnyEnabledFollowerNotReady(CopyGroup group)
        {
            foreach (var follower in group.Followers)
            {
                if (!follower.Enabled)
                {
                    continue;
                }

                if (!_config.GetAccount(follower.Account).IsReadyForNewEntries)
                {
                    return true;
                }
            }

            return false;
        }

        private CopyGroup? FindGroupForLeader(AccountKey leader)
        {
            foreach (var group in _config.Groups)
            {
                if (group.Leader == leader)
                {
                    return group;
                }
            }

            return null;
        }

        private static bool IsEligibleSubmissionState(LeaderOrderState state)
        {
            return state == LeaderOrderState.PendingSubmission
                || state == LeaderOrderState.Working
                || state == LeaderOrderState.PartiallyFilled
                || state == LeaderOrderState.Filled;
        }

        private static bool IsNewWorkingOrPending(NormalizedOrderEvent evt)
        {
            return IsEligibleSubmissionState(evt.State);
        }

        private static bool LooksLikeCopierName(string name)
        {
            return !string.IsNullOrEmpty(name) && name.StartsWith("TC:", StringComparison.Ordinal);
        }

        private static void ApplyObservedMutation(LogicalOrder order, NormalizedOrderEvent evt)
        {
            order.RequestedQuantity = evt.Quantity;
            order.FilledQuantity = evt.FilledQuantity;
            order.LimitPrice = evt.LimitPrice;
            order.StopPrice = evt.StopPrice;
            order.TimeInForce = evt.TimeInForce;
            order.LeaderOco = evt.OcoIdentity;
        }

        private static IntentClassification Classify(NormalizedOrderEvent evt)
        {
            if (evt.State == LeaderOrderState.Canceled)
            {
                return IntentClassification.CancelRemainder;
            }

            return evt.LooksLikeEntry ? IntentClassification.Entry : IntentClassification.Exit;
        }

        private static bool IsExposureIncreasingChange(NormalizedOrderEvent evt, LogicalOrder logical)
        {
            return evt.Quantity > logical.RequestedQuantity;
        }

        private ExecutionIntent CreateDivergence(
            NormalizedOrderEvent evt,
            CopyGroup group,
            LogicalOrder logical,
            AccountKey account,
            DivergenceClass klass,
            string reason)
        {
            return new ExecutionIntent(
                CommandId.New(),
                evt.EventId,
                IntentKind.RaiseDivergence,
                group.Id,
                account,
                logical.Id,
                evt.Instrument,
                evt.OrderType,
                evt.Action,
                0,
                null,
                null,
                string.Empty,
                klass + ":" + reason,
                _clock.UtcNow);
        }

        private CoordinatorResult Finish(NormalizedOrderEvent evt, CoordinatorResult result)
        {
            result.Latency = new LatencySample(evt.ObservedHighResTicks, _clock.HighResolutionTicks);
            return result;
        }

        private IReadOnlyList<LogicalOrder> SnapshotOrders()
        {
            return new List<LogicalOrder>(_orders.Values);
        }
    }
}
