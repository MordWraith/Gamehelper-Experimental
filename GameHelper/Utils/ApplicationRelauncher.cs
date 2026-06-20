namespace GameHelper.Utils
{
    using System;
    using System.Diagnostics;
    using System.IO;

    internal static class ApplicationRelauncher
    {
        internal static bool TryRestart(out string error)
        {
            error = string.Empty;
            var installDir = AppContext.BaseDirectory;
            var exePath = ResolveExecutablePath(installDir);
            if (string.IsNullOrWhiteSpace(exePath))
            {
                error = "GameHelper executable not found.";
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = installDir,
                    UseShellExecute = true,
                });
                Core.GHSettings.IsOverlayRunning = false;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string? ResolveExecutablePath(string installDir)
        {
            foreach (var name in new[] { "GameHelper.exe", "GameHelper.App.exe" })
            {
                var path = Path.Combine(installDir, name);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return Environment.ProcessPath;
        }
    }
}
