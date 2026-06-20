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

            ["Atlas"] = new("Nekkoy", "yokkenUA/Atlas v0.1.4", "Endgame atlas overlay: chevron routes, hide available maps, expedition targets, Grand Mirror badge, content icons."),

            ["Radar"] = new("Gordin", "GameHelper2", "Radar overlay for entities and map awareness."),

            ["RitualHelper"] = new("caio", "AutoRitualPricer lineage", "Ritual reward prices in the Ritual panel."),

            ["RuneforgeHelper"] = new("Nekkoy", "GameHelper2 plugin ecosystem", "Runeshape prices (fork: poe2scout, DE/EN). Use only RuneforgeHelper OR RunecraftHelper."),

            ["RunecraftHelper"] = new("Nekkoy", "yokkenUA upstream 1:1", "Runeshape prices (upstream poe.ninja). Use only RunecraftHelper OR RuneforgeHelper."),

            ["SekhemaHelper"] = new("Nekkoy", "Sekhema Trial path helper", "Sekhema trial map pathing assistance."),

            ["AutoPot"] = new("MordWraith", "written for this fork", "Automatic flask usage helper."),

            ["Autopot"] = new("MordWraith", "written for this fork", "Automatic flask usage helper."),

            ["AutoHotKeyTrigger"] = new("GameHelper2 upstream", "KronosDesign / community", "Configurable hotkey triggers for in-game actions."),

            ["HealthBars"] = new("GameHelper2 upstream", "KronosDesign / community", "Custom health bars on screen."),

            ["SimpleBars"] = new("Reynbow", "MordWraith/SimpleBars port", "Lightweight on-screen health bars."),

            ["PreloadAlert"] = new("GameHelper2 upstream", "ExileAPI PreloadAlert concept", "Alerts for preloaded map areas."),

            ["AuraTracker"] = new("Skrip", "MordWraith/AuraTracker port", "Nearby enemies: HP/ES, buffs, DPS panel."),

            ["MapKillCounter"] = new("MordWraith", "MordWraith/MapKillCounter", "Kill counter for the current map and session."),

            ["FarmTracker"] = new("Senbry", "ported by MordWraith for this fork", "Farm tracker: loot profit, kills, maps, session stats."),

            ["AmanamuVoidAlert"] = new("1k4ru5g3", "MordWraith/AmanamuVoidAlert port", "Abyss / Amanamu void cloud tracker."),

            ["PlayerBuffBar"] = new("MordWraith", "written for this fork", "Compact player buff display."),

            ["Hiveblood"] = new("MordWraith", "written for this fork", "Genesis Tree Hiveblood tracker with inventory overlay (PoE2)."),

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

            ["Atlas"] = "https://github.com/yokkenUA/Atlas",

            ["AutoHotKeyTrigger"] = "https://github.com/Gordin/GameHelper2",

            ["AuraTracker"] = "https://github.com/MordWraith/AuraTracker",

            ["AmanamuVoidAlert"] = "https://github.com/MordWraith/AmanamuVoidAlert",

            ["MapKillCounter"] = "https://github.com/MordWraith/MapKillCounter",

            ["FarmTracker"] = "https://github.com/MordWraith/FarmTracker",

            ["RitualHelper"] = "https://github.com/Queuete/GameHelper",

            ["RuneforgeHelper"] = "https://github.com/yokkenUA/RunecraftHelper",

            ["RunecraftHelper"] = "https://github.com/yokkenUA/RunecraftHelper",

            ["SekhemaHelper"] = "https://github.com/yokkenUA/SekhemaHelper",

            ["SimpleBars"] = "https://github.com/MordWraith/SimpleBars",

            ["Wraedar"] = "https://github.com/diesal/Wraedar",

        };



        private readonly record struct PluginCreditInfo(string Author, string Notes, string CatalogDescription = "");

    }

}

