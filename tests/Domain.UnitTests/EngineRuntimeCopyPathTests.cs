using TradeCopia.Domain;
using TradeCopia.Domain.Events;
using TradeCopia.Domain.Intents;
using TradeCopia.Domain.Safety;
using TradeCopia.Native.Adapter;
using TradeCopia.Protocol;

namespace TradeCopia.Domain.UnitTests
{
    public class EngineRuntimeCopyPathTests
    {
        [Fact]
        public void Enabled_leader_order_dispatches_to_demo_follower_executor()
        {
            var recorder = new RecordingOrderExecutor();
            var pipe = EnginePipeName.FromMaterial("copy-" + Guid.NewGuid().ToString("N"));
            using var runtime = new EngineRuntime(pipe, recorder, key =>
                key.Value == "sim-1" || key.Value == "demo-1" ? TriState.KnownTrue : TriState.Unknown);

            runtime.PublishAccounts(new[]
            {
                new EngineAccountRecord("sim-1", "Sim", "Simulator", "Simulation", false, AccountSafetyClass.Simulation),
                new EngineAccountRecord("demo-1", "Demo", "Provider31", "Live", false, AccountSafetyClass.DemoPaper)
            });

            Assert.True(runtime.Session.Handle(Envelope(ProtocolMessageTypes.Hello)).Accepted);
            Assert.True(runtime.Session.Handle(Envelope(
                ProtocolMessageTypes.ActivateConfig,
                "{\"leader\":\"sim-1\",\"followers\":[\"demo-1\"]}")).Accepted);
            Assert.True(runtime.Session.Handle(Envelope(ProtocolMessageTypes.EnableCopying)).Accepted);
            Assert.True(runtime.Session.CopyingEnabled);

            var results = runtime.HandleOrder(new NormalizedOrderEvent(
                EventId.New(),
                DateTime.UtcNow,
                1,
                new AccountKey("sim-1"),
                new LeaderOrderKey("leader-1"),
                new InstrumentKey("MNQ 09-26"),
                OrderActionKind.Buy,
                DomainOrderType.Market,
                LeaderOrderState.Working,
                1,
                0,
                null,
                null,
                "Day",
                string.Empty,
                "MNQ"));

            Assert.Contains(results, r => r.Accepted);
            Assert.Equal(1, recorder.SubmitAttempts);
            Assert.NotNull(recorder.Last);
            Assert.Equal(IntentKind.SubmitFollowerOrder, recorder.Last!.Kind);
            Assert.Equal("demo-1", recorder.Last.Follower!.Value.Value);
            Assert.Equal(1, recorder.Last.Quantity);
        }

        [Fact]
        public void Disabled_copying_does_not_reach_executor()
        {
            var recorder = new RecordingOrderExecutor();
            var pipe = EnginePipeName.FromMaterial("copy-off-" + Guid.NewGuid().ToString("N"));
            using var runtime = new EngineRuntime(pipe, recorder, _ => TriState.KnownTrue);
            runtime.PublishAccounts(new[]
            {
                new EngineAccountRecord("sim-1", "Sim", "Simulator", "Simulation", false, AccountSafetyClass.Simulation),
                new EngineAccountRecord("demo-1", "Demo", "Provider31", "Live", false, AccountSafetyClass.DemoPaper)
            });
            runtime.Session.Handle(Envelope(ProtocolMessageTypes.Hello));
            runtime.Session.Handle(Envelope(
                ProtocolMessageTypes.ActivateConfig,
                "{\"leader\":\"sim-1\",\"followers\":[\"demo-1\"]}"));

            runtime.HandleOrder(new NormalizedOrderEvent(
                EventId.New(),
                DateTime.UtcNow,
                1,
                new AccountKey("sim-1"),
                new LeaderOrderKey("leader-off"),
                new InstrumentKey("MNQ 09-26"),
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

            Assert.Equal(0, recorder.SubmitAttempts);
        }

        [Fact]
        public void Stable_key_matches_provider_and_id_not_only_display_name()
        {
            Assert.True(NativeAccountIdentity.Matches("Provider31|3", "Provider31", "3", "other", "other"));
            Assert.False(NativeAccountIdentity.Matches("Provider31|3", "Provider31", "9", "DEMO", "DEMO"));
            Assert.Equal("Provider31|3", NativeAccountIdentity.StableKey("Provider31", "3"));
        }

        private static ProtocolEnvelope Envelope(string type, string payload = "{}")
        {
            return new ProtocolEnvelope(1, Guid.NewGuid().ToString("N"), type, DateTime.UtcNow, "", payload);
        }
    }
}
