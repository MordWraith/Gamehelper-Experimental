namespace Downloader
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Windows.Forms;

    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            var force = false;
            var preferFullPackage = false;
            string? targetDir = null;
            string? installPlugin = null;
            string? removePlugin = null;

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg is "--force" or "-f")
                {
                    force = true;
                    continue;
                }

                if (arg is "--full")
                {
                    preferFullPackage = true;
                    continue;
                }

                if (arg is "--help" or "-h" or "/?")
                {
                    PrintHelp();
                    return 0;
                }

                if (arg is "--install-plugin")
                {
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine(DownloaderLocalization.B(
                            "Missing value for --install-plugin",
                            "Fehlender Wert fuer --install-plugin"));
                        return 1;
                    }

                    installPlugin = args[++i];
                    continue;
                }

                if (arg is "--remove-plugin")
                {
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine(DownloaderLocalization.B(
                            "Missing value for --remove-plugin",
                            "Fehlender Wert fuer --remove-plugin"));
                        return 1;
                    }

                    removePlugin = args[++i];
                    continue;
                }

                if (arg is "--target" or "-t")
                {
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine(DownloaderLocalization.B(
                            "Missing value for --target",
                            "Fehlender Wert fuer --target"));
                        return 1;
                    }

                    targetDir = args[++i];
                    continue;
                }

                if (!arg.StartsWith('-'))
                {
                    targetDir = arg;
                }
            }

            if (!string.IsNullOrWhiteSpace(installPlugin) && !string.IsNullOrWhiteSpace(removePlugin))
            {
                Console.Error.WriteLine(DownloaderLocalization.B(
                    "Use either --install-plugin or --remove-plugin, not both.",
                    "Entweder --install-plugin oder --remove-plugin verwenden, nicht beides."));
                return 1;
            }

            var pluginAction = !string.IsNullOrWhiteSpace(installPlugin) || !string.IsNullOrWhiteSpace(removePlugin);
            if (pluginAction && preferFullPackage)
            {
                Console.Error.WriteLine(DownloaderLocalization.B(
                    "--full cannot be combined with plugin install/remove.",
                    "--full kann nicht mit Plugin-Install/Remove kombiniert werden."));
                return 1;
            }

            if (string.IsNullOrWhiteSpace(targetDir))
            {
                targetDir = pluginAction
                    ? ResolveExistingInstallDirectory()
                    : PromptForTargetDirectory() ?? Path.Combine(AppContext.BaseDirectory, "GameHelper");
            }

            if (pluginAction && string.IsNullOrWhiteSpace(targetDir))
            {
                Console.Error.WriteLine(DownloaderLocalization.B(
                    "Specify the GameHelper folder with --target or run from the install directory.",
                    "GameHelper-Ordner mit --target angeben oder im Installationsordner starten."));
                return 1;
            }

            Console.WriteLine(pluginAction
                ? "=== GameHelper Plugin Manager ==="
                : "=== GameHelper Download ===");
            Console.WriteLine($"{DownloaderLocalization.B("Target folder", "Zielordner")}: {Path.GetFullPath(targetDir!)}");
            Console.WriteLine();

            var progress = new Progress<string>(line => Console.WriteLine(line));
            DownloadResult result;

            try
            {
                if (!string.IsNullOrWhiteSpace(installPlugin))
                {
                    result = new PluginPackageService()
                        .InstallPluginAsync(targetDir!, installPlugin, progress, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }
                else if (!string.IsNullOrWhiteSpace(removePlugin))
                {
                    result = new PluginPackageService()
                        .RemovePluginAsync(targetDir!, removePlugin, progress, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }
                else
                {
                    result = new GameHelperDownloadService()
                        .DownloadAsync(targetDir!, force, progress, CancellationToken.None, preferFullPackage)
                        .GetAwaiter()
                        .GetResult();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DownloaderLocalization.B("Error", "Fehler")}: {ex.Message}");
                Console.ResetColor();
                WaitForKey();
                return 1;
            }

            Console.WriteLine();
            if (result.ExitCode != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(result.Message);
                Console.ResetColor();
                WaitForKey();
                return result.ExitCode;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            if (pluginAction)
            {
                Console.WriteLine(result.Message);
            }
            else
            {
                Console.WriteLine(DownloaderLocalization.B(
                    "Done. GameHelper is installed in:",
                    "Fertig. GameHelper liegt in:"));
                Console.WriteLine($"  {result.TargetDir}");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine(DownloaderLocalization.B("Start with:", "Starten mit:"));
                Console.WriteLine($"  {Path.Combine(result.TargetDir!, "GameHelper.exe")}");
            }

            Console.ResetColor();
            WaitForKey();
            return 0;
        }

        private static string? ResolveExistingInstallDirectory()
        {
            foreach (var candidate in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                if (File.Exists(Path.Combine(candidate, "GameHelper.exe")))
                {
                    return Path.GetFullPath(candidate);
                }
            }

            return null;
        }

        private static string? PromptForTargetDirectory()
        {
            try
            {
                Application.EnableVisualStyles();
                using var dialog = new FolderBrowserDialog
                {
                    Description = DownloaderLocalization.B(
                        "Choose target folder for GameHelper",
                        "Zielordner fuer GameHelper waehlen"),
                    UseDescriptionForTitle = true,
                    SelectedPath = AppContext.BaseDirectory,
                };

                return dialog.ShowDialog() == DialogResult.OK
                    ? dialog.SelectedPath
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("GameHelperDownloader");
            Console.WriteLine();
            Console.WriteLine(DownloaderLocalization.B("Usage:", "Verwendung:"));
            Console.WriteLine("  GameHelperDownloader.exe [target folder] [--force] [--full]");
            Console.WriteLine("  GameHelperDownloader.exe --target \"D:\\Games\\GameHelper\" --install-plugin Atlas");
            Console.WriteLine("  GameHelperDownloader.exe --target \"D:\\Games\\GameHelper\" --remove-plugin Atlas");
            Console.WriteLine();
            Console.WriteLine(DownloaderLocalization.B(
                "Without target folder: folder picker dialog, otherwise .\\GameHelper",
                "Ohne Zielordner: Ordnerauswahl-Dialog, sonst .\\GameHelper"));
            Console.WriteLine(DownloaderLocalization.B(
                "Plugin commands use an existing install folder (or current directory if GameHelper.exe is there).",
                "Plugin-Befehle nutzen einen bestehenden Installationsordner (oder aktuelles Verzeichnis mit GameHelper.exe)."));
            Console.WriteLine(DownloaderLocalization.B(
                "--full downloads the complete package with all plugins (default is core only).",
                "--full laedt das Vollstaendige Paket mit allen Plugins (Standard ist nur Core)."));
        }

        private static void WaitForKey()
        {
            if (!Environment.UserInteractive)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine(DownloaderLocalization.B(
                "Press Enter to exit ...",
                "Enter zum Beenden ..."));
            Console.ReadLine();
        }
    }
}
