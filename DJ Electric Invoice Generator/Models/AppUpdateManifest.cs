namespace DJ_Electric_Invoice_Generator.Models;

public sealed class AppUpdateManifest
{
    public string ProductName { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public string PackageUrl { get; init; } = string.Empty;

    public string InstallerUrl { get; init; } = string.Empty;

    public string PublishedAtUtc { get; init; } = string.Empty;

    public string ReleaseNotes { get; init; } = string.Empty;
}
