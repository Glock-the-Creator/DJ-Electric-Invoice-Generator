using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using DJ_Electric_Invoice_Generator.Models;
using UglyToad.PdfPig;

namespace DJ_Electric_Invoice_Generator.Services;

public static class TemplateLoader
{
    private const string TemplateResourceName = "DJ_Electric_Invoice_Generator.Resources.InvoiceTemplate.pdf";
    private static readonly InvoiceTemplateDefaults TemplateFallbackDefaults = new()
    {
        CompanyName = "DJ ELECTRIC HOME SERVICES",
        CompanyTagline = "Licensed and Insured",
        CompanyAddress = "3721 Kawkawlin River Dr." + Environment.NewLine + "Bay City, MI 48706",
        CompanyPhone = "989-239-9040",
        PaymentNote = "Make all checks payable to DJ Electric",
        ContactLabel = "If you have any questions concerning this invoice, contact:",
        ContactPhone = "989-239-9040"
    };

    private static readonly string[] KnownLabels =
    {
        "Company name",
        "Company",
        "Licensed and Insured",
        "Address",
        "Phone",
        "Telephone",
        "Email",
        "E-mail",
        "Date",
        "Invoice date",
        "Bid number",
        "Bid #",
        "Bid No",
        "Bid",
        "Bill to",
        "Charges",
        "Amount",
        "Description",
        "Addendums",
        "Balance",
        "Total",
        "Make all checks payable to DJ Electric",
        "If you have any questions concerning this invoice, contact:"
    };

    public static InvoiceTemplateDefaults LoadFromResources()
    {
        using var templateStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TemplateResourceName);
        if (templateStream == null)
        {
            return MergeWithFallback(new InvoiceTemplateDefaults
            {
                StatusMessage = "Template PDF not found in app resources. Using defaults."
            });
        }

        return LoadFromStream(templateStream, "Template defaults loaded from app resources.");
    }

    public static InvoiceTemplateDefaults LoadFromPdf(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return LoadFromStream(stream, "Template loaded from PDF.");
        }
        catch (Exception ex)
        {
            return MergeWithFallback(new InvoiceTemplateDefaults
            {
                StatusMessage = $"Template load failed: {ex.Message}"
            });
        }
    }

    private static InvoiceTemplateDefaults LoadFromStream(Stream stream, string successStatus)
    {
        try
        {
            using var document = PdfDocument.Open(stream);
            var buffer = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                buffer.AppendLine(page.Text);
            }

            var rawText = buffer.ToString();
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return MergeWithFallback(new InvoiceTemplateDefaults
                {
                    StatusMessage = "Template PDF has no readable text. Using defaults."
                });
            }

            var defaults = ParseText(rawText);
            return MergeWithFallback(new InvoiceTemplateDefaults
            {
                CompanyName = defaults.CompanyName,
                CompanyTagline = defaults.CompanyTagline,
                CompanyAddress = defaults.CompanyAddress,
                CompanyPhone = defaults.CompanyPhone,
                CompanyEmail = defaults.CompanyEmail,
                InvoiceDate = defaults.InvoiceDate,
                BidNumber = defaults.BidNumber,
                PaymentNote = defaults.PaymentNote,
                ContactLabel = defaults.ContactLabel,
                ContactPhone = defaults.ContactPhone,
                StatusMessage = defaults.HasValues
                    ? successStatus
                    : "Template text found, but no fields matched. Using defaults."
            });
        }
        catch (Exception ex)
        {
            return MergeWithFallback(new InvoiceTemplateDefaults
            {
                StatusMessage = $"Template load failed: {ex.Message}"
            });
        }
    }

    private static InvoiceTemplateDefaults ParseText(string rawText)
    {
        var lines = rawText
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();

        var companyName = ExtractAfterLabel(lines, new[] { "Company name", "Company", "Sender", "From" });
        if (string.IsNullOrWhiteSpace(companyName))
        {
            companyName = ExtractCompanyNameFallback(lines);
        }

        var companyTagline = ExtractExactLine(lines, "Licensed and Insured");
        var companyAddress = ExtractMultiLineAfterLabel(lines, new[] { "Address", "Company address" });
        var companyPhone = ExtractAfterLabel(lines, new[] { "Phone", "Telephone" });
        var companyEmail = ExtractAfterLabel(lines, new[] { "Email", "E-mail" });
        var invoiceDate = NormalizeTemplateValue(ExtractAfterLabel(lines, new[] { "Date", "Invoice date" }));
        var bidNumber = NormalizeTemplateValue(ExtractAfterLabel(lines, new[] { "Bid number", "Bid #", "Bid No", "Bid" }));
        var paymentNote = ExtractExactLine(lines, "Make all checks payable to DJ Electric");
        var contactLabel = ExtractExactLine(lines, "If you have any questions concerning this invoice, contact:");
        var contactPhone = string.Empty;

        if (string.IsNullOrWhiteSpace(companyEmail))
        {
            companyEmail = ExtractRegex(rawText, @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase);
        }

        if (string.IsNullOrWhiteSpace(companyPhone))
        {
            companyPhone = ExtractRegex(rawText, @"\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}", RegexOptions.None);
        }

        if (string.IsNullOrWhiteSpace(invoiceDate))
        {
            invoiceDate = NormalizeTemplateValue(ExtractRegex(rawText, @"\b\d{1,2}[/-]\d{1,2}[/-]\d{2,4}\b", RegexOptions.None));
        }

        if (string.IsNullOrWhiteSpace(companyAddress) || string.IsNullOrWhiteSpace(companyPhone))
        {
            var companyBlock = ExtractCompanyBlock(lines, companyName);
            if (string.IsNullOrWhiteSpace(companyAddress))
            {
                companyAddress = companyBlock.Address;
            }

            if (string.IsNullOrWhiteSpace(companyPhone))
            {
                companyPhone = companyBlock.Phone;
            }
        }

        if (!string.IsNullOrWhiteSpace(contactLabel))
        {
            contactPhone = ExtractLineAfter(lines, contactLabel);
        }

        return new InvoiceTemplateDefaults
        {
            CompanyName = NormalizeTemplateValue(companyName) ?? string.Empty,
            CompanyTagline = NormalizeTemplateValue(companyTagline) ?? string.Empty,
            CompanyAddress = NormalizeTemplateValue(companyAddress) ?? string.Empty,
            CompanyPhone = NormalizeTemplateValue(companyPhone) ?? string.Empty,
            CompanyEmail = NormalizeTemplateValue(companyEmail) ?? string.Empty,
            InvoiceDate = invoiceDate ?? string.Empty,
            BidNumber = bidNumber ?? string.Empty,
            PaymentNote = NormalizeTemplateValue(paymentNote) ?? string.Empty,
            ContactLabel = NormalizeTemplateValue(contactLabel) ?? string.Empty,
            ContactPhone = NormalizeTemplateValue(contactPhone) ?? string.Empty
        };
    }

    private static string? ExtractAfterLabel(IReadOnlyList<string> lines, string[] labels)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            foreach (var label in labels)
            {
                if (!line.Contains(label, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var inline = ExtractInlineValue(line, label);
                if (!string.IsNullOrWhiteSpace(inline))
                {
                    return inline;
                }

                var next = FindNextValue(lines, i + 1);
                if (!string.IsNullOrWhiteSpace(next))
                {
                    return next;
                }
            }
        }

        return null;
    }

    private static string? ExtractExactLine(IEnumerable<string> lines, string target)
    {
        return lines.FirstOrDefault(line => string.Equals(line, target, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractMultiLineAfterLabel(IReadOnlyList<string> lines, string[] labels, int maxLines = 3)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            foreach (var label in labels)
            {
                if (!line.Contains(label, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var inline = ExtractInlineValue(line, label);
                if (!string.IsNullOrWhiteSpace(inline))
                {
                    return inline;
                }

                var block = new List<string>();
                for (var j = i + 1; j < lines.Count && block.Count < maxLines; j++)
                {
                    if (IsLabel(lines[j]))
                    {
                        break;
                    }

                    block.Add(lines[j]);
                }

                return block.Count > 0 ? string.Join(Environment.NewLine, block) : null;
            }
        }

        return null;
    }

    private static string? ExtractLineAfter(IReadOnlyList<string> lines, string label)
    {
        var index = lines
            .Select((line, lineIndex) => new { line, lineIndex })
            .FirstOrDefault(item => string.Equals(item.line, label, StringComparison.OrdinalIgnoreCase))
            ?.lineIndex ?? -1;

        if (index < 0)
        {
            return null;
        }

        return FindNextValue(lines, index + 1);
    }

    private static string? ExtractInlineValue(string line, string label)
    {
        var index = CultureInfo.InvariantCulture.CompareInfo
            .IndexOf(line, label, CompareOptions.IgnoreCase);

        if (index < 0)
        {
            return null;
        }

        var after = line[(index + label.Length)..].Trim();
        after = after.TrimStart(':', '-', '#');
        return string.IsNullOrWhiteSpace(after) ? null : after;
    }

    private static string? FindNextValue(IReadOnlyList<string> lines, int startIndex)
    {
        for (var i = startIndex; i < lines.Count; i++)
        {
            if (IsLabel(lines[i]))
            {
                continue;
            }

            return lines[i];
        }

        return null;
    }

    private static bool IsLabel(string line)
    {
        foreach (var label in KnownLabels)
        {
            if (string.Equals(line, label, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static (string Address, string Phone) ExtractCompanyBlock(IReadOnlyList<string> lines, string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            return (string.Empty, string.Empty);
        }

        var companyIndex = lines
            .Select((line, index) => new { line, index })
            .FirstOrDefault(item => string.Equals(item.line, companyName, StringComparison.OrdinalIgnoreCase))
            ?.index ?? -1;

        if (companyIndex < 0)
        {
            return (string.Empty, string.Empty);
        }

        var addressLines = new List<string>();
        var phone = string.Empty;

        for (var i = companyIndex + 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.Equals(line, "Licensed and Insured", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsLabel(line))
            {
                break;
            }

            var cleanedLine = Regex.Replace(line, @"\s+DATE:.*$", string.Empty, RegexOptions.IgnoreCase).Trim();
            if (string.IsNullOrWhiteSpace(cleanedLine))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(phone) &&
                Regex.IsMatch(cleanedLine, @"\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}", RegexOptions.None))
            {
                phone = cleanedLine;
                break;
            }

            addressLines.Add(cleanedLine);
        }

        return (string.Join(Environment.NewLine, addressLines), phone);
    }

    private static string? ExtractCompanyNameFallback(IReadOnlyList<string> lines)
    {
        foreach (var line in lines)
        {
            if (line.Contains("DJ", StringComparison.OrdinalIgnoreCase))
            {
                return line;
            }
        }

        return null;
    }

    private static string? ExtractRegex(string text, string pattern, RegexOptions options)
    {
        var match = Regex.Match(text, pattern, options);
        return match.Success ? match.Value : null;
    }

    private static string? NormalizeTemplateValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (Regex.IsMatch(trimmed, @"^[A-Z]{2}/[A-Z]{2}/[A-Z]{2,4}$", RegexOptions.IgnoreCase))
        {
            return null;
        }

        return KnownLabels.Contains(trimmed, StringComparer.OrdinalIgnoreCase) ? null : trimmed;
    }

    private static InvoiceTemplateDefaults MergeWithFallback(InvoiceTemplateDefaults parsed)
    {
        return new InvoiceTemplateDefaults
        {
            CompanyName = string.IsNullOrWhiteSpace(parsed.CompanyName) ? TemplateFallbackDefaults.CompanyName : parsed.CompanyName,
            CompanyTagline = string.IsNullOrWhiteSpace(parsed.CompanyTagline) ? TemplateFallbackDefaults.CompanyTagline : parsed.CompanyTagline,
            CompanyAddress = string.IsNullOrWhiteSpace(parsed.CompanyAddress) ? TemplateFallbackDefaults.CompanyAddress : parsed.CompanyAddress,
            CompanyPhone = string.IsNullOrWhiteSpace(parsed.CompanyPhone) ? TemplateFallbackDefaults.CompanyPhone : parsed.CompanyPhone,
            CompanyEmail = parsed.CompanyEmail,
            InvoiceDate = parsed.InvoiceDate,
            BidNumber = parsed.BidNumber,
            PaymentNote = string.IsNullOrWhiteSpace(parsed.PaymentNote) ? TemplateFallbackDefaults.PaymentNote : parsed.PaymentNote,
            ContactLabel = string.IsNullOrWhiteSpace(parsed.ContactLabel) ? TemplateFallbackDefaults.ContactLabel : parsed.ContactLabel,
            ContactPhone = string.IsNullOrWhiteSpace(parsed.ContactPhone) ? TemplateFallbackDefaults.ContactPhone : parsed.ContactPhone,
            StatusMessage = parsed.StatusMessage
        };
    }
}
