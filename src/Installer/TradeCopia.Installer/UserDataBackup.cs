using System.Text.Json;

namespace TradeCopia.Installer;

public sealed class BackupManifest
{
    public string Source { get; set; } = "";
    public string Destination { get; set; } = "";
    public string TimestampUtc { get; set; } = "";
    public int FileCount { get; set; }
    public int DirectoryCount { get; set; }
    public long TotalBytes { get; set; }
    public List<string> Errors { get; set; } = new();
}

public static class UserDataBackup
{
    public static BackupManifest CopyTree(string source, string destination)
    {
        var manifest = new BackupManifest
        {
            Source = source,
            Destination = destination,
            TimestampUtc = DateTime.UtcNow.ToString("o")
        };

        if (!Directory.Exists(source))
        {
            manifest.Errors.Add("source-missing");
            return manifest;
        }

        Directory.CreateDirectory(destination);
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, dir);
            Directory.CreateDirectory(Path.Combine(destination, rel));
            manifest.DirectoryCount++;
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            try
            {
                var rel = Path.GetRelativePath(source, file);
                var dest = Path.Combine(destination, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest, overwrite: false);
                manifest.FileCount++;
                manifest.TotalBytes += new FileInfo(dest).Length;
            }
            catch (Exception ex)
            {
                manifest.Errors.Add(ex.GetType().Name + ":" + Path.GetFileName(file));
            }
        }

        File.WriteAllText(Path.Combine(destination, "BACKUP-MANIFEST.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        return manifest;
    }

    public static bool VerifyReadable(string destination)
    {
        var manifestPath = Path.Combine(destination, "BACKUP-MANIFEST.json");
        if (!File.Exists(manifestPath))
        {
            return false;
        }

        var manifest = JsonSerializer.Deserialize<BackupManifest>(File.ReadAllText(manifestPath));
        if (manifest == null || manifest.FileCount <= 0)
        {
            return false;
        }

        return Directory.GetFiles(destination, "*", SearchOption.AllDirectories).Length >= manifest.FileCount;
    }
}
