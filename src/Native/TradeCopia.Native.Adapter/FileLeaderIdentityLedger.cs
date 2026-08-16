using System;
using System.Collections.Generic;
using System.IO;
using TradeCopia.Domain.Engine;

namespace TradeCopia.Native.Adapter
{
    public sealed class FileLeaderIdentityLedger : ILeaderIdentityLedger
    {
        private readonly object _gate = new object();
        private readonly string _path;
        private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);

        public FileLeaderIdentityLedger(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Ledger path is required.", nameof(path));
            }

            _path = path;
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(path))
                {
                    foreach (var line in File.ReadAllLines(path))
                    {
                        var key = (line ?? string.Empty).Trim();
                        if (key.Length > 0)
                        {
                            _seen.Add(key);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public bool Contains(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
            {
                return false;
            }

            lock (_gate)
            {
                return _seen.Contains(identity);
            }
        }

        public void Remember(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
            {
                return;
            }

            lock (_gate)
            {
                if (!_seen.Add(identity))
                {
                    return;
                }

                try
                {
                    File.AppendAllText(_path, identity + Environment.NewLine);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
