using TradeCopia.Domain;
using TradeCopia.Domain.Engine;
using TradeCopia.Domain.Intents;
using TradeCopia.Domain.Origin;
using TradeCopia.Domain.Telemetry;
using TradeCopia.Domain.Time;
using TradeCopia.Native.Adapter;

namespace TradeCopia.Domain.UnitTests
{
    public class RestartTelemetryAndSimTests
    {
        [Fact]
        public void Restart_disables_copying_and_clears_mappings()
        {
            var coordinator = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), new FrozenClock(System.DateTime.UtcNow, 10));
            var first = coordinator.ProcessOrder(TestSupport.Order("L-rst", LeaderOrderState.Working));
            Assert.Equal(1, first.SubmitCount);
            coordinator.ResetAfterEngineRestart();
            Assert.Equal(EngineSafetyState.Disabled, coordinator.Config.EngineState);
            var after = coordinator.ProcessOrder(TestSupport.Order("L-rst-2", LeaderOrderState.Working));
            Assert.Equal(0, after.SubmitCount);
        }

        [Fact]
        public void Hot_path_records_latency_without_io()
        {
            var clock = new FrozenClock(System.DateTime.UtcNow, 5000);
            var coordinator = new CopyCoordinator(TestSupport.Config(), new OriginRegistry(), clock);
            var result = coordinator.ProcessOrder(TestSupport.Order("L-lat", LeaderOrderState.Working));
            Assert.NotNull(result.Latency);
            Assert.True(result.Latency.DecidedTicks >= result.Latency.ObservedTicks);
        }

        [Fact]
        public void Telemetry_queue_drops_low_priority_under_pressure()
        {
            var queue = new BoundedTelemetryQueue(4);
            Assert.True(queue.TryEnqueue(new TelemetryItem(2, "P2-a", System.DateTime.UtcNow)));
            Assert.True(queue.TryEnqueue(new TelemetryItem(2, "P2-b", System.DateTime.UtcNow)));
            Assert.True(queue.TryEnqueue(new TelemetryItem(2, "P2-c", System.DateTime.UtcNow)));
            Assert.True(queue.TryEnqueue(new TelemetryItem(2, "P2-d", System.DateTime.UtcNow)));
            Assert.False(queue.TryEnqueue(new TelemetryItem(2, "P2-e", System.DateTime.UtcNow)));
            Assert.True(queue.Dropped >= 1);
            Assert.True(queue.TryEnqueue(new TelemetryItem(0, "P0-safety", System.DateTime.UtcNow)));
        }

        [Fact]
        public void Simulation_guard_blocks_unknown_and_allows_known_true()
        {
            var recorder = new RecordingOrderExecutor();
            var guarded = new SimulationGuardedExecutor(recorder, key =>
                key.Value == "SIM-FOLLOWER-01" ? TriState.KnownTrue : TriState.Unknown);

            var submit = new ExecutionIntent(
                CommandId.New(),
                EventId.New(),
                IntentKind.SubmitFollowerOrder,
                CopyGroupId.New(),
                new AccountKey("SIM-LIVE-UNKNOWN"),
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

            var blocked = guarded.Execute(submit);
            Assert.False(blocked.Accepted);
            Assert.Contains("simulation-not-positive", blocked.Reason);
            Assert.Equal(0, recorder.SubmitAttempts);

            var simSubmit = new ExecutionIntent(
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
            var ok = guarded.Execute(simSubmit);
            Assert.True(ok.Accepted);
            Assert.Equal(1, recorder.SubmitAttempts);
        }

        [Fact]
        public void Default_disabled_executor_never_submits()
        {
            var disabled = new DisabledOrderExecutor();
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
                "x",
                System.DateTime.UtcNow);
            Assert.False(disabled.Execute(intent).Accepted);
        }

        [Fact]
        public void Name_substring_is_not_sufficient_for_simulation()
        {
            Assert.False(SimulationIdentity.IsPositiveSimulation(TriState.Unknown));
            Assert.False(SimulationIdentity.IsPositiveSimulation(TriState.KnownFalse));
            Assert.True(SimulationIdentity.IsPositiveSimulation(TriState.KnownTrue));
        }
    }
}
