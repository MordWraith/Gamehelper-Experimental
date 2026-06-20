// <copyright file="SettingsWindow.cs" company="None">
// Copyright (c) None. All rights reserved.
// </copyright>

namespace GameHelper.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using ClickableTransparentOverlay;
    using ClickableTransparentOverlay.Win32;
    using Coroutine;
    using CoroutineEvents;
    using ImGuiNET;
    using Plugin;
    using Utils;
    using GameOffsets.Objects.States.InGameState;
    using GameHelper.RemoteEnums.Entity;
    using GameHelper.RemoteEnums;
    using GameHelper.Localization;
    using GameHelper.PluginStore;
    using GameHelper.Ui;
    using Shared.PluginPackages;

    /// <summary>
    ///     Creates the MainMenu on the UI.
    /// </summary>
    internal static class SettingsWindow
    {
        private static bool isOverlayRunningLocal = true;
        private static bool isSettingsWindowVisible = true;
        private static Vector2 mainWindowPos;
        private static Vector2 mainWindowSize;

        internal static Vector2 MainWindowPos => mainWindowPos;

        internal static Vector2 MainWindowSize => mainWindowSize;

        internal static bool IsSettingsWindowVisible => isSettingsWindowVisible;

        private static EntityFilterType efilterType = EntityFilterType.PATH;
        private static string filterText = string.Empty;
        private static Rarity erarity = Rarity.Normal;
        private static GameStats eStats = 0;
        private static int filterGroup = 0;

        private static string specialNpcPath = string.Empty;

        private static string specialMiscObjPath = string.Empty;

        private static string monterPathToIgnore = string.Empty;

#if DEBUG
        private static string pluginForHotReload = string.Empty;
        private static bool pluginLoaded = true;
        private static bool showImGuiDemo = false;
#endif

        private static uint pluginsHubBarGeneration;

        /// <summary>
        ///     Initializes the Main Menu.
        /// </summary>
        internal static void InitializeCoroutines()
        {
            HideOnStartCheck();
            CoroutineHandler.Start(SaveCoroutine());
            Core.CoroutinesRegistrar.Add(CoroutineHandler.Start(
                RenderCoroutine(),
                "[Settings] Draw Core/Plugin settings",
                int.MaxValue));
        }

        private static void DrawManuBar()
        {
            if (!ImGui.BeginMenuBar())
            {
                return;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.TextMuted);
            ImGui.Text($"GameHelper {Core.GetVersion()}");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.TextDisabled("|");
            ImGui.SameLine();
            ImGui.TextDisabled($"{OverlayLocalization.L("Hide/show menu", "Menue ein/aus")}: {Core.GHSettings.MainMenuHotKey}");

#if DEBUG
            ImGui.SameLine(ImGui.GetWindowWidth() - 280f);
            ImGui.Checkbox("ImGui Demo", ref showImGuiDemo);
            if (showImGuiDemo)
            {
                ImGui.ShowDemoWindow(ref showImGuiDemo);
            }
#endif

            const string forkCredit = "Fork by MordWraith · basis Lafko / Gordin";
            var forkWidth = ImGui.CalcTextSize(forkCredit).X;
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0f, ImGui.GetContentRegionAvail().X - forkWidth));
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.TextMuted);
            ImGui.Text(forkCredit);
            ImGui.PopStyleColor();

            ImGui.EndMenuBar();
        }

        private static void DrawOptionalPluginUpdateNotice()
        {
            var updates = PluginStoreController.Default.GetInstalledPluginsNeedingUpdate();
            if (updates.Count == 0)
            {
                return;
            }

            var pluginList = string.Join(", ", updates);
            var text = OverlayLocalization.L(
                $"Update available: {pluginList}",
                $"Update verfügbar: {pluginList}");

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.92f, 0.16f, 1f));
            ImGui.TextUnformatted(text);
            ImGui.PopStyleColor();
            ImGui.Spacing();
        }

        private static void DrawHubToolbar()
        {
            var logLabel = OverlayLocalization.L("Log", "Log");
            var logButtonWidth = 130f;
            var logWasVisible = ActivityLogWindow.IsVisible;
            var logX = ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X - logButtonWidth;
            ImGui.SetCursorPosX(logX);
            if (logWasVisible)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiTheme.AccentMuted);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGuiTheme.Accent);
            }

            if (ImGui.Button(logLabel, new Vector2(logButtonWidth, 28)))
            {
                ActivityLogWindow.ToggleVisible();
            }

            if (logWasVisible)
            {
                ImGui.PopStyleColor(2);
            }

            ImGui.Spacing();
        }

        private static void DrawMainHubContent()
        {
            if (!ImGui.BeginTabBar("mainHubBar", ImGuiTabBarFlags.None))
            {
                return;
            }

            if (ImGui.BeginTabItem(OverlayLocalization.L("General", "Allgemein")))
            {
                ImGuiTheme.BeginPanel("MainHubGeneralPanel");
                DrawCoreSettings();
                ImGuiTheme.EndPanel();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(OverlayLocalization.L("Plugin settings", "Plugin-Einstellungen")))
            {
                ImGuiTheme.BeginPanel("MainHubPluginSettingsPanel");
                DrawPluginTabs();
                ImGuiTheme.EndPanel();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(OverlayLocalization.L("Plugins", "Plugins")))
            {
                ImGuiTheme.BeginPanel("MainHubPluginsPanel");
                DrawPluginsPanel(ImGui.IsItemActivated());
                ImGuiTheme.EndPanel();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        private static void DrawPluginTabs()
        {
            var enabledPlugins = PManager.Plugins
                .Where(p => p.Metadata.Enable)
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (enabledPlugins.Count == 0)
            {
                ImGui.TextDisabled(OverlayLocalization.L(
                    "No active plugins. Open the Plugins tab to enable some.",
                    "Keine aktiven Plugins. Tab Plugins oeffnen und welche aktivieren."));
                return;
            }

            if (!ImGui.BeginTabBar("pluginSettingsBar", ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.Reorderable))
            {
                return;
            }

            foreach (var container in enabledPlugins)
            {
                ImGuiTheme.PushPluginTabColors();

                if (ImGui.BeginTabItem($"{container.Name}##pluginCfg"))
                {
                    container.Plugin.DrawSettings();
                    ImGui.EndTabItem();
                }

                ImGuiTheme.PopPluginTabColors();
            }

            ImGui.EndTabBar();
        }

        private static void DrawOverlayLanguageAndFontWidget()
        {
            ImGuiTheme.SectionHeader(
                OverlayLocalization.L("Language & overlay font", "Sprache & Overlay-Schrift"),
                OverlayLocalization.L(
                    "UI language for menus and plugins. Font settings apply to the whole overlay.",
                    "Menuesprache fuer Einstellungen und Plugins. Schrifteinstellungen gelten fuer das ganze Overlay."));

            var fieldWidth = ImGui.GetContentRegionAvail().X;

            ImGui.Text(OverlayLocalization.L("UI language", "Menuesprache"));
            ImGui.SetNextItemWidth(fieldWidth);
            var lang = Core.GHSettings.OverlayLanguage;
            var preview = lang == OverlayLanguage.German
                ? OverlayLocalization.L("German", "Deutsch")
                : OverlayLocalization.L("English", "Englisch");
            if (ImGui.BeginCombo("##overlay_language", preview))
            {
                if (ImGui.Selectable(OverlayLocalization.L("English", "Englisch"), lang == OverlayLanguage.English) &&
                    lang != OverlayLanguage.English)
                {
                    Core.GHSettings.OverlayLanguage = OverlayLanguage.English;
                    PManager.RequestSaveAllSettings();
                }

                if (ImGui.Selectable(OverlayLocalization.L("German", "Deutsch"), lang == OverlayLanguage.German) &&
                    lang != OverlayLanguage.German)
                {
                    Core.GHSettings.OverlayLanguage = OverlayLanguage.German;
                    PManager.RequestSaveAllSettings();
                }

                ImGui.EndCombo();
            }

            ImGui.Spacing();
            CheckboxLabeled(
                OverlayLocalization.L("Universal font", "Universalschrift"),
                ref Core.GHSettings.UniversalFont,
                "Loads a bundled merged font (DejaVuSans + the font below + GNU Unifont over the whole " +
                "Unicode BMP) so text in any language renders everywhere. The font below is still merged in as the " +
                "priority for its language. Building the full atlas is heavier, so this is off by default.",
                "Laedt eine gebündelte Mischschrift (DejaVuSans + Schrift unten + GNU Unifont ueber die ganze " +
                "Unicode-BMP), damit Text in jeder Sprache ueberall angezeigt wird. Die Schrift unten bleibt fuer " +
                "ihre Sprache priorisiert. Der volle Atlas ist aufwaendiger — deshalb standardmaessig aus.");

            ImGui.Text(OverlayLocalization.L("Font path", "Schriftpfad"));
            ImGui.SetNextItemWidth(fieldWidth);
            InputTextTooltip(
                "##font_path",
                ref Core.GHSettings.FontPathName,
                300,
                "Path to a .ttf/.ttc font file on disk. Used as the priority font for your chosen glyph language.",
                "Pfad zu einer .ttf/.ttc-Schriftdatei. Wird als Prioritaetsschrift fuer die gewaehlte Glyph-Sprache genutzt.");

            ImGui.Text(OverlayLocalization.L("Font size", "Schriftgroesse"));
            ImGui.SetNextItemWidth(fieldWidth);
            DragIntTooltip(
                "##font_size",
                ref Core.GHSettings.FontSize,
                0.1f,
                13,
                40,
                "Overlay font size in pixels.",
                "Schriftgroesse des Overlays in Pixel.");

            ImGui.Text(OverlayLocalization.L("Glyph language", "Glyph-Sprache"));
            ImGui.SetNextItemWidth(fieldWidth);
            var languageChanged = ImGuiHelper.EnumComboBox("##font_glyph_language", ref Core.GHSettings.FontLanguage);
            ImGuiHelper.ToolTip(OverlayLocalization.L(
                "Which glyph subset to load from the font file above (e.g. CJK, Cyrillic).",
                "Welcher Glyph-Teilsatz aus der Schriftdatei oben geladen wird (z. B. CJK, Kyrillisch)."));

            ImGui.Text(OverlayLocalization.L("Custom glyph ranges", "Eigene Glyph-Bereiche"));
            ImGui.SetNextItemWidth(fieldWidth);
            var customLanguage = ImGui.InputText("##font_custom_glyph_ranges", ref Core.GHSettings.FontCustomGlyphRange, 100);
            ImGuiHelper.ToolTip(OverlayLocalization.L(
                "Advanced: only change if you know what you are doing. Example: with ArialUnicodeMS.ttf use " +
                "0x0020, 0xFFFF, 0x00 to load the full font texture. The final 0x00 ends the range.",
                "Expertenoption: nur aendern, wenn du weisst, was du tust. Beispiel: mit ArialUnicodeMS.ttf " +
                "0x0020, 0xFFFF, 0x00 fuer die volle Schrifttextur. Das letzte 0x00 beendet den Bereich."));
            if (languageChanged)
            {
                Core.GHSettings.FontCustomGlyphRange = string.Empty;
            }

            if (customLanguage)
            {
                Core.GHSettings.FontLanguage = FontGlyphRangeType.English;
            }

            if (ImGui.Button(OverlayLocalization.L("Apply font changes", "Schrift anwenden")))
            {
                UniversalFont.ApplyFromSettings();
            }
        }

        private static void DrawPluginsPanel(bool selectCatalogTab = false)
        {
            if (selectCatalogTab)
            {
                pluginsHubBarGeneration++;
            }

            ImGuiTheme.PushPluginTabColors();
            if (!ImGui.BeginTabBar($"pluginsHubBar###{pluginsHubBarGeneration}", ImGuiTabBarFlags.None))
            {
                ImGuiTheme.PopPluginTabColors();
                return;
            }

            var catalogTabOpen = true;
            var catalogTabFlags = selectCatalogTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
            if (ImGui.BeginTabItem(
                OverlayLocalization.L("Plugin catalog", "Plugin-Katalog"),
                ref catalogTabOpen,
                catalogTabFlags))
            {
                DrawOptionalPluginStore();
                ImGui.EndTabItem();
            }

            var installedTabOpen = true;
            if (ImGui.BeginTabItem(
                OverlayLocalization.L("Installed plugins", "Installierte Plugins"),
                ref installedTabOpen))
            {
                DrawInstalledPluginManagement();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
            ImGuiTheme.PopPluginTabColors();

            DrawPluginStoreStatus(PluginStoreController.Default);
        }

        private static void DrawInstalledPluginManagement()
        {
            ImGuiTheme.SectionHeader(
                OverlayLocalization.L("Plugin management", "Plugin-Verwaltung"),
                OverlayLocalization.L(
                    "Auto-start plugins load on GameHelper launch. Settings are saved immediately.",
                    "Autostart-Plugins werden beim Start geladen. Einstellungen werden sofort gespeichert."));

            var enabledCount = PManager.Plugins.Count(p => p.Metadata.Enable);
            ImGui.TextDisabled($"{OverlayLocalization.L("Active", "Aktiv")}: {enabledCount} / {PManager.Plugins.Count}");
            ImGui.SameLine();
            if (ImGui.SmallButton(OverlayLocalization.L("Enable all", "Alle an")))
            {
                SetAllPlugins(true);
            }

            ImGui.SameLine();
            if (ImGui.SmallButton(OverlayLocalization.L("Disable all", "Alle aus")))
            {
                SetAllPlugins(false);
            }

            ImGui.Spacing();

            var pluginCount = PManager.Plugins.Count;
            var tableFlags = BuildAdaptiveTableLayout(pluginCount, out var tableHeight);
            if (!ImGui.BeginTable("pluginTable", 4, tableFlags, new Vector2(0, tableHeight)))
            {
                return;
            }

            ImGui.TableSetupColumn(OverlayLocalization.L("Plugin", "Plugin"), ImGuiTableColumnFlags.WidthStretch, 0.52f);
            ImGui.TableSetupColumn(OverlayLocalization.L("Author", "Ersteller"), ImGuiTableColumnFlags.WidthFixed, 96f);
            ImGui.TableSetupColumn(OverlayLocalization.L("Status", "Status"), ImGuiTableColumnFlags.WidthFixed, 64f);
            ImGui.TableSetupColumn(OverlayLocalization.L("Enable", "Aktivieren"), ImGuiTableColumnFlags.WidthFixed, 52f);
            ImGui.TableHeadersRow();

            foreach (var container in PManager.Plugins.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(container.Name);

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.TextMuted);
                ImGui.Text(PluginCredits.GetOriginalAuthor(container.Name));
                ImGui.PopStyleColor();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                if (container.Metadata.Enable)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.Success);
                    ImGui.Text(OverlayLocalization.L("Active", "Aktiv"));
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.TextMuted);
                    ImGui.Text(OverlayLocalization.L("Off", "Aus"));
                    ImGui.PopStyleColor();
                }

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                var autoStart = container.Metadata.AutoStart;
                if (ImGui.Checkbox($"##enable_{container.Name}", ref autoStart))
                {
                    SetPluginAutoStart(container, autoStart);
                }
            }

            ImGui.EndTable();
        }

        private static void DrawOptionalPluginStore()
        {
            var store = PluginStoreController.Default;

            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.TextMuted);
            ImGui.TextWrapped(OverlayLocalization.L(
                "Download optional plugins from GitHub. Core plugins ship with the main install.",
                "Optionale Plugins von GitHub herunterladen. Core-Plugins liegen im Hauptpaket bei."));
            ImGui.PopStyleColor();
            ImGui.Spacing();

            var busy = store.IsBusy;
            if (busy)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.SmallButton(OverlayLocalization.L("Refresh catalog", "Katalog aktualisieren")))
            {
                store.RefreshCatalog();
            }

            if (busy)
            {
                ImGui.EndDisabled();
            }

            var entries = store.Entries;
            if (entries.Count == 0)
            {
                ImGui.TextDisabled(OverlayLocalization.L(
                    "No catalog loaded. Use Refresh catalog or reinstall from the Experimental downloader.",
                    "Kein Katalog geladen. Katalog aktualisieren oder neu per Experimental-Downloader installieren."));
            }
            else
            {
                var statusReserve = ComputePluginStoreStatusReserve(store);
                var tableFlags = BuildAdaptiveTableLayout(
                    entries.Count,
                    out var tableHeight,
                    reservedBelow: statusReserve,
                    fillAvailable: true);
                if (ImGui.BeginTable("optionalPluginStore", 7, tableFlags, new Vector2(0, tableHeight)))
                {
                ImGui.TableSetupColumn(OverlayLocalization.L("Plugin", "Plugin"), ImGuiTableColumnFlags.WidthFixed, 132f);
                ImGui.TableSetupColumn(OverlayLocalization.L("Author", "Ersteller"), ImGuiTableColumnFlags.WidthFixed, 88f);
                ImGui.TableSetupColumn(OverlayLocalization.L("Info", "Info"), ImGuiTableColumnFlags.WidthStretch, 0.42f);
                ImGui.TableSetupColumn(OverlayLocalization.L("Source", "Quelle"), ImGuiTableColumnFlags.WidthFixed, 52f);
                ImGui.TableSetupColumn(OverlayLocalization.L("Version", "Version"), ImGuiTableColumnFlags.WidthFixed, 56f);
                ImGui.TableSetupColumn(OverlayLocalization.L("Status", "Status"), ImGuiTableColumnFlags.WidthFixed, 108f);
                ImGui.TableSetupColumn(OverlayLocalization.L("Action", "Aktion"), ImGuiTableColumnFlags.WidthFixed, 148f);
                ImGui.TableHeadersRow();

                var pendingRestartUpdates = new HashSet<string>(
                    store.PendingRestartUpdates,
                    StringComparer.OrdinalIgnoreCase);

                foreach (var entry in entries)
                {
                    var awaitingRestart = entry.IsPendingRemoval
                                          || entry.IsPendingUpdate
                                          || pendingRestartUpdates.Contains(entry.Id);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(entry.Id);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    DrawCatalogAuthorLink(entry);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.TextMuted);
                    ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X);
                    ImGui.TextWrapped(PluginCatalogUi.Description(entry));
                    ImGui.PopTextWrapPos();
                    ImGui.PopStyleColor();

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    DrawCatalogLinkButton(entry);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(PluginCatalogUi.DisplayVersion(entry));

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    if (entry.IsPendingRemoval)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.Accent);
                        ImGui.Text(OverlayLocalization.L("Removal pending (restart)", "Entfernung ausstehend (Neustart)"));
                        ImGui.PopStyleColor();
                    }
                    else if (entry.IsPendingUpdate || pendingRestartUpdates.Contains(entry.Id))
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.Accent);
                        ImGui.Text(OverlayLocalization.L("Update pending (restart)", "Update ausstehend (Neustart)"));
                        ImGui.PopStyleColor();
                    }
                    else if (!entry.IsInstalled)
                    {
                        ImGui.TextDisabled(OverlayLocalization.L("Not installed", "Nicht installiert"));
                    }
                    else if (entry.NeedsUpdate)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.Accent);
                        ImGui.Text(OverlayLocalization.L("Update available", "Update verfuegbar"));
                        ImGui.PopStyleColor();
                    }
                    else if (entry.IsLocalOrUnknownInstall)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.Success);
                        ImGui.Text(OverlayLocalization.L("Installed (local)", "Installiert (lokal)"));
                        ImGui.PopStyleColor();
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.Success);
                        ImGui.Text(OverlayLocalization.L("Installed", "Installiert"));
                        ImGui.PopStyleColor();
                    }

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    if (busy)
                    {
                        ImGui.BeginDisabled();
                    }

                    if (!entry.IsInstalled)
                    {
                        if (ColoredStoreActionButton(
                            OverlayLocalization.L("Download", "Herunterladen"),
                            ImGuiTheme.Success,
                            $"store_{entry.Id}"))
                        {
                            store.Install(entry.Id);
                        }
                    }
                    else if (awaitingRestart)
                    {
                        ImGui.TextDisabled(OverlayLocalization.L("Restart required", "Neustart noetig"));
                    }
                    else
                    {
                        if (entry.NeedsUpdate)
                        {
                            if (ColoredStoreActionButton(
                                PluginCatalogUi.UpdateButtonLabel(entry),
                                ImGuiTheme.Accent,
                                $"store_update_{entry.Id}"))
                            {
                                store.Update(entry.Id);
                            }

                            ImGui.SameLine();
                        }

                        if (ColoredStoreActionButton(
                            OverlayLocalization.L("Remove", "Entfernen"),
                            ImGuiTheme.Danger,
                            $"store_remove_{entry.Id}"))
                        {
                            store.Remove(entry.Id);
                        }
                    }

                    if (busy)
                    {
                        ImGui.EndDisabled();
                    }
                }

                ImGui.EndTable();
                }
            }
        }

        private static int ComputePluginStoreStatusLineCount(PluginStoreController store)
        {
            var lines = store.ProgressLines.Count;
            if (!string.IsNullOrWhiteSpace(store.StatusMessage))
            {
                lines++;
            }

            if (store.PendingRestartRemovals.Count > 0)
            {
                lines += 3;
            }

            if (store.PendingRestartUpdates.Count > 0)
            {
                lines += 3;
            }

            return Math.Clamp(lines, 1, PluginStoreController.MaxStatusProgressLines + 8);
        }

        private static float ComputePluginStoreStatusReserve(PluginStoreController store)
        {
            var progressCount = store.ProgressLines.Count;
            var hasStatus = !string.IsNullOrWhiteSpace(store.StatusMessage);
            var hasPending = store.PendingRestartRemovals.Count > 0 || store.PendingRestartUpdates.Count > 0;
            if (progressCount == 0 && !hasStatus && !hasPending)
            {
                return ImGui.GetStyle().ItemSpacing.Y;
            }

            var lineHeight = ImGui.GetTextLineHeightWithSpacing();
            var style = ImGui.GetStyle();
            var height = lineHeight * ComputePluginStoreStatusLineCount(store)
                         + style.WindowPadding.Y * 2f
                         + style.ItemSpacing.Y;
            if (store.NeedsRestart)
            {
                height += ImGui.GetFrameHeightWithSpacing() * 1.35f + style.ItemSpacing.Y;
            }

            return height + style.ItemSpacing.Y;
        }

        private static bool ColoredStoreActionButton(string label, Vector4 textColor, string id)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, textColor);
            var clicked = ImGui.SmallButton($"{label}##{id}");
            ImGui.PopStyleColor();
            return clicked;
        }

        private static void DrawCatalogAuthorLink(PluginCatalogEntry entry)
        {
            var author = PluginCatalogUi.Author(entry);
            var url = PluginCatalogUi.UpstreamUrl(entry);
            var hasUrl = Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.TextMuted);
            if (hasUrl)
            {
                if (ImGui.SmallButton($"{author}##catalog_author_{entry.Id}"))
                {
                    if (!ExternalLinkHelper.TryOpen(url, out var error))
                    {
                        PluginStoreController.Default.SetStatusMessage(
                            OverlayLocalization.L(
                                $"Could not open link: {error}",
                                $"Link konnte nicht geoeffnet werden: {error}"),
                            isError: true);
                    }
                }

                ImGuiHelper.ToolTip(OverlayLocalization.L(
                    $"Link to original author\n{url}",
                    $"Link zum Original-Ersteller\n{url}"));
            }
            else
            {
                ImGui.Text(author);
            }

            ImGui.PopStyleColor();
        }

        private static void DrawCatalogLinkButton(PluginCatalogEntry entry)
        {
            var url = PluginCatalogUi.ForkBrowseUrl(entry);
            var hasUrl = Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            if (!hasUrl)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.SmallButton($"{OverlayLocalization.L("Open", "Öffnen")}##catalog_link_{entry.Id}"))
            {
                if (!ExternalLinkHelper.TryOpen(url, out var error))
                {
                    PluginStoreController.Default.SetStatusMessage(
                        OverlayLocalization.L(
                            $"Could not open link: {error}",
                            $"Link konnte nicht geoeffnet werden: {error}"),
                        isError: true);
                }
            }

            if (!hasUrl)
            {
                ImGui.EndDisabled();
            }

            ImGuiHelper.ToolTip(OverlayLocalization.L(
                $"Browse MordWraith fork source on GitHub\n{url}",
                $"MordWraith-Fork-Quellcode auf GitHub ansehen\n{url}"));
        }

        private static void DrawPluginStoreStatus(PluginStoreController store)
        {
            var progressLines = store.ProgressLines;
            var statusMessage = store.StatusMessage;
            var pendingRemovals = store.PendingRestartRemovals;
            var pendingUpdates = store.PendingRestartUpdates;
            if (progressLines.Count == 0
                && string.IsNullOrWhiteSpace(statusMessage)
                && pendingRemovals.Count == 0
                && pendingUpdates.Count == 0)
            {
                return;
            }

            ImGui.Spacing();
            var lineHeight = ImGui.GetTextLineHeightWithSpacing();
            var statusHeight = lineHeight * ComputePluginStoreStatusLineCount(store)
                               + ImGui.GetStyle().WindowPadding.Y * 2f;
            ImGui.BeginChild("pluginStoreStatus", new Vector2(0, statusHeight), ImGuiChildFlags.Borders);
            foreach (var line in progressLines)
            {
                ImGui.TextWrapped(line);
            }

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                var color = store.StatusIsError ? ImGuiTheme.Danger : ImGuiTheme.Success;
                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X);
                ImGui.TextWrapped(statusMessage);
                ImGui.PopTextWrapPos();
                ImGui.PopStyleColor();
            }

            if (pendingRemovals.Count > 0)
            {
                if (progressLines.Count > 0 || !string.IsNullOrWhiteSpace(statusMessage))
                {
                    ImGui.Spacing();
                }

                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.Accent);
                ImGui.TextWrapped(OverlayLocalization.L("Restart required", "Neustart erforderlich"));
                ImGui.PopStyleColor();

                var pluginList = string.Join(", ", pendingRemovals);
                ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X);
                ImGui.TextWrapped(pendingRemovals.Count == 1
                    ? OverlayLocalization.L(
                        $"GameHelper must be restarted to finish removing {pluginList}.",
                        $"GameHelper muss neu gestartet werden, um die Entfernung von {pluginList} abzuschliessen.")
                    : OverlayLocalization.L(
                        $"GameHelper must be restarted to finish removing these plugins: {pluginList}.",
                        $"GameHelper muss neu gestartet werden, um die Entfernung dieser Plugins abzuschliessen: {pluginList}."));
                ImGui.PopTextWrapPos();
            }

            if (pendingUpdates.Count > 0)
            {
                if (progressLines.Count > 0 || !string.IsNullOrWhiteSpace(statusMessage) || pendingRemovals.Count > 0)
                {
                    ImGui.Spacing();
                }

                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.Accent);
                ImGui.TextWrapped(OverlayLocalization.L("Restart required", "Neustart erforderlich"));
                ImGui.PopStyleColor();

                var pluginList = string.Join(", ", pendingUpdates);
                ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X);
                ImGui.TextWrapped(pendingUpdates.Count == 1
                    ? OverlayLocalization.L(
                        $"GameHelper must be restarted to apply the {pluginList} update.",
                        $"GameHelper muss neu gestartet werden, um das {pluginList}-Update anzuwenden.")
                    : OverlayLocalization.L(
                        $"GameHelper must be restarted to apply these plugin updates: {pluginList}.",
                        $"GameHelper muss neu gestartet werden, um diese Plugin-Updates anzuwenden: {pluginList}."));
                ImGui.PopTextWrapPos();
            }

            ImGui.EndChild();

            if (store.NeedsRestart)
            {
                ImGui.Spacing();
                if (ImGui.Button(
                    OverlayLocalization.L("Restart GameHelper", "GameHelper neu starten"),
                    new Vector2(220f, ImGui.GetFrameHeightWithSpacing() * 1.35f)))
                {
                    if (!ApplicationRelauncher.TryRestart(out var error))
                    {
                        store.SetStatusMessage(
                            OverlayLocalization.L(
                                $"Restart failed: {error}",
                                $"Neustart fehlgeschlagen: {error}"),
                            isError: true);
                    }
                }
            }
        }

        private static ImGuiTableFlags BuildAdaptiveTableLayout(
            int rowCount,
            out float tableHeight,
            int minVisibleRows = 0,
            float reservedBelow = 8f,
            bool fillAvailable = false)
        {
            var style = ImGui.GetStyle();
            var rowHeight = ImGui.GetFrameHeightWithSpacing();
            var headerHeight = rowHeight + style.CellPadding.Y * 2f;
            var availableHeight = Math.Max(ImGui.GetContentRegionAvail().Y - reservedBelow, 120f);
            var maxRowsInAvail = Math.Max(
                1,
                (int)((availableHeight - headerHeight - style.CellPadding.Y) / rowHeight));

            int displayRows;
            bool needsScroll;
            if (fillAvailable)
            {
                displayRows = maxRowsInAvail;
                needsScroll = rowCount > displayRows;
            }
            else if (minVisibleRows > 0 && rowCount > minVisibleRows)
            {
                displayRows = Math.Min(minVisibleRows, maxRowsInAvail);
                needsScroll = true;
            }
            else
            {
                var targetRows = minVisibleRows > 0 ? Math.Max(rowCount, minVisibleRows) : rowCount;
                displayRows = Math.Min(targetRows, maxRowsInAvail);
                needsScroll = rowCount > displayRows;
            }

            tableHeight = headerHeight + displayRows * rowHeight + style.CellPadding.Y;

            var flags = ImGuiTableFlags.RowBg
                        | ImGuiTableFlags.BordersOuter
                        | ImGuiTableFlags.BordersInnerV
                        | ImGuiTableFlags.Resizable;
            if (needsScroll)
            {
                flags |= ImGuiTableFlags.ScrollY;
            }

            return flags;
        }

        private static void SetAllPlugins(bool enabled)
        {
            foreach (var container in PManager.Plugins.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (container.Metadata.AutoStart != enabled)
                {
                    SetPluginAutoStart(container, enabled);
                }
            }
        }

        private static void SetPluginAutoStart(PluginContainer container, bool autoStart)
        {
            if (container.Metadata.AutoStart == autoStart)
            {
                return;
            }

            container.Metadata.AutoStart = autoStart;

            if (autoStart)
            {
                if (!container.Metadata.Enable)
                {
                    container.Metadata.Enable = true;
                    try
                    {
                        container.Plugin.OnEnable(Core.Process.Address != IntPtr.Zero);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Plugin '{container.Name}' konnte nicht gestartet werden: {ex.Message}");
                        container.Metadata.Enable = false;
                        container.Metadata.AutoStart = false;
                    }
                }
            }
            else if (container.Metadata.Enable)
            {
                container.Metadata.Enable = false;
                container.Plugin.SaveSettings();
                container.Plugin.OnDisable();
            }

            PManager.RequestSaveAllSettings();
        }

        /// <summary>
        ///     Draws the currently selected settings on ImGui.
        /// </summary>
        private static void DrawCoreSettings()
        {
            ImGui.PushItemWidth(-1);
            DrawOverlayLanguageAndFontWidget();

            ImGuiTheme.SectionHeader(
                OverlayLocalization.L("Status", "Status"),
                OverlayLocalization.L(
                    $"Settings are saved when you close the menu ({Core.GHSettings.MainMenuHotKey}) and when plugins change.",
                    $"Einstellungen werden beim Schliessen des Menues ({Core.GHSettings.MainMenuHotKey}) und bei Plugin-Aenderungen gespeichert."));

            ImGui.Text(OverlayLocalization.L("Game state", "Spielzustand"));
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.Accent);
            ImGui.Text($"{Core.States.GameCurrentState}");
            ImGui.PopStyleColor();
            InputTextTooltip(
                "##PartyLeaderName",
                ref Core.GHSettings.LeaderName,
                200,
                "Party leader name for party-related features.",
                "Name des Party-Leaders fuer Party-Funktionen.");

            ImGuiTheme.SectionHeader(
                OverlayLocalization.L("Controls & display", "Steuerung & Anzeige"));
            DrawInputConfigWidget();
            DrawNearbyWidget();
            DrawToolsConfig();

            ImGuiTheme.SectionHeader(
                OverlayLocalization.L("Filters & tracking", "Filter & Tracking"),
                OverlayLocalization.L(
                    "Advanced entity filters. Change zone or restart after edits.",
                    "Erweiterte Entity-Filter. Nach Aenderungen Zone wechseln oder neu starten."));
            DrawPoiWidget();
            DrawMonstersToIgnore();
            DrawNPCWidget();
            DrawMiscObjWidget();

            ImGuiTheme.SectionHeader(OverlayLocalization.L("Advanced", "Erweitert"));
            DrawMiscConfig();
            DrawReloadPluginWidget();
            ImGui.PopItemWidth();
        }

        private static void DrawNearbyWidget()
        {
            if (ImGui.CollapsingHeader(
                OverlayLocalization.L("Monster range", "Monster-Reichweite"),
                ImGuiTreeNodeFlags.DefaultOpen))
            {
                DragIntTooltip(
                    "##SmallMonsterRange",
                    ref Core.GHSettings.InnerCircle.Meaning,
                    1f,
                    0,
                    Core.GHSettings.OuterCircle.Meaning,
                    "Small monster range radius. Hover for details.",
                    "Kleine Monster-Reichweite. Fuer Details Maus darueber halten.");
                CheckboxLabeled(
                    OverlayLocalization.L("Visible##smallRange", "Sichtbar##smallRange"),
                    ref Core.GHSettings.InnerCircle.IsVisible,
                    "Show the small monster range circle on the overlay.",
                    "Kleinen Monster-Radius im Overlay anzeigen.");

                DragIntTooltip(
                    "##LargeMonsterRange",
                    ref Core.GHSettings.OuterCircle.Meaning,
                    1f,
                    Core.GHSettings.InnerCircle.Meaning,
                    AreaInstanceConstants.NETWORK_BUBBLE_RADIUS,
                    "Large monster range radius (network bubble limit). Hover for details.",
                    "Grosse Monster-Reichweite (Netzwerk-Grenze). Fuer Details Maus darueber halten.");
                CheckboxLabeled(
                    OverlayLocalization.L("Visible##largeRange", "Sichtbar##largeRange"),
                    ref Core.GHSettings.OuterCircle.IsVisible,
                    "Show the large monster range circle on the overlay.",
                    "Grossen Monster-Radius im Overlay anzeigen.");

                // ImGui.SameLine(0f, 30f);
                // ImGui.Checkbox($"Follow Mouse##{name}", ref value.FollowMouse);
            }
        }

        /// <summary>
        ///     Draws the ImGui widget for changing POI monsters.
        /// </summary>
        private static void DrawPoiWidget()
        {
            var isOpened = ImGui.CollapsingHeader("Special Monster Tracker (A.K.A Monster POI)");
            ImGuiHelper.ToolTip("In order to figure out the path/mod to add " +
                "please open DV -> States -> InGameState -> CurrentAreaInstance -> " +
                "Awake Entities -> click dump button against the entity you want to add. " +
                "This will create a new file in entity_dumps folder with all mod names and " +
                "path of that entity.");
            if (isOpened)
            {
                ImGui.TextWrapped("Please restart gamehelper or change area/zone if you make any changes over here.");
                for (var i = Core.GHSettings.PoiMonstersCategories2.Count - 1; i >= 0; i--)
                {
                    var (filtertype, filter, rarity, stat, group) = Core.GHSettings.PoiMonstersCategories2[i];
                    var isChanged = false;
                    ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
                    if (ImGuiHelper.EnumComboBox($"Filter type     ##{i}MonsterPoiWidget", ref filtertype))
                    {
                        isChanged = true;
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ImGui.GetFontSize() * 27);
                    if (ImGui.InputText($"Filter     ##{i}MonsterPoiWidget", ref filter, 200))
                    {
                        isChanged = true;
                    }

                    ImGuiHelper.ToolTip(filtertype == EntityFilterType.PATH ||
                        filtertype == EntityFilterType.PATHANDRARITY ||
                        filtertype == EntityFilterType.PATHANDSTAT ?
                        "Path is going to be checked from left to right (i.e. String.StartsWith), up till the filter length." :
                        "Mod name is fully checked, it need to be 100% match.");
                    ImGui.SameLine();
                    if (filtertype == EntityFilterType.PATHANDRARITY || filtertype == EntityFilterType.MODANDRARITY)
                    {
                        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 5);
                        if (ImGuiHelper.EnumComboBox($"Rarity     ##{i}MonsterPoiWidget", ref rarity))
                        {
                            isChanged = true;
                        }

                        ImGui.SameLine();
                    }

                    if (filtertype == EntityFilterType.PATHANDSTAT)
                    {
                        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 5);
                        if (ImGuiHelper.NonContinuousEnumComboBox($"Stat        ##{i}MonsterPoiWidget", ref stat))
                        {
                            isChanged = true;
                        }

                        ImGui.SameLine();
                    }

                    ImGui.SetNextItemWidth(ImGui.GetFontSize() * 5);
                    if (ImGui.InputInt($"Group Number##{i}MonsterPoiWidget", ref group))
                    {
                        if (group < 0)
                        {
                            group = 0;
                        }

                        isChanged = true;
                    }

                    if (isChanged)
                    {
                        Core.GHSettings.PoiMonstersCategories2[i] = new(filtertype, filter, rarity, stat, group);
                    }

                    ImGui.SameLine();
                    if (ImGui.Button($"delete##{i}MonsterPoiWidget"))
                    {
                        Core.GHSettings.PoiMonstersCategories2.RemoveAt(i);
                    }
                }

                ImGui.Separator();
                ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
                ImGuiHelper.EnumComboBox($"Filter type     ##addMonsterPoiWidget", ref efilterType);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetFontSize() * 17);
                ImGui.InputText($"Filter     ##addMonsterPoiWidget", ref filterText, 200);
                ImGuiHelper.ToolTip(efilterType == EntityFilterType.PATH ||
                    efilterType == EntityFilterType.PATHANDRARITY ||
                    efilterType == EntityFilterType.PATHANDSTAT ?
                    "Path is going to be checked from left to right (i.e. String.StartsWith), up till the filter length." :
                    "Mod name is fully checked, it need to be 100% match.");
                ImGui.SameLine();
                if (efilterType == EntityFilterType.PATHANDRARITY || efilterType == EntityFilterType.MODANDRARITY)
                {
                    ImGui.SetNextItemWidth(ImGui.GetFontSize() * 5);
                    ImGuiHelper.EnumComboBox($"Rarity     ##addMonsterPoiWidget", ref erarity);
                    ImGui.SameLine();
                }

                if (efilterType == EntityFilterType.PATHANDSTAT)
                {
                    ImGui.SetNextItemWidth(ImGui.GetFontSize() * 5);
                    ImGuiHelper.NonContinuousEnumComboBox($"Stat        ##addMonsterPoiWidget", ref eStats);
                    ImGui.SameLine();
                }

                ImGui.SetNextItemWidth(ImGui.GetFontSize() * 5);
                if (ImGui.InputInt($"Group Number##addMonsterPoiWidget", ref filterGroup) && filterGroup < 0)
                {
                    filterGroup = 0;
                }

                ImGui.SameLine();
                if(ImGui.Button("add##MonsterPoiWidget"))
                {
                    Core.GHSettings.PoiMonstersCategories2.Add(new(efilterType, filterText, erarity, eStats, filterGroup));
                    efilterType = EntityFilterType.PATH;
                    eStats = GameStats.is_capturable_monster;
                    filterText = string.Empty;
                    filterGroup = 0;
                }
            }
        }

        /// <summary>
        ///     Draws the ImGui widget for ignoring monsters.
        /// </summary>
        private static void DrawMonstersToIgnore()
        {
            var isOpened = ImGui.CollapsingHeader("Ignore Monsters");
            ImGuiHelper.ToolTip("In order to figure out the path, please open " +
                "DV -> States -> InGameState -> CurrentAreaInstance -> Awake Entities -> " +
                "Click Path -> see NPC path in the game world");
            if (isOpened)
            {
                ImGui.TextWrapped("Please restart gamehelper or change area/zone if you make any changes over here.");
                ImGui.InputText("Monster metadata path##ToRemove", ref monterPathToIgnore, 200);
                ImGuiHelper.ToolTip("Path is going to be checked from left to right (i.e. String.StartsWith), up till the filter length.");
                ImGui.SameLine();
                if (ImGui.Button("Add##monsterPathToRemove") && !string.IsNullOrEmpty(monterPathToIgnore))
                {
                    Core.GHSettings.MonstersPathsToIgnore.Add(monterPathToIgnore);
                    monterPathToIgnore = string.Empty;
                }

                for (var i = Core.GHSettings.MonstersPathsToIgnore.Count - 1; i >= 0; i--)
                {
                    ImGui.Text($"Path: {Core.GHSettings.MonstersPathsToIgnore[i]}");
                    ImGui.SameLine();
                    if (ImGui.Button($"Delete##{i}monsterPathToRemove"))
                    {
                        Core.GHSettings.MonstersPathsToIgnore.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        ///     Draws the ImGui widget for defining important NPCs.
        /// </summary>
        private static void DrawNPCWidget()
        {
            var isOpened = ImGui.CollapsingHeader("Special NPC Metadata Paths");
            ImGuiHelper.ToolTip("In order to figure out the path, please open " +
                "DV -> States -> InGameState -> CurrentAreaInstance -> Awake Entities -> " +
                "Click Path -> see NPC path in the game world");
            if (isOpened)
            {
                ImGui.TextWrapped("Please restart gamehelper or change area/zone if you make any changes over here.");
                ImGui.InputText("NPC Path##specialNPCPath", ref specialNpcPath, 200);
                ImGuiHelper.ToolTip("Path is going to be checked from left to right (i.e. String.StartsWith), up till the filter length.");
                ImGui.SameLine();
                if (ImGui.Button("Add##specialNPCPath") && !string.IsNullOrEmpty(specialNpcPath))
                {
                    Core.GHSettings.SpecialNPCPaths.Add(specialNpcPath);
                    specialNpcPath = string.Empty;
                }

                for (var i = Core.GHSettings.SpecialNPCPaths.Count - 1; i >= 0; i--)
                {
                    ImGui.Text($"Path: {Core.GHSettings.SpecialNPCPaths[i]}");
                    ImGui.SameLine();
                    if(ImGui.Button($"Delete##{i}specialNPCPath"))
                    {
                        Core.GHSettings.SpecialNPCPaths.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        ///     Draws the ImGui widget for defining important MiscellaneousObjects.
        /// </summary>
        private static void DrawMiscObjWidget()
        {
            var isOpened = ImGui.CollapsingHeader("Special Objects Metadata Paths");
            ImGuiHelper.ToolTip("In order to figure out the path, please open " +
                "DV -> States -> InGameState -> CurrentAreaInstance -> Awake Entities -> " +
                "Click Path -> see objects path in the game world");
            if (isOpened)
            {
                ImGui.TextWrapped("Please restart gamehelper or change area/zone if you make any changes over here.");
                ImGui.InputText("Object Path##MiscObjWidget", ref specialMiscObjPath, 200);
                ImGuiHelper.ToolTip("Path is going to be checked from left to right (i.e. String.StartsWith), up till the filter length.");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetFontSize() * 5);
                if (ImGui.InputInt($"Group Number##MiscObjgroup", ref filterGroup) && filterGroup < 0)
                {
                    filterGroup = 0;
                }

                ImGui.SameLine();
                if (ImGui.Button("add##MiscObjadd"))
                {
                    Core.GHSettings.SpecialMiscObjPaths.Add(new(specialMiscObjPath, filterGroup));
                    specialMiscObjPath = string.Empty;
                    filterGroup = 0;
                }

                for (var i = Core.GHSettings.SpecialMiscObjPaths.Count - 1; i >= 0; i--)
                {
                    ImGui.Text($"Path: {Core.GHSettings.SpecialMiscObjPaths[i].path}, GroupId: {Core.GHSettings.SpecialMiscObjPaths[i].group}");
                    ImGui.SameLine();
                    if (ImGui.Button($"Delete##MiscObjDel{i}"))
                    {
                        Core.GHSettings.SpecialMiscObjPaths.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        ///     Draws the ImGui widget for changing keyboard related settings
        /// </summary>
        private static void DrawInputConfigWidget()
        {
            if (ImGui.CollapsingHeader(
                OverlayLocalization.L("Keys & input", "Tasten & Eingabe"),
                ImGuiTreeNodeFlags.DefaultOpen))
            {
                var fieldWidth = ImGui.GetContentRegionAvail().X;

                ImGui.Text(OverlayLocalization.L("Timeout", "Timeout"));
                ImGui.SetNextItemWidth(fieldWidth);
                DragIntTooltip(
                    "##KeyPressTimeout",
                    ref Core.GHSettings.KeyPressTimeout,
                    0.2f,
                    60,
                    300,
                    "Key timeout (ms). When GameHelper sends a key press, the server needs time (about latency x 3). " +
                    "Set this to latency x 3 (e.g. 90 for 30 ms). Do not go below 60.",
                    "Tasten-Timeout (ms). Wenn GameHelper eine Taste sendet, braucht der Server Zeit (ca. Latenz x 3). " +
                    "Wert auf Latenz x 3 setzen (z. B. 90 bei 30 ms). Nicht unter 60.");

                ImGui.Text(OverlayLocalization.L("Menu", "Menue"));
                ImGui.SetNextItemWidth(fieldWidth);
                ImGuiHelper.NonContinuousEnumComboBox("##MainMenuHotKey", ref Core.GHSettings.MainMenuHotKey);
                ImGuiHelper.ToolTip(OverlayLocalization.L(
                    "Hide/show settings menu — press this key to show or hide GameHelper (default: F11).",
                    "Einstellungsmenue ein/aus — mit dieser Taste GameHelper ein- oder ausblenden (Standard: F11)."));

                ImGui.Text(OverlayLocalization.L("Overlay", "Overlay"));
                ImGui.SetNextItemWidth(fieldWidth);
                ImGuiHelper.NonContinuousEnumComboBox("##DisableRenderingKey", ref Core.GHSettings.DisableAllRenderingKey);
                ImGuiHelper.ToolTip(OverlayLocalization.L(
                    "Toggle overlay rendering — enable or disable all overlay drawing (default: F9).",
                    "Overlay-Darstellung ein/aus — gesamtes Overlay ein- oder ausschalten (Standard: F9)."));

                ImGui.Spacing();
                ImGui.Text(OverlayLocalization.L("Hideout", "Hideout"));
                ImGui.Checkbox("##HideoutAutomationEnabled", ref Core.GHSettings.HideoutAutomationEnabled);
                ImGui.SameLine();
                ImGui.Text(OverlayLocalization.L("Auto /hideout", "Auto /hideout"));
                ImGui.SameLine();
                ImGui.SetNextItemWidth(Math.Max(120f, fieldWidth - ImGui.CalcTextSize("Auto /hideout").X - 40f));
                ImGuiHelper.NonContinuousEnumComboBox("##HideoutAutomationKey", ref Core.GHSettings.HideoutAutomationKey);
                ImGuiHelper.ToolTip(OverlayLocalization.L(
                    "When enabled, the hotkey opens chat and sends /hideout (paste if possible, otherwise types it). " +
                    "Works only while the game window is focused and chat is closed.",
                    "Wenn aktiv, oeffnet die Taste den Chat und sendet /hideout (einfuegen wenn moeglich, sonst tippen). " +
                    "Funktioniert nur bei fokussiertem Spielfenster und geschlossenem Chat."));
            }
        }

        /// <summary>
        ///     Draws the imgui widget for enabling/disabling tools.
        /// </summary>
        private static void DrawToolsConfig()
        {
            if (ImGui.CollapsingHeader(
                OverlayLocalization.L("Developer tools", "Entwickler-Tools"),
                ImGuiTreeNodeFlags.DefaultOpen))
            {
                CheckboxLabeled(
                    OverlayLocalization.L("Performance stats", "Performance-Statistik"),
                    ref Core.GHSettings.ShowPerfStats,
                    "Show FPS and frame timing overlay.",
                    "FPS und Frame-Zeiten anzeigen.");
                if (Core.GHSettings.ShowPerfStats)
                {
                    CheckboxLabeled(
                        OverlayLocalization.L("Hide when game is in background", "Ausblenden wenn Spiel im Hintergrund"),
                        ref Core.GHSettings.HidePerfStatsWhenBg);
                    CheckboxLabeled(
                        OverlayLocalization.L("Show minimum stats", "Nur Mindest-Statistik"),
                        ref Core.GHSettings.MinimumPerfStats);
                }

                CheckboxLabeled(
                    OverlayLocalization.L("Game UI explorer (GE)", "Game-UI-Explorer (GE)"),
                    ref Core.GHSettings.ShowGameUiExplorer);
                CheckboxLabeled(
                    OverlayLocalization.L("Data visualization (DV)", "Daten-Visualisierung (DV)"),
                    ref Core.GHSettings.ShowDataVisualization);
                CheckboxLabeled(
                    OverlayLocalization.L("Performance profiler", "Performance-Profiler"),
                    ref Core.GHSettings.ShowPerfProfiler);
#if DEBUG
                ImGui.Checkbox("Krangled Passive Detector", ref Core.GHSettings.ShowKrangledPassiveDetector);
#endif
            }
        }

        /// <summary>
        ///     Draws the imgui widget for showing misc config
        /// </summary>
        private static void DrawMiscConfig()
        {
            if (ImGui.CollapsingHeader("Miscellaneous Config"))
            {
                if (ImGui.Checkbox("Fix Taskbar not showing", ref Core.GHSettings.FixTaskbarNotShowing))
                {
                    if (Core.States.GameCurrentState != GameStateTypes.GameNotLoaded)
                    {
                        CoroutineHandler.RaiseEvent(GameHelperEvents.OnMoved);
                    }
                }

                ImGui.Checkbox("Disable entity processing when in town or hideout",
                    ref Core.GHSettings.DisableEntityProcessingInTownOrHideout);
                ImGui.Checkbox("Hide overlay settings upon start", ref Core.GHSettings.HideSettingWindowOnStart);
                CheckboxLabeled(
                    OverlayLocalization.L(
                        "Hide overlay when game is in background",
                        "Overlay ausblenden wenn Spiel im Hintergrund"),
                    ref Core.GHSettings.HideOverlayWhenGameInBackground,
                    "Hide the entire GameHelper overlay while Path of Exile is not the active window.",
                    "Blendet das gesamte GameHelper-Overlay aus, solange Path of Exile nicht das aktive Fenster ist.");
                ImGui.Checkbox("Close GameHelper when Game Exit", ref Core.GHSettings.CloseWhenGameExit);
                if (ImGui.Checkbox("V-Sync", ref Core.Overlay.VSync))
                {
                    Core.GHSettings.Vsync = Core.Overlay.VSync;
                }

                ImGui.BeginDisabled(Core.Overlay.VSync);
                if (ImGui.InputInt("FPS Limiter (0 to disable)", ref Core.GHSettings.FPSLimit))
                {
                    Core.Overlay.FPSLimit = Core.GHSettings.FPSLimit;
                }

                ImGui.EndDisabled();

                ImGuiHelper.ToolTip("WARNING: There is no rate limiter in GameHelper, once V-Sync is off,\n" +
                    "it's your responsibility to use external rate limiter e.g. NVIDIA Control Panel\n" +
                    "-> Manage 3D Settings -> Set Max Framerate to what your monitor support.");
                ImGui.Checkbox("Process all renderable entities", ref Core.GHSettings.ProcessAllRenderableEntities);
                ImGuiHelper.ToolTip("WARNING: This will greatly reduce GH speed as well as increase crashes/glitches. Always keep it unchecked.");
                ImGui.Checkbox("Disable debug counters (do it on 6 man party + juiced maps only)", ref Core.GHSettings.DisableAllCounters);
                ImGui.Text("Entity MaxDegreeOfParallelism");
                ImGuiHelper.ToolTip("This limits the entity reading algorithm to a set number of CPUs." +
                    " Select -1 to disable this limit. Use Task Manager CPU usage stat + Misc Tools -> performance stats" +
                    " to figure out best FPS to CPU usage ratio.");
                ImGui.SameLine();
                if (ImGui.RadioButton("-1", Core.GHSettings.EntityReaderMaxDegreeOfParallelism == -1))
                {
                    Core.GHSettings.EntityReaderMaxDegreeOfParallelism = -1;
                }
                ImGui.SameLine();

                for (var i = 2; i < 128; i*=2)
                {
                    if (ImGui.RadioButton(i.ToString(), Core.GHSettings.EntityReaderMaxDegreeOfParallelism == i))
                    {
                        Core.GHSettings.EntityReaderMaxDegreeOfParallelism = i;
                    }

                    if (i*2 < 128)
                    {
                        ImGui.SameLine();
                    }
                }

                ImGui.Checkbox("Is Taiwan client", ref Core.GHSettings.IsTaiwanClient);

                ImGui.Separator();
                ImGui.Text("Entity Staleness Fixes");
                ImGuiHelper.ToolTip("These options help detect and fix stale entity data " +
                    "(e.g. NPCs that teleport but keep old position in memory).");

                ImGui.Checkbox("Enable NPC entity cleanup", ref Core.GHSettings.EnableNpcEntityCleanup);
                ImGuiHelper.ToolTip("Include NPC entities in the removal logic when they go invalid.\n" +
                    "Prevents stale NPC entities from lingering in the entity dictionary.");

                ImGui.Checkbox("Enable stale entity cleanup", ref Core.GHSettings.EnableStaleEntityCleanup);
                ImGuiHelper.ToolTip("Remove any entity that stays invalid for many consecutive frames,\n" +
                    "regardless of entity type. Catches NPCs and other entities that\n" +
                    "the default cleanup misses.");

                if (Core.GHSettings.EnableStaleEntityCleanup)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(80);
                    ImGui.InputInt("threshold (frames)", ref Core.GHSettings.StaleEntityFrameThreshold);
                    if (Core.GHSettings.StaleEntityFrameThreshold < 10)
                        Core.GHSettings.StaleEntityFrameThreshold = 10;
                }
            }
        }

        /// <summary>
        ///     Draws the imgui widget for reloading plugins
        /// </summary>
        private static void DrawReloadPluginWidget()
        {
#if DEBUG
            if (ImGui.CollapsingHeader("Reload Plugin"))
            {
                ImGuiHelper.IEnumerableComboBox<string>("Plugins", PManager.PluginNames, ref pluginForHotReload);
                ImGui.BeginDisabled(!pluginLoaded || string.IsNullOrEmpty(pluginForHotReload));
                if (ImGui.Button("Unload Plugin"))
                {
                    if (PManager.UnloadPlugin(pluginForHotReload))
                    {
                        pluginLoaded = false;
                    }
                }

                ImGui.EndDisabled();
                ImGui.SameLine();
                ImGui.BeginDisabled(pluginLoaded || string.IsNullOrEmpty(pluginForHotReload));
                if (ImGui.Button("Load Plugin"))
                {
                    if (PManager.LoadPlugin(pluginForHotReload))
                    {
                        pluginLoaded = true;
                    }
                }

                ImGui.EndDisabled();
            }
#endif
        }

        /// <summary>
        ///     Draws the closing confirmation popup on ImGui.
        /// </summary>
        private static void DrawConfirmationPopup()
        {
            ImGui.SetNextWindowPos(new Vector2(Core.Overlay.Size.Width / 3f, Core.Overlay.Size.Height / 3f));
            if (ImGui.BeginPopup("GameHelperCloseConfirmation"))
            {
                ImGui.Text("Do you want to quit the GameHelper overlay?");
                ImGui.Separator();
                if (ImGui.Button("Yes", new Vector2(ImGui.GetContentRegionAvail().X / 2f, ImGui.GetTextLineHeight() * 2)))
                {
                    Core.GHSettings.IsOverlayRunning = false;
                    ImGui.CloseCurrentPopup();
                    isOverlayRunningLocal = true;
                }

                ImGui.SameLine();
                if (ImGui.Button("No", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 2)))
                {
                    ImGui.CloseCurrentPopup();
                    isOverlayRunningLocal = true;
                }

                ImGui.EndPopup();
            }
        }

        /// <summary>
        ///     Hides the overlay on startup.
        /// </summary>
        private static void HideOnStartCheck()
        {
            if (Core.GHSettings.HideSettingWindowOnStart)
            {
                isSettingsWindowVisible = false;
                Core.IsSettingsMenuOpen = false;
            }
        }

        /// <summary>
        ///     Draws the Settings Window.
        /// </summary>
        /// <returns>co-routine IWait.</returns>
        private static IEnumerator<Wait> RenderCoroutine()
        {
            while (true)
            {
                yield return new Wait(GameHelperEvents.OnRender);
                if (Utils.IsKeyPressedAndNotTimeout(Core.GHSettings.MainMenuHotKey))
                {
                    isSettingsWindowVisible = !isSettingsWindowVisible;
                    Core.IsSettingsMenuOpen = isSettingsWindowVisible;
                    ImGui.GetIO().WantCaptureMouse = true;
                    if (!isSettingsWindowVisible)
                    {
                        CoroutineHandler.RaiseEvent(GameHelperEvents.TimeToSaveAllSettings);
                    }
                }

                if (!isSettingsWindowVisible)
                {
                    Core.IsSettingsMenuOpen = false;
                    continue;
                }

                Core.IsSettingsMenuOpen = true;

                ImGui.SetNextWindowSizeConstraints(new Vector2(860, 620), Vector2.One * float.MaxValue);
                var isMainMenuExpanded = ImGui.Begin(
                    $"{OverlayLocalization.L("GameHelper Settings", "GameHelper Einstellungen")}  |  {Core.GetVersion()}###GameHelperMainSettings",
                    ref isOverlayRunningLocal,
                    ImGuiWindowFlags.MenuBar);

                if (!isOverlayRunningLocal)
                {
                    ImGui.OpenPopup("GameHelperCloseConfirmation");
                }

                DrawConfirmationPopup();
                if (!Core.GHSettings.IsOverlayRunning)
                {
                    CoroutineHandler.RaiseEvent(GameHelperEvents.TimeToSaveAllSettings);
                }

                if (!isMainMenuExpanded)
                {
                    ImGui.End();
                    continue;
                }

                DrawManuBar();
                DrawOptionalPluginUpdateNotice();
                DrawHubToolbar();
                DrawMainHubContent();
                mainWindowPos = ImGui.GetWindowPos();
                mainWindowSize = ImGui.GetWindowSize();
                ImGui.End();

            }
        }

        private static void DragIntTooltip(string id, ref int value, float speed, int min, int max, string english, string german)
        {
            ImGui.DragInt(id, ref value, speed, min, max);
            ImGuiHelper.ToolTip(OverlayLocalization.L(english, german));
        }

        private static void CheckboxLabeled(string label, ref bool value, string? english = null, string? german = null)
        {
            ImGui.Checkbox(label, ref value);
            if (english != null && german != null)
            {
                ImGuiHelper.ToolTip(OverlayLocalization.L(english, german));
            }
        }

        private static void InputTextTooltip(string id, ref string value, uint maxLength, string english, string german)
        {
            ImGui.InputText(id, ref value, maxLength);
            ImGuiHelper.ToolTip(OverlayLocalization.L(english, german));
        }

        /// <summary>
        ///     Saves the GameHelper settings to disk.
        /// </summary>
        /// <returns>co-routine IWait.</returns>
        private static IEnumerator<Wait> SaveCoroutine()
        {
            while (true)
            {
                yield return new Wait(GameHelperEvents.TimeToSaveAllSettings);
                JsonHelper.SafeToFile(Core.GHSettings, State.CoreSettingFile);
            }
        }
    }
}
