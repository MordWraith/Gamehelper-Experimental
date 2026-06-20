namespace Shared.PluginPackages
{
    /// <summary>
    ///     Result of an optional plugin install/remove operation.
    /// </summary>
    public sealed class PluginPackageResult
    {
        public int ExitCode { get; init; }

        public string Message { get; init; } = string.Empty;

        public bool Success => ExitCode == 0;

        public static PluginPackageResult Ok(string message) =>
            new() { ExitCode = 0, Message = message };

        public static PluginPackageResult Fail(int exitCode, string message) =>
            new() { ExitCode = exitCode, Message = message };
    }
}
