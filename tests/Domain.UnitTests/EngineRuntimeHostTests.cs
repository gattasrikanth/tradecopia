using TradeCopia.Native.Adapter;
using TradeCopia.Protocol;

namespace TradeCopia.Domain.UnitTests
{
    public class EngineRuntimeHostTests
    {
        [Fact]
        public void Start_hosts_named_pipe_and_pause_updates_snapshot()
        {
            var pipe = EnginePipeName.FromMaterial("runtime-" + Guid.NewGuid().ToString("N"));
            using var runtime = new EngineRuntime(pipe);
            Assert.False(runtime.PipeStarted);
            runtime.Start();
            Assert.True(runtime.PipeStarted);
            Assert.Equal(pipe, runtime.PipeName);

            using var client = new NamedPipeCompanionClient(pipe);
            Assert.True(WaitConnect(client));
            var hello = client.Send(new ProtocolEnvelope(1, Guid.NewGuid().ToString("N"), ProtocolMessageTypes.Hello, DateTime.UtcNow, "", "{}"));
            Assert.True(hello.Accepted);

            var pause = client.Send(new ProtocolEnvelope(1, Guid.NewGuid().ToString("N"), ProtocolMessageTypes.PauseNewEntries, DateTime.UtcNow, "", "{}"));
            Assert.True(pause.Accepted);
            Assert.Equal("PausedNewEntries", runtime.Session.EngineState);
            Assert.False(runtime.Session.CopyingEnabled);
            Assert.Contains("PausedNewEntries", pause.Reply.PayloadJson);
        }

        [Fact]
        public void Shipped_engine_host_start_opens_named_pipe()
        {
            var pipe = EnginePipeName.FromMaterial("host-" + Guid.NewGuid().ToString("N"));
            using var runtime = new EngineRuntime(pipe);
            var host = new TradeCopia.Native.TradeCopiaEngineHost(runtime);
            Assert.False(host.PipeStarted);
            host.Start();
            Assert.True(host.PipeStarted);
            Assert.Equal(pipe, host.PipeName);

            using var client = new NamedPipeCompanionClient(pipe);
            Assert.True(WaitConnect(client));
            var hello = client.Send(new ProtocolEnvelope(1, Guid.NewGuid().ToString("N"), ProtocolMessageTypes.Hello, DateTime.UtcNow, "", "{}"));
            Assert.True(hello.Accepted);
            host.Stop();
            Assert.False(host.PipeStarted);
        }

        private static bool WaitConnect(NamedPipeCompanionClient client)
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
