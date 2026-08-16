using TradeCopia.Domain.Safety;
using TradeCopia.Protocol;

namespace TradeCopia.Protocol.UnitTests
{
    public class AccountSnapshotTests
    {
        [Fact]
        public void Snapshot_includes_accounts_and_enable_requires_non_live_active_group()
        {
            var session = new ProtocolSession();
            session.ReplaceAccounts(new[]
            {
                new EngineAccountRecord("sim-1", "Sim", "Simulator", "Simulation", false, AccountSafetyClass.Simulation),
                new EngineAccountRecord("live-1", "Live", "InteractiveBrokers", "Live", false, AccountSafetyClass.Live)
            });
            session.Handle(new ProtocolEnvelope(1, "h", ProtocolMessageTypes.Hello, DateTime.UtcNow, "", "{}"));
            var snap = session.Handle(new ProtocolEnvelope(1, "s", ProtocolMessageTypes.RequestSnapshot, DateTime.UtcNow, "", "{}"));
            Assert.Contains("\"stableKey\":\"sim-1\"", snap.Reply.PayloadJson);
            Assert.DoesNotContain("SIM-LEADER-01", snap.Reply.PayloadJson);

            var enableNone = session.Handle(new ProtocolEnvelope(1, "e0", ProtocolMessageTypes.EnableCopying, DateTime.UtcNow, "", "{}"));
            Assert.False(enableNone.Accepted);
            Assert.Equal("no-active-group", enableNone.Reason);

            var liveActivate = session.Handle(new ProtocolEnvelope(1, "a0", ProtocolMessageTypes.ActivateConfig, DateTime.UtcNow, "",
                "{\"leader\":\"live-1\",\"followers\":[\"sim-1\"]}"));
            Assert.False(liveActivate.Accepted);
            Assert.Contains("non-live-required", liveActivate.Reason);

            var self = session.Handle(new ProtocolEnvelope(1, "a1", ProtocolMessageTypes.ActivateConfig, DateTime.UtcNow, "",
                "{\"leader\":\"sim-1\",\"followers\":[\"sim-1\"]}"));
            Assert.False(self.Accepted);
            Assert.Equal("leader-and-follower-required", self.Reason);

            session.ReplaceAccounts(new[]
            {
                new EngineAccountRecord("sim-1", "Sim", "Simulator", "Simulation", false, AccountSafetyClass.Simulation),
                new EngineAccountRecord("demo-1", "Demo", "Provider31", "Live", true, AccountSafetyClass.DemoPaper)
            });
            var activate = session.Handle(new ProtocolEnvelope(1, "a2", ProtocolMessageTypes.ActivateConfig, DateTime.UtcNow, "",
                "{\"leader\":\"sim-1\",\"followers\":[\"demo-1\"]}"));
            Assert.True(activate.Accepted);
            var enable = session.Handle(new ProtocolEnvelope(1, "e1", ProtocolMessageTypes.EnableCopying, DateTime.UtcNow, "", "{}"));
            Assert.True(enable.Accepted);
            Assert.True(session.CopyingEnabled);
        }
    }
}
