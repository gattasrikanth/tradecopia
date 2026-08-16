using System.Diagnostics;
using TradeCopia.Installer;

var root = ProductLayout.PerUserRoot(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
var app = ProductLayout.AppDirectory(root);
var companion = Path.Combine(app, ProductInfo.CompanionExe);

if (Process.GetProcessesByName("TradeCopia.ControlPlane").Length == 0 && File.Exists(companion))
{
    Process.Start(new ProcessStartInfo
    {
        FileName = companion,
        WorkingDirectory = app,
        UseShellExecute = false,
        CreateNoWindow = true,
        ArgumentList = { "--hidden" }
    });
    Thread.Sleep(800);
}

Process.Start(new ProcessStartInfo
{
    FileName = ProductInfo.LoopbackUrl,
    UseShellExecute = true
});
