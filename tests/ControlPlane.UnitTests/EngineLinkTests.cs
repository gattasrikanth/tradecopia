using TradeCopia.ControlPlane;
using TradeCopia.Protocol;

namespace TradeCopia.ControlPlane.UnitTests;

public class EngineLinkTests
{
    [Fact]
    public void Send_without_attach_is_engine_disconnected()
    {
        using var link = new EngineLink();
        Assert.False(link.IsConnected);
        var result = link.Send(ProtocolMessageTypes.PauseNewEntries);
        Assert.False(result.Accepted);
        Assert.Equal("engine-disconnected", result.Reason);
    }

    [Fact]
    public void Attach_and_pause_dispatch_over_named_pipe()
    {
        var pipe = EnginePipeName.FromMaterial("link-" + Guid.NewGuid().ToString("N"));
        using var host = new NamedPipeEngineHost(pipe, new ProtocolSession());
        host.Start();
        using var link = new EngineLink();
        Assert.True(WaitAttach(link, pipe));
        Assert.True(link.IsConnected);
        var pause = link.Send(ProtocolMessageTypes.PauseNewEntries);
        Assert.True(pause.Accepted);
        Assert.Equal("PausedNewEntries", link.EngineState);
        Assert.False(link.CopyingEnabled);
        Assert.Contains("PausedNewEntries", pause.Reply.PayloadJson);
    }

    [Fact]
    public void Retry_attach_connects_to_shipped_engine_runtime()
    {
        var pipe = EnginePipeName.FromMaterial("retry-" + Guid.NewGuid().ToString("N"));
        using var runtime = new TradeCopia.Native.Adapter.EngineRuntime(pipe);
        runtime.Start();
        using var link = new EngineLink();
        using var cts = new CancellationTokenSource();
        link.StartRetryAttach(pipe, cts.Token);
        Assert.True(WaitConnected(link));
        cts.Cancel();
        Assert.True(link.IsConnected);
        Assert.Equal("Disabled", link.EngineState);
        Assert.False(link.CopyingEnabled);
    }

    private static bool WaitConnected(EngineLink link)
    {
        for (var i = 0; i < 40; i++)
        {
            if (link.IsConnected)
            {
                return true;
            }

            Thread.Sleep(50);
        }

        return false;
    }

    private static bool WaitAttach(EngineLink link, string pipe)
    {
        for (var i = 0; i < 40; i++)
        {
            if (link.TryAttach(pipe, 250))
            {
                return true;
            }

            Thread.Sleep(50);
        }

        return false;
    }
}
