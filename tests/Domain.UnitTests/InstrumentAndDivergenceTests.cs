using TradeCopia.Domain;
using TradeCopia.Domain.Divergence;
using TradeCopia.Domain.Engine;
using TradeCopia.Domain.Events;
using TradeCopia.Domain.Mapping;
using TradeCopia.Domain.Model;
using TradeCopia.Domain.Origin;
using TradeCopia.Domain.Time;

namespace TradeCopia.Domain.UnitTests
{
    public class InstrumentAndDivergenceTests
    {
        [Fact]
        public void Mini_micro_mapping_keeps_expiry_suffix()
        {
            var follower = new FollowerRule(
                TestSupport.Follower1,
                true,
                SizingPolicy.OneToOne(),
                new[] { new InstrumentMapping("NQ", "MNQ", 10m, 10m, ExpiryMappingPolicy.ExactMonthOnly) });

            var result = InstrumentMapper.Map(follower, new InstrumentKey("NQ 06-26"));
            Assert.True(result.Succeeded);
            Assert.Equal("MNQ 06-26", result.Instrument!.Value.Value);
        }

        [Fact]
        public void Mapping_without_expiry_suffix_fails_closed()
        {
            var follower = new FollowerRule(
                TestSupport.Follower1,
                true,
                SizingPolicy.OneToOne(),
                new[] { new InstrumentMapping("NQ", "MNQ", 10m, 10m, ExpiryMappingPolicy.ExactMonthOnly) });

            var result = InstrumentMapper.Map(follower, new InstrumentKey("NQ"));
            Assert.False(result.Succeeded);
            Assert.Equal("expiry-unresolved", result.Reason);
        }

        [Fact]
        public void Coordinator_maps_nq_to_mnq_on_submit()
        {
            var sizing = SizingPolicy.OneToOne();
            var follower = new FollowerRule(
                TestSupport.Follower1,
                true,
                sizing,
                new[] { new InstrumentMapping("NQ", "MNQ", 10m, 10m, ExpiryMappingPolicy.ExactMonthOnly) });
            var group = new CopyGroup(CopyGroupId.New(), "map", TestSupport.Leader, new[] { follower }, CopyMode.OrderMirror, GroupEnabledState.Enabled);
            var config = new TradeCopia.Domain.Config.ActiveConfigSnapshot(
                new ConfigVersion(1),
                EngineSafetyState.Enabled,
                RiskPolicy.Default(),
                new[] { group },
                new System.Collections.Generic.Dictionary<AccountKey, AccountDescriptor>
                {
                    [TestSupport.Leader] = new AccountDescriptor(TestSupport.Leader, "L", "Sim", AccountReadiness.Ready, TriState.KnownTrue),
                    [TestSupport.Follower1] = new AccountDescriptor(TestSupport.Follower1, "F", "Sim", AccountReadiness.Ready, TriState.KnownTrue)
                });
            var coordinator = new CopyCoordinator(config, new OriginRegistry(), new FrozenClock(System.DateTime.UtcNow, 1));
            var result = coordinator.ProcessOrder(TestSupport.Order("L-map", LeaderOrderState.Working, type: DomainOrderType.StopMarket, stop: 18000m));
            Assert.Equal(1, result.SubmitCount);
            Assert.Equal("MNQ 06-26", result.Intents[0].Instrument!.Value.Value);
            Assert.Equal(DomainOrderType.StopMarket, result.Intents[0].OrderType);
        }

        [Theory]
        [InlineData(DomainOrderType.Limit)]
        [InlineData(DomainOrderType.StopLimit)]
        [InlineData(DomainOrderType.Mit)]
        public void Supported_working_types_submit(DomainOrderType type)
        {
            var coordinator = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(System.DateTime.UtcNow, 1));
            var result = coordinator.ProcessOrder(TestSupport.Order("L-" + type, LeaderOrderState.Working, type: type, limit: 100m, stop: 99m));
            Assert.Equal(1, result.SubmitCount);
            Assert.Equal(type, result.Intents[0].OrderType);
        }

        [Fact]
        public void Dry_run_does_not_submit()
        {
            var snapshot = TestSupport.Config();
            var dry = new TradeCopia.Domain.Config.ActiveConfigSnapshot(
                snapshot.Version,
                snapshot.EngineState,
                new RiskPolicy(false, true, true),
                snapshot.Groups,
                snapshot.Accounts);
            var coordinator = new CopyCoordinator(dry, new OriginRegistry(), new FrozenClock(System.DateTime.UtcNow, 1));
            var result = coordinator.ProcessOrder(TestSupport.Order("L-dry", LeaderOrderState.Working));
            Assert.Equal(0, result.SubmitCount);
            Assert.Contains(result.Warnings, w => w.StartsWith("dry-run"));
        }

        [Fact]
        public void Rejected_link_is_a_visible_divergence()
        {
            var order = new LogicalOrder(
                LogicalOrderId.New(),
                CopyGroupId.New(),
                new NormalizedOrderEvent(
                    EventId.New(),
                    System.DateTime.UtcNow,
                    1,
                    TestSupport.Leader,
                    new LeaderOrderKey("L-div"),
                    TestSupport.Nq,
                    OrderActionKind.Buy,
                    DomainOrderType.Market,
                    LeaderOrderState.Working,
                    1,
                    0,
                    null,
                    null,
                    "Day",
                    string.Empty,
                    string.Empty));
            order.Links.Add(new FollowerLink(TestSupport.Follower1, 1, TestSupport.Nq)
            {
                Health = FollowerLinkHealth.Rejected,
                LastError = "order rejected"
            });

            var findings = DivergenceEvaluator.Evaluate(order);
            Assert.Contains(findings, f => f.Class == DivergenceClass.FollowerRejected);
            Assert.DoesNotContain(findings, f => f.Severity == Severity.Info && f.Class == DivergenceClass.FollowerRejected);
        }
    }
}
