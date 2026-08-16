using System;
using TradeCopia.Domain;

namespace TradeCopia.Domain.UnitTests
{
    public class IdentifierSurfaceTests
    {
        [Fact]
        public void LogicalOrderId_full_surface()
        {
            var a = LogicalOrderId.New();
            var b = new LogicalOrderId(a.Value);
            var c = LogicalOrderId.New();
            Assert.Equal(a.Value, b.Value);
            Assert.True(a.Equals(b));
            Assert.True(a.Equals((object)b));
            Assert.False(a.Equals((object)c));
            Assert.False(a.Equals(new object()));
            Assert.True(a == b);
            Assert.True(a != c);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.Equal(a.Value.ToString("D"), a.ToString());
            Assert.Throws<ArgumentException>(() => new LogicalOrderId(Guid.Empty));
        }

        [Fact]
        public void EventId_and_DivergenceId_full_surface()
        {
            var e1 = EventId.New();
            var e2 = new EventId(e1.Value);
            Assert.True(e1 == e2);
            Assert.True(e1 != EventId.New());
            Assert.True(e1.Equals((object)e2));
            Assert.False(e1.Equals(new object()));
            Assert.Equal(e1.ToString(), e1.Value.ToString("D"));
            Assert.Throws<ArgumentException>(() => new EventId(Guid.Empty));

            var d1 = DivergenceId.New();
            var d2 = new DivergenceId(d1.Value);
            Assert.True(d1 == d2);
            Assert.True(d1 != DivergenceId.New());
            Assert.True(d1.Equals((object)d2));
            Assert.False(d1.Equals(new object()));
            Assert.Equal(d1.GetHashCode(), d2.GetHashCode());
            Assert.Throws<ArgumentException>(() => new DivergenceId(Guid.Empty));
        }

        [Fact]
        public void Order_keys_full_surface()
        {
            var l1 = new LeaderOrderKey("L");
            var l2 = new LeaderOrderKey("L");
            Assert.True(l1.Equals(l2));
            Assert.True(l1.Equals((object)l2));
            Assert.False(l1.Equals(new object()));
            Assert.True(l1 == l2);
            Assert.True(l1 != new LeaderOrderKey("X"));
            Assert.Equal("L", l1.ToString());
            Assert.Equal(l1.GetHashCode(), l2.GetHashCode());

            var f1 = new FollowerOrderKey("F");
            Assert.True(f1 == new FollowerOrderKey("F"));
            Assert.True(f1 != new FollowerOrderKey("G"));
            Assert.True(f1.Equals((object)new FollowerOrderKey("F")));
            Assert.False(f1.Equals(new object()));
            Assert.Equal("F", f1.ToString());

            var x1 = new ExecutionKey("E");
            Assert.True(x1 == new ExecutionKey("E"));
            Assert.True(x1 != new ExecutionKey("Z"));
            Assert.True(x1.Equals((object)new ExecutionKey("E")));
            Assert.False(x1.Equals(new object()));
            Assert.Equal("E", x1.ToString());
        }
    }
}
