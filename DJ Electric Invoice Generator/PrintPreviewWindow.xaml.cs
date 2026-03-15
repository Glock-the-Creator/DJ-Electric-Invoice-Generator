using System.Windows;
using System.Windows.Controls;
using DJ_Electric_Invoice_Generator.Services;
using DJ_Electric_Invoice_Generator.ViewModels;

namespace DJ_Electric_Invoice_Generator;

public partial class PrintPreviewWindow : Window
{
    private readonly InvoiceViewModel viewModel;

    public PrintPreviewWindow(InvoiceViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        PreviewViewer.Document = InvoicePrintService.CreatePreviewDocument(viewModel);
    }

    public string? LastPrintedPrinterName { get; private set; }

    private void PreviewPrintButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        InvoicePrintService.PrintInvoice(viewModel, dialog);
        LastPrintedPrinterName = dialog.PrintQueue?.FullName ?? "selected printer";
        DialogResult = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
