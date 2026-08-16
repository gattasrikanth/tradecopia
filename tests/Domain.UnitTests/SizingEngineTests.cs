using TradeCopia.Domain;
using TradeCopia.Domain.Model;
using TradeCopia.Domain.Sizing;

namespace TradeCopia.Domain.UnitTests
{
    public class SizingEngineTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(3, 3)]
        [InlineData(10, 10)]
        public void OneToOne_matches_leader(int leader, int expected)
        {
            var result = SizingEngine.ComputeEntryQuantity(leader, SizingPolicy.OneToOne(), 0);
            Assert.Equal(expected, result.Quantity);
            Assert.False(result.BlockedByCap);
        }

        [Theory]
        [InlineData(3, 0.5, false, 1)]
        [InlineData(1, 0.4, false, 0)]
        [InlineData(1, 0.4, true, 1)]
        [InlineData(5, 2, false, 10)]
        public void Multiplier_floors_toward_zero(int leader, double multiplier, bool minimumOne, int expected)
        {
            var policy = new SizingPolicy(SizingMode.Multiplier, (decimal)multiplier, 0, minimumOne, null, null);
            var result = SizingEngine.ComputeEntryQuantity(leader, policy, 0);
            Assert.Equal(expected, result.Quantity);
        }

        [Fact]
        public void Fixed_uses_configured_quantity()
        {
            var policy = new SizingPolicy(SizingMode.Fixed, 1m, 2, false, null, null);
            var result = SizingEngine.ComputeEntryQuantity(5, policy, 0);
            Assert.Equal(2, result.Quantity);
        }

        [Fact]
        public void Disabled_emits_zero()
        {
            var policy = new SizingPolicy(SizingMode.Disabled, 1m, 0, false, null, null);
            var result = SizingEngine.ComputeEntryQuantity(5, policy, 0);
            Assert.Equal(0, result.Quantity);
        }

        [Fact]
        public void Max_quantity_blocks_rather_than_clamps()
        {
            var policy = new SizingPolicy(SizingMode.OneToOne, 1m, 0, false, 2, null);
            var result = SizingEngine.ComputeEntryQuantity(3, policy, 0);
            Assert.True(result.BlockedByCap);
            Assert.Equal(0, result.Quantity);
            Assert.Equal("max-quantity", result.Reason);
        }

        [Fact]
        public void Max_absolute_position_blocks_projected_exposure()
        {
            var policy = new SizingPolicy(SizingMode.OneToOne, 1m, 0, false, null, 2);
            var result = SizingEngine.ComputeEntryQuantity(2, policy, currentFollowerPosition: 1);
            Assert.True(result.BlockedByCap);
            Assert.Equal("max-absolute-position", result.Reason);
        }

        [Theory]
        [InlineData(3, 2, 2, 2, 1)]
        [InlineData(3, 0, 2, 2, 0)]
        [InlineData(3, 3, 2, 2, 2)]
        [InlineData(5, 2, 4, 4, 1)]
        public void Scale_out_never_reverses(
            int leaderInitial,
            int leaderRemaining,
            int followerInitial,
            int followerActual,
            int expectedRemaining)
        {
            var remaining = SizingEngine.ComputeScaleOutRemaining(
                leaderInitial, leaderRemaining, followerInitial, followerActual);
            Assert.Equal(expectedRemaining, remaining);
            Assert.True(remaining >= 0);
            Assert.True(remaining <= followerActual);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 50)]
        [InlineData(3, 99)]
        public void Property_reduce_never_reverses_for_small_integers(int follower, int leaderMax)
        {
            for (var leaderInitial = 1; leaderInitial <= leaderMax; leaderInitial++)
            {
                for (var remaining = 0; remaining <= leaderInitial; remaining++)
                {
                    var target = SizingEngine.ComputeScaleOutRemaining(leaderInitial, remaining, follower, follower);
                    Assert.InRange(target, 0, follower);
                }
            }
        }
    }
}
