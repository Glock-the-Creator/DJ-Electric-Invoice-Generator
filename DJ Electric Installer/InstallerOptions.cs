namespace DJ_Electric_Invoice_Generator_Installer;

internal enum InstallerMode
{
    Install,
    ApplyUpdate,
    Uninstall
}

internal sealed class InstallerOptions
{
    public InstallerMode Mode { get; init; } = InstallerMode.Install;

    public string PackagePath { get; init; } = InstallService.DefaultPackagePath;

    public string InstallDirectory { get; init; } = InstallService.DefaultInstallDirectory;

    public bool RestartAfterUpdate { get; init; }

    public static InstallerOptions Parse(string[] args)
    {
        var mode = InstallerMode.Install;
        var packagePath = InstallService.DefaultPackagePath;
        var installDirectory = InstallService.DefaultInstallDirectory;
        var restartAfterUpdate = false;

        for (var i = 0; i < args.Length; i++)
        {
            var argument = args[i];
            switch (argument)
            {
                case "--apply-update":
                    mode = InstallerMode.ApplyUpdate;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        packagePath = args[++i];
                    }
                    break;

                case "--package":
                    if (i + 1 < args.Length)
                    {
                        packagePath = args[++i];
                    }
                    break;

                case "--install-dir":
                    if (i + 1 < args.Length)
                    {
                        installDirectory = NormalizeInstallDirectory(args[++i], ref restartAfterUpdate);
                    }
                    break;

                case "--restart":
                    restartAfterUpdate = true;
                    break;

                case "--uninstall":
                    mode = InstallerMode.Uninstall;
                    break;
            }
        }

        return new InstallerOptions
        {
            Mode = mode,
            PackagePath = packagePath,
            InstallDirectory = installDirectory,
            RestartAfterUpdate = restartAfterUpdate
        };
    }

    private static string NormalizeInstallDirectory(string rawValue, ref bool restartAfterUpdate)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return InstallService.DefaultInstallDirectory;
        }

        var trimmed = rawValue.Trim();
        const string appendedRestart = "\" --restart";

        if (trimmed.EndsWith(appendedRestart, StringComparison.OrdinalIgnoreCase))
        {
            restartAfterUpdate = true;
            trimmed = trimmed[..^appendedRestart.Length];
        }

        trimmed = trimmed.Trim().Trim('"');

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return InstallService.DefaultInstallDirectory;
        }

        return Path.TrimEndingDirectorySeparator(trimmed);
    }
}
