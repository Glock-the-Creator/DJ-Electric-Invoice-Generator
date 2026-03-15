using System.Diagnostics;
using System.IO;
using System.Text;

namespace DJ_Electric_Invoice_Generator.Services;

public static class PdfExportService
{
    private static readonly string[] CandidateBrowserPaths =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe")
    };

    public static async Task ExportHtmlToPdfAsync(string html, string outputPdfPath, CancellationToken cancellationToken = default)
    {
        var browserPath = FindBrowserPath();
        if (browserPath == null)
        {
            throw new InvalidOperationException("Microsoft Edge or Google Chrome is required to save invoices as PDF.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPdfPath)!);

        var tempHtmlPath = Path.Combine(Path.GetTempPath(), $"dj-electric-invoice-{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(tempHtmlPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);

        try
        {
            var fileUri = new Uri(tempHtmlPath).AbsoluteUri;
            var attemptWithNewHeadless = await RunBrowserAsync(
                browserPath,
                $"--headless=new --disable-gpu --disable-breakpad --disable-crash-reporter --allow-file-access-from-files --no-pdf-header-footer --print-to-pdf=\"{outputPdfPath}\" \"{fileUri}\"",
                outputPdfPath,
                cancellationToken);

            if (!attemptWithNewHeadless.Success)
            {
                var fallbackAttempt = await RunBrowserAsync(
                    browserPath,
                    $"--headless --disable-gpu --disable-breakpad --disable-crash-reporter --allow-file-access-from-files --no-pdf-header-footer --print-to-pdf=\"{outputPdfPath}\" \"{fileUri}\"",
                    outputPdfPath,
                    cancellationToken);

                if (!fallbackAttempt.Success)
                {
                    throw new InvalidOperationException(fallbackAttempt.ErrorMessage ?? attemptWithNewHeadless.ErrorMessage ?? "PDF export failed.");
                }
            }
        }
        finally
        {
            try
            {
                if (File.Exists(tempHtmlPath))
                {
                    File.Delete(tempHtmlPath);
                }
            }
            catch
            {
                // Temporary HTML cleanup should not break the saved PDF flow.
            }
        }
    }

    private static string? FindBrowserPath()
    {
        return CandidateBrowserPaths.FirstOrDefault(File.Exists);
    }

    private static async Task<PdfExportAttemptResult> RunBrowserAsync(
        string browserPath,
        string arguments,
        string outputPdfPath,
        CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists(outputPdfPath))
            {
                File.Delete(outputPdfPath);
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = browserPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode == 0 && File.Exists(outputPdfPath))
            {
                return PdfExportAttemptResult.Succeeded;
            }

            var message = string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Browser exited with code {process.ExitCode}.",
                    output,
                    error
                }.Where(text => !string.IsNullOrWhiteSpace(text)));

            return new PdfExportAttemptResult(false, string.IsNullOrWhiteSpace(message) ? "PDF export failed." : message);
        }
        catch (Exception ex)
        {
            return new PdfExportAttemptResult(false, ex.Message);
        }
    }

    private readonly record struct PdfExportAttemptResult(bool Success, string? ErrorMessage)
    {
        public static PdfExportAttemptResult Succeeded => new(true, null);
    }
}
