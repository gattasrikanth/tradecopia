using System;
using System.IO;

namespace TradeCopia.Platform
{
    public static class CloudPathDetector
    {
        private static readonly string[] Markers =
        {
            "OneDrive",
            "OneDrive - ",
            "Dropbox",
            "Google Drive",
            "iCloudDrive",
            "iCloud Drive"
        };

        public static bool IsCloudBacked(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var full = path.Replace('/', Path.DirectorySeparatorChar);
            foreach (var marker in Markers)
            {
                if (full.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            var oneDrive = Environment.GetEnvironmentVariable("OneDrive");
            if (!string.IsNullOrEmpty(oneDrive) && PathStartsWith(full, oneDrive))
            {
                return true;
            }

            var commercial = Environment.GetEnvironmentVariable("OneDriveCommercial");
            if (!string.IsNullOrEmpty(commercial) && PathStartsWith(full, commercial))
            {
                return true;
            }

            return false;
        }

        private static bool PathStartsWith(string path, string root)
        {
            var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            if (path.Length < normalizedRoot.Length)
            {
                return false;
            }

            if (!path.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return path.Length == normalizedRoot.Length
                || path[normalizedRoot.Length] == Path.DirectorySeparatorChar;
        }
    }
}
