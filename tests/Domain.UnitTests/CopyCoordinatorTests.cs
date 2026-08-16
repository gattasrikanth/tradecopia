using TradeCopia.Domain;
using TradeCopia.Domain.Engine;
using TradeCopia.Domain.Intents;
using TradeCopia.Domain.Model;
using TradeCopia.Domain.Origin;
using TradeCopia.Domain.Time;

namespace TradeCopia.Domain.UnitTests
{
    public class CopyCoordinatorTests
    {
        private static CopyCoordinator Coordinator(EngineSafetyState engine = EngineSafetyState.Enabled)
        {
            return new CopyCoordinator(TestSupport.Config(engine), new OriginRegistry(), new FrozenClock(System.DateTime.UtcNow, 1));
        }

        [Fact]
        public void Engine_disabled_does_not_submit()
        {
            var coordinator = Coordinator(EngineSafetyState.Disabled);
            var result = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working));
            Assert.Equal(0, result.SubmitCount);
        }

        [Fact]
        public void Pause_new_entries_blocks_entry_submit()
        {
            var coordinator = Coordinator(EngineSafetyState.PausedNewEntries);
            var result = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working));
            Assert.Equal(0, result.SubmitCount);
        }

        [Fact]
        public void Session_then_broker_identity_submits_one_contract()
        {
            var coordinator = Coordinator();
            var first = coordinator.ProcessOrder(TestSupport.Order(
                "12",
                LeaderOrderState.PendingSubmission,
                quantity: 1,
                type: DomainOrderType.Limit,
                limit: 30190m,
                alternate: "542218035271"));
            Assert.Equal(1, first.SubmitCount);
            Assert.Equal(1, first.Intents[0].Quantity);

            var second = coordinator.ProcessOrder(TestSupport.Order(
                "542218035271",
                LeaderOrderState.Working,
                quantity: 1,
                type: DomainOrderType.Limit,
                limit: 30190m));
            Assert.Equal(0, second.SubmitCount);
        }

        [Fact]
        public void First_observation_of_fill_does_not_submit()
        {
            var coordinator = Coordinator();
            var result = coordinator.ProcessOrder(TestSupport.Order("L9", LeaderOrderState.Filled, quantity: 1, filled: 1));
            Assert.Equal(0, result.SubmitCount);
        }

        [Fact]
        public void Ledger_blocks_replay_of_known_leader_order()
        {
            var ledger = new MemoryLedger();
            ledger.Remember("L1");
            var coordinator = new CopyCoordinator(
                TestSupport.Config(),
                new OriginRegistry(),
                new FrozenClock(System.DateTime.UtcNow, 1),
                ledger);
            var result = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working));
            Assert.Equal(0, result.SubmitCount);
        }

        [Fact]
        public void Copier_named_fill_updates_follower_without_resubmit()
        {
            var coordinator = Coordinator();
            var first = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working, quantity: 1));
            Assert.Equal(1, first.SubmitCount);
            var command = first.Intents[0].CommandId.Value.ToString("N");
            var observe = coordinator.ProcessOrder(TestSupport.Order(
                "f1",
                LeaderOrderState.Filled,
                quantity: 1,
                filled: 1,
                name: "TC:" + command,
                account: TestSupport.Follower1));
            Assert.Equal(0, observe.SubmitCount);
            var link = System.Linq.Enumerable.Single(first.Orders[0].Links);
            Assert.Equal(1, link.FilledQuantity);
            Assert.Equal(FollowerLinkHealth.Filled, link.Health);
        }

        [Fact]
        public void Working_market_order_submits_once_per_follower()
        {
            var coordinator = Coordinator();
            var first = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working, quantity: 2));
            Assert.Equal(1, first.SubmitCount);
            Assert.Equal(2, first.Intents[0].Quantity);

            var replay = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working, quantity: 2));
            Assert.Equal(0, replay.SubmitCount);
            Assert.Contains(replay.Intents, i => i.ReasonCode == "duplicate-fingerprint");
        }

        [Fact]
        public void Copier_originated_order_is_never_copied()
        {
            var coordinator = Coordinator();
            var result = coordinator.ProcessOrder(TestSupport.Order("TC:abc", LeaderOrderState.Working, name: "TC:abc"));
            Assert.Equal(0, result.SubmitCount);
            Assert.Contains(result.Intents, i => i.ReasonCode == "loop-prevention");
        }

        [Fact]
        public void Leader_cancel_emits_follower_cancel()
        {
            var coordinator = Coordinator();
            coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working, limit: 100m, type: DomainOrderType.Limit));
            var cancel = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Canceled, type: DomainOrderType.Limit, limit: 100m));
            Assert.Contains(cancel.Intents, i => i.Kind == IntentKind.CancelFollowerOrder);
        }

        [Fact]
        public void Limit_price_change_emits_change_intent()
        {
            var coordinator = Coordinator();
            coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working, type: DomainOrderType.Limit, limit: 100m));
            var change = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working, type: DomainOrderType.Limit, limit: 101m));
            Assert.Contains(change.Intents, i => i.Kind == IntentKind.ChangeFollowerOrder);
        }

        [Fact]
        public void Unsupported_order_type_raises_divergence()
        {
            var coordinator = Coordinator();
            var result = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working, type: DomainOrderType.Unsupported));
            Assert.Equal(0, result.SubmitCount);
            Assert.Contains(result.Intents, i => i.Kind == IntentKind.RaiseDivergence);
        }

        [Fact]
        public void Disconnected_follower_blocks_group_entries()
        {
            var config = TestSupport.Config(followerReady: AccountReadiness.Disconnected);
            var coordinator = new CopyCoordinator(config, new OriginRegistry(), new FrozenClock(System.DateTime.UtcNow, 1));
            var result = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working));
            Assert.Equal(0, result.SubmitCount);
            Assert.Contains(result.Intents, i => i.Kind == IntentKind.RaiseDivergence);
        }

        [Fact]
        public void Two_followers_get_independent_submits()
        {
            var config = TestSupport.Config(extraFollowers: new[] { TestSupport.Follower2 });
            var coordinator = new CopyCoordinator(config, new OriginRegistry(), new FrozenClock(System.DateTime.UtcNow, 1));
            var result = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working));
            Assert.Equal(2, result.SubmitCount);
        }

        [Fact]
        public void Scale_out_does_not_reverse()
        {
            var coordinator = Coordinator();
            coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working, quantity: 3, type: DomainOrderType.Limit, limit: 10m));
            var reduced = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working, quantity: 2, type: DomainOrderType.Limit, limit: 10m));
            var change = System.Linq.Enumerable.Single(reduced.Intents, i => i.Kind == IntentKind.ChangeFollowerOrder);
            Assert.True(change.Quantity >= 0);
            Assert.True(change.Quantity < 3);
        }

        [Fact]
        public void Duplicate_execution_is_ignored()
        {
            var coordinator = Coordinator();
            coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working));
            var exec = new TradeCopia.Domain.Events.NormalizedExecutionEvent(
                EventId.New(),
                System.DateTime.UtcNow,
                TestSupport.Leader,
                new ExecutionKey("E1"),
                new LeaderOrderKey("L1"),
                TestSupport.Nq,
                OrderActionKind.Buy,
                1,
                100m);
            var first = coordinator.ProcessExecution(exec);
            var second = coordinator.ProcessExecution(exec);
            Assert.Contains(second.Intents, i => i.ReasonCode == "duplicate-execution");
            Assert.Empty(first.Intents);
        }

        [Fact]
        public void Terminal_logical_order_does_not_resubmit_on_same_identity()
        {
            var coordinator = Coordinator();
            coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working));
            coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Filled, filled: 1));
            var again = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Filled, filled: 1));
            Assert.Equal(0, again.SubmitCount);
        }

        [Fact]
        public void Simulation_only_rejects_unknown_simulation_flag()
        {
            var sizing = SizingPolicy.OneToOne();
            var follower = new FollowerRule(TestSupport.Follower1, true, sizing, null);
            var group = new CopyGroup(CopyGroupId.New(), "g", TestSupport.Leader, new[] { follower }, CopyMode.OrderMirror, GroupEnabledState.Enabled);
            var accounts = new System.Collections.Generic.Dictionary<AccountKey, AccountDescriptor>
            {
                [TestSupport.Leader] = new AccountDescriptor(TestSupport.Leader, "L", "X", AccountReadiness.Ready, TriState.Unknown),
                [TestSupport.Follower1] = new AccountDescriptor(TestSupport.Follower1, "F", "X", AccountReadiness.Ready, TriState.Unknown)
            };
            var config = new TradeCopia.Domain.Config.ActiveConfigSnapshot(
                new ConfigVersion(1),
                EngineSafetyState.Enabled,
                new RiskPolicy(true, false, true),
                new[] { group },
                accounts);
            var coordinator = new CopyCoordinator(config, new OriginRegistry(), new FrozenClock(System.DateTime.UtcNow, 1));
            var result = coordinator.ProcessOrder(TestSupport.Order("L1", LeaderOrderState.Working));
            Assert.Equal(0, result.SubmitCount);
            Assert.Contains(result.Intents, i => i.ReasonCode.Contains("SimulationRequired"));
        }

        private sealed class MemoryLedger : ILeaderIdentityLedger
        {
            private readonly System.Collections.Generic.HashSet<string> _seen = new(System.StringComparer.Ordinal);

            public bool Contains(string identity) => !string.IsNullOrEmpty(identity) && _seen.Contains(identity);

            public void Remember(string identity)
            {
                if (!string.IsNullOrEmpty(identity))
                {
                    _seen.Add(identity);
                }
            }
        }
    }
}
