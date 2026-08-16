using System;
using System.Runtime.InteropServices;

namespace TradeCopia.Platform
{
    public interface IDocumentsFolder
    {
        string GetPath();
    }

    /// <summary>
    /// Resolves the Windows Documents known folder through SHGetKnownFolderPath
    /// when available. Does not assume %USERPROFILE%\Documents.
    /// </summary>
    public sealed class WindowsDocumentsFolder : IDocumentsFolder
    {
        private static readonly Guid FolderIdDocuments = new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7");

        public string GetPath()
        {
            if (TryGetKnownFolder(FolderIdDocuments, out var known) && !string.IsNullOrWhiteSpace(known))
            {
                return known;
            }

            var fallback = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback;
            }

            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents");
        }

        public static bool TrySetPath(string path, out string error)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "documents-path-required";
                return false;
            }

            var id = FolderIdDocuments;
            var hr = SHSetKnownFolderPath(ref id, 0, IntPtr.Zero, path);
            if (hr != 0)
            {
                error = "shsetknownfolderpath-0x" + hr.ToString("x8");
                return false;
            }

            SHChangeNotify(0x08000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
            error = string.Empty;
            return true;
        }

        private static bool TryGetKnownFolder(Guid id, out string path)
        {
            path = string.Empty;
            try
            {
                var hr = SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out var ptr);
                if (hr != 0 || ptr == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    path = Marshal.PtrToStringUni(ptr) ?? string.Empty;
                    return !string.IsNullOrWhiteSpace(path);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(ptr);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        [DllImport("shell32.dll")]
        private static extern int SHGetKnownFolderPath(ref Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHSetKnownFolderPath(ref Guid rfid, uint dwFlags, IntPtr hToken, string pszPath);

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }

    public sealed class FixedDocumentsFolder : IDocumentsFolder
    {
        private readonly string _path;

        public FixedDocumentsFolder(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public string GetPath()
        {
            return _path;
        }
    }
}
