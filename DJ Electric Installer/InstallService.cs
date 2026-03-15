using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Win32;

namespace DJ_Electric_Invoice_Generator_Installer;

internal static class InstallService
{
    private const string UninstallRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\DJ Electric Invoice Generator";

    public const string ProductName = "DJ Electric Invoice Generator";
    public const string MainExecutableName = "DJ Electric Invoice Generator.exe";
    public const string SetupExecutableName = "DJ Electric Invoice Generator Setup.exe";
    public const string PackageFileName = "app-package.zip";

    public static string DefaultInstallDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            ProductName);

    public static string DefaultPackagePath => Path.Combine(AppContext.BaseDirectory, PackageFileName);

    public static InstallResult Install(string packagePath, string installDirectory, bool createDesktopShortcut)
    {
        return InstallInternal(packagePath, installDirectory, createDesktopShortcut);
    }

    public static InstallResult ApplyUpdate(string packagePath, string installDirectory)
    {
        var desktopShortcutExists = File.Exists(GetDesktopShortcutPath());
        return InstallInternal(packagePath, installDirectory, desktopShortcutExists);
    }

    public static void Uninstall(string installDirectory)
    {
        var resolvedInstallDirectory = string.IsNullOrWhiteSpace(installDirectory)
            ? DefaultInstallDirectory
            : installDirectory;

        RemoveShortcuts();
        DeleteUninstallEntry();
        StartDeferredDelete(resolvedInstallDirectory);
    }

    private static InstallResult InstallInternal(string packagePath, string installDirectory, bool createDesktopShortcut)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException($"The installer could not find {PackageFileName}.", packagePath);
        }

        var resolvedInstallDirectory = string.IsNullOrWhiteSpace(installDirectory)
            ? DefaultInstallDirectory
            : installDirectory;

        var extractionDirectory = Path.Combine(Path.GetTempPath(), $"dj-electric-invoice-{Guid.NewGuid():N}");
        Directory.CreateDirectory(extractionDirectory);

        try
        {
            ZipFile.ExtractToDirectory(packagePath, extractionDirectory, overwriteFiles: true);

            Directory.CreateDirectory(resolvedInstallDirectory);
            CopyDirectory(extractionDirectory, resolvedInstallDirectory);
            CopySetupRuntime(resolvedInstallDirectory);

            var mainExecutablePath = Path.Combine(resolvedInstallDirectory, MainExecutableName);
            CreateStartMenuShortcut(mainExecutablePath);
            CreateStartMenuUninstallShortcut(resolvedInstallDirectory);

            if (createDesktopShortcut)
            {
                CreateShortcut(GetDesktopShortcutPath(), mainExecutablePath, string.Empty);
            }
            else
            {
                RemoveDesktopShortcut();
            }

            WriteUninstallEntry(resolvedInstallDirectory, mainExecutablePath);

            return new InstallResult(resolvedInstallDirectory, mainExecutablePath);
        }
        finally
        {
            TryDeleteDirectory(extractionDirectory);
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativeDirectory = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relativeDirectory));
        }

        foreach (var filePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativeFile = Path.GetRelativePath(sourceDirectory, filePath);
            var destinationPath = Path.Combine(destinationDirectory, relativeFile);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            CopyFileWithRetries(filePath, destinationPath);
        }
    }

    private static void CopyFileWithRetries(string sourcePath, string destinationPath)
    {
        const int maxAttempts = 20;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                File.Copy(sourcePath, destinationPath, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(250);
            }
        }

        File.Copy(sourcePath, destinationPath, overwrite: true);
    }

    private static void CopySetupRuntime(string installDirectory)
    {
        var currentExecutablePath = Environment.ProcessPath ?? throw new InvalidOperationException("Installer executable path is unavailable.");
        var sourceDirectory = Path.GetDirectoryName(currentExecutablePath)
            ?? throw new InvalidOperationException("Installer directory is unavailable.");
        var setupBaseName = Path.GetFileNameWithoutExtension(currentExecutablePath);

        foreach (var sourcePath in Directory.GetFiles(sourceDirectory, $"{setupBaseName}*"))
        {
            var destinationPath = Path.Combine(installDirectory, Path.GetFileName(sourcePath));
            if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
    }

    private static void WriteUninstallEntry(string installDirectory, string mainExecutablePath)
    {
        using var uninstallKey = Registry.CurrentUser.CreateSubKey(UninstallRegistryKey);
        if (uninstallKey == null)
        {
            return;
        }

        uninstallKey.SetValue("DisplayName", ProductName);
        uninstallKey.SetValue("Publisher", "DJ Electric");
        uninstallKey.SetValue("DisplayIcon", mainExecutablePath);
        uninstallKey.SetValue("InstallLocation", installDirectory);
        uninstallKey.SetValue("DisplayVersion", GetProductVersion(mainExecutablePath));
        uninstallKey.SetValue("UninstallString", $"\"{Path.Combine(installDirectory, SetupExecutableName)}\" --uninstall --install-dir \"{installDirectory}\"");
        uninstallKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
        uninstallKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    private static void DeleteUninstallEntry()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(UninstallRegistryKey, throwOnMissingSubKey: false);
        }
        catch
        {
        }
    }

    private static void CreateStartMenuShortcut(string mainExecutablePath)
    {
        var shortcutPath = Path.Combine(GetStartMenuDirectory(), $"{ProductName}.lnk");
        CreateShortcut(shortcutPath, mainExecutablePath, string.Empty);
    }

    private static void CreateStartMenuUninstallShortcut(string installDirectory)
    {
        var shortcutPath = Path.Combine(GetStartMenuDirectory(), $"Uninstall {ProductName}.lnk");
        var setupPath = Path.Combine(installDirectory, SetupExecutableName);
        CreateShortcut(shortcutPath, setupPath, $"--uninstall --install-dir \"{installDirectory}\"");
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string arguments)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);

        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
        {
            throw new InvalidOperationException("Windows Script Host is unavailable, so shortcuts could not be created.");
        }

        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Windows Script Host could not be started.");
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.Arguments = arguments;
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
        shortcut.IconLocation = targetPath;
        shortcut.Save();
    }

    private static void RemoveShortcuts()
    {
        TryDeleteFile(GetDesktopShortcutPath());
        TryDeleteFile(Path.Combine(GetStartMenuDirectory(), $"{ProductName}.lnk"));
        TryDeleteFile(Path.Combine(GetStartMenuDirectory(), $"Uninstall {ProductName}.lnk"));
        TryDeleteDirectory(GetStartMenuDirectory());
    }

    private static void RemoveDesktopShortcut()
    {
        TryDeleteFile(GetDesktopShortcutPath());
    }

    private static string GetDesktopShortcutPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            $"{ProductName}.lnk");
    }

    private static string GetStartMenuDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            "DJ Electric");
    }

    private static string GetProductVersion(string mainExecutablePath)
    {
        return FileVersionInfo.GetVersionInfo(mainExecutablePath).ProductVersion ?? "1.0.0";
    }

    private static void StartDeferredDelete(string installDirectory)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"dj-electric-uninstall-{Guid.NewGuid():N}.cmd");
        var script = string.Join(
            Environment.NewLine,
            "@echo off",
            "ping 127.0.0.1 -n 3 > nul",
            $"rmdir /s /q \"{installDirectory}\"",
            "del \"%~f0\"");

        File.WriteAllText(scriptPath, script);

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{scriptPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
        }
    }
}

internal sealed record InstallResult(string InstallDirectory, string MainExecutablePath);
