using TradeCopia.Domain;
using TradeCopia.Domain.Divergence;
using TradeCopia.Domain.Engine;
using TradeCopia.FakeNinjaTrader;
using TradeCopia.Native.Adapter;
using TradeCopia.Protocol;

namespace TradeCopia.Domain.UnitTests
{
    public class FailureInjectionTests
    {
        [Fact]
        public void Duplicate_leader_event_does_not_double_submit()
        {
            var broker = new FakeBroker(TestSupport.Config(), new TradeCopia.Domain.Time.FrozenClock(System.DateTime.UtcNow, 1), new DisabledOrderExecutor());
            var first = broker.InjectOrder(TestSupport.Order("L-dup", LeaderOrderState.Working));
            var second = broker.InjectOrder(TestSupport.Order("L-dup", LeaderOrderState.Working));
            Assert.Equal(1, first.SubmitCount);
            Assert.Equal(0, second.SubmitCount);
        }

        [Fact]
        public void Ipc_loss_does_not_invent_execute_order()
        {
            var session = new ProtocolSession();
            session.Handle(new ProtocolEnvelope(1, "1", ProtocolMessageTypes.Hello, System.DateTime.UtcNow, "s", "{}"));
            session.Disconnect();
            var result = session.Handle(new ProtocolEnvelope(1, "2", "ExecuteOrder", System.DateTime.UtcNow, "s", "{}"));
            Assert.False(result.Accepted);
            Assert.DoesNotContain("ExecuteOrder", result.Reply.MessageType);
        }

        [Fact]
        public void Rejected_follower_is_visible_not_healthy()
        {
            var order = new LogicalOrder(
                LogicalOrderId.New(),
                CopyGroupId.New(),
                TestSupport.Order("L-rej", LeaderOrderState.Working));
            order.Links.Add(new FollowerLink(TestSupport.Follower1, 1, TestSupport.Nq)
            {
                Health = FollowerLinkHealth.Rejected,
                LastError = "sim-reject"
            });
            var findings = DivergenceEvaluator.Evaluate(order);
            Assert.Contains(findings, f => f.Class == DivergenceClass.FollowerRejected);
        }
    }
}
