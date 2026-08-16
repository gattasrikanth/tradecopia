using TradeCopia.Platform;

namespace TradeCopia.Installer;

public sealed class RemediationResult
{
    public bool Succeeded { get; init; }
    public string Reason { get; init; } = "";
    public string BackupPath { get; init; } = "";
    public string TargetPath { get; init; } = "";
}

public static class UserDataRemediator
{
    public static RemediationResult RelocateToLocalDocuments(
        string sourceUserData,
        string localDocuments,
        string backupRoot,
        bool ninjaTraderRunning,
        Func<string, string, bool>? setDocumentsFolder = null)
    {
        if (ninjaTraderRunning)
        {
            return new RemediationResult { Succeeded = false, Reason = "ninjatrader-running" };
        }

        if (CloudPathDetector.IsCloudBacked(localDocuments))
        {
            return new RemediationResult { Succeeded = false, Reason = "target-still-cloud" };
        }

        if (!Directory.Exists(sourceUserData))
        {
            return new RemediationResult { Succeeded = false, Reason = "source-missing" };
        }

        Directory.CreateDirectory(localDocuments);
        var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        var backup = Path.Combine(backupRoot, stamp);
        var manifest = UserDataBackup.CopyTree(sourceUserData, backup);
        if (manifest.Errors.Count > 0 || !UserDataBackup.VerifyReadable(backup))
        {
            return new RemediationResult { Succeeded = false, Reason = "backup-failed", BackupPath = backup };
        }

        var target = NinjaTraderPaths.UserDataDirectory(localDocuments);
        if (!Directory.Exists(target))
        {
            var copy = UserDataBackup.CopyTree(sourceUserData, target);
            if (copy.Errors.Count > 0)
            {
                return new RemediationResult { Succeeded = false, Reason = "copy-failed", BackupPath = backup, TargetPath = target };
            }
        }

        if (setDocumentsFolder != null && !setDocumentsFolder(localDocuments, ""))
        {
            return new RemediationResult { Succeeded = false, Reason = "known-folder-not-set", BackupPath = backup, TargetPath = target };
        }

        return new RemediationResult { Succeeded = true, Reason = "relocated", BackupPath = backup, TargetPath = target };
    }
}
