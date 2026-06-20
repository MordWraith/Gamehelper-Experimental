namespace Shared.PluginPackages
{
    using System;
    /// <summary>
    ///     Optional plugin row for catalog UI.
    /// </summary>
    public sealed class PluginCatalogEntry
    {
        public required string Id { get; init; }

        public required string Folder { get; init; }

        public required string CatalogVersion { get; init; }

        public string PluginVersion { get; init; } = string.Empty;

        public string Author { get; init; } = string.Empty;

        public string DescriptionEn { get; init; } = string.Empty;

        public string DescriptionDe { get; init; } = string.Empty;

        /// <summary>Original upstream / author repository.</summary>
        public string UpstreamUrl { get; init; } = string.Empty;

        /// <summary>MordWraith Experimental release package (adapted fork build).</summary>
        public string SourceUrl { get; init; } = string.Empty;

        public string PackageName { get; init; } = string.Empty;

        public required string PackageHash { get; init; }

        public string? InstalledVersion { get; init; }

        public string? InstalledPackageHash { get; init; }

        public bool IsInstalled { get; init; }

        public bool NeedsUpdate =>
            IsInstalled &&
            !string.IsNullOrWhiteSpace(PackageHash) &&
            !string.IsNullOrWhiteSpace(InstalledPackageHash) &&
            !string.Equals(InstalledPackageHash, PackageHash, StringComparison.OrdinalIgnoreCase);

        public bool IsLocalOrUnknownInstall =>
            IsInstalled && string.IsNullOrWhiteSpace(InstalledPackageHash);

        public bool IsPendingRemoval { get; init; }

        public bool IsPendingUpdate { get; init; }
    }
}
