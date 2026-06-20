namespace Shared.PluginPackages
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Shared.UpdateSecurity;

    public sealed class PluginPackageManager
    {
        private static readonly HttpClient HttpClient = CreateClient();

        private static readonly HashSet<string> CorePluginIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "AutoPot",
            "HealthBars",
            "Radar",
            "PreloadAlert",
        };

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GameHelperPluginManager/1.0");
            return client;
        }

        public IReadOnlyList<PluginCatalogEntry> GetOptionalPlugins(string installDir)
        {
            installDir = Path.GetFullPath(installDir);
            var catalog = TryLoadLocalCatalog(installDir, requireSignature: false);
            if (catalog == null)
            {
                return Array.Empty<PluginCatalogEntry>();
            }

            var catalogVersion = catalog["version"]?.ToString() ?? string.Empty;
            var installedState = LoadInstalledPluginsState(installDir);
            var entries = new List<PluginCatalogEntry>();

            if (catalog["plugins"] is not JArray plugins)
            {
                return entries;
            }

            foreach (var token in plugins)
            {
                if (token is not JObject obj)
                {
                    continue;
                }

                var id = obj["id"]?.ToString();
                if (string.IsNullOrWhiteSpace(id) || CorePluginIds.Contains(id))
                {
                    continue;
                }

                var folder = obj["folder"]?.ToString() ?? id;
                var packageHash = obj["package"]?["hash"]?.ToString() ?? string.Empty;
                var pluginVersion = obj["version"]?.ToString() ?? string.Empty;
                var author = ReadCatalogScalar(obj["author"]);
                ReadCatalogLocalized(obj["description"], out var descriptionEn, out var descriptionDe);
                var upstreamUrl = obj["upstreamUrl"]?.ToString() ?? string.Empty;
                var sourceUrl = obj["sourceUrl"]?.ToString()
                                ?? obj["repository"]?.ToString()
                                ?? obj["github"]?.ToString()
                                ?? string.Empty;
                var packageName = obj["package"]?["name"]?.ToString() ?? string.Empty;
                installedState.Optional.TryGetValue(id, out var installed);
                var pluginDir = Path.Combine(installDir, "Plugins", folder);
                var pendingRemovals = LoadPendingRemovals(installDir);
                var isPendingRemoval = pendingRemovals.Contains(folder);
                var pendingUpdates = LoadPendingUpdates(installDir);
                var isPendingUpdate = pendingUpdates.Exists(
                    u => string.Equals(u.PluginId, id, StringComparison.OrdinalIgnoreCase)
                         || string.Equals(u.Folder, folder, StringComparison.OrdinalIgnoreCase));
                var isInstalled = Directory.Exists(pluginDir);

                entries.Add(new PluginCatalogEntry
                {
                    Id = id,
                    Folder = folder,
                    CatalogVersion = catalogVersion,
                    PluginVersion = pluginVersion,
                    Author = author,
                    DescriptionEn = descriptionEn,
                    DescriptionDe = descriptionDe,
                    UpstreamUrl = upstreamUrl,
                    SourceUrl = sourceUrl,
                    PackageName = packageName,
                    PackageHash = packageHash,
                    InstalledVersion = installed?.Version,
                    InstalledPackageHash = installed?.PackageHash,
                    IsInstalled = isInstalled,
                    IsPendingRemoval = isPendingRemoval,
                    IsPendingUpdate = isPendingUpdate,
                });
            }

            return entries.OrderBy(static e => e.Id, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public async Task<PluginPackageResult> RefreshCatalogAsync(
            string installDir,
            IProgress<string>? log,
            CancellationToken cancellationToken)
        {
            if (!TryValidateInstallRoot(installDir, out var validationError))
            {
                return PluginPackageResult.Fail(2, validationError);
            }

            try
            {
                var manifest = await FetchManifestAsync(cancellationToken).ConfigureAwait(false);
                var version = manifest["version"]?.ToString();
                if (string.IsNullOrWhiteSpace(version))
                {
                    return PluginPackageResult.Fail(1, "manifest.json is invalid or empty.");
                }

                await EnsurePluginsCatalogAsync(manifest, version, installDir, log, cancellationToken).ConfigureAwait(false);
                return PluginPackageResult.Ok("Plugins catalog updated.");
            }
            catch (Exception ex)
            {
                return PluginPackageResult.Fail(1, $"Plugins catalog failed: {ex.Message}");
            }
        }

        public async Task<PluginPackageResult> InstallPluginAsync(
            string installDir,
            string pluginId,
            IProgress<string>? log,
            CancellationToken cancellationToken)
        {
            if (!TryValidateInstallRoot(installDir, out var validationError))
            {
                return PluginPackageResult.Fail(2, validationError);
            }

            if (CorePluginIds.Contains(pluginId))
            {
                return PluginPackageResult.Fail(2, $"{pluginId} is a core plugin and ships with the Core package.");
            }

            log?.Report($"Repository: {UpdateRepositoryConfig.Repository}");
            log?.Report("Loading manifest.json ...");

            JObject manifest;
            try
            {
                manifest = await FetchManifestAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return PluginPackageResult.Fail(1, $"Manifest not reachable: {ex.Message}");
            }

            var version = manifest["version"]?.ToString();
            if (string.IsNullOrWhiteSpace(version))
            {
                return PluginPackageResult.Fail(1, "manifest.json is invalid or empty.");
            }

            JObject catalog;
            try
            {
                catalog = await EnsurePluginsCatalogAsync(manifest, version, installDir, log, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return PluginPackageResult.Fail(1, $"Plugins catalog failed: {ex.Message}");
            }

            if (!TryFindCatalogEntry(catalog, pluginId, out var entry, out var resolvedId))
            {
                return PluginPackageResult.Fail(2, $"Plugin '{pluginId}' was not found in the catalog.");
            }

            var package = entry["package"] as JObject;
            var packageName = package?["name"]?.ToString();
            var packageHash = package?["hash"]?.ToString();
            var pluginVersion = catalog["version"]?.ToString() ?? version;
            var folder = entry["folder"]?.ToString() ?? resolvedId;
            var defaultAutoStart = entry["defaultAutoStart"]?.Value<bool>() ?? false;

            if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(packageHash))
            {
                return PluginPackageResult.Fail(1, "Plugin catalog entry is missing package metadata.");
            }

            var installedState = LoadInstalledPluginsState(installDir);
            if (installedState.Optional.TryGetValue(resolvedId, out var existing) &&
                string.Equals(existing.PackageHash, packageHash, StringComparison.OrdinalIgnoreCase) &&
                Directory.Exists(Path.Combine(installDir, "Plugins", folder)))
            {
                return PluginPackageResult.Ok($"Restart GameHelper to load {resolvedId}.");
            }

            var zipPath = Path.Combine(Path.GetTempPath(), packageName);
            try
            {
                var url = UpdateRepositoryConfig.FileDownloadUrl(version, packageName);
                log?.Report($"Downloading: {packageName}");
                await DownloadFileAsync(url, zipPath, cancellationToken).ConfigureAwait(false);

                var actualHash = ComputeSha256(zipPath);
                if (!actualHash.Equals(packageHash, StringComparison.OrdinalIgnoreCase))
                {
                    return PluginPackageResult.Fail(
                        1,
                        $"Hash mismatch (got {actualHash}, expected {packageHash})");
                }

                log?.Report("Extracting plugin package ...");
                await Task.Delay(350, cancellationToken).ConfigureAwait(false);
                var configBackup = PluginInstallPreserver.BackupPluginConfig(installDir, folder);
                var extracted = false;
                try
                {
                    extracted = await TryExtractPluginPackageAsync(
                        zipPath,
                        installDir,
                        log,
                        cancellationToken).ConfigureAwait(false);
                    if (!extracted)
                    {
                        if (TrySchedulePendingUpdate(installDir, resolvedId, folder, pluginVersion, packageHash, zipPath))
                        {
                            return PluginPackageResult.Ok(
                                $"Update scheduled for {resolvedId}. Restart GameHelper to apply.");
                        }

                        return PluginPackageResult.Fail(
                            1,
                            "Plugin files are still in use. Restart GameHelper, then try again.");
                    }
                }
                finally
                {
                    PluginInstallPreserver.RestorePluginConfig(installDir, folder, configBackup);
                    PluginInstallPreserver.DeleteBackup(configBackup);
                }
            }
            catch (Exception ex)
            {
                return PluginPackageResult.Fail(1, $"Plugin install failed: {ex.Message}");
            }
            finally
            {
                try { File.Delete(zipPath); } catch { }
            }

            installedState.Optional[resolvedId] = new InstalledOptionalPlugin
            {
                Version = pluginVersion,
                Folder = folder,
                PackageHash = packageHash,
                InstalledAtUtc = DateTime.UtcNow,
            };
            SaveInstalledPluginsState(installDir, installedState);
            EnsurePluginMetadata(installDir, resolvedId, defaultAutoStart);
            RemovePendingRemoval(installDir, folder);

            return PluginPackageResult.Ok(
                $"Installed {resolvedId} {pluginVersion}. Restart GameHelper to load the plugin.");
        }

        public async Task<PluginPackageResult> RemovePluginAsync(
            string installDir,
            string pluginId,
            IProgress<string>? log,
            CancellationToken cancellationToken)
        {
            if (!TryValidateInstallRoot(installDir, out var validationError))
            {
                return PluginPackageResult.Fail(2, validationError);
            }

            if (CorePluginIds.Contains(pluginId))
            {
                return PluginPackageResult.Fail(2, $"Core plugin '{pluginId}' cannot be removed.");
            }

            var installedState = LoadInstalledPluginsState(installDir);
            string? folder = null;
            string? resolvedId = null;

            foreach (var pair in installedState.Optional)
            {
                if (string.Equals(pair.Key, pluginId, StringComparison.OrdinalIgnoreCase))
                {
                    resolvedId = pair.Key;
                    folder = pair.Value.Folder;
                    break;
                }
            }

            if (folder == null)
            {
                try
                {
                    var manifest = await FetchManifestAsync(cancellationToken).ConfigureAwait(false);
                    var version = manifest["version"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(version))
                    {
                        var catalog = await EnsurePluginsCatalogAsync(
                            manifest,
                            version,
                            installDir,
                            log,
                            cancellationToken).ConfigureAwait(false);
                        if (TryFindCatalogEntry(catalog, pluginId, out var entry, out resolvedId))
                        {
                            folder = entry["folder"]?.ToString() ?? resolvedId;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log?.Report($"Catalog lookup skipped: {ex.Message}");
                }
            }

            resolvedId ??= pluginId;
            folder ??= resolvedId;

            var pluginDir = Path.Combine(installDir, "Plugins", folder);
            if (!Directory.Exists(pluginDir))
            {
                return PluginPackageResult.Fail(2, $"Plugin '{resolvedId}' is not installed.");
            }

            log?.Report($"Removing: {pluginDir}");
            DisablePluginMetadata(installDir, resolvedId);
            installedState.Optional.Remove(resolvedId);
            SaveInstalledPluginsState(installDir, installedState);

            await Task.Delay(350, cancellationToken).ConfigureAwait(false);

            if (await TryDeletePluginDirectoryAsync(pluginDir, log, cancellationToken).ConfigureAwait(false))
            {
                RemovePendingRemoval(installDir, folder);
                return PluginPackageResult.Ok(
                    $"Removed {resolvedId}. Restart GameHelper to apply changes.");
            }

            SchedulePendingRemoval(installDir, folder);
            return PluginPackageResult.Ok(
                $"Scheduled removal of {resolvedId}. Restart GameHelper to finish.");
        }

        public static void ApplyPendingRemovals(string installDir)
        {
            installDir = Path.GetFullPath(installDir);
            ApplyPendingUpdates(installDir);
            var pending = LoadPendingRemovals(installDir);
            if (pending.Count == 0)
            {
                return;
            }

            var remaining = new List<string>();
            foreach (var folder in pending)
            {
                var pluginDir = Path.Combine(installDir, "Plugins", folder);
                if (!Directory.Exists(pluginDir))
                {
                    continue;
                }

                if (TryDeletePluginDirectoryAsync(pluginDir, log: null, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult())
                {
                    continue;
                }

                remaining.Add(folder);
            }

            SavePendingRemovals(installDir, remaining);
        }

        private static async Task<bool> TryDeletePluginDirectoryAsync(
            string pluginDir,
            IProgress<string>? log,
            CancellationToken cancellationToken)
        {
            const int maxAttempts = 2;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (Directory.Exists(pluginDir))
                    {
                        Directory.Delete(pluginDir, recursive: true);
                    }

                    return true;
                }
                catch (Exception ex) when (IsFileAccessBlocked(ex))
                {
                    if (attempt == maxAttempts)
                    {
                        return false;
                    }

                    log?.Report("Waiting for plugin files to unlock ...");
                    await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public static void ApplyPendingUpdates(string installDir)
        {
            installDir = Path.GetFullPath(installDir);
            var pending = LoadPendingUpdates(installDir);
            if (pending.Count == 0)
            {
                return;
            }

            var remaining = new List<PendingPluginUpdate>();
            var installedState = LoadInstalledPluginsState(installDir);
            foreach (var update in pending)
            {
                var zipPath = Path.Combine(installDir, update.ZipRelativePath);
                if (!File.Exists(zipPath))
                {
                    continue;
                }

                try
                {
                    var configBackup = PluginInstallPreserver.BackupPluginConfig(installDir, update.Folder);
                    try
                    {
                        UpdateZipPackage.ExtractToDirectory(zipPath, installDir);
                    }
                    finally
                    {
                        PluginInstallPreserver.RestorePluginConfig(installDir, update.Folder, configBackup);
                        PluginInstallPreserver.DeleteBackup(configBackup);
                    }

                    PluginInstallPreserver.RestorePendingConfigBackup(installDir, update.PluginId, update.Folder);
                    installedState.Optional[update.PluginId] = new InstalledOptionalPlugin
                    {
                        Version = update.Version,
                        Folder = update.Folder,
                        PackageHash = update.PackageHash,
                        InstalledAtUtc = DateTime.UtcNow,
                    };
                    RemovePendingRemoval(installDir, update.Folder);
                    EnsurePluginMetadata(installDir, update.PluginId, autoStart: false);
                    try { File.Delete(zipPath); } catch { }
                }
                catch
                {
                    remaining.Add(update);
                }
            }

            if (installedState.Optional.Count > 0)
            {
                SaveInstalledPluginsState(installDir, installedState);
            }

            SavePendingUpdates(installDir, remaining);
        }

        private static async Task<bool> TryExtractPluginPackageAsync(
            string zipPath,
            string installDir,
            IProgress<string>? log,
            CancellationToken cancellationToken)
        {
            const int maxAttempts = 10;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    UpdateZipPackage.ExtractToDirectory(zipPath, installDir);
                    return true;
                }
                catch (Exception ex) when (IsFileAccessBlocked(ex))
                {
                    if (attempt == maxAttempts)
                    {
                        return false;
                    }

                    log?.Report("Waiting for plugin files to unlock ...");
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    await Task.Delay(attempt * 200, cancellationToken).ConfigureAwait(false);
                }
            }

            return false;
        }

        private static bool TrySchedulePendingUpdate(
            string installDir,
            string pluginId,
            string folder,
            string version,
            string packageHash,
            string zipPath)
        {
            try
            {
                var pendingDir = Path.Combine(installDir, "configs", "pending-plugin-updates");
                Directory.CreateDirectory(pendingDir);
                var zipName = $"{pluginId}.zip";
                var destination = Path.Combine(pendingDir, zipName);
                File.Copy(zipPath, destination, overwrite: true);
                PluginInstallPreserver.BackupPluginConfigForPendingUpdate(installDir, pluginId, folder);

                var pending = LoadPendingUpdates(installDir);
                pending.RemoveAll(u => string.Equals(u.PluginId, pluginId, StringComparison.OrdinalIgnoreCase));
                pending.Add(new PendingPluginUpdate
                {
                    PluginId = pluginId,
                    Folder = folder,
                    Version = version,
                    PackageHash = packageHash,
                    ZipRelativePath = Path.Combine("configs", "pending-plugin-updates", zipName),
                });
                SavePendingUpdates(installDir, pending);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private sealed class PendingPluginUpdate
        {
            internal string PluginId { get; init; } = string.Empty;

            internal string Folder { get; init; } = string.Empty;

            internal string Version { get; init; } = string.Empty;

            internal string PackageHash { get; init; } = string.Empty;

            internal string ZipRelativePath { get; init; } = string.Empty;
        }

        private static List<PendingPluginUpdate> LoadPendingUpdates(string installDir)
        {
            var path = Path.Combine(installDir, "configs", "pending-plugin-updates.json");
            if (!File.Exists(path))
            {
                return new List<PendingPluginUpdate>();
            }

            try
            {
                var root = JObject.Parse(File.ReadAllText(path));
                if (root["updates"] is not JArray updates)
                {
                    return new List<PendingPluginUpdate>();
                }

                var result = new List<PendingPluginUpdate>();
                foreach (var token in updates)
                {
                    if (token is not JObject obj)
                    {
                        continue;
                    }

                    var pluginId = obj["pluginId"]?.ToString();
                    if (string.IsNullOrWhiteSpace(pluginId))
                    {
                        continue;
                    }

                    result.Add(new PendingPluginUpdate
                    {
                        PluginId = pluginId,
                        Folder = obj["folder"]?.ToString() ?? pluginId,
                        Version = obj["version"]?.ToString() ?? string.Empty,
                        PackageHash = obj["packageHash"]?.ToString() ?? string.Empty,
                        ZipRelativePath = obj["zipRelativePath"]?.ToString() ?? string.Empty,
                    });
                }

                return result;
            }
            catch
            {
                return new List<PendingPluginUpdate>();
            }
        }

        private static void SavePendingUpdates(string installDir, IReadOnlyList<PendingPluginUpdate> updates)
        {
            var path = Path.Combine(installDir, "configs", "pending-plugin-updates.json");
            if (updates.Count == 0)
            {
                try { File.Delete(path); } catch { }
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var array = new JArray();
            foreach (var update in updates.OrderBy(static u => u.PluginId, StringComparer.OrdinalIgnoreCase))
            {
                array.Add(new JObject
                {
                    ["pluginId"] = update.PluginId,
                    ["folder"] = update.Folder,
                    ["version"] = update.Version,
                    ["packageHash"] = update.PackageHash,
                    ["zipRelativePath"] = update.ZipRelativePath,
                });
            }

            var root = new JObject
            {
                ["schema"] = 1,
                ["updates"] = array,
            };
            File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        private static bool IsFileAccessBlocked(Exception ex)
        {
            if (ex is UnauthorizedAccessException)
            {
                return true;
            }

            if (ex is not IOException)
            {
                return false;
            }

            return ex.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("because it is being used", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("is denied", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("cannot access the file", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryValidateInstallRoot(string installDir, out string error)
        {
            installDir = Path.GetFullPath(installDir);
            if (!Directory.Exists(installDir))
            {
                error = "Target folder does not exist.";
                return false;
            }

            if (!File.Exists(Path.Combine(installDir, "GameHelper.exe")) &&
                !File.Exists(Path.Combine(installDir, "GameHelper.App.exe")))
            {
                error = "Folder is not a GameHelper installation.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static JObject? TryLoadLocalCatalog(string installDir, bool requireSignature)
        {
            var catalogPath = Path.Combine(installDir, "plugins-catalog.json");
            var catalogSigPath = Path.Combine(installDir, "plugins-catalog.sig");
            if (!File.Exists(catalogPath))
            {
                return null;
            }

            try
            {
                if (requireSignature || File.Exists(catalogSigPath))
                {
                    var content = File.ReadAllText(catalogPath);
                    if (File.Exists(catalogSigPath))
                    {
                        var signature = File.ReadAllText(catalogSigPath);
                        if (!UpdateManifestVerifier.TryVerify(content, signature, out _))
                        {
                            return null;
                        }
                    }

                    return JObject.Parse(content);
                }

                return JObject.Parse(File.ReadAllText(catalogPath));
            }
            catch
            {
                return null;
            }
        }

        private static async Task<JObject> EnsurePluginsCatalogAsync(
            JObject manifest,
            string version,
            string installDir,
            IProgress<string>? log,
            CancellationToken cancellationToken)
        {
            var catalogMeta = manifest["pluginsCatalog"] as JObject;
            var catalogName = catalogMeta?["name"]?.ToString() ?? "plugins-catalog.json";
            var catalogPath = Path.Combine(installDir, catalogName);
            var catalogSigPath = Path.Combine(installDir, $"{Path.GetFileNameWithoutExtension(catalogName)}.sig");
            var expectedHash = catalogMeta?["hash"]?.ToString();
            var needsDownload = !File.Exists(catalogPath) || !File.Exists(catalogSigPath);

            if (!needsDownload && !string.IsNullOrWhiteSpace(expectedHash) && File.Exists(catalogPath))
            {
                var localHash = ComputeSha256(catalogPath);
                needsDownload = !localHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
            }

            if (needsDownload)
            {
                if (catalogMeta == null)
                {
                    throw new InvalidOperationException("Plugins catalog is not available in this release.");
                }

                if (string.IsNullOrWhiteSpace(expectedHash))
                {
                    throw new InvalidOperationException("Plugins catalog metadata is incomplete.");
                }

                log?.Report("Downloading plugins catalog ...");

                TryDeleteLocalFile(catalogPath);
                TryDeleteLocalFile(catalogSigPath);

                var manager = new PluginPackageManager();
                await manager.DownloadFileAsync(
                    UpdateRepositoryConfig.FileDownloadUrl(version, catalogName),
                    catalogPath,
                    cancellationToken).ConfigureAwait(false);

                var actualHash = ComputeSha256(catalogPath);
                if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    TryDeleteLocalFile(catalogPath);
                    throw new InvalidOperationException("Plugins catalog hash mismatch.");
                }

                await manager.DownloadFileAsync(
                    UpdateRepositoryConfig.FileDownloadUrl(version, Path.GetFileName(catalogSigPath)),
                    catalogSigPath,
                    cancellationToken).ConfigureAwait(false);
            }

            return await LoadVerifiedCatalogAsync(catalogPath, catalogSigPath, cancellationToken).ConfigureAwait(false);
        }

        private static void TryDeleteLocalFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // ignore cleanup errors
            }
        }

        private static async Task<JObject> LoadVerifiedCatalogAsync(
            string catalogPath,
            string catalogSigPath,
            CancellationToken cancellationToken)
        {
            var content = await File.ReadAllTextAsync(catalogPath, cancellationToken).ConfigureAwait(false);
            var signature = await File.ReadAllTextAsync(catalogSigPath, cancellationToken).ConfigureAwait(false);
            if (!UpdateManifestVerifier.TryVerify(content, signature, out var verifyError))
            {
                throw new InvalidOperationException(verifyError);
            }

            return JObject.Parse(content);
        }

        private static bool TryFindCatalogEntry(
            JObject catalog,
            string pluginId,
            out JObject entry,
            out string resolvedId)
        {
            entry = null!;
            resolvedId = string.Empty;
            if (catalog["plugins"] is not JArray plugins)
            {
                return false;
            }

            foreach (var token in plugins)
            {
                if (token is not JObject obj)
                {
                    continue;
                }

                var id = obj["id"]?.ToString();
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (!string.Equals(id, pluginId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                entry = obj;
                resolvedId = id;
                return true;
            }

            return false;
        }

        private static async Task<JObject> FetchManifestAsync(CancellationToken cancellationToken)
        {
            using var manifestRequest = new HttpRequestMessage(HttpMethod.Get, UpdateRepositoryConfig.ManifestUrl);
            using var manifestResponse = await HttpClient.SendAsync(manifestRequest, cancellationToken).ConfigureAwait(false);
            manifestResponse.EnsureSuccessStatusCode();
            var content = await manifestResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            using var signatureRequest = new HttpRequestMessage(HttpMethod.Get, UpdateRepositoryConfig.ManifestSignatureUrl);
            using var signatureResponse = await HttpClient.SendAsync(signatureRequest, cancellationToken).ConfigureAwait(false);
            signatureResponse.EnsureSuccessStatusCode();
            var signature = await signatureResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!UpdateManifestVerifier.TryVerify(content, signature, out var verifyError))
            {
                throw new InvalidOperationException(verifyError);
            }

            return JObject.Parse(content);
        }

        private async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
        {
            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await HttpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var file = File.Create(destinationPath);
            await stream.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
        }

        private static string ComputeSha256(string filePath)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            return Convert.ToHexString(sha.ComputeHash(stream));
        }

        private sealed class InstalledPluginsState
        {
            internal Dictionary<string, InstalledOptionalPlugin> Optional { get; } =
                new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class InstalledOptionalPlugin
        {
            internal string Version { get; init; } = string.Empty;

            internal string Folder { get; init; } = string.Empty;

            internal string PackageHash { get; init; } = string.Empty;

            internal DateTime InstalledAtUtc { get; init; }
        }

        private static InstalledPluginsState LoadInstalledPluginsState(string installDir)
        {
            var path = Path.Combine(installDir, "configs", "installed-plugins.json");
            var state = new InstalledPluginsState();
            if (!File.Exists(path))
            {
                return state;
            }

            try
            {
                var root = JObject.Parse(File.ReadAllText(path));
                if (root["optional"] is not JObject optional)
                {
                    return state;
                }

                foreach (var prop in optional.Properties())
                {
                    if (prop.Value is not JObject obj)
                    {
                        continue;
                    }

                    state.Optional[prop.Name] = new InstalledOptionalPlugin
                    {
                        Version = obj["version"]?.ToString() ?? string.Empty,
                        Folder = obj["folder"]?.ToString() ?? prop.Name,
                        PackageHash = obj["packageHash"]?.ToString() ?? string.Empty,
                        InstalledAtUtc = DateTime.TryParse(
                            obj["installedAtUtc"]?.ToString(),
                            out var parsed)
                            ? parsed.ToUniversalTime()
                            : DateTime.MinValue,
                    };
                }
            }
            catch
            {
                return new InstalledPluginsState();
            }

            return state;
        }

        private static void SaveInstalledPluginsState(string installDir, InstalledPluginsState state)
        {
            var path = Path.Combine(installDir, "configs", "installed-plugins.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var optional = new JObject();
            foreach (var pair in state.Optional.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                optional[pair.Key] = new JObject
                {
                    ["version"] = pair.Value.Version,
                    ["folder"] = pair.Value.Folder,
                    ["packageHash"] = pair.Value.PackageHash,
                    ["installedAtUtc"] = pair.Value.InstalledAtUtc.ToString("o"),
                };
            }

            var root = new JObject
            {
                ["schema"] = 1,
                ["optional"] = optional,
            };

            File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        private static void EnsurePluginMetadata(string installDir, string pluginId, bool autoStart)
        {
            var path = Path.Combine(installDir, "configs", "plugins.json");
            JObject root;
            if (File.Exists(path))
            {
                root = JObject.Parse(File.ReadAllText(path));
            }
            else
            {
                root = new JObject();
            }

            if (root[pluginId] is JObject)
            {
                return;
            }

            root[pluginId] = new JObject
            {
                ["Enable"] = false,
                ["AutoStart"] = autoStart,
            };

            File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented));
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

        private static void SavePendingRemovals(string installDir, IEnumerable<string> folders)
        {
            var path = Path.Combine(installDir, "configs", "pending-plugin-removals.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var unique = folders
                .Where(static folder => !string.IsNullOrWhiteSpace(folder))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static folder => folder, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (unique.Length == 0)
            {
                try { File.Delete(path); } catch { }
                return;
            }

            var root = new JObject
            {
                ["schema"] = 1,
                ["folders"] = new JArray(unique),
            };
            File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        private static void SchedulePendingRemoval(string installDir, string folder)
        {
            var pending = LoadPendingRemovals(installDir);
            pending.Add(folder);
            SavePendingRemovals(installDir, pending);
        }

        private static void RemovePendingRemoval(string installDir, string folder)
        {
            var pending = LoadPendingRemovals(installDir);
            if (!pending.Remove(folder))
            {
                return;
            }

            SavePendingRemovals(installDir, pending);
        }

        private static void DisablePluginMetadata(string installDir, string pluginId)
        {
            var path = Path.Combine(installDir, "configs", "plugins.json");
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                var root = JObject.Parse(File.ReadAllText(path));
                if (root[pluginId] is not JObject meta)
                {
                    return;
                }

                meta["Enable"] = false;
                meta["AutoStart"] = false;
                File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented));
            }
            catch
            {
            }
        }

        private static string ReadCatalogScalar(JToken? token)
        {
            return token?.Type switch
            {
                JTokenType.String => token.ToString().Trim(),
                JTokenType.Object => token["en"]?.ToString()?.Trim() ?? string.Empty,
                _ => string.Empty,
            };
        }

        private static void ReadCatalogLocalized(JToken? token, out string english, out string german)
        {
            english = string.Empty;
            german = string.Empty;
            if (token == null)
            {
                return;
            }

            if (token.Type == JTokenType.String)
            {
                english = token.ToString().Trim();
                return;
            }

            if (token.Type != JTokenType.Object)
            {
                return;
            }

            english = token["en"]?.ToString()?.Trim() ?? string.Empty;
            german = token["de"]?.ToString()?.Trim() ?? string.Empty;
        }
    }
}
