using System.IO.Compression;
using System.Reflection;

namespace TradeCopia.Installer;

public static class PayloadBundle
{
    public static string Resolve(string baseDirectory, Assembly? hostAssembly = null)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new ArgumentException("Base directory is required.", nameof(baseDirectory));
        }

        var beside = Path.Combine(baseDirectory, "payload");
        if (Directory.Exists(beside) && Directory.EnumerateFileSystemEntries(beside).Any())
        {
            return beside;
        }

        var zipBeside = Path.Combine(baseDirectory, "payload.zip");
        if (File.Exists(zipBeside))
        {
            return ExtractZip(zipBeside);
        }

        if (hostAssembly != null)
        {
            var resource = hostAssembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase));
            if (resource != null)
            {
                using var stream = hostAssembly.GetManifestResourceStream(resource);
                if (stream != null)
                {
                    var tmpZip = Path.Combine(Path.GetTempPath(), "tradecopia-embedded-payload.zip");
                    using (var file = File.Create(tmpZip))
                    {
                        stream.CopyTo(file);
                    }

                    return ExtractZip(tmpZip);
                }
            }
        }

        return beside;
    }

    public static string ExtractZip(string zipPath)
    {
        var dest = Path.Combine(Path.GetTempPath(), "tradecopia-payload-" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
        Directory.CreateDirectory(dest);
        ZipFile.ExtractToDirectory(zipPath, dest);
        var nested = Path.Combine(dest, "payload");
        return Directory.Exists(nested) ? nested : dest;
    }
}
