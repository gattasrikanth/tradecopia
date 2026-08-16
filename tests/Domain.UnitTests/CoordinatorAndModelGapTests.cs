using System;
using System.Collections.Generic;
using TradeCopia.Domain;
using TradeCopia.Domain.Config;
using TradeCopia.Domain.Engine;
using TradeCopia.Domain.Events;
using TradeCopia.Domain.Intents;
using TradeCopia.Domain.Model;
using TradeCopia.Domain.Origin;
using TradeCopia.Domain.Telemetry;
using TradeCopia.Domain.Time;

namespace TradeCopia.Domain.UnitTests
{
    public class CoordinatorAndModelGapTests
    {
        [Fact]
        public void Execution_mirror_group_does_not_submit()
        {
            var follower = new FollowerRule(TestSupport.Follower1, true, SizingPolicy.OneToOne(), Array.Empty<InstrumentMapping>());
            var group = new CopyGroup(CopyGroupId.New(), "exec", TestSupport.Leader, new[] { follower }, CopyMode.ExecutionMirror, GroupEnabledState.Enabled);
            var accounts = new Dictionary<AccountKey, AccountDescriptor>
            {
                [TestSupport.Leader] = new AccountDescriptor(TestSupport.Leader, "L", "S", AccountReadiness.Ready, TriState.KnownTrue),
                [TestSupport.Follower1] = new AccountDescriptor(TestSupport.Follower1, "F", "S", AccountReadiness.Ready, TriState.KnownTrue)
            };
            var snapshot = new ActiveConfigSnapshot(new ConfigVersion(1), EngineSafetyState.Enabled, RiskPolicy.Default(), new[] { group }, accounts);
            var coordinator = new CopyCoordinator(snapshot, new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            var result = coordinator.ProcessOrder(TestSupport.Order("Lm", LeaderOrderState.Working));
            Assert.Equal(0, result.SubmitCount);
            Assert.Contains(result.Intents, i => i.ReasonCode == "mode-not-order-mirror");
        }

        [Fact]
        public void Pending_submission_and_unknown_account_descriptor()
        {
            var coordinator = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 9));
            var result = coordinator.ProcessOrder(TestSupport.Order("Lp", LeaderOrderState.PendingSubmission));
            Assert.Equal(1, result.SubmitCount);
            var missing = coordinator.Config.GetAccount(new AccountKey("SIM-UNKNOWN-99"));
            Assert.Equal(AccountReadiness.Unknown, missing.Readiness);
            Assert.Equal(TriState.Unknown, missing.IsSimulation);
            Assert.Equal("SIM-UNKNOWN-99", missing.DisplayName);
        }

        [Fact]
        public void Execution_without_order_is_recorded_once()
        {
            var coordinator = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            var exec = new NormalizedExecutionEvent(
                EventId.New(), DateTime.UtcNow, TestSupport.Leader, new ExecutionKey("EX1"),
                new LeaderOrderKey("no-such"), TestSupport.Nq, OrderActionKind.Buy, 1, 10m);
            var first = coordinator.ProcessExecution(exec);
            var second = coordinator.ProcessExecution(exec);
            Assert.Empty(first.Intents);
            Assert.Contains(second.Intents, i => i.ReasonCode == "duplicate-execution");
            Assert.Equal(TestSupport.Leader, exec.Account);
            Assert.Equal(1, exec.Quantity);
            Assert.Equal(10m, exec.Price);
        }

        [Fact]
        public void Config_validator_and_sizing_policy_ctors()
        {
            var group = new CopyGroup(
                CopyGroupId.New(), "bad", TestSupport.Leader,
                new[] { new FollowerRule(TestSupport.Follower1, true, SizingPolicy.OneToOne(), Array.Empty<InstrumentMapping>()) },
                CopyMode.ExecutionMirror, GroupEnabledState.Enabled);
            var snapshot = new ActiveConfigSnapshot(
                new ConfigVersion(1), EngineSafetyState.Disabled, RiskPolicy.Default(),
                new[] { group }, TestSupport.Config().Accounts);
            Assert.False(ConfigValidator.Validate(snapshot).IsValid);
            Assert.Throws<ArgumentOutOfRangeException>(() => new SizingPolicy(SizingMode.Multiplier, 0m, 0, false, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SizingPolicy(SizingMode.Fixed, 1m, 0, false, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SizingPolicy(SizingMode.OneToOne, 1m, 0, false, 0, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SizingPolicy(SizingMode.OneToOne, 1m, 0, false, null, 0));
            Assert.Throws<ArgumentException>(() => new InstrumentMapping("", "X", 1, 1, ExpiryMappingPolicy.ExactMonthOnly));
            Assert.Throws<ArgumentException>(() => new InstrumentMapping("X", "", 1, 1, ExpiryMappingPolicy.ExactMonthOnly));
            Assert.Throws<ArgumentOutOfRangeException>(() => new InstrumentMapping("X", "Y", 0, 1, ExpiryMappingPolicy.ExactMonthOnly));
            Assert.Throws<ArgumentOutOfRangeException>(() => new InstrumentMapping("X", "Y", 1, 0, ExpiryMappingPolicy.ExactMonthOnly));
        }

        [Fact]
        public void Telemetry_and_intent_helpers()
        {
            var item = new TelemetryItem(1, "CODE", DateTime.UtcNow);
            Assert.Equal(1, item.Priority);
            Assert.Equal("CODE", item.Code);
            Assert.Equal(DateTimeKind.Utc, item.Utc.Kind);
            var sample = new LatencySample(10, 25);
            Assert.Equal(15, sample.DecisionDeltaTicks);
            var inverted = new LatencySample(30, 10);
            Assert.Equal(0, inverted.DecisionDeltaTicks);
            var noop = ExecutionIntent.NoOp(EventId.New(), DateTime.UtcNow, "r");
            Assert.Equal(IntentKind.NoOp, noop.Kind);
            Assert.Equal("r", noop.ReasonCode);
        }

        [Fact]
        public void Copy_group_and_account_getters()
        {
            var g = TestSupport.Config().Groups[0];
            Assert.False(string.IsNullOrEmpty(g.Name));
            Assert.True(g.AllowsNewEntries);
            Assert.True(g.AllowsRiskReducingActions);
            Assert.NotEmpty(g.Followers);
            var acct = TestSupport.Config().GetAccount(TestSupport.Leader);
            Assert.Equal("Sim", acct.ConnectionName);
            Assert.True(acct.IsPositivelySimulation);
        }

        [Fact]
        public void Disabled_follower_is_skipped()
        {
            var disabled = new FollowerRule(TestSupport.Follower1, false, SizingPolicy.OneToOne(), Array.Empty<InstrumentMapping>());
            var group = new CopyGroup(CopyGroupId.New(), "off", TestSupport.Leader, new[] { disabled }, CopyMode.OrderMirror, GroupEnabledState.Enabled);
            var snapshot = new ActiveConfigSnapshot(
                new ConfigVersion(1), EngineSafetyState.Enabled, RiskPolicy.Default(),
                new[] { group }, TestSupport.Config().Accounts);
            var coordinator = new CopyCoordinator(snapshot, new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            var result = coordinator.ProcessOrder(TestSupport.Order("Ld", LeaderOrderState.Working));
            Assert.Equal(0, result.SubmitCount);
        }

        [Fact]
        public void Observed_and_rejected_first_events_do_not_submit()
        {
            var coordinator = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            Assert.Equal(0, coordinator.ProcessOrder(TestSupport.Order("Lo", LeaderOrderState.Observed)).SubmitCount);
            var rejected = coordinator.ProcessOrder(TestSupport.Order("Lr", LeaderOrderState.Rejected));
            Assert.Equal(0, rejected.SubmitCount);
            var canceled = coordinator.ProcessOrder(TestSupport.Order("Lz", LeaderOrderState.Canceled));
            Assert.Equal(0, canceled.SubmitCount);
        }

        [Fact]
        public void Cap_block_and_lifecycle_cancel_change()
        {
            var policy = new SizingPolicy(SizingMode.OneToOne, 1m, 0, false, 1, null);
            var coordinator = new CopyCoordinator(TestSupport.Config(sizing: policy), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            var blocked = coordinator.ProcessOrder(TestSupport.Order("Lcap", LeaderOrderState.Working, quantity: 5));
            Assert.Equal(0, blocked.SubmitCount);
            Assert.Contains(blocked.Intents, i => i.Kind == IntentKind.RaiseDivergence);

            var live = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            live.ProcessOrder(TestSupport.Order("Lcyc", LeaderOrderState.Working, type: DomainOrderType.Limit, limit: 10m));
            var changed = live.ProcessOrder(TestSupport.Order("Lcyc", LeaderOrderState.Working, type: DomainOrderType.Limit, limit: 11m));
            Assert.Contains(changed.Intents, i => i.Kind == IntentKind.ChangeFollowerOrder);
            var canceled = live.ProcessOrder(TestSupport.Order("Lcyc", LeaderOrderState.Canceled, type: DomainOrderType.Limit, limit: 11m));
            Assert.Contains(canceled.Intents, i => i.Kind == IntentKind.CancelFollowerOrder);
        }

        [Fact]
        public void Telemetry_queue_edges()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedTelemetryQueue(2));
            var q = new BoundedTelemetryQueue(4);
            Assert.Throws<ArgumentNullException>(() => q.TryEnqueue(null!));
            Assert.True(q.TryEnqueue(new TelemetryItem(0, "a", DateTime.UtcNow)));
            Assert.True(q.TryEnqueue(new TelemetryItem(0, "b", DateTime.UtcNow)));
            Assert.True(q.TryEnqueue(new TelemetryItem(0, "c", DateTime.UtcNow)));
            Assert.True(q.TryEnqueue(new TelemetryItem(0, "d", DateTime.UtcNow)));
            Assert.False(q.TryEnqueue(new TelemetryItem(0, "e", DateTime.UtcNow)));
            Assert.NotNull(q.Dequeue());
            Assert.True(q.Count >= 0);
            var oco = OcoGroupId.NewForFollower(TestSupport.Follower1, "");
            Assert.StartsWith("TC-", oco.ToString());
        }

        [Fact]
        public void Null_events_throw_and_sim_only_blocks_unknown_leader()
        {
            var coordinator = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            Assert.Throws<ArgumentNullException>(() => coordinator.ProcessOrder(null!));
            Assert.Throws<ArgumentNullException>(() => coordinator.ProcessExecution(null!));

            var accounts = new Dictionary<AccountKey, AccountDescriptor>
            {
                [TestSupport.Leader] = new AccountDescriptor(TestSupport.Leader, "L", "X", AccountReadiness.Ready, TriState.Unknown),
                [TestSupport.Follower1] = new AccountDescriptor(TestSupport.Follower1, "F", "X", AccountReadiness.Ready, TriState.KnownTrue)
            };
            var snap = new ActiveConfigSnapshot(
                new ConfigVersion(1), EngineSafetyState.Enabled, new RiskPolicy(true, false, true),
                TestSupport.Config().Groups, accounts);
            var sim = new CopyCoordinator(snap, new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            var blocked = sim.ProcessOrder(TestSupport.Order("Lsim", LeaderOrderState.Working));
            Assert.Contains(blocked.Intents, i => i.ReasonCode.Contains("SimulationRequired"));
        }

        [Fact]
        public void Mapping_fail_zero_qty_and_scale_out_to_zero()
        {
            var map = new FollowerRule(
                TestSupport.Follower1, true, SizingPolicy.OneToOne(),
                new[] { new InstrumentMapping("NQ", "MNQ", 10, 10, ExpiryMappingPolicy.ExactMonthOnly) });
            var group = new CopyGroup(CopyGroupId.New(), "m", TestSupport.Leader, new[] { map }, CopyMode.OrderMirror, GroupEnabledState.Enabled);
            var snap = new ActiveConfigSnapshot(
                new ConfigVersion(1), EngineSafetyState.Enabled, RiskPolicy.Default(),
                new[] { group }, TestSupport.Config().Accounts);
            var c1 = new CopyCoordinator(snap, new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            var noExpiry = new NormalizedOrderEvent(
                EventId.New(), DateTime.UtcNow, 1, TestSupport.Leader, new LeaderOrderKey("Lmap"),
                new InstrumentKey("NQ"), OrderActionKind.Buy, DomainOrderType.Market, LeaderOrderState.Working,
                1, 0, null, null, "Day", "", "");
            Assert.Contains(c1.ProcessOrder(noExpiry).Intents, i => i.Kind == IntentKind.RaiseDivergence);

            var tiny = new SizingPolicy(SizingMode.Multiplier, 0.4m, 0, false, null, null);
            var c2 = new CopyCoordinator(TestSupport.Config(sizing: tiny), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            var zero = c2.ProcessOrder(TestSupport.Order("Lzero", LeaderOrderState.Working, quantity: 1));
            Assert.Equal(0, zero.SubmitCount);

            var c3 = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            c3.ProcessOrder(TestSupport.Order("Lso", LeaderOrderState.Working, quantity: 2, type: DomainOrderType.Limit, limit: 1m));
            var flat = c3.ProcessOrder(TestSupport.Order("Lso", LeaderOrderState.Working, quantity: 0, type: DomainOrderType.Limit, limit: 1m));
            Assert.Contains(flat.Intents, i => i.Kind == IntentKind.CancelFollowerOrder);
        }

        [Fact]
        public void Disable_without_clear_blocks_cancel_and_skips_terminal_links()
        {
            var coordinator = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            coordinator.ProcessOrder(TestSupport.Order("Ldis", LeaderOrderState.Working, type: DomainOrderType.Limit, limit: 5m));
            coordinator.ActiveOrders[0].Links[0].Health = FollowerLinkHealth.Filled;
            coordinator.DisableCopying();
            var cancel = coordinator.ProcessOrder(TestSupport.Order("Ldis", LeaderOrderState.Canceled, type: DomainOrderType.Limit, limit: 5m));
            Assert.Contains(cancel.Warnings, w => w == "cancel-blocked-engine-disabled");
        }

        [Fact]
        public void Partial_fill_execution_updates_state()
        {
            var coordinator = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            coordinator.ProcessOrder(TestSupport.Order("Lpf", LeaderOrderState.Working, quantity: 3));
            var exec = new NormalizedExecutionEvent(
                EventId.New(), DateTime.UtcNow, TestSupport.Leader, new ExecutionKey("P1"),
                new LeaderOrderKey("Lpf"), TestSupport.Nq, OrderActionKind.Buy, 1, 10m);
            var result = coordinator.ProcessExecution(exec);
            Assert.Equal(LogicalCopyState.PartiallySatisfied, coordinator.ActiveOrders[0].State);
            var exec2 = new NormalizedExecutionEvent(
                EventId.New(), DateTime.UtcNow, TestSupport.Leader, new ExecutionKey("P2"),
                new LeaderOrderKey("Lpf"), TestSupport.Nq, OrderActionKind.Buy, 2, 10m);
            coordinator.ProcessExecution(exec2);
            Assert.Equal(LogicalCopyState.Satisfied, coordinator.ActiveOrders[0].State);
        }

        [Fact]
        public void Follower_unknown_sim_and_not_ready_when_group_not_blocked()
        {
            var accounts = new Dictionary<AccountKey, AccountDescriptor>
            {
                [TestSupport.Leader] = new AccountDescriptor(TestSupport.Leader, "L", "S", AccountReadiness.Ready, TriState.KnownTrue),
                [TestSupport.Follower1] = new AccountDescriptor(TestSupport.Follower1, "F", "S", AccountReadiness.Disconnected, TriState.Unknown)
            };
            var snap = new ActiveConfigSnapshot(
                new ConfigVersion(1),
                EngineSafetyState.Enabled,
                new RiskPolicy(true, false, false),
                TestSupport.Config().Groups,
                accounts);
            var coordinator = new CopyCoordinator(snap, new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            var result = coordinator.ProcessOrder(TestSupport.Order("Lfnr", LeaderOrderState.Working));
            Assert.Equal(0, result.SubmitCount);
            Assert.Contains(result.Intents, i => i.Kind == IntentKind.RaiseDivergence);
        }

        [Fact]
        public void Price_change_skips_filled_link_and_paused_qty_increase_is_blocked()
        {
            var coordinator = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            coordinator.ProcessOrder(TestSupport.Order("Lch", LeaderOrderState.Working, quantity: 1, type: DomainOrderType.Limit, limit: 1m));
            coordinator.ActiveOrders[0].Links[0].Health = FollowerLinkHealth.Filled;
            var change = coordinator.ProcessOrder(TestSupport.Order("Lch", LeaderOrderState.Working, quantity: 1, type: DomainOrderType.Limit, limit: 2m));
            Assert.DoesNotContain(change.Intents, i => i.Kind == IntentKind.ChangeFollowerOrder);

            var enabled = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            enabled.ProcessOrder(TestSupport.Order("Lpq", LeaderOrderState.Working, quantity: 1, type: DomainOrderType.Limit, limit: 1m));
            enabled.DisableCopying();
            var blocked = enabled.ProcessOrder(TestSupport.Order("Lpq", LeaderOrderState.Working, quantity: 2, type: DomainOrderType.Limit, limit: 2m));
            Assert.Contains(blocked.Warnings, w => w == "change-blocked" || w == "cancel-blocked-engine-disabled" || blocked.SubmitCount == 0);
        }
    }
}
