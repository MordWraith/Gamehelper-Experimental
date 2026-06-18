namespace GameHelper.Plugin

{

    using System;

    using System.Collections.Generic;



    /// <summary>Upstream authors and fork maintenance credits for bundled plugins.</summary>

    internal static class PluginCredits

    {

        private const string UnknownAuthor = "upstream (unknown)";

        private const string ForkMaintainer = "MordWraith";

        private const string ForkBasis = "Lafko / Gordin";



        private static readonly Dictionary<string, PluginCreditInfo> Credits = new(StringComparer.OrdinalIgnoreCase)

        {

            ["Atlas"] = new("Nekkoy", "Gordin / yokkenUA", "Endgame atlas overlay (Gordin upstream): search, pathing, content badges."),

            ["Radar"] = new("Gordin", "GameHelper2", "Radar overlay for entities and map awareness."),

            ["RitualHelper"] = new("caio", "AutoRitualPricer lineage", "Ritual reward prices in the Ritual panel."),

            ["RuneforgeHelper"] = new("Nekkoy", "GameHelper2 plugin ecosystem", "Runeshape prices (fork: poe2scout, DE/EN). Use only RuneforgeHelper OR RunecraftHelper."),

            ["RunecraftHelper"] = new("Nekkoy", "yokkenUA upstream 1:1", "Runeshape prices (upstream poe.ninja). Use only RunecraftHelper OR RuneforgeHelper."),

            ["SekhemaHelper"] = new("Nekkoy", "Sekhema Trial path helper", "Sekhema trial map pathing assistance."),

            ["AutoPot"] = new("MordWraith", "written for this fork", "Automatic flask usage helper."),

            ["Autopot"] = new("MordWraith", "written for this fork", "Automatic flask usage helper."),

            ["AutoHotKeyTrigger"] = new("GameHelper2 upstream", "KronosDesign / community", "Configurable hotkey triggers for in-game actions."),

            ["HealthBars"] = new("GameHelper2 upstream", "KronosDesign / community", "Custom health bars on screen."),

            ["SimpleBars"] = new("Reynbow", "Reynbow/simplebars fork", "Lightweight on-screen health bars."),

            ["PreloadAlert"] = new("GameHelper2 upstream", "ExileAPI PreloadAlert concept", "Alerts for preloaded map areas."),

            ["AuraTracker"] = new("Skrip", "derekShaheen/AuraTracker", "Tracks player auras and reservation."),

            ["MapKillCounter"] = new("MordWraith", "written for this fork", "Kill counter for the current map."),

            ["FarmTracker"] = new("Senbry", "ported by MordWraith for this fork", "Farm tracker: loot profit, kills, maps, session stats."),

            ["AmanamuVoidAlert"] = new("1k4ru5g3", "POEFixer AmanamuVoidAlertPlugin port", "Alerts for Amanamu void mechanics."),

            ["PlayerBuffBar"] = new("MordWraith", "written for this fork", "Compact player buff display."),

            ["Wraedar"] = new("Wraedar upstream", "Wraedar map tools", "Map pins, tiles, and navigation helpers."),

        };



        internal static string ForkByLine => $"Fork maintained by {ForkMaintainer} (basis: {ForkBasis})";



        internal static string GetOriginalAuthor(string pluginName) =>

            Credits.TryGetValue(pluginName, out var credit) ? credit.Author : UnknownAuthor;



        internal static string GetUpstreamNote(string pluginName) =>

            Credits.TryGetValue(pluginName, out var credit) ? credit.Notes : string.Empty;



        internal static string GetCatalogDescription(string pluginName)

        {

            if (!Credits.TryGetValue(pluginName, out var credit))

            {

                return string.Empty;

            }

            if (!string.IsNullOrWhiteSpace(credit.CatalogDescription))

            {

                return credit.CatalogDescription;

            }

            return credit.Notes;

        }



        internal static string GetUpstreamSourceUrl(string pluginName) =>

            UpstreamSourceUrls.TryGetValue(pluginName, out var url)

                ? url

                : string.Empty;



        private static readonly Dictionary<string, string> UpstreamSourceUrls = new(StringComparer.OrdinalIgnoreCase)

        {

            ["Atlas"] = "https://github.com/Gordin/GameHelper2/tree/main/Plugins/Atlas",

            ["AutoHotKeyTrigger"] = "https://github.com/Gordin/GameHelper2",

            ["AuraTracker"] = "https://github.com/derekShaheen/AuraTracker",

            ["AmanamuVoidAlert"] = "https://github.com/1k4ru5g3/AmanamuVoidAlertPlugin",

            ["RitualHelper"] = "https://github.com/Queuete/GameHelper",

            ["RuneforgeHelper"] = "https://github.com/yokkenUA/RunecraftHelper",

            ["RunecraftHelper"] = "https://github.com/yokkenUA/RunecraftHelper",

            ["SekhemaHelper"] = "https://github.com/yokkenUA/SekhemaHelper",

            ["SimpleBars"] = "https://github.com/Reynbow/simplebars",

            ["Wraedar"] = "https://github.com/diesal/Wraedar",

        };



        private readonly record struct PluginCreditInfo(string Author, string Notes, string CatalogDescription = "");

    }

}

