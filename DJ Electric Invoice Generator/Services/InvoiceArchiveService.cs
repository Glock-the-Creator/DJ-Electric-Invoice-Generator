using System.Globalization;
using System.IO;
using System.Text;
using DJ_Electric_Invoice_Generator.ViewModels;

namespace DJ_Electric_Invoice_Generator.Services;

public static class InvoiceArchiveService
{
    public static async Task<InvoiceArchiveResult> SaveInvoicePdfAsync(InvoiceViewModel model, string html)
    {
        var now = DateTime.Now;
        var folderPath = AppPaths.ArchiveRootDirectory;

        Directory.CreateDirectory(folderPath);

        var fileName = BuildUniqueFileName(folderPath, BuildFileStem(model, now), ".pdf");
        var fullPath = Path.Combine(folderPath, fileName);

        await PdfExportService.ExportHtmlToPdfAsync(html, fullPath);

        return new InvoiceArchiveResult(folderPath, fullPath);
    }

    private static string BuildFileStem(InvoiceViewModel model, DateTime now)
    {
        var bid = BuildBidSegment(model.BidNumber);
        var customer = SanitizeSegment(GetFirstMeaningfulLine(model.BillTo));
        var invoiceDate = GetInvoiceDateSegment(model.InvoiceDate, now);

        return $"{invoiceDate}_{customer}_{now:HH-mm-ss}_invoice_{bid}";
    }

    private static string GetInvoiceDateSegment(string? invoiceDateText, DateTime fallbackDate)
    {
        if (!string.IsNullOrWhiteSpace(invoiceDateText) &&
            (DateTime.TryParse(invoiceDateText, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsedDate) ||
             DateTime.TryParse(invoiceDateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate)))
        {
            return parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return fallbackDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string BuildUniqueFileName(string folderPath, string fileStem, string extension)
    {
        var candidate = $"{fileStem}{extension}";
        var index = 2;

        while (File.Exists(Path.Combine(folderPath, candidate)))
        {
            candidate = $"{fileStem}_{index}{extension}";
            index++;
        }

        return candidate;
    }

    private static string GetFirstMeaningfulLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "invoice";
        }

        foreach (var line in value.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                return trimmed;
            }
        }

        return "invoice";
    }

    private static string SanitizeSegment(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder();

        foreach (var character in value)
        {
            builder.Append(invalidChars.Contains(character) ? '_' : character);
        }

        var collapsed = string.Join("-",
            builder.ToString()
                .Split(new[] { ' ', '\t', '\r', '\n', '-', '_' }, StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(collapsed) ? "invoice" : collapsed.ToLowerInvariant();
    }

    private static string BuildBidSegment(string? bidText)
    {
        if (string.IsNullOrWhiteSpace(bidText))
        {
            return "no-bid";
        }

        if (decimal.TryParse(bidText, NumberStyles.Currency, CultureInfo.CurrentCulture, out var amount) ||
            decimal.TryParse(bidText, NumberStyles.Currency, CultureInfo.InvariantCulture, out amount))
        {
            var normalized = amount.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', '-');
            return $"bid-{normalized}";
        }

        return SanitizeSegment(bidText);
    }
}

public sealed record InvoiceArchiveResult(string DirectoryPath, string FilePath);
