using TradeCopia.Domain;
using TradeCopia.Domain.Model;
using TradeCopia.Domain.Topology;

namespace TradeCopia.Domain.UnitTests
{
    public class TopologyValidatorTests
    {
        [Fact]
        public void Star_is_valid()
        {
            var group = new CopyGroup(
                CopyGroupId.New(),
                "star",
                new AccountKey("SIM-LEADER-01"),
                new[]
                {
                    new FollowerRule(new AccountKey("SIM-FOLLOWER-01"), true, SizingPolicy.OneToOne(), null),
                    new FollowerRule(new AccountKey("SIM-FOLLOWER-02"), true, SizingPolicy.OneToOne(), null)
                },
                CopyMode.OrderMirror,
                GroupEnabledState.Enabled);

            var result = TopologyValidator.Validate(new[] { group });
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Self_edge_is_rejected()
        {
            var account = new AccountKey("SIM-LEADER-01");
            var group = new CopyGroup(
                CopyGroupId.New(),
                "loop",
                account,
                new[] { new FollowerRule(account, true, SizingPolicy.OneToOne(), null) },
                CopyMode.OrderMirror,
                GroupEnabledState.Enabled);

            var result = TopologyValidator.Validate(new[] { group });
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Two_node_cycle_is_rejected()
        {
            var a = new AccountKey("SIM-A");
            var b = new AccountKey("SIM-B");
            var g1 = new CopyGroup(
                CopyGroupId.New(), "g1", a,
                new[] { new FollowerRule(b, true, SizingPolicy.OneToOne(), null) },
                CopyMode.OrderMirror, GroupEnabledState.Enabled);
            var g2 = new CopyGroup(
                CopyGroupId.New(), "g2", b,
                new[] { new FollowerRule(a, true, SizingPolicy.OneToOne(), null) },
                CopyMode.OrderMirror, GroupEnabledState.Enabled);

            var result = TopologyValidator.Validate(new[] { g1, g2 });
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Account_cannot_be_leader_and_follower()
        {
            var a = new AccountKey("SIM-A");
            var b = new AccountKey("SIM-B");
            var c = new AccountKey("SIM-C");
            var g1 = new CopyGroup(
                CopyGroupId.New(), "g1", a,
                new[] { new FollowerRule(b, true, SizingPolicy.OneToOne(), null) },
                CopyMode.OrderMirror, GroupEnabledState.Enabled);
            var g2 = new CopyGroup(
                CopyGroupId.New(), "g2", b,
                new[] { new FollowerRule(c, true, SizingPolicy.OneToOne(), null) },
                CopyMode.OrderMirror, GroupEnabledState.Enabled);

            var result = TopologyValidator.Validate(new[] { g1, g2 });
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Duplicate_follower_in_group_is_rejected()
        {
            var follower = new AccountKey("SIM-FOLLOWER-01");
            var group = new CopyGroup(
                CopyGroupId.New(),
                "dup",
                new AccountKey("SIM-LEADER-01"),
                new[]
                {
                    new FollowerRule(follower, true, SizingPolicy.OneToOne(), null),
                    new FollowerRule(follower, true, SizingPolicy.OneToOne(), null)
                },
                CopyMode.OrderMirror,
                GroupEnabledState.Enabled);

            var result = TopologyValidator.Validate(new[] { group });
            Assert.False(result.IsValid);
        }
    }
}
