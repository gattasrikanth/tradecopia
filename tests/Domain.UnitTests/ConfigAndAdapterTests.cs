using TradeCopia.Domain;
using TradeCopia.Domain.Config;
using TradeCopia.Domain.Intents;
using TradeCopia.Domain.Model;
using TradeCopia.FakeNinjaTrader;
using TradeCopia.Native.Adapter;

namespace TradeCopia.Domain.UnitTests
{
    public class ConfigAndAdapterTests
    {
        [Fact]
        public void Config_validator_rejects_empty_groups()
        {
            var snapshot = new ActiveConfigSnapshot(
                new ConfigVersion(1),
                EngineSafetyState.Disabled,
                RiskPolicy.Default(),
                System.Array.Empty<CopyGroup>(),
                new System.Collections.Generic.Dictionary<AccountKey, AccountDescriptor>());
            var result = ConfigValidator.Validate(snapshot);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Config_validator_rejects_execution_mirror_in_v1()
        {
            var group = new CopyGroup(
                CopyGroupId.New(),
                "exec",
                TestSupport.Leader,
                new[] { new FollowerRule(TestSupport.Follower1, true, SizingPolicy.OneToOne(), null) },
                CopyMode.ExecutionMirror,
                GroupEnabledState.Enabled);
            var snapshot = TestSupport.Config();
            var invalid = new ActiveConfigSnapshot(
                snapshot.Version,
                snapshot.EngineState,
                snapshot.Risk,
                new[] { group },
                snapshot.Accounts);
            Assert.False(ConfigValidator.Validate(invalid).IsValid);
        }

        [Fact]
        public void Disabled_executor_blocks_submit()
        {
            var executor = new DisabledOrderExecutor();
            var intent = new ExecutionIntent(
                CommandId.New(),
                EventId.New(),
                IntentKind.SubmitFollowerOrder,
                CopyGroupId.New(),
                TestSupport.Follower1,
                LogicalOrderId.New(),
                TestSupport.Nq,
                DomainOrderType.Market,
                OrderActionKind.Buy,
                1,
                null,
                null,
                string.Empty,
                "test",
                System.DateTime.UtcNow);
            var result = executor.Execute(intent);
            Assert.False(result.Accepted);
        }

        [Fact]
        public void Subscription_registry_rejects_duplicates()
        {
            var registry = new SubscriptionRegistry();
            registry.Register("account:SIM-LEADER-01:OrderUpdate");
            Assert.Throws<System.InvalidOperationException>(() =>
                registry.Register("account:SIM-LEADER-01:OrderUpdate"));
            registry.UnregisterAll();
            Assert.Equal(0, registry.Count);
        }

        [Fact]
        public void Fake_broker_plus_disabled_executor_never_accepts_submit()
        {
            var broker = new FakeBroker(TestSupport.Config(), new TradeCopia.Domain.Time.FrozenClock(System.DateTime.UtcNow, 1), new DisabledOrderExecutor());
            broker.InjectOrder(TestSupport.Order("L1", LeaderOrderState.Working));
            Assert.Empty(broker.AcceptedIntents);
            Assert.Contains(broker.Results, r => !r.Accepted);
        }
    }
}
