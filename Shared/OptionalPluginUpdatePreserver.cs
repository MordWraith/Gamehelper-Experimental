namespace Shared.PluginPackages
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     Preserves optional plugin folders across core ZIP updates (installed-plugins.json).
    /// </summary>
    public static class OptionalPluginUpdatePreserver
    {
        private static readonly HashSet<string> CorePluginIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "AutoPot",
            "HealthBars",
            "Radar",
            "PreloadAlert",
        };

        public static bool ShouldPreserveForManifest(JObject manifest)
        {
            var variant = manifest["package"]?["variant"]?.ToString();
            if (string.Equals(variant, "core", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var packageName = manifest["package"]?["name"]?.ToString();
            return !string.IsNullOrWhiteSpace(packageName) &&
                   packageName.Contains("-core.zip", StringComparison.OrdinalIgnoreCase);
        }

        public static IReadOnlyList<string> GetInstalledOptionalFolders(string installDir)
        {
            installDir = Path.GetFullPath(installDir);
            var pendingRemovals = LoadPendingRemovals(installDir);
            var folders = new List<string>();

            foreach (var entry in LoadInstalledOptionalEntries(installDir))
            {
                if (CorePluginIds.Contains(entry.PluginId) || pendingRemovals.Contains(entry.Folder))
                {
                    continue;
                }

                var pluginDir = Path.Combine(installDir, "Plugins", entry.Folder);
                if (!Directory.Exists(pluginDir))
                {
                    continue;
                }

                if (!folders.Contains(entry.Folder, StringComparer.OrdinalIgnoreCase))
                {
                    folders.Add(entry.Folder);
                }
            }

            return folders.OrderBy(static folder => folder, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static void BackupOptionalPlugins(string installDir, string backupRoot, IReadOnlyList<string> folders)
        {
            if (folders.Count == 0)
            {
                return;
            }

            installDir = Path.GetFullPath(installDir);
            backupRoot = Path.GetFullPath(backupRoot);
            Directory.CreateDirectory(backupRoot);

            foreach (var folder in folders)
            {
                var source = Path.Combine(installDir, "Plugins", folder);
                if (!Directory.Exists(source))
                {
                    continue;
                }

                var destination = Path.Combine(backupRoot, folder);
                if (Directory.Exists(destination))
                {
                    Directory.Delete(destination, recursive: true);
                }

                CopyDirectory(source, destination);
            }
        }

        public static IEnumerable<string> BuildRestoreRobocopyCommands(
            string backupRoot,
            string installDir,
            IReadOnlyList<string> folders)
        {
            backupRoot = Path.GetFullPath(backupRoot);
            installDir = Path.GetFullPath(installDir);

            foreach (var folder in folders)
            {
                var source = Path.Combine(backupRoot, folder);
                if (!Directory.Exists(source))
                {
                    continue;
                }

                var destination = Path.Combine(installDir, "Plugins", folder);
                yield return
                    $"robocopy {QuoteBatchPath(source)} {QuoteBatchPath(destination)} /E /COPY:DAT /R:3 /W:2 /NFL /NDL /NJH /NJS /NC /NS /NP";
            }
        }

        private static string QuoteBatchPath(string path) =>
            $"\"{path.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

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

        private static IEnumerable<(string PluginId, string Folder)> LoadInstalledOptionalEntries(string installDir)
        {
            var path = Path.Combine(installDir, "configs", "installed-plugins.json");
            if (!File.Exists(path))
            {
                yield break;
            }

            JObject root;
            try
            {
                root = JObject.Parse(File.ReadAllText(path));
            }
            catch
            {
                yield break;
            }

            if (root["optional"] is not JObject optional)
            {
                yield break;
            }

            foreach (var prop in optional.Properties())
            {
                if (prop.Value is not JObject obj)
                {
                    continue;
                }

                var folder = obj["folder"]?.ToString() ?? prop.Name;
                if (string.IsNullOrWhiteSpace(folder))
                {
                    continue;
                }

                yield return (prop.Name, folder);
            }
        }

        private static HashSet<string> LoadPendingRemovals(string installDir)
        {
            var path = Path.Combine(installDir, "configs", "pending-plugin-removals.json");
            if (!File.Exists(path))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var root = JObject.Parse(File.ReadAllText(path));
                if (root["folders"] is not JArray folders)
                {
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                return folders
                    .Select(static token => token?.ToString())
                    .Where(static folder => !string.IsNullOrWhiteSpace(folder))
                    .Select(static folder => folder!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
