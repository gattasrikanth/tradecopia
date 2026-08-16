using System;
using System.IO;

namespace TradeCopia.Platform
{
    public sealed class NinjaTraderLocation
    {
        public NinjaTraderLocation(string documentsPath, string userDataPath, string customPath, bool cloudBacked)
        {
            DocumentsPath = documentsPath ?? string.Empty;
            UserDataPath = userDataPath ?? string.Empty;
            CustomPath = customPath ?? string.Empty;
            CloudBacked = cloudBacked;
        }

        public string DocumentsPath { get; }
        public string UserDataPath { get; }
        public string CustomPath { get; }
        public bool CloudBacked { get; }
    }

    public static class NinjaTraderPaths
    {
        public const string UserDataFolderName = "NinjaTrader 8";

        public static string UserDataDirectory(string documentsPath)
        {
            if (string.IsNullOrWhiteSpace(documentsPath))
            {
                throw new ArgumentException("Documents path is required.", nameof(documentsPath));
            }

            return Path.Combine(documentsPath, UserDataFolderName);
        }

        public static string CustomDirectory(string documentsPath)
        {
            return Path.Combine(UserDataDirectory(documentsPath), "bin", "Custom");
        }

        public static NinjaTraderLocation Resolve(IDocumentsFolder documents)
        {
            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            var docs = documents.GetPath();
            var userData = UserDataDirectory(docs);
            return new NinjaTraderLocation(
                docs,
                userData,
                CustomDirectory(docs),
                CloudPathDetector.IsCloudBacked(userData) || CloudPathDetector.IsCloudBacked(docs));
        }
    }
}
