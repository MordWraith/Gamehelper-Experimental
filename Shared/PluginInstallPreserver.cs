namespace Shared.PluginPackages
{
    using System;
    using System.IO;

    /// <summary>
    ///     Backs up and restores per-plugin <c>config/</c> folders across plugin package installs.
    /// </summary>
    public static class PluginInstallPreserver
    {
        public static string? BackupPluginConfig(string installDir, string folder)
        {
            installDir = Path.GetFullPath(installDir);
            var source = Path.Combine(installDir, "Plugins", folder, "config");
            if (!Directory.Exists(source))
            {
                return null;
            }

            var backupRoot = Path.Combine(
                installDir,
                "configs",
                "plugin-install-backup",
                folder);
            try
            {
                if (Directory.Exists(backupRoot))
                {
                    Directory.Delete(backupRoot, recursive: true);
                }

                CopyDirectory(source, backupRoot);
                return backupRoot;
            }
            catch
            {
                return null;
            }
        }

        public static void RestorePluginConfig(string installDir, string folder, string? backupRoot)
        {
            if (string.IsNullOrWhiteSpace(backupRoot) || !Directory.Exists(backupRoot))
            {
                return;
            }

            installDir = Path.GetFullPath(installDir);
            var destination = Path.Combine(installDir, "Plugins", folder, "config");
            try
            {
                Directory.CreateDirectory(destination);
                CopyDirectory(backupRoot, destination);
            }
            catch
            {
            }
        }

        public static void DeleteBackup(string? backupRoot)
        {
            if (string.IsNullOrWhiteSpace(backupRoot))
            {
                return;
            }

            try
            {
                if (Directory.Exists(backupRoot))
                {
                    Directory.Delete(backupRoot, recursive: true);
                }
            }
            catch
            {
            }
        }

        public static string GetPendingConfigBackupDir(string installDir, string pluginId) =>
            Path.Combine(Path.GetFullPath(installDir), "configs", "pending-plugin-updates", $"{pluginId}-config");

        public static void BackupPluginConfigForPendingUpdate(string installDir, string pluginId, string folder)
        {
            var backup = BackupPluginConfig(installDir, folder);
            if (backup == null)
            {
                return;
            }

            var pendingDir = GetPendingConfigBackupDir(installDir, pluginId);
            try
            {
                if (Directory.Exists(pendingDir))
                {
                    Directory.Delete(pendingDir, recursive: true);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(pendingDir)!);
                Directory.Move(backup, pendingDir);
            }
            catch
            {
                DeleteBackup(backup);
            }
        }

        public static void RestorePendingConfigBackup(string installDir, string pluginId, string folder)
        {
            var pendingDir = GetPendingConfigBackupDir(installDir, pluginId);
            RestorePluginConfig(installDir, folder, pendingDir);
            DeleteBackup(pendingDir);
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(source, file);
                var destPath = Path.Combine(destination, relativePath);
                var parent = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                File.Copy(file, destPath, overwrite: true);
            }
        }
    }
}
