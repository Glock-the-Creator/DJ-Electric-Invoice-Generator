using System.IO;

namespace DJ_Electric_Invoice_Generator.Services;

public static class AppPaths
{
    public static string ArchiveRootDirectory =>
        EnsureDirectory(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DJ Electric Invoices"));

    public static string UpdateCacheDirectory =>
        EnsureDirectory(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DJ Electric Invoice Generator",
            "Updates"));

    public static string? FindBundledFile(string relativePath)
    {
        foreach (var root in EnumerateSearchRoots())
        {
            var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                continue;
            }

            var current = new DirectoryInfo(path);
            while (current != null)
            {
                if (seen.Add(current.FullName))
                {
                    yield return current.FullName;
                }

                current = current.Parent;
            }
        }
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
