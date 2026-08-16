using System;
using TradeCopia.Domain;
using TradeCopia.Domain.Model;
using TradeCopia.Domain.Sizing;
using TradeCopia.Domain.Time;

namespace TradeCopia.Domain.UnitTests
{
    public class IdentifierAndModelTests
    {
        [Fact]
        public void String_keys_reject_empty_and_compare_by_value()
        {
            Assert.Throws<ArgumentException>(() => new AccountKey(" "));
            Assert.Throws<ArgumentException>(() => new InstrumentKey(""));
            Assert.Throws<ArgumentException>(() => new LeaderOrderKey(null!));
            Assert.Throws<ArgumentException>(() => new FollowerOrderKey(""));
            Assert.Throws<ArgumentException>(() => new ExecutionKey(""));
            Assert.Throws<ArgumentException>(() => new OcoGroupId(""));

            var a = new AccountKey("SIM-A");
            var b = new AccountKey("SIM-A");
            var c = new AccountKey("SIM-B");
            Assert.True(a == b);
            Assert.True(a != c);
            Assert.True(a.Equals((object)b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.Equal("SIM-A", a.ToString());

            var i1 = new InstrumentKey("NQ 06-26");
            Assert.True(i1 == new InstrumentKey("NQ 06-26"));
            Assert.True(i1 != new InstrumentKey("ES 06-26"));
            Assert.Equal("NQ 06-26", i1.ToString());
            Assert.Equal(i1.GetHashCode(), new InstrumentKey("NQ 06-26").GetHashCode());

            var o1 = new LeaderOrderKey("L1");
            Assert.True(o1 == new LeaderOrderKey("L1"));
            Assert.True(o1 != new LeaderOrderKey("L2"));
            Assert.Equal("L1", o1.ToString());

            var f1 = new FollowerOrderKey("F1");
            Assert.True(f1 == new FollowerOrderKey("F1"));
            Assert.True(f1 != new FollowerOrderKey("F2"));
            Assert.Equal("F1", f1.ToString());

            var e1 = new ExecutionKey("E1");
            Assert.True(e1 == new ExecutionKey("E1"));
            Assert.True(e1 != new ExecutionKey("E2"));
            Assert.Equal("E1", e1.ToString());
        }

        [Fact]
        public void Guid_ids_reject_empty_and_support_operators()
        {
            Assert.Throws<ArgumentException>(() => new CopyGroupId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new LogicalOrderId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new LogicalTradeId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new CommandId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new EventId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new DivergenceId(Guid.Empty));

            var g = CopyGroupId.New();
            Assert.True(g == new CopyGroupId(g.Value));
            Assert.True(g != CopyGroupId.New());
            Assert.Equal(g.Value.ToString("D"), g.ToString());
            Assert.True(g.Equals((object)new CopyGroupId(g.Value)));

            var trade = LogicalTradeId.New();
            Assert.True(trade == new LogicalTradeId(trade.Value));
            Assert.True(trade != LogicalTradeId.New());
            Assert.Equal(trade.Value.GetHashCode(), new LogicalTradeId(trade.Value).GetHashCode());

            var div = DivergenceId.New();
            Assert.True(div == new DivergenceId(div.Value));
            Assert.True(div != DivergenceId.New());

            var cmd = CommandId.New();
            Assert.True(cmd == new CommandId(cmd.Value));
            Assert.True(cmd != CommandId.New());
            Assert.Equal(cmd.ToString(), cmd.Value.ToString("D"));

            var ev = EventId.New();
            Assert.True(ev == new EventId(ev.Value));
            Assert.True(ev != EventId.New());
        }

        [Fact]
        public void Config_version_is_monotonic()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConfigVersion(-1));
            var v = new ConfigVersion(3);
            Assert.Equal(4, v.Next().Value);
            Assert.True(v == new ConfigVersion(3));
            Assert.True(v != new ConfigVersion(4));
            Assert.True(v.CompareTo(new ConfigVersion(4)) < 0);
            Assert.Equal("3", v.ToString());
            Assert.Equal(v.GetHashCode(), new ConfigVersion(3).GetHashCode());
        }

        [Fact]
        public void Oco_ids_are_follower_specific()
        {
            var a = OcoGroupId.NewForFollower(TestSupport.Follower1, "leader-oco");
            var b = OcoGroupId.NewForFollower(TestSupport.Follower2, "leader-oco");
            Assert.NotEqual(a, b);
            Assert.StartsWith("TC-", a.ToString());
            Assert.True(a != b);
            Assert.True(a == new OcoGroupId(a.Value));
        }

        [Fact]
        public void Instrument_models_validate_inputs()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new InstrumentDescriptor(TestSupport.Nq, "NQ", "NQ", "06-26", -1));
            var d = new InstrumentDescriptor(TestSupport.Nq, "NQ 06-26", "NQ", "06-26", 0.25m);
            Assert.Equal("NQ", d.RootSymbol);
            Assert.Equal(0.25m, d.TickSize);

            Assert.Throws<ArgumentException>(() => new InstrumentMapping("", "MNQ", 10, 10, ExpiryMappingPolicy.ExactMonthOnly));
            var map = new InstrumentMapping("NQ", "MNQ", 10, 10, ExpiryMappingPolicy.ExactMonthOnly);
            Assert.Equal("MNQ", map.TargetRoot);
        }

        [Fact]
        public void Scale_out_reduction_and_clock_helpers()
        {
            Assert.Equal(1, SizingEngine.ComputeScaleOutReduction(3, 2, 2, 2));
            var clock = new FrozenClock(DateTime.UtcNow, 10);
            clock.Advance(TimeSpan.FromMilliseconds(5));
            Assert.True(clock.HighResolutionTicks > 10);
            var system = new SystemClock();
            Assert.True(system.UtcNow.Kind == DateTimeKind.Utc || system.UtcNow.Kind == DateTimeKind.Unspecified);
            Assert.True(system.HighResolutionTicks > 0);
        }
    }
}
