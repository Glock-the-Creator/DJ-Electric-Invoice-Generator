namespace DJ_Electric_Invoice_Generator.Models;

public sealed class AppUpdateInfo
{
    public Version CurrentVersion { get; init; } = new(1, 0, 0, 0);

    public Version AvailableVersion { get; init; } = new(1, 0, 0, 0);

    public Uri? PackageUri { get; init; }

    public Uri? InstallerUri { get; init; }

    public string ReleaseNotes { get; init; } = string.Empty;

    public DateTimeOffset? PublishedAtUtc { get; init; }

    public string AvailableVersionDisplay => AvailableVersion.ToString(3);
}
