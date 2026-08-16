using System;
using TradeCopia.Domain;
using TradeCopia.Domain.Config;
using TradeCopia.Domain.Divergence;
using TradeCopia.Domain.Engine;
using TradeCopia.Domain.Events;
using TradeCopia.Domain.Mapping;
using TradeCopia.Domain.Model;
using TradeCopia.Domain.Origin;
using TradeCopia.Domain.Reconcile;
using TradeCopia.Domain.Sizing;
using TradeCopia.Domain.Time;

namespace TradeCopia.Domain.UnitTests
{
    public class CoverageBoostTests
    {
        [Fact]
        public void Identifier_object_equals_rejects_wrong_type()
        {
            Assert.False(new AccountKey("A").Equals("A"));
            Assert.False(new InstrumentKey("I").Equals(1));
            Assert.False(new LeaderOrderKey("L").Equals(new object()));
            Assert.False(new FollowerOrderKey("F").Equals(new AccountKey("A")));
            Assert.False(new ExecutionKey("E").Equals(DateTime.UtcNow));
            Assert.False(CopyGroupId.New().Equals(Guid.Empty));
            Assert.False(LogicalOrderId.New().Equals("x"));
            Assert.False(LogicalTradeId.New().Equals(0));
            Assert.False(CommandId.New().Equals(false));
            Assert.False(EventId.New().Equals(1.2));
            Assert.False(new ConfigVersion(1).Equals("1"));
            Assert.False(DivergenceId.New().Equals(new object()));
            Assert.False(new OcoGroupId("oco").Equals(new object()));
        }

        [Fact]
        public void Divergence_evaluator_covers_missing_disconnected_unknown()
        {
            var failed = new LogicalOrder(LogicalOrderId.New(), CopyGroupId.New(), TestSupport.Order("L-f", LeaderOrderState.Working));
            failed.State = LogicalCopyState.Failed;
            var missing = DivergenceEvaluator.Evaluate(failed);
            Assert.Contains(missing, f => f.Class == DivergenceClass.MissingFollowerOrder);

            var order = new LogicalOrder(LogicalOrderId.New(), CopyGroupId.New(), TestSupport.Order("L-d", LeaderOrderState.Working));
            order.Links.Add(new FollowerLink(TestSupport.Follower1, 1, TestSupport.Nq) { Health = FollowerLinkHealth.Disconnected });
            order.Links.Add(new FollowerLink(TestSupport.Follower2, 1, TestSupport.Nq) { Health = FollowerLinkHealth.Unknown });
            var findings = DivergenceEvaluator.Evaluate(order);
            Assert.Contains(findings, f => f.Class == DivergenceClass.FollowerDisconnected);
            Assert.Contains(findings, f => f.Class == DivergenceClass.UnknownNativeOrderState);
            Assert.Throws<ArgumentNullException>(() => DivergenceEvaluator.Evaluate(null!));
        }

        [Fact]
        public void Mapper_and_sizing_edge_paths()
        {
            Assert.Throws<ArgumentNullException>(() => InstrumentMapper.Map(null!, TestSupport.Nq));
            var none = new FollowerRule(TestSupport.Follower1, true, SizingPolicy.OneToOne(), Array.Empty<InstrumentMapping>());
            Assert.Equal("same-instrument", InstrumentMapper.Map(none, TestSupport.Nq).Reason);

            var unmappedRoot = new FollowerRule(
                TestSupport.Follower1,
                true,
                SizingPolicy.OneToOne(),
                new[] { new InstrumentMapping("ES", "MES", 10, 10, ExpiryMappingPolicy.ExactMonthOnly) });
            Assert.Equal("same-instrument", InstrumentMapper.Map(unmappedRoot, TestSupport.Nq).Reason);

            Assert.Equal(0, SizingEngine.ComputeEntryQuantity(0, SizingPolicy.OneToOne(), 0).Quantity);
            Assert.Equal(2, SizingEngine.ComputeScaleOutRemaining(0, 0, 2, 2)); // unknown leader basis: do not invent a flatten
            Assert.Equal(2, SizingEngine.ComputeScaleOutRemaining(3, 5, 2, 2)); // remaining clamped to initial; never reverse
            Assert.Equal(0, SizingEngine.ComputeScaleOutRemaining(3, -1, 2, 2));
            Assert.Equal(0, SizingEngine.ComputeScaleOutRemaining(3, 1, 2, 0));
            Assert.Equal(1, SizingEngine.ComputeScaleOutReduction(3, 2, 2, 2));
            Assert.Equal(-3, SizingEngine.FloorTowardZero(-3.2m));
            Assert.Throws<ArgumentNullException>(() => SizingEngine.ComputeEntryQuantity(1, null!, 0));
        }

        [Fact]
        public void Coordinator_rejects_invalid_replacement_config()
        {
            var coordinator = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            Assert.Throws<ArgumentNullException>(() => coordinator.ReplaceConfig(null!));
            var empty = new ActiveConfigSnapshot(
                new ConfigVersion(2),
                EngineSafetyState.Disabled,
                RiskPolicy.Default(),
                Array.Empty<CopyGroup>(),
                TestSupport.Config().Accounts);
            Assert.Throws<InvalidOperationException>(() => coordinator.ReplaceConfig(empty));
            coordinator.ReplaceConfig(TestSupport.Config());
        }

        [Fact]
        public void Reconcile_null_request_cannot_execute()
        {
            Assert.False(ReconcilePlanner.CanExecute(null!, null!, DateTime.UtcNow));
            var plan = ReconcilePlanner.Preview(new ConfigVersion(1), "h", Array.Empty<ReconcileAction>(), DateTime.UtcNow);
            Assert.False(ReconcilePlanner.CanExecute(plan, new ReconcileExecutionRequest(Guid.NewGuid(), plan.ConfigVersion, "h"), DateTime.UtcNow));
        }

        [Fact]
        public void Descriptors_and_clocks_cover_remaining_getters()
        {
            var d = new InstrumentDescriptor(TestSupport.Nq, "NQ 06-26", "NQ", "06-26", 0.25m);
            Assert.Equal(TestSupport.Nq, d.Key);
            Assert.Equal("NQ 06-26", d.FullName);
            Assert.Equal("06-26", d.Expiry);
            var map = new InstrumentMapping("NQ", "MNQ", 10, 10, ExpiryMappingPolicy.ExactMonthOnly);
            Assert.Equal(10m, map.ContractValueRatio);
            Assert.Equal(10m, map.DefaultQuantityRatio);
            Assert.Equal(ExpiryMappingPolicy.ExactMonthOnly, map.ExpiryPolicy);
            var clock = new SystemClock();
            Assert.True(clock.UtcNow <= DateTime.UtcNow.AddMinutes(1));
            var frozen = new FrozenClock(DateTime.UtcNow, 1);
            frozen.Advance(TimeSpan.FromTicks(2));
            Assert.Equal(3, frozen.HighResolutionTicks);
        }

        [Fact]
        public void Event_helpers_and_origin_empty_key()
        {
            var evt = TestSupport.Order("L-e", LeaderOrderState.Filled, quantity: 2, filled: 2);
            Assert.True(evt.IsTerminal);
            Assert.Equal(0, evt.WorkingQuantity);
            var origin = new OriginRegistry();
            Assert.False(origin.IsCopierOriginated(TestSupport.Follower1, " "));
            Assert.False(origin.IsCopierOriginated(TestSupport.Follower1, ""));
        }

        [Fact]
        public void Coordinator_no_group_and_disabled_cancel()
        {
            var coordinator = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(DateTime.UtcNow, 1));
            var unknown = coordinator.ProcessOrder(TestSupport.Order("Lx", LeaderOrderState.Working, account: new AccountKey("SIM-OTHER")));
            Assert.Contains(unknown.Intents, i => i.ReasonCode == "no-group");

            coordinator.ProcessOrder(TestSupport.Order("Lc", LeaderOrderState.Working, type: DomainOrderType.Limit, limit: 1m));
            coordinator.ResetAfterEngineRestart();
            var cancel = coordinator.ProcessOrder(TestSupport.Order("Lc", LeaderOrderState.Canceled, type: DomainOrderType.Limit, limit: 1m));
            Assert.Equal(0, cancel.SubmitCount);
        }
    }
}
