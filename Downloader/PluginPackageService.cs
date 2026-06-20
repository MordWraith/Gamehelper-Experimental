namespace Downloader
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Shared.PluginPackages;

    internal sealed class PluginPackageService
    {
        private readonly PluginPackageManager manager = new();

        internal Task<DownloadResult> InstallPluginAsync(
            string targetDir,
            string pluginId,
            IProgress<string>? log,
            CancellationToken cancellationToken) =>
            WrapAsync(targetDir, this.manager.InstallPluginAsync(targetDir, pluginId, log, cancellationToken));

        internal Task<DownloadResult> RemovePluginAsync(
            string targetDir,
            string pluginId,
            IProgress<string>? log,
            CancellationToken cancellationToken) =>
            WrapAsync(targetDir, this.manager.RemovePluginAsync(targetDir, pluginId, log, cancellationToken));

        private static async Task<DownloadResult> WrapAsync(string targetDir, Task<PluginPackageResult> task)
        {
            var result = await task.ConfigureAwait(false);
            if (result.Success)
            {
                return DownloadResult.PluginActionOk(targetDir, Localize(result.Message));
            }

            return DownloadResult.Fail(result.ExitCode, Localize(result.Message));
        }

        private static string Localize(string message) =>
            message switch
            {
                "manifest.json is invalid or empty." =>
                    DownloaderLocalization.B(
                        "manifest.json is invalid or empty.",
                        "manifest.json ist ungueltig oder leer."),
                "Plugins catalog updated." =>
                    DownloaderLocalization.B(
                        "Plugins catalog updated.",
                        "Plugin-Katalog aktualisiert."),
                "Target folder does not exist." =>
                    DownloaderLocalization.B(
                        "Target folder does not exist.",
                        "Zielordner existiert nicht."),
                "Folder is not a GameHelper installation." =>
                    DownloaderLocalization.B(
                        "Target folder is not a GameHelper installation (GameHelper.exe missing).",
                        "Zielordner ist keine GameHelper-Installation (GameHelper.exe fehlt)."),
                _ when message.StartsWith("Repository:", StringComparison.Ordinal) => message,
                _ when message.StartsWith("Loading manifest", StringComparison.Ordinal) =>
                    DownloaderLocalization.B("Loading manifest.json ...", "Lade manifest.json ..."),
                _ when message.StartsWith("Downloading:", StringComparison.Ordinal) =>
                    $"{DownloaderLocalization.B("Downloading", "Lade")}: {message["Downloading:".Length..].Trim()}",
                _ when message.StartsWith("Extracting plugin", StringComparison.Ordinal) =>
                    DownloaderLocalization.B(
                        "Extracting plugin package ...",
                        "Entpacke Plugin-Paket ..."),
                _ when message.StartsWith("Removing:", StringComparison.Ordinal) =>
                    $"{DownloaderLocalization.B("Removing", "Entferne")}: {message["Removing:".Length..].Trim()}",
                _ when message.StartsWith("Manifest not reachable:", StringComparison.Ordinal) =>
                    $"{DownloaderLocalization.B("Manifest not reachable", "Manifest nicht erreichbar")}: {message["Manifest not reachable:".Length..].Trim()}",
                _ when message.StartsWith("Plugins catalog failed:", StringComparison.Ordinal) =>
                    $"{DownloaderLocalization.B("Plugins catalog failed", "Plugin-Katalog fehlgeschlagen")}: {message["Plugins catalog failed:".Length..].Trim()}",
                _ when message.StartsWith("Plugin install failed:", StringComparison.Ordinal) =>
                    $"{DownloaderLocalization.B("Plugin install failed", "Plugin-Installation fehlgeschlagen")}: {message["Plugin install failed:".Length..].Trim()}",
                _ when message.StartsWith("Plugin remove failed:", StringComparison.Ordinal) =>
                    $"{DownloaderLocalization.B("Plugin remove failed", "Plugin-Entfernen fehlgeschlagen")}: {message["Plugin remove failed:".Length..].Trim()}",
                _ when message.Contains(" is a core plugin", StringComparison.Ordinal) =>
                    DownloaderLocalization.B(message, message.Replace("ships with the Core package", "liegt im Core-Paket bei")),
                _ when message.StartsWith("Core plugin '", StringComparison.Ordinal) =>
                    DownloaderLocalization.B(message, message.Replace("cannot be removed", "kann nicht entfernt werden")),
                _ when message.StartsWith("Plugin '", StringComparison.Ordinal) && message.Contains("not found", StringComparison.Ordinal) =>
                    DownloaderLocalization.B(
                        message,
                        message.Replace("was not found in the catalog", "wurde im Katalog nicht gefunden")),
                _ when message.StartsWith("Plugin '", StringComparison.Ordinal) && message.Contains("not installed", StringComparison.Ordinal) =>
                    DownloaderLocalization.B(
                        message,
                        message.Replace("is not installed", "ist nicht installiert")),
                _ when message.StartsWith("Installed ", StringComparison.Ordinal) =>
                    DownloaderLocalization.B(
                        message,
                        message.Replace("Restart GameHelper to load the plugin.", "GameHelper neu starten, um das Plugin zu laden.")),
                _ when message.StartsWith("Removed ", StringComparison.Ordinal) =>
                    DownloaderLocalization.B(
                        message,
                        message.Replace("Restart GameHelper to apply changes.", "GameHelper neu starten, um die Aenderung zu uebernehmen.")),
                _ when message.StartsWith("Restart GameHelper to load ", StringComparison.Ordinal) =>
                    DownloaderLocalization.B(
                        message,
                        "GameHelper neu starten, um Plugins zu laden."),
                _ => message,
            };
    }
}
