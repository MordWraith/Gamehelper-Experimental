namespace GameHelper.PluginStore
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using GameHelper.Localization;
    using GameHelper.Plugin;
    using Shared.PluginPackages;

    internal enum PluginStoreAction
    {
        None,
        RefreshCatalog,
        Install,
        Remove,
        Update,
    }

    internal sealed class PluginStoreController
    {
        private const int MaxProgressLines = 4;

        internal const int MaxStatusProgressLines = MaxProgressLines;

        private static readonly PluginStoreController Instance = new();

        private readonly PluginPackageManager manager = new();
        private readonly object sync = new();
        private readonly List<string> progressLines = new();

        private Task<PluginPackageResult>? runningTask;
        private PluginStoreAction runningAction;
        private string? runningPluginId;
        private string? pendingUnloadPluginId;
        private string? pendingLoadPluginId;
        private string statusMessage = string.Empty;
        private bool statusIsError;
        private IReadOnlyList<PluginCatalogEntry> entries = Array.Empty<PluginCatalogEntry>();
        private bool entriesDirty = true;
        private bool startupCatalogEnsured;
        private readonly HashSet<string> pendingRestartRemovals = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> pendingRestartUpdates = new(StringComparer.OrdinalIgnoreCase);

        private PluginStoreController()
        {
        }

        internal static PluginStoreController Default => Instance;

        internal bool IsBusy
        {
            get
            {
                lock (this.sync)
                {
                    return this.runningTask is { IsCompleted: false };
                }
            }
        }

        internal string StatusMessage
        {
            get
            {
                lock (this.sync)
                {
                    return this.statusMessage;
                }
            }
        }

        internal bool StatusIsError
        {
            get
            {
                lock (this.sync)
                {
                    return this.statusIsError;
                }
            }
        }

        internal IReadOnlyList<string> ProgressLines
        {
            get
            {
                lock (this.sync)
                {
                    return this.progressLines.ToArray();
                }
            }
        }

        internal IReadOnlyList<PluginCatalogEntry> Entries
        {
            get
            {
                this.EnsureEntriesLoaded();
                lock (this.sync)
                {
                    return this.entries;
                }
            }
        }

        internal IReadOnlyList<string> PendingRestartRemovals
        {
            get
            {
                lock (this.sync)
                {
                    return this.pendingRestartRemovals
                        .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
            }
        }

        internal bool NeedsRestartAfterRemoval
        {
            get
            {
                lock (this.sync)
                {
                    return this.pendingRestartRemovals.Count > 0;
                }
            }
        }

        internal IReadOnlyList<string> PendingRestartUpdates
        {
            get
            {
                lock (this.sync)
                {
                    return this.pendingRestartUpdates
                        .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
            }
        }

        internal bool NeedsRestartAfterUpdate
        {
            get
            {
                lock (this.sync)
                {
                    return this.pendingRestartUpdates.Count > 0;
                }
            }
        }

        internal bool NeedsRestart => this.NeedsRestartAfterRemoval || this.NeedsRestartAfterUpdate;

        internal void EnsureStartupCatalog()
        {
            if (this.startupCatalogEnsured)
            {
                return;
            }

            this.startupCatalogEnsured = true;
            if (!this.IsBusy)
            {
                this.RefreshCatalog();
            }
        }

        internal IReadOnlyList<string> GetInstalledPluginsNeedingUpdate()
        {
            if (this.IsBusy)
            {
                return Array.Empty<string>();
            }

            this.EnsureEntriesLoaded();
            lock (this.sync)
            {
                return this.entries
                    .Where(entry => entry.IsInstalled
                                    && entry.NeedsUpdate
                                    && !entry.IsPendingRemoval
                                    && !entry.IsPendingUpdate
                                    && !this.pendingRestartUpdates.Contains(entry.Id))
                    .Select(entry => entry.Id)
                    .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        internal void Tick()
        {
            this.ProcessPendingPluginQueue();

            Task<PluginPackageResult>? completedTask = null;
            PluginStoreAction completedAction = PluginStoreAction.None;
            string? completedPluginId = null;

            lock (this.sync)
            {
                if (this.runningTask is { IsCompleted: true } task)
                {
                    completedTask = task;
                    completedAction = this.runningAction;
                    completedPluginId = this.runningPluginId;
                    this.runningTask = null;
                    this.runningAction = PluginStoreAction.None;
                    this.runningPluginId = null;
                }
            }

            if (completedTask == null)
            {
                return;
            }

            var result = completedTask.Result;
            lock (this.sync)
            {
                this.statusMessage = LocalizeResultMessage(result.Message);
                this.statusIsError = !result.Success;
                this.entriesDirty = true;
            }

            if (result.Success && completedAction is PluginStoreAction.Remove && completedPluginId != null)
            {
                lock (this.sync)
                {
                    this.pendingRestartRemovals.Add(completedPluginId);
                }
            }

            if (result.Success && completedAction is PluginStoreAction.Update && completedPluginId != null)
            {
                lock (this.sync)
                {
                    this.pendingRestartUpdates.Add(completedPluginId);
                }
            }

            if (result.Success &&
                completedPluginId != null &&
                completedAction is PluginStoreAction.Install)
            {
                this.pendingLoadPluginId = completedPluginId;
            }

            if (completedAction is PluginStoreAction.Install or PluginStoreAction.Update or PluginStoreAction.Remove)
            {
                Console.WriteLine($"[PluginStore] {result.Message}");
            }
        }

        internal void RefreshCatalog()
        {
            this.StartTask(PluginStoreAction.RefreshCatalog, null, token =>
                this.manager.RefreshCatalogAsync(this.GetInstallDir(), this.CreateProgress(), token));
        }

        internal void Install(string pluginId)
        {
            if (this.IsPluginAwaitingRestart(pluginId))
            {
                this.SetStatus(
                    OverlayLocalization.L(
                        "Restart GameHelper first to finish a pending plugin change.",
                        "GameHelper zuerst neu starten, um eine ausstehende Plugin-Aenderung abzuschliessen."),
                    isError: true);
                return;
            }

            if (!this.TryPreparePluginForFileChange(pluginId, out var error))
            {
                this.SetStatus(error, isError: true);
                return;
            }

            this.StartTask(PluginStoreAction.Install, pluginId, token =>
                this.manager.InstallPluginAsync(this.GetInstallDir(), pluginId, this.CreateProgress(), token));
        }

        internal void Remove(string pluginId)
        {
            if (this.IsPluginAwaitingRestart(pluginId))
            {
                this.SetStatus(
                    OverlayLocalization.L(
                        "Restart GameHelper first to finish a pending plugin change.",
                        "GameHelper zuerst neu starten, um eine ausstehende Plugin-Aenderung abzuschliessen."),
                    isError: true);
                return;
            }

            if (!this.TryPreparePluginForFileChange(pluginId, out var error))
            {
                this.SetStatus(error, isError: true);
                return;
            }

            this.StartTask(PluginStoreAction.Remove, pluginId, token =>
                this.manager.RemovePluginAsync(this.GetInstallDir(), pluginId, this.CreateProgress(), token));
        }

        internal void Update(string pluginId)
        {
            if (this.IsPluginAwaitingRestart(pluginId))
            {
                this.SetStatus(
                    OverlayLocalization.L(
                        "Restart GameHelper first to apply the pending update.",
                        "GameHelper zuerst neu starten, um das ausstehende Update anzuwenden."),
                    isError: true);
                return;
            }

            if (!this.TryPreparePluginForFileChange(pluginId, out var error))
            {
                this.SetStatus(error, isError: true);
                return;
            }

            this.StartTask(PluginStoreAction.Update, pluginId, token =>
                this.manager.InstallPluginAsync(this.GetInstallDir(), pluginId, this.CreateProgress(), token));
        }

        private void ProcessPendingPluginQueue()
        {
            string? unloadId;
            string? loadId;
            lock (this.sync)
            {
                unloadId = this.pendingUnloadPluginId;
                this.pendingUnloadPluginId = null;
                loadId = this.pendingLoadPluginId;
                this.pendingLoadPluginId = null;
            }

            if (!string.IsNullOrWhiteSpace(unloadId))
            {
                PManager.UnloadPlugin(unloadId);
                PManager.RequestSaveAllSettings();
            }

            if (!string.IsNullOrWhiteSpace(loadId))
            {
                if (PManager.LoadPlugin(loadId))
                {
                    lock (this.sync)
                    {
                        this.statusMessage = OverlayLocalization.L(
                            $"{loadId} loaded. Enable it in Plugins > Plugin catalog.",
                            $"{loadId} geladen. Unter Plugins > Plugin-Katalog aktivieren.");
                        this.statusIsError = false;
                    }
                }
            }
        }

        private bool TryPreparePluginForFileChange(string pluginId, out string error)
        {
            if (this.IsBusy)
            {
                error = OverlayLocalization.L(
                    "Another plugin operation is still running.",
                    "Eine andere Plugin-Operation laeuft noch.");
                return false;
            }

            var container = PManager.Plugins.FirstOrDefault(
                p => string.Equals(p.Name, pluginId, StringComparison.OrdinalIgnoreCase));
            if (container == null)
            {
                error = string.Empty;
                return true;
            }

            try
            {
                if (container.Metadata.Enable)
                {
                    container.Metadata.Enable = false;
                    container.Plugin.SaveSettings();
                    container.Plugin.OnDisable();
                }
                else
                {
                    container.Plugin.SaveSettings();
                }
            }
            catch (Exception ex)
            {
                error = OverlayLocalization.L(
                    $"Could not disable {pluginId}: {ex.Message}",
                    $"Konnte {pluginId} nicht deaktivieren: {ex.Message}");
                return false;
            }

            PManager.UnloadPlugin(pluginId);
            PManager.RequestSaveAllSettings();
            error = string.Empty;
            return true;
        }

        private void StartTask(
            PluginStoreAction action,
            string? pluginId,
            Func<CancellationToken, Task<PluginPackageResult>> work)
        {
            lock (this.sync)
            {
                if (this.runningTask is { IsCompleted: false })
                {
                    return;
                }

                this.runningAction = action;
                this.runningPluginId = pluginId;
                this.progressLines.Clear();
                this.statusMessage = string.Empty;
                this.statusIsError = false;
                this.runningTask = Task.Run(() => work(CancellationToken.None));
            }
        }

        private void SetStatus(string message, bool isError)
        {
            lock (this.sync)
            {
                this.statusMessage = message;
                this.statusIsError = isError;
            }
        }

        internal void SetStatusMessage(string message, bool isError) => this.SetStatus(message, isError);

        private void EnsureEntriesLoaded()
        {
            lock (this.sync)
            {
                if (!this.entriesDirty)
                {
                    return;
                }

                this.entries = this.manager.GetOptionalPlugins(this.GetInstallDir());
                foreach (var entry in this.entries)
                {
                    if (entry.IsPendingUpdate)
                    {
                        this.pendingRestartUpdates.Add(entry.Id);
                    }
                }

                this.entriesDirty = false;
            }
        }

        internal bool IsPluginAwaitingRestart(string pluginId)
        {
            this.EnsureEntriesLoaded();

            lock (this.sync)
            {
                if (this.pendingRestartRemovals.Contains(pluginId)
                    || this.pendingRestartUpdates.Contains(pluginId))
                {
                    return true;
                }

                return this.entries.Any(
                    entry => string.Equals(entry.Id, pluginId, StringComparison.OrdinalIgnoreCase)
                             && (entry.IsPendingRemoval || entry.IsPendingUpdate));
            }
        }

        private string GetInstallDir() => AppContext.BaseDirectory;

        private IProgress<string> CreateProgress() => new BackgroundProgress(line =>
        {
            lock (this.sync)
            {
                this.progressLines.Add(line);
                while (this.progressLines.Count > MaxProgressLines)
                {
                    this.progressLines.RemoveAt(0);
                }
            }
        });

        private static string LocalizeResultMessage(string message)
        {
            if (message.StartsWith("Update scheduled for ", StringComparison.Ordinal))
            {
                return OverlayLocalization.L(
                    message,
                    message.Replace("Update scheduled for ", "Update geplant fuer ", StringComparison.Ordinal)
                        .Replace("Restart GameHelper to apply.", "GameHelper neu starten zum Anwenden."));
            }

            if (message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("cannot access the file", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("is denied", StringComparison.OrdinalIgnoreCase))
            {
                return OverlayLocalization.L(
                    "Plugin files are still in use. Restart GameHelper, then try again.",
                    "Plugin-Dateien sind noch gesperrt. GameHelper neu starten und erneut versuchen.");
            }

            if (message.StartsWith("Scheduled removal of ", StringComparison.Ordinal))
            {
                return OverlayLocalization.L(
                    message,
                    message.Replace("Scheduled removal of ", "Entfernung geplant fuer ", StringComparison.Ordinal));
            }

            if (message.StartsWith("Installed ", StringComparison.Ordinal))
            {
                return OverlayLocalization.L(
                    message.Replace(" Restart GameHelper to load the plugin.", string.Empty, StringComparison.Ordinal),
                    message.Replace(" Restart GameHelper to load the plugin.", string.Empty, StringComparison.Ordinal));
            }

            if (message.StartsWith("Removed ", StringComparison.Ordinal))
            {
                if (message.Contains("Restart GameHelper", StringComparison.OrdinalIgnoreCase))
                {
                    return message;
                }

                return OverlayLocalization.L(
                    message + " Restart GameHelper to apply changes.",
                    message + " GameHelper neu starten, um die Aenderung zu uebernehmen.");
            }

            return message;
        }

        private sealed class BackgroundProgress : IProgress<string>
        {
            private readonly Action<string> handler;

            internal BackgroundProgress(Action<string> handler) => this.handler = handler;

            public void Report(string value) => this.handler(value);
        }
    }
}
