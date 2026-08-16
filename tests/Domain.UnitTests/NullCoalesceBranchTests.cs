using System;
using TradeCopia.Domain;
using TradeCopia.Domain.Config;
using TradeCopia.Domain.Divergence;
using TradeCopia.Domain.Events;
using TradeCopia.Domain.Intents;
using TradeCopia.Domain.Mapping;
using TradeCopia.Domain.Model;
using TradeCopia.Domain.Reconcile;
using TradeCopia.Domain.Sizing;
using TradeCopia.Domain.Telemetry;
using TradeCopia.Domain.Topology;

namespace TradeCopia.Domain.UnitTests
{
    public class NullCoalesceBranchTests
    {
        [Fact]
        public void Constructors_accept_null_optional_strings()
        {
            var acct = new AccountDescriptor(TestSupport.Leader, null!, null!, AccountReadiness.Unknown, TriState.Unknown);
            Assert.Equal(string.Empty, acct.DisplayName);
            Assert.Equal(string.Empty, acct.ConnectionName);

            var inst = new InstrumentDescriptor(TestSupport.Nq, null!, null!, null!, 0.25m);
            Assert.Equal(string.Empty, inst.FullName);
            Assert.Equal(string.Empty, inst.RootSymbol);
            Assert.Equal(string.Empty, inst.Expiry);

            var item = new TelemetryItem(0, null!, DateTime.UtcNow);
            Assert.Equal(string.Empty, item.Code);

            var action = new ReconcileAction(null!, false);
            Assert.Equal(string.Empty, action.Description);

            var finding = new DivergenceFinding(
                DivergenceClass.ConfigMismatch, Severity.Warning, TestSupport.Leader, null, null, null!);
            Assert.Equal(string.Empty, finding.Detail);

            var sizing = new SizingResult(0, false, null!);
            Assert.Equal(string.Empty, sizing.Reason);

            var topo = new TopologyValidationResult(true, null!);
            Assert.Empty(topo.Errors);

            var map = new InstrumentMapResult(false, null, null!);
            Assert.Equal(string.Empty, map.Reason);

            var evt = new NormalizedOrderEvent(
                EventId.New(), DateTime.UtcNow, 1, TestSupport.Leader, new LeaderOrderKey("n"),
                TestSupport.Nq, OrderActionKind.Sell, DomainOrderType.Market, LeaderOrderState.Working,
                1, 0, null, null, null!, null!, null!);
            Assert.Equal(string.Empty, evt.TimeInForce);
            Assert.Equal(string.Empty, evt.OcoIdentity);
            Assert.Equal(string.Empty, evt.OrderName);
            Assert.False(evt.LooksLikeEntry);

            var intent = new ExecutionIntent(
                CommandId.New(), EventId.New(), IntentKind.NoOp, null, null, null, null,
                DomainOrderType.Market, OrderActionKind.Buy, 0, null, null, null!, null!, DateTime.UtcNow);
            Assert.Equal(string.Empty, intent.OcoId);
            Assert.Equal(string.Empty, intent.ReasonCode);

            var plan = new ReconcilePlan(
                Guid.NewGuid(), new ConfigVersion(1), null!, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(1),
                ReconcileRiskLevel.None, null!, null!, null!);
            Assert.Empty(plan.Actions);
            Assert.Empty(plan.Warnings);
            Assert.Empty(plan.Unresolvable);

            var rule = new FollowerRule(TestSupport.Follower1, true, SizingPolicy.OneToOne(), null);
            Assert.Empty(rule.InstrumentMappings);
        }
    }
}
