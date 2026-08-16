using System;
using System.Threading;
using TradeCopia.Protocol;

namespace TradeCopia.Protocol.UnitTests
{
    public class NamedPipeTransportTests
    {
        [Fact]
        public void Pipe_name_is_versioned_and_does_not_embed_raw_material()
        {
            var name = EnginePipeName.FromMaterial("S-1-5-21-example");
            Assert.StartsWith(EnginePipeName.Prefix, name);
            Assert.DoesNotContain("S-1-5-21-example", name);
        }

        [Fact]
        public void For_current_user_is_stable_and_versioned()
        {
            var first = EnginePipeName.ForCurrentUser();
            var second = EnginePipeName.ForCurrentUser();
            Assert.Equal(first, second);
            Assert.StartsWith(EnginePipeName.Prefix, first);
            Assert.DoesNotContain(Environment.UserName, first);
        }

        [Fact]
        public void Engine_server_accepts_companion_hello_and_rejects_execute_order()
        {
            var pipe = EnginePipeName.FromMaterial("test-" + Guid.NewGuid().ToString("N"));
            using var host = new NamedPipeEngineHost(pipe, new ProtocolSession());
            host.Start();

            using var client = new NamedPipeCompanionClient(pipe);
            Assert.True(WaitForConnect(client));

            var hello = client.Send(new ProtocolEnvelope(
                1, Guid.NewGuid().ToString("N"), ProtocolMessageTypes.Hello, DateTime.UtcNow, "", "{}"));
            Assert.True(hello.Accepted);
            Assert.Equal(ProtocolMessageTypes.EngineHello, hello.Reply.MessageType);

            var banned = client.Send(new ProtocolEnvelope(
                1, Guid.NewGuid().ToString("N"), "ExecuteOrder", DateTime.UtcNow, "", "{}"));
            Assert.False(banned.Accepted);
        }

        [Fact]
        public void Companion_fails_closed_when_engine_is_absent()
        {
            using var client = new NamedPipeCompanionClient(EnginePipeName.FromMaterial("missing-" + Guid.NewGuid().ToString("N")));
            Assert.False(client.TryConnect(200));
            var result = client.Send(new ProtocolEnvelope(
                1, "1", ProtocolMessageTypes.Hello, DateTime.UtcNow, "", "{}"));
            Assert.False(result.Accepted);
            Assert.Equal("engine-disconnected", result.Reason);
        }

        [Fact]
        public void Malformed_oversize_frame_is_rejected_by_framing()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ProtocolFraming.Encode(new string('x', ProtocolLimits.MaxMessageBytes + 8)));
        }

        private static bool WaitForConnect(NamedPipeCompanionClient client)
        {
            for (var i = 0; i < 40; i++)
            {
                if (client.TryConnect(250))
                {
                    return true;
                }

                Thread.Sleep(50);
            }

            return false;
        }
    }
}
