namespace GameHelper.PluginStore
{
    using System;
    using GameHelper.Localization;
    using GameHelper.Plugin;
    using Shared.PluginPackages;
    using Shared.UpdateSecurity;

    internal static class PluginCatalogUi
    {
        private const string ForkSourceRepository = "MordWraith/Gamehelper";
        private const string ForkSourceBranch = "main";

        internal static string Author(PluginCatalogEntry entry) =>
            !string.IsNullOrWhiteSpace(entry.Author)
                ? entry.Author
                : PluginCredits.GetOriginalAuthor(entry.Id);

        internal static string Description(PluginCatalogEntry entry)
        {
            if (OverlayLocalization.IsGerman && !string.IsNullOrWhiteSpace(entry.DescriptionDe))
            {
                return entry.DescriptionDe;
            }

            if (!string.IsNullOrWhiteSpace(entry.DescriptionEn))
            {
                return entry.DescriptionEn;
            }

            return PluginCredits.GetCatalogDescription(entry.Id);
        }

        internal static string DisplayVersion(PluginCatalogEntry entry) =>
            !string.IsNullOrWhiteSpace(entry.PluginVersion)
                ? entry.PluginVersion
                : entry.CatalogVersion;

        internal static string UpstreamUrl(PluginCatalogEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.UpstreamUrl))
            {
                return entry.UpstreamUrl.Trim();
            }

            return PluginCredits.GetUpstreamSourceUrl(entry.Id);
        }

        internal static string ForkBrowseUrl(PluginCatalogEntry entry)
        {
            var fromCatalog = entry.SourceUrl?.Trim();
            if (!string.IsNullOrWhiteSpace(fromCatalog) && IsForkBrowseUrl(fromCatalog))
            {
                return fromCatalog;
            }

            return BuildForkSourceTreeUrl(entry.Id);
        }

        internal static string UpdateButtonLabel(PluginCatalogEntry entry)
        {
            var version = DisplayVersion(entry);
            return OverlayLocalization.L(
                $"Update ({version})",
                $"Aktualisieren ({version})");
        }

        private static string BuildForkSourceTreeUrl(string id) =>
            $"{UpdateRepositoryConfig.GitHubHost}/{ForkSourceRepository}/tree/{ForkSourceBranch}/Plugins/{id}";

        private static bool IsForkBrowseUrl(string url) =>
            (url.Contains("/tree/", StringComparison.OrdinalIgnoreCase) ||
             url.Contains("/releases/tag/", StringComparison.OrdinalIgnoreCase)) &&
            !url.Contains("/releases/download/", StringComparison.OrdinalIgnoreCase);
    }
}
