namespace Downloader

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



    internal sealed class GameHelperDownloadService

    {

        private const string DefaultPluginsJson = """

            {

              "AutoPot": { "Enable": true, "AutoStart": true },

              "HealthBars": { "Enable": true, "AutoStart": true },

              "Radar": { "Enable": true, "AutoStart": true },

              "PreloadAlert": { "Enable": false, "AutoStart": false },

              "Atlas": { "Enable": false, "AutoStart": false },

              "AutoHotKeyTrigger": { "Enable": false, "AutoStart": false },

              "AuraTracker": { "Enable": false, "AutoStart": false },

              "MapKillCounter": { "Enable": false, "AutoStart": false },

              "FarmTracker": { "Enable": false, "AutoStart": false },

              "AmanamuVoidAlert": { "Enable": false, "AutoStart": false },

              "PlayerBuffBar": { "Enable": false, "AutoStart": false },

              "RitualHelper": { "Enable": false, "AutoStart": false },

              "RuneforgeHelper": { "Enable": false, "AutoStart": false },

              "RunecraftHelper": { "Enable": false, "AutoStart": false },

              "SekhemaHelper": { "Enable": false, "AutoStart": false },

              "Hiveblood": { "Enable": false, "AutoStart": false },

              "SimpleBars": { "Enable": false, "AutoStart": false },

              "Wraedar": { "Enable": false, "AutoStart": false }

            }

            """;

        private const string DefaultInstalledPluginsJson = """

            {

              "schema": 1,

              "optional": {}

            }

            """;



        private static readonly HttpClient HttpClient = CreateClient();



        private static readonly string[] RequiredRootFiles =

        [

            "GameHelper.exe",

            "GameHelper.App.exe",

            "AsmResolver.dll",

            "AsmResolver.PE.dll",

            "AsmResolver.PE.File.dll",

            "AsmResolver.PE.Win32Resources.dll",

        ];



        private static HttpClient CreateClient()

        {

            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

            client.DefaultRequestHeaders.UserAgent.ParseAdd("GameHelperDownloader/1.0");

            return client;

        }



        internal async Task<DownloadResult> DownloadAsync(

            string targetDir,

            bool force,

            IProgress<string>? log,

            CancellationToken cancellationToken,

            bool preferFullPackage = false)

        {
            targetDir = Path.GetFullPath(targetDir);

            if (Directory.Exists(targetDir) &&

                Directory.EnumerateFileSystemEntries(targetDir).Any() &&

                !force)

            {

                return DownloadResult.Fail(

                    2,

                    DownloaderLocalization.B(

                        "The target folder is not empty. Choose an empty folder or use --force.",

                        "Der Zielordner ist nicht leer. Leeren Ordner waehlen oder --force verwenden."));

            }



            Directory.CreateDirectory(targetDir);



            log?.Report($"{DownloaderLocalization.B("Repository", "Repository")}: {UpdateRepositoryConfig.Repository}");



            log?.Report(DownloaderLocalization.B(

                "Loading manifest.json ...",

                "Lade manifest.json ..."));

            JObject manifest;

            try

            {

                manifest = await this.FetchManifestAsync(cancellationToken).ConfigureAwait(false);

            }

            catch (Exception ex)

            {

                return DownloadResult.Fail(

                    1,

                    $"{DownloaderLocalization.B("Manifest not reachable", "Manifest nicht erreichbar")}: {ex.Message}. " +

                    DownloaderLocalization.B(

                        "Is a release published on GitHub?",

                        "Ist ein Release auf GitHub veroeffentlicht?"));

            }



            var version = manifest["version"]?.ToString();

            if (string.IsNullOrWhiteSpace(version))

            {

                return DownloadResult.Fail(

                    1,

                    DownloaderLocalization.B(

                        "manifest.json is invalid or empty. Is a release published on GitHub?",

                        "manifest.json ist ungueltig oder leer. Ist ein Release auf GitHub veroeffentlicht?"));

            }



            log?.Report($"{DownloaderLocalization.B("Version", "Version")}: {version}");

            var published = manifest["published"]?.ToString();

            if (!string.IsNullOrWhiteSpace(published))

            {

                log?.Report($"{DownloaderLocalization.B("Published", "Veroeffentlicht")}: {published}");

            }



            if (UpdateZipPackage.TryResolveDownloadPackage(manifest, preferFullPackage, out var zipPackage))

            {

                log?.Report($"{DownloaderLocalization.B("Package", "Paket")}: {zipPackage.Name}");

                var zipPath = Path.Combine(targetDir, zipPackage.Name);

                try

                {

                    var url = DownloadConfig.FileDownloadUrl(version!, zipPackage.Name);

                    log?.Report(DownloaderLocalization.B(

                        preferFullPackage ? "Downloading full package ..." : "Downloading core package ...",

                        preferFullPackage ? "Lade Vollstaendiges Paket ..." : "Lade Core-Paket ..."));

                    await this.DownloadFileAsync(url, zipPath, cancellationToken).ConfigureAwait(false);

                    var actualHash = ComputeSha256(zipPath);

                    if (!actualHash.Equals(zipPackage.Hash, StringComparison.OrdinalIgnoreCase))

                    {

                        throw new InvalidOperationException(DownloaderLocalization.B(

                            $"Hash mismatch (got {actualHash}, expected {zipPackage.Hash})",

                            $"Hash stimmt nicht (ist {actualHash}, erwartet {zipPackage.Hash})"));

                    }



                    log?.Report(DownloaderLocalization.B(

                        "Extracting package ...",

                        "Entpacke Paket ..."));

                    UpdateZipPackage.ExtractToDirectory(zipPath, targetDir);

                    try { File.Delete(zipPath); } catch { }

                }

                catch (Exception ex)

                {

                    return DownloadResult.Fail(

                        1,

                        $"{DownloaderLocalization.B("Package download failed", "Paket-Download fehlgeschlagen")}: {ex.Message}");

                }

            }

            else

            {

            var files = manifest["files"] as JArray;

            if (files == null || files.Count == 0)

            {

                return DownloadResult.Fail(

                    1,

                    DownloaderLocalization.B(

                        "manifest.json is invalid or empty. Is a release published on GitHub?",

                        "manifest.json ist ungueltig oder leer. Ist ein Release auf GitHub veroeffentlicht?"));

            }



            log?.Report($"{DownloaderLocalization.B("Files", "Dateien")}: {files.Count}");



            var failed = new List<string>();

            var index = 0;

            foreach (var entry in files)

            {

                cancellationToken.ThrowIfCancellationRequested();

                index++;



                var relativePath = entry["path"]?.ToString();

                var expectedHash = entry["hash"]?.ToString();

                if (string.IsNullOrWhiteSpace(relativePath) || string.IsNullOrWhiteSpace(expectedHash))

                {

                    continue;

                }



                var packageName = entry["package"]?.ToString()

                    ?? relativePath.Replace('\\', '/').Replace('/', '.');

                if (!UpdatePathSecurity.TryResolvePath(targetDir, relativePath, out var destPath))
                {
                    failed.Add(relativePath);
                    log?.Report($"  [{index}/{files.Count}] {DownloaderLocalization.B("ERROR", "FEHLER")}: {relativePath} - unsafe path");
                    continue;
                }

                if (File.Exists(destPath))

                {

                    var localHash = ComputeSha256(destPath);

                    if (localHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))

                    {

                        log?.Report($"  [{index}/{files.Count}] {DownloaderLocalization.B("skipped", "uebersprungen")}: {relativePath}");

                        continue;

                    }

                }



                var url = DownloadConfig.FileDownloadUrl(version!, packageName);

                try

                {

                    await this.DownloadFileAsync(url, destPath, cancellationToken).ConfigureAwait(false);

                    var actualHash = ComputeSha256(destPath);

                    if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))

                    {

                        throw new InvalidOperationException(DownloaderLocalization.B(

                            $"Hash mismatch (got {actualHash}, expected {expectedHash})",

                            $"Hash stimmt nicht (ist {actualHash}, erwartet {expectedHash})"));

                    }



                    log?.Report($"  [{index}/{files.Count}] OK: {relativePath}");

                }

                catch (Exception ex)

                {

                    failed.Add(relativePath);

                    log?.Report($"  [{index}/{files.Count}] {DownloaderLocalization.B("ERROR", "FEHLER")}: {relativePath} - {ex.Message}");

                }

            }



            if (failed.Count > 0)

            {

                return DownloadResult.Fail(

                    1,

                    DownloaderLocalization.B(

                        $"Download finished with errors ({failed.Count} file(s)).",

                        $"Download mit Fehlern beendet ({failed.Count} Datei(en))."));

            }

            }



            await File.WriteAllTextAsync(

                Path.Combine(targetDir, "VERSION.txt"),

                $"GameHelper {version}\nDownloaded: {DateTime.UtcNow:o}",

                cancellationToken).ConfigureAwait(false);



            var pluginsJson = Path.Combine(targetDir, "configs", "plugins.json");

            if (!File.Exists(pluginsJson))

            {

                Directory.CreateDirectory(Path.GetDirectoryName(pluginsJson)!);

                await File.WriteAllTextAsync(pluginsJson, DefaultPluginsJson, cancellationToken).ConfigureAwait(false);

                log?.Report(DownloaderLocalization.B(

                    "Default plugins.json created.",

                    "Standard plugins.json erstellt."));

            }



            await this.EnsureInstalledPluginsStateAsync(targetDir, cancellationToken).ConfigureAwait(false);

            await this.TryDownloadPluginsCatalogAsync(manifest, version!, targetDir, log, cancellationToken).ConfigureAwait(false);



            UpdateFileHashesCatalog.SaveFromManifest(targetDir, manifest);

            UpdateStateHelper.Save(targetDir, published ?? string.Empty, version!,

                UpdateZipPackage.TryResolveDownloadPackage(manifest, preferFullPackage, out var installedPkg) ? installedPkg.Hash : null);



            var missingRequired = this.FindMissingRequiredFiles(targetDir);

            if (missingRequired != null)

            {

                return DownloadResult.Fail(

                    1,

                    DownloaderLocalization.B(

                        $"Installation incomplete. Missing: {missingRequired}. Delete the folder and download again into an empty folder.",

                        $"Installation unvollstaendig. Fehlend: {missingRequired}. Ordner loeschen und erneut in einen LEEREN Ordner laden."));

            }



            return DownloadResult.Ok(targetDir, version!);

        }



        private async Task EnsureInstalledPluginsStateAsync(string targetDir, CancellationToken cancellationToken)

        {

            var path = Path.Combine(targetDir, "configs", "installed-plugins.json");

            if (File.Exists(path))

            {

                return;

            }



            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            await File.WriteAllTextAsync(path, DefaultInstalledPluginsJson, cancellationToken).ConfigureAwait(false);

        }



        private async Task TryDownloadPluginsCatalogAsync(

            JObject manifest,

            string version,

            string targetDir,

            IProgress<string>? log,

            CancellationToken cancellationToken)

        {

            if (manifest["pluginsCatalog"] is not JObject catalogMeta)

            {

                return;

            }



            var catalogName = catalogMeta["name"]?.ToString();

            var catalogHash = catalogMeta["hash"]?.ToString();

            if (string.IsNullOrWhiteSpace(catalogName) || string.IsNullOrWhiteSpace(catalogHash))

            {

                return;

            }



            var catalogPath = Path.Combine(targetDir, catalogName);

            var catalogSigPath = Path.Combine(targetDir, $"{Path.GetFileNameWithoutExtension(catalogName)}.sig");

            try

            {

                var url = DownloadConfig.FileDownloadUrl(version, catalogName);

                log?.Report(DownloaderLocalization.B(

                    "Downloading plugins catalog ...",

                    "Lade Plugin-Katalog ..."));

                await this.DownloadFileAsync(url, catalogPath, cancellationToken).ConfigureAwait(false);

                var actualHash = ComputeSha256(catalogPath);

                if (!actualHash.Equals(catalogHash, StringComparison.OrdinalIgnoreCase))

                {

                    throw new InvalidOperationException(DownloaderLocalization.B(

                        $"Plugins catalog hash mismatch (got {actualHash}, expected {catalogHash})",

                        $"Plugin-Katalog Hash stimmt nicht (ist {actualHash}, erwartet {catalogHash})"));

                }



                var sigUrl = DownloadConfig.FileDownloadUrl(version, Path.GetFileName(catalogSigPath));

                await this.DownloadFileAsync(sigUrl, catalogSigPath, cancellationToken).ConfigureAwait(false);

            }

            catch (Exception ex)

            {

                log?.Report(DownloaderLocalization.B(

                    $"Plugins catalog skipped: {ex.Message}",

                    $"Plugin-Katalog uebersprungen: {ex.Message}"));

            }

        }



        private async Task<JObject> FetchManifestAsync(CancellationToken cancellationToken)

        {

            using var manifestRequest = new HttpRequestMessage(HttpMethod.Get, DownloadConfig.ManifestUrl);

            using var manifestResponse = await HttpClient.SendAsync(manifestRequest, cancellationToken).ConfigureAwait(false);

            manifestResponse.EnsureSuccessStatusCode();

            var content = await manifestResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            using var signatureRequest = new HttpRequestMessage(HttpMethod.Get, DownloadConfig.ManifestSignatureUrl);

            using var signatureResponse = await HttpClient.SendAsync(signatureRequest, cancellationToken).ConfigureAwait(false);

            signatureResponse.EnsureSuccessStatusCode();

            var signature = await signatureResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!UpdateManifestVerifier.TryVerify(content, signature, out var verifyError))

            {

                throw new InvalidOperationException(verifyError);

            }

            return JObject.Parse(content);

        }



        private async Task DownloadFileAsync(

            string url,

            string destinationPath,

            CancellationToken cancellationToken)

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



        private string? FindMissingRequiredFiles(string targetDir)

        {

            var missing = RequiredRootFiles

                .Where(file => !File.Exists(Path.Combine(targetDir, file)))

                .ToList();

            return missing.Count == 0 ? null : string.Join(", ", missing);

        }



        private static string ComputeSha256(string filePath)

        {

            using var sha = SHA256.Create();

            using var stream = File.OpenRead(filePath);

            return Convert.ToHexString(sha.ComputeHash(stream));

        }

    }



    internal sealed class DownloadResult

    {

        internal int ExitCode { get; init; }

        internal string Message { get; init; } = string.Empty;

        internal string? TargetDir { get; init; }

        internal string? Version { get; init; }



        internal static DownloadResult Ok(string targetDir, string version) =>

            new() { ExitCode = 0, TargetDir = targetDir, Version = version, Message = "OK" };



        internal static DownloadResult Fail(int exitCode, string message) =>

            new() { ExitCode = exitCode, Message = message };

        internal static DownloadResult PluginActionOk(string targetDir, string message) =>

            new() { ExitCode = 0, TargetDir = targetDir, Message = message };

    }

}


