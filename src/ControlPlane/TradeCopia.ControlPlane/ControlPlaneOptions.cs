using TradeCopia.Protocol;

namespace TradeCopia.ControlPlane;

public sealed class ControlPlaneOptions
{
    public const int DefaultPort = 17841;
    public const string DefaultBindAddress = "127.0.0.1";

    public string BindAddress { get; init; } = DefaultBindAddress;
    public int Port { get; init; } = DefaultPort;
    public bool DemoMode { get; init; } = true;
    public string DataDirectory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TradeCopia",
        "data");
    public string WebRoot { get; init; } = "wwwroot";
    public string PipeName { get; init; } = TradeCopia.Protocol.EnginePipeName.ForCurrentUser();

    public static ControlPlaneOptions FromArgs(string[] args)
    {
        var demo = true;
        var port = DefaultPort;
        foreach (var arg in args)
        {
            if (string.Equals(arg, "--no-demo", StringComparison.OrdinalIgnoreCase))
            {
                demo = false;
            }

            if (arg.StartsWith("--port=", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(arg.AsSpan("--port=".Length), out var parsed)
                && parsed > 0)
            {
                port = parsed;
            }
        }

        return new ControlPlaneOptions
        {
            DemoMode = demo,
            Port = port
        };
    }
}
