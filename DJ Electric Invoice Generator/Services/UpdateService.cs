using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using DJ_Electric_Invoice_Generator.Models;

namespace DJ_Electric_Invoice_Generator.Services;

public static class UpdateService
{
    public const string InstalledSetupFileName = "DJ Electric Invoice Generator Setup.exe";

    private static readonly HttpClient Client = CreateClient();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string? GetConfiguredFeedUrl()
    {
        return typeof(UpdateService).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, "UpdateFeedUrl", StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    public static Version GetCurrentVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version ??
               typeof(UpdateService).Assembly.GetName().Version ??
               new Version(1, 0, 0, 0);
    }

    public static async Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken)
    {
        var feedUrl = GetConfiguredFeedUrl();
        if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out var feedUri))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, feedUri);
        request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true
        };

        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var manifest = await JsonSerializer.DeserializeAsync<AppUpdateManifest>(responseStream, JsonOptions, cancellationToken);
        if (manifest == null || !TryParseVersion(manifest.Version, out var availableVersion))
        {
            return null;
        }

        var currentVersion = GetCurrentVersion();
        if (availableVersion <= currentVersion)
        {
            return null;
        }

        return new AppUpdateInfo
        {
            CurrentVersion = currentVersion,
            AvailableVersion = availableVersion,
            PackageUri = ResolveUri(feedUri, manifest.PackageUrl),
            InstallerUri = ResolveUri(feedUri, manifest.InstallerUrl),
            ReleaseNotes = manifest.ReleaseNotes ?? string.Empty,
            PublishedAtUtc = DateTimeOffset.TryParse(manifest.PublishedAtUtc, out var publishedAt)
                ? publishedAt
                : null
        };
    }

    public static async Task<string?> DownloadPackageAsync(AppUpdateInfo update, CancellationToken cancellationToken)
    {
        if (update.PackageUri == null)
        {
            return null;
        }

        Directory.CreateDirectory(AppPaths.UpdateCacheDirectory);

        var extension = Path.GetExtension(update.PackageUri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".zip";
        }

        var packagePath = Path.Combine(
            AppPaths.UpdateCacheDirectory,
            $"dj-electric-invoice-generator-{SanitizeVersion(update.AvailableVersion)}{extension}");

        using var response = await Client.GetAsync(update.PackageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = File.Create(packagePath);
        await source.CopyToAsync(destination, cancellationToken);

        return packagePath;
    }

    public static bool TryLaunchInstalledUpdater(string packagePath, string installDirectory, bool restartAfterUpdate)
    {
        var setupPath = Path.Combine(installDirectory, InstalledSetupFileName);
        if (!File.Exists(setupPath))
        {
            return false;
        }

        var arguments = $"--apply-update \"{packagePath}\" --install-dir \"{installDirectory}\"";
        if (restartAfterUpdate)
        {
            arguments += " --restart";
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = setupPath,
            Arguments = arguments,
            WorkingDirectory = installDirectory,
            UseShellExecute = true
        });

        return true;
    }

    public static bool TryOpenInstallerDownload(AppUpdateInfo update)
    {
        if (update.InstallerUri == null)
        {
            return false;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = update.InstallerUri.AbsoluteUri,
            UseShellExecute = true
        });

        return true;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("DJElectricInvoiceGenerator-Updater");
        return client;
    }

    private static Uri? ResolveUri(Uri baseUri, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        return Uri.TryCreate(baseUri, candidate, out var resolved) ? resolved : null;
    }

    private static bool TryParseVersion(string? versionText, out Version version)
    {
        if (Version.TryParse(versionText, out version!))
        {
            return true;
        }

        version = new Version(1, 0, 0, 0);
        return false;
    }

    private static string SanitizeVersion(Version version)
    {
        return version.ToString().Replace('.', '-');
    }
}
