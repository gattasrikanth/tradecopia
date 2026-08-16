using TradeCopia.Domain;
using TradeCopia.Domain.Intents;
using TradeCopia.Native.Adapter;

namespace TradeCopia.Domain.UnitTests
{
    public class SimulationGateTests
    {
        [Theory]
        [InlineData("Simulator", TriState.KnownTrue)]
        [InlineData("Playback", TriState.KnownTrue)]
        [InlineData("Unknown", TriState.Unknown)]
        [InlineData("", TriState.Unknown)]
        [InlineData("InteractiveBrokers", TriState.KnownFalse)]
        [InlineData("TradingTechnologies", TriState.KnownFalse)]
        public void Provider_classification_is_official_enum_not_name_substring(string provider, TriState expected)
        {
            Assert.Equal(expected, AccountSimulationGate.ClassifyProvider(provider));
        }

        [Fact]
        public void Spoofed_sim_account_name_does_not_pass_live_provider()
        {
            Assert.Equal(TriState.KnownFalse, AccountSimulationGate.ClassifyProvider("InteractiveBrokers"));
            Assert.False(AccountSimulationGate.AllowsNativeSubmit(TriState.KnownFalse));
            Assert.False(AccountSimulationGate.AllowsNativeSubmit(TriState.Unknown));
        }

        [Fact]
        public void Guarded_runtime_blocks_when_copying_disabled_or_unknown()
        {
            var inner = new RecordingOrderExecutor();
            var runtime = new GuardedNativeRuntime(inner, _ => TriState.KnownTrue, () => false);
            var intent = SubmitIntent();
            var blocked = runtime.Dispatch(intent);
            Assert.False(blocked.Accepted);
            Assert.Equal("copying-disabled", blocked.Reason);
            Assert.Equal(0, inner.SubmitAttempts);

            var live = new GuardedNativeRuntime(inner, _ => TriState.KnownFalse, () => true);
            var liveBlocked = live.Dispatch(intent);
            Assert.False(liveBlocked.Accepted);
            Assert.Contains("simulation-not-positive", liveBlocked.Reason);
            Assert.Equal(0, inner.SubmitAttempts);

            var unknown = new GuardedNativeRuntime(inner, _ => TriState.Unknown, () => true);
            Assert.False(unknown.Dispatch(intent).Accepted);
            Assert.Equal(0, inner.SubmitAttempts);

            var sim = new GuardedNativeRuntime(inner, _ => TriState.KnownTrue, () => true);
            Assert.True(sim.Dispatch(intent).Accepted);
            Assert.Equal(1, inner.SubmitAttempts);
        }

        private static ExecutionIntent SubmitIntent()
        {
            return new ExecutionIntent(
                CommandId.New(),
                EventId.New(),
                IntentKind.SubmitFollowerOrder,
                null,
                new AccountKey("SIM-FOLLOWER-01"),
                null,
                new InstrumentKey("NQ 06-26"),
                DomainOrderType.Market,
                OrderActionKind.Buy,
                1,
                null,
                null,
                string.Empty,
                "test",
                DateTime.UtcNow);
        }
    }
}
