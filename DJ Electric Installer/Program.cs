using System.Diagnostics;

namespace DJ_Electric_Invoice_Generator_Installer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var options = InstallerOptions.Parse(args);
        switch (options.Mode)
        {
            case InstallerMode.ApplyUpdate:
                RunUpdate(options);
                return;

            case InstallerMode.Uninstall:
                RunUninstall(options);
                return;

            default:
                Application.Run(new InstallerForm(options));
                return;
        }
    }

    private static void RunUpdate(InstallerOptions options)
    {
        try
        {
            var result = InstallService.ApplyUpdate(options.PackagePath, options.InstallDirectory);

            if (options.RestartAfterUpdate)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = result.MainExecutablePath,
                    WorkingDirectory = Path.GetDirectoryName(result.MainExecutablePath),
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Update Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void RunUninstall(InstallerOptions options)
    {
        var choice = MessageBox.Show(
            $"Remove {InstallService.ProductName} from this computer?",
            "Uninstall",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (choice != DialogResult.Yes)
        {
            return;
        }

        try
        {
            InstallService.Uninstall(options.InstallDirectory);
            MessageBox.Show(
                $"{InstallService.ProductName} is being removed.",
                "Uninstall",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Uninstall Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
