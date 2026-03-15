using System.Net;
using System.Text;
using DJ_Electric_Invoice_Generator.ViewModels;

namespace DJ_Electric_Invoice_Generator.Services;

public static class InvoiceDocumentBuilder
{
    public static string BuildHtml(InvoiceViewModel model)
    {
        var documentTitle = BuildDocumentTitle(model);
        var documentSummary = BuildDocumentSummary(model);
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine($"<title>{Html(documentTitle)}</title>");
        sb.AppendLine($"<meta name=\"title\" content=\"{Html(documentTitle)}\" />");
        sb.AppendLine($"<meta name=\"description\" content=\"{Html(documentSummary)}\" />");
        sb.AppendLine($"<meta name=\"author\" content=\"{Html(model.CompanyName)}\" />");
        sb.AppendLine("<meta name=\"generator\" content=\"DJ Electric Invoice Generator\" />");
        sb.AppendLine("<style>");
        sb.AppendLine("@page{size:Letter;margin:.45in;}");
        sb.AppendLine("html,body{margin:0;padding:0;}");
        sb.AppendLine("body{font-family:'Trebuchet MS',Arial,sans-serif;background:#f3efe8;color:#1f2530;padding:32px;}");
        sb.AppendLine(".sheet{max-width:860px;margin:0 auto;background:#fffdf8;border-radius:28px;padding:36px;border:1px solid #e6dfd2;box-shadow:0 18px 45px rgba(37,44,58,.08);}");
        sb.AppendLine(".brand{display:flex;justify-content:space-between;gap:24px;align-items:flex-start;}");
        sb.AppendLine(".mark{width:54px;height:54px;border-radius:18px;background:linear-gradient(135deg,#f5c655,#e8891c);display:inline-flex;align-items:center;justify-content:center;color:#fff;font-size:26px;font-weight:700;box-shadow:0 12px 22px rgba(232,137,28,.24);}");
        sb.AppendLine(".company-name{font-size:30px;line-height:1.05;font-weight:700;margin:0;}");
        sb.AppendLine(".tagline{margin:6px 0 14px;color:#8b5e1a;font-weight:600;letter-spacing:.08em;text-transform:uppercase;font-size:12px;}");
        sb.AppendLine(".muted{color:#65707f;line-height:1.55;}");
        sb.AppendLine(".invoice-title{text-align:right;}");
        sb.AppendLine(".invoice-title h2{font-size:34px;margin:0;color:#da7f1f;letter-spacing:.08em;}");
        sb.AppendLine(".label{font-size:12px;color:#65707f;text-transform:uppercase;letter-spacing:.08em;}");
        sb.AppendLine(".value{font-size:16px;font-weight:700;color:#1f2530;}");
        sb.AppendLine(".panel{margin-top:24px;border:1px solid #ebe4d7;border-radius:20px;padding:18px 20px;background:#fffaf0;}");
        sb.AppendLine(".panel h3{margin:0 0 10px;font-size:13px;letter-spacing:.08em;text-transform:uppercase;color:#65707f;}");
        sb.AppendLine(".table{width:100%;border-collapse:collapse;margin-top:26px;}");
        sb.AppendLine(".table thead{display:table-header-group;}");
        sb.AppendLine(".table th{padding:0 0 12px;text-align:left;font-size:12px;color:#65707f;letter-spacing:.08em;text-transform:uppercase;border-bottom:2px solid #ece4d5;}");
        sb.AppendLine(".table td{padding:12px 0;border-bottom:1px solid #f1eadf;vertical-align:top;}");
        sb.AppendLine(".table th:last-child,.table td:last-child{text-align:right;white-space:nowrap;}");
        sb.AppendLine(".table tr{break-inside:avoid;page-break-inside:avoid;}");
        sb.AppendLine(".split{display:grid;grid-template-columns:minmax(0,1fr) 260px;gap:26px;margin-top:24px;}");
        sb.AppendLine(".notes{background:#f9f2e3;border-radius:18px;padding:18px;}");
        sb.AppendLine(".totals{border:1px solid #ece4d8;border-radius:20px;padding:18px;background:#fff;}");
        sb.AppendLine(".total-row{display:flex;justify-content:space-between;gap:16px;padding:6px 0;color:#485160;}");
        sb.AppendLine(".grand-total{display:flex;justify-content:space-between;gap:16px;margin-top:10px;padding:14px 16px;border-radius:16px;background:#1f2530;color:#fff;font-size:18px;font-weight:700;}");
        sb.AppendLine(".footer{margin-top:28px;padding-top:18px;border-top:1px solid #ede6dc;color:#5b6472;line-height:1.7;}");
        sb.AppendLine("@media print{body{background:#fff;padding:0;} .sheet{max-width:none;margin:0;border:none;border-radius:0;box-shadow:none;padding:0;} .panel,.notes,.totals,.footer{break-inside:avoid;page-break-inside:avoid;} .brand{break-inside:avoid;page-break-inside:avoid;}}");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"sheet\">");
        sb.AppendLine("<div class=\"brand\">");
        sb.AppendLine("<div>");
        sb.AppendLine("<div style=\"display:flex;align-items:flex-start;gap:14px;\">");
        sb.AppendLine("<div class=\"mark\">&#128161;</div>");
        sb.AppendLine("<div>");
        sb.AppendLine($"<h1 class=\"company-name\">{Html(model.CompanyName)}</h1>");

        if (!string.IsNullOrWhiteSpace(model.CompanyTagline))
        {
            sb.AppendLine($"<div class=\"tagline\">{Html(model.CompanyTagline)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(model.CompanyAddress))
        {
            sb.AppendLine($"<div class=\"muted\">{HtmlLines(model.CompanyAddress)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(model.CompanyPhone))
        {
            sb.AppendLine($"<div class=\"muted\">{Html(model.CompanyPhone)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(model.CompanyEmail))
        {
            sb.AppendLine($"<div class=\"muted\">{Html(model.CompanyEmail)}</div>");
        }

        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"invoice-title\">");
        sb.AppendLine("<h2>INVOICE</h2>");
        sb.AppendLine("<div class=\"label\">Date</div>");
        sb.AppendLine($"<div class=\"value\">{Html(model.InvoiceDate)}</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"panel\">");
        sb.AppendLine("<h3>Bill To</h3>");
        sb.AppendLine($"{HtmlLines(model.BillTo)}");
        sb.AppendLine("</div>");

        sb.AppendLine("<table class=\"table\">");
        sb.AppendLine("<thead><tr><th>Description</th><th>Amount</th></tr></thead>");
        sb.AppendLine("<tbody>");

        var hasItems = false;
        foreach (var item in model.Charges)
        {
            if (item.IsEmpty)
            {
                continue;
            }

            hasItems = true;
            sb.AppendLine($"<tr><td>{Html(item.Description)}</td><td>{Html(item.AmountDisplay)}</td></tr>");
        }

        if (!hasItems)
        {
            sb.AppendLine("<tr><td class=\"muted\">No charges listed</td><td></td></tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        sb.AppendLine("<div class=\"split\">");
        sb.AppendLine("<div class=\"notes\">");
        sb.AppendLine("<div class=\"label\">Addendums</div>");
        sb.AppendLine($"<div style=\"margin-top:10px;line-height:1.65;\">{HtmlLines(model.AddendumNotes)}</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"totals\">");
        sb.AppendLine($"<div class=\"total-row\"><span>Charges</span><strong>{Html(model.ChargesTotalDisplay)}</strong></div>");
        sb.AppendLine($"<div class=\"total-row\"><span>Bid</span><strong>{Html(model.BidDisplay)}</strong></div>");
        sb.AppendLine($"<div class=\"total-row\"><span>Balance</span><strong>{Html(model.BalanceDisplay)}</strong></div>");
        sb.AppendLine($"<div class=\"total-row\"><span>Addendums</span><strong>{Html(model.AddendumAmountDisplay)}</strong></div>");
        sb.AppendLine($"<div class=\"grand-total\"><span>Total</span><span>{Html(model.TotalDisplay)}</span></div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"footer\">");
        if (!string.IsNullOrWhiteSpace(model.PaymentNote))
        {
            sb.AppendLine($"<div>{Html(model.PaymentNote)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(model.ContactLabel))
        {
            sb.AppendLine($"<div>{Html(model.ContactLabel)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(model.ContactPhone))
        {
            sb.AppendLine($"<div>{Html(model.ContactPhone)}</div>");
        }

        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string BuildDocumentTitle(InvoiceViewModel model)
    {
        var billTo = GetFirstMeaningfulLine(model.BillTo);
        var invoiceDate = NormalizeInline(model.InvoiceDate);

        if (!string.IsNullOrWhiteSpace(billTo) && !string.IsNullOrWhiteSpace(invoiceDate))
        {
            return $"DJ Electric Invoice - {billTo} - {invoiceDate}";
        }

        if (!string.IsNullOrWhiteSpace(billTo))
        {
            return $"DJ Electric Invoice - {billTo}";
        }

        if (!string.IsNullOrWhiteSpace(invoiceDate))
        {
            return $"DJ Electric Invoice - {invoiceDate}";
        }

        return "DJ Electric Invoice";
    }

    private static string BuildDocumentSummary(InvoiceViewModel model)
    {
        var billTo = GetFirstMeaningfulLine(model.BillTo);
        var invoiceDate = NormalizeInline(model.InvoiceDate);
        var summaryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(billTo))
        {
            summaryParts.Add($"Invoice for {billTo}");
        }
        else
        {
            summaryParts.Add("DJ Electric invoice");
        }

        if (!string.IsNullOrWhiteSpace(invoiceDate))
        {
            summaryParts.Add($"dated {invoiceDate}");
        }

        summaryParts.Add($"total {NormalizeInline(model.TotalDisplay)}");

        return string.Join(", ", summaryParts) + ".";
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static string HtmlLines(string? value)
    {
        return Html(value).Replace("\r\n", "<br />").Replace("\n", "<br />");
    }

    private static string GetFirstMeaningfulLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        foreach (var line in value.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = NormalizeInline(line);
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                return trimmed;
            }
        }

        return string.Empty;
    }

    private static string NormalizeInline(string? value)
    {
        return string.Join(
            " ",
            (value ?? string.Empty)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
