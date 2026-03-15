using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using DJ_Electric_Invoice_Generator.ViewModels;

namespace DJ_Electric_Invoice_Generator.Services;

public static class InvoicePrintService
{
    private const double DefaultPageWidth = 8.5 * 96;
    private const double DefaultPageHeight = 11 * 96;
    private const double PageMargin = 42;

    public static void PrintInvoice(InvoiceViewModel model, PrintDialog dialog)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(dialog);

        var document = BuildDocument(model, dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);
        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
        dialog.PrintDocument(paginator, BuildJobDescription(model));
    }

    public static FlowDocument CreatePreviewDocument(InvoiceViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return BuildDocument(model, DefaultPageWidth, DefaultPageHeight);
    }

    public static void SaveInvoiceAsXps(InvoiceViewModel model, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var document = BuildDocument(model, DefaultPageWidth, DefaultPageHeight);
        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
        var directoryPath = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        using var xpsDocument = new XpsDocument(outputPath, FileAccess.ReadWrite, CompressionOption.Maximum);
        var writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
        writer.Write(paginator);
    }

    private static FlowDocument BuildDocument(InvoiceViewModel model, double pageWidth, double pageHeight)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            PageWidth = NormalizePageDimension(pageWidth, DefaultPageWidth),
            PageHeight = NormalizePageDimension(pageHeight, DefaultPageHeight),
            PagePadding = new Thickness(PageMargin),
            ColumnWidth = double.PositiveInfinity,
            Background = Brushes.White
        };

        document.Blocks.Add(BuildHeaderTable(model));
        document.Blocks.Add(BuildBillToSection(model));
        document.Blocks.Add(BuildChargesTable(model));
        document.Blocks.Add(BuildTotalsTable(model));
        document.Blocks.Add(BuildFooterSection(model));

        return document;
    }

    private static Table BuildHeaderTable(InvoiceViewModel model)
    {
        var table = CreateTable(4, 2);
        table.Columns[0].Width = new GridLength(4, GridUnitType.Star);
        table.Columns[1].Width = new GridLength(2, GridUnitType.Star);

        var row = new TableRow();
        row.Cells.Add(new TableCell(BuildCompanyBlock(model))
        {
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0, 0, 18, 0)
        });
        row.Cells.Add(new TableCell(BuildInvoiceMetaBlock(model))
        {
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0)
        });

        table.RowGroups[0].Rows.Add(row);
        return table;
    }

    private static Section BuildCompanyBlock(InvoiceViewModel model)
    {
        var section = new Section
        {
            Margin = new Thickness(0),
            Padding = new Thickness(0)
        };

        section.Blocks.Add(new Paragraph(new Run(model.CompanyName))
        {
            Margin = new Thickness(0),
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = CreateBrush("#1F2530")
        });

        if (!string.IsNullOrWhiteSpace(model.CompanyTagline))
        {
            section.Blocks.Add(new Paragraph(new Run(model.CompanyTagline))
            {
                Margin = new Thickness(0, 4, 0, 0),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = CreateBrush("#8B5E1A")
            });
        }

        foreach (var line in GetNonEmptyLines(model.CompanyAddress))
        {
            section.Blocks.Add(CreateMutedParagraph(line, 10));
        }

        if (!string.IsNullOrWhiteSpace(model.CompanyPhone))
        {
            section.Blocks.Add(CreateMutedParagraph(model.CompanyPhone, 0));
        }

        if (!string.IsNullOrWhiteSpace(model.CompanyEmail))
        {
            section.Blocks.Add(CreateMutedParagraph(model.CompanyEmail, 0));
        }

        return section;
    }

    private static Section BuildInvoiceMetaBlock(InvoiceViewModel model)
    {
        var section = new Section
        {
            Margin = new Thickness(0),
            Padding = new Thickness(0)
        };

        section.Blocks.Add(new Paragraph(new Run(BuildInvoiceHeaderTitle(model)))
        {
            Margin = new Thickness(0),
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Right,
            Foreground = CreateBrush("#8B5E1A")
        });

        section.Blocks.Add(new Paragraph(new Run("DATE"))
        {
            Margin = new Thickness(0, 16, 0, 0),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Right,
            Foreground = CreateBrush("#697483")
        });

        section.Blocks.Add(new Paragraph(new Run(model.InvoiceDate))
        {
            Margin = new Thickness(0, 2, 0, 0),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Right,
            Foreground = CreateBrush("#1F2530")
        });

        return section;
    }

    private static string BuildInvoiceHeaderTitle(InvoiceViewModel model)
    {
        return string.IsNullOrWhiteSpace(model.InvoiceNumber) ? "INVOICE" : model.InvoiceNumber.Trim();
    }

    private static Section BuildBillToSection(InvoiceViewModel model)
    {
        var section = new Section
        {
            Margin = new Thickness(0, 18, 0, 0),
            Padding = new Thickness(14),
            BorderBrush = CreateBrush("#E7DFD0"),
            BorderThickness = new Thickness(1)
        };

        section.Blocks.Add(new Paragraph(new Run("Bill To"))
        {
            Margin = new Thickness(0),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = CreateBrush("#697483")
        });

        if (string.IsNullOrWhiteSpace(model.BillTo))
        {
            section.Blocks.Add(new Paragraph(new Run(" "))
            {
                Margin = new Thickness(0, 6, 0, 0)
            });
            return section;
        }

        foreach (var line in GetNonEmptyLines(model.BillTo))
        {
            section.Blocks.Add(new Paragraph(new Run(line))
            {
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = CreateBrush("#1F2530")
            });
        }

        return section;
    }

    private static Table BuildChargesTable(InvoiceViewModel model)
    {
        var table = CreateTable(0, 2);
        table.Columns[0].Width = new GridLength(4, GridUnitType.Star);
        table.Columns[1].Width = new GridLength(2, GridUnitType.Star);
        table.RowGroups[0].Rows.Add(CreateHeaderRow("DESCRIPTION", "AMOUNT"));

        var visibleCharges = model.Charges.Where(item => !item.IsEmpty).ToList();
        if (visibleCharges.Count == 0)
        {
            var emptyRow = new TableRow();
            emptyRow.Cells.Add(CreateBodyCell("No charges listed", 10, CreateBrush("#697483")));
            emptyRow.Cells.Add(CreateBodyCell(string.Empty, 10, CreateBrush("#697483"), TextAlignment.Right));
            table.RowGroups[0].Rows.Add(emptyRow);
            return table;
        }

        foreach (var charge in visibleCharges)
        {
            var row = new TableRow();
            row.Cells.Add(CreateBodyCell(charge.Description, 10, CreateBrush("#1F2530")));
            row.Cells.Add(CreateBodyCell(charge.AmountDisplay, 10, CreateBrush("#1F2530"), TextAlignment.Right));
            table.RowGroups[0].Rows.Add(row);
        }

        return table;
    }

    private static Table BuildTotalsTable(InvoiceViewModel model)
    {
        var table = CreateTable(16, 2);
        table.Columns[0].Width = new GridLength(3, GridUnitType.Star);
        table.Columns[1].Width = new GridLength(2, GridUnitType.Star);

        table.RowGroups[0].Rows.Add(CreateSummaryRow("Charges total", model.ChargesTotalDisplay, false));
        table.RowGroups[0].Rows.Add(CreateSummaryRow("Bid", model.BidDisplay, false));
        table.RowGroups[0].Rows.Add(CreateSummaryRow("Balance", model.BalanceDisplay, false));
        table.RowGroups[0].Rows.Add(CreateSummaryRow("Addendums", model.AddendumAmountDisplay, false));
        table.RowGroups[0].Rows.Add(CreateSummaryRow("TOTAL", model.TotalDisplay, true));

        if (!string.IsNullOrWhiteSpace(model.AddendumNotes))
        {
            var noteRow = new TableRow();
            noteRow.Cells.Add(new TableCell(new Paragraph(new Run("Addendum notes"))
            {
                Margin = new Thickness(0, 10, 0, 0),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = CreateBrush("#697483")
            })
            {
                ColumnSpan = 2,
                BorderThickness = new Thickness(0)
            });
            table.RowGroups[0].Rows.Add(noteRow);

            foreach (var line in GetNonEmptyLines(model.AddendumNotes))
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(line))
                {
                    Margin = new Thickness(0, 4, 0, 0),
                    Foreground = CreateBrush("#1F2530")
                })
                {
                    ColumnSpan = 2,
                    BorderThickness = new Thickness(0)
                });
                table.RowGroups[0].Rows.Add(row);
            }
        }

        return table;
    }

    private static Section BuildFooterSection(InvoiceViewModel model)
    {
        var section = new Section
        {
            Margin = new Thickness(0, 18, 0, 0),
            Padding = new Thickness(0)
        };

        if (!string.IsNullOrWhiteSpace(model.PaymentNote))
        {
            section.Blocks.Add(new Paragraph(new Run(model.PaymentNote))
            {
                Margin = new Thickness(0),
                Foreground = CreateBrush("#1F2530")
            });
        }

        if (!string.IsNullOrWhiteSpace(model.ContactLabel))
        {
            section.Blocks.Add(new Paragraph(new Run(model.ContactLabel))
            {
                Margin = new Thickness(0, 8, 0, 0),
                Foreground = CreateBrush("#697483")
            });
        }

        if (!string.IsNullOrWhiteSpace(model.ContactPhone))
        {
            section.Blocks.Add(new Paragraph(new Run(model.ContactPhone))
            {
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = CreateBrush("#1F2530")
            });
        }

        return section;
    }

    private static Table CreateTable(double marginTop, int columnCount)
    {
        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, marginTop, 0, 0)
        };

        for (var i = 0; i < columnCount; i++)
        {
            table.Columns.Add(new TableColumn());
        }

        table.RowGroups.Add(new TableRowGroup());
        return table;
    }

    private static TableRow CreateHeaderRow(string leftText, string rightText)
    {
        var row = new TableRow
        {
            Background = CreateBrush("#FCF7EA")
        };

        row.Cells.Add(CreateHeaderCell(leftText, TextAlignment.Left));
        row.Cells.Add(CreateHeaderCell(rightText, TextAlignment.Right));
        return row;
    }

    private static TableRow CreateSummaryRow(string label, string value, bool emphasize)
    {
        var foreground = emphasize ? Brushes.White : CreateBrush("#1F2530");
        var background = emphasize ? CreateBrush("#1F2530") : Brushes.Transparent;
        var row = new TableRow
        {
            Background = background
        };

        row.Cells.Add(CreateSummaryCell(label, foreground, emphasize, TextAlignment.Left));
        row.Cells.Add(CreateSummaryCell(value, foreground, emphasize, TextAlignment.Right));
        return row;
    }

    private static TableCell CreateHeaderCell(string text, TextAlignment alignment)
    {
        return new TableCell(new Paragraph(new Run(text))
        {
            Margin = new Thickness(0),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = alignment,
            Foreground = CreateBrush("#1F2530")
        })
        {
            Padding = new Thickness(10, 8, 10, 8),
            BorderBrush = CreateBrush("#E7DFD0"),
            BorderThickness = new Thickness(0, 1, 0, 1)
        };
    }

    private static TableCell CreateBodyCell(string text, double marginTop, Brush foreground, TextAlignment alignment = TextAlignment.Left)
    {
        return new TableCell(new Paragraph(new Run(text))
        {
            Margin = new Thickness(0, marginTop, 0, 0),
            TextAlignment = alignment,
            Foreground = foreground
        })
        {
            Padding = new Thickness(10, 4, 10, 4),
            BorderThickness = new Thickness(0)
        };
    }

    private static TableCell CreateSummaryCell(string text, Brush foreground, bool emphasize, TextAlignment alignment)
    {
        return new TableCell(new Paragraph(new Run(text))
        {
            Margin = new Thickness(0),
            FontSize = emphasize ? 14 : 12,
            FontWeight = emphasize ? FontWeights.Bold : FontWeights.SemiBold,
            TextAlignment = alignment,
            Foreground = foreground
        })
        {
            Padding = emphasize ? new Thickness(10, 10, 10, 10) : new Thickness(10, 6, 10, 6),
            BorderBrush = emphasize ? CreateBrush("#1F2530") : CreateBrush("#E7DFD0"),
            BorderThickness = emphasize ? new Thickness(0) : new Thickness(0, 1, 0, 0)
        };
    }

    private static Paragraph CreateMutedParagraph(string text, double marginTop)
    {
        return new Paragraph(new Run(text))
        {
            Margin = new Thickness(0, marginTop, 0, 0),
            FontSize = 11,
            Foreground = CreateBrush("#697483")
        };
    }

    private static IEnumerable<string> GetNonEmptyLines(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));
    }

    private static string BuildJobDescription(InvoiceViewModel model)
    {
        var customer = GetNonEmptyLines(model.BillTo).FirstOrDefault();
        return string.IsNullOrWhiteSpace(customer)
            ? "DJ Electric Invoice"
            : $"DJ Electric Invoice - {customer}";
    }

    private static SolidColorBrush CreateBrush(string hexColor)
    {
        return (SolidColorBrush)new BrushConverter().ConvertFromString(hexColor)!;
    }

    private static double NormalizePageDimension(double value, double fallback)
    {
        return double.IsNaN(value) || value <= 0 ? fallback : value;
    }
}
