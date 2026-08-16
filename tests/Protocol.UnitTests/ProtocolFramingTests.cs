using System;
using System.IO;
using TradeCopia.Protocol;

namespace TradeCopia.Protocol.UnitTests
{
    public class ProtocolFramingTests
    {
        [Fact]
        public void Round_trip_preserves_payload()
        {
            var json = "{\"protocolVersion\":1,\"messageType\":\"Hello\"}";
            var decoded = ProtocolFraming.Decode(ProtocolFraming.Encode(json));
            Assert.Equal(json, decoded);
        }

        [Fact]
        public void Rejects_oversized_payload()
        {
            var huge = new string('x', ProtocolLimits.MaxMessageBytes + 1);
            Assert.Throws<InvalidOperationException>(() => ProtocolFraming.Encode(huge));
        }

        [Fact]
        public void Rejects_truncated_frame()
        {
            Assert.Throws<InvalidDataException>(() => ProtocolFraming.Decode(new byte[] { 0, 0 }));
        }

        [Fact]
        public void Incompatible_major_version_fails_closed()
        {
            Assert.False(ProtocolFraming.IsCompatible(2));
            Assert.True(ProtocolFraming.IsCompatible(1));
        }
    }
}
