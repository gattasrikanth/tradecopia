using TradeCopia.Domain;
using TradeCopia.Domain.Fingerprints;
using TradeCopia.Domain.Origin;

namespace TradeCopia.Domain.UnitTests
{
    public class FingerprintAndOriginTests
    {
        [Fact]
        public void Fingerprint_ignores_timestamp()
        {
            var a = TestSupport.Order("L1", LeaderOrderState.Working, quantity: 2, limit: 10m, type: DomainOrderType.Limit);
            var b = TestSupport.Order("L1", LeaderOrderState.Working, quantity: 2, limit: 10m, type: DomainOrderType.Limit);
            Assert.Equal(SemanticFingerprint.Compute(a), SemanticFingerprint.Compute(b));
        }

        [Fact]
        public void Fingerprint_changes_with_price()
        {
            var a = TestSupport.Order("L1", LeaderOrderState.Working, type: DomainOrderType.Limit, limit: 10m);
            var b = TestSupport.Order("L1", LeaderOrderState.Working, type: DomainOrderType.Limit, limit: 11m);
            Assert.NotEqual(SemanticFingerprint.Compute(a), SemanticFingerprint.Compute(b));
        }

        [Fact]
        public void Correlation_marker_fits_ninjatrader_name_limit()
        {
            var marker = OriginRegistry.CorrelationMarker(CommandId.New());
            Assert.StartsWith("TC:", marker);
            Assert.True(marker.Length <= 50);
        }

        [Fact]
        public void Origin_registry_binds_native_order()
        {
            var registry = new OriginRegistry();
            var command = CommandId.New();
            registry.RegisterPending(command, TestSupport.Follower1, TestSupport.Nq);
            registry.Bind(command, new FollowerOrderKey("NT-1"));
            Assert.True(registry.IsCopierOriginated(new FollowerOrderKey("NT-1")));
            Assert.True(registry.IsCopierOriginated(TestSupport.Follower1, "TC:deadbeef"));
        }
    }
}
