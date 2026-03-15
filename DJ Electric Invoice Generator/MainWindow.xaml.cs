using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Interop;
using DJ_Electric_Invoice_Generator.Models;
using DJ_Electric_Invoice_Generator.Services;
using DJ_Electric_Invoice_Generator.ViewModels;

namespace DJ_Electric_Invoice_Generator;

public partial class MainWindow : Window
{
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

    private readonly InvoiceViewModel viewModel;
    private bool updateCheckStarted;

    public MainWindow()
    {
        InitializeComponent();
        viewModel = new InvoiceViewModel();
        DataContext = viewModel;
        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        ApplyCaptionTheme();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var defaults = TemplateLoader.LoadFromResources();
        viewModel.ApplyTemplateDefaults(defaults);
        if (!string.IsNullOrWhiteSpace(defaults.StatusMessage))
        {
            viewModel.TemplateStatus = defaults.StatusMessage;
        }

        viewModel.StatusMessage = GetReadyStatusMessage();
        AdjustAutoHeightTextBox(BillToTextBox);
        AdjustAutoHeightTextBox(InvoiceNumberTextBox);

        if (!updateCheckStarted)
        {
            updateCheckStarted = true;
            _ = ScheduleStartupUpdateCheckAsync();
        }
    }

    private void AddCharge_Click(object sender, RoutedEventArgs e)
    {
        viewModel.Charges.Add(new ChargeItem());
    }

    private void RemoveCharge_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ChargeItem selected)
        {
            viewModel.Charges.Remove(selected);
        }
    }

    private async void DownloadInvoiceButton_Click(object sender, RoutedEventArgs e)
    {
        InvoiceArchiveResult? archive = null;

        try
        {
            viewModel.StatusMessage = "Saving PDF to Documents...";
            var html = InvoiceDocumentBuilder.BuildHtml(viewModel);
            archive = await InvoiceArchiveService.SaveInvoicePdfAsync(viewModel, html);
            viewModel.LastSavedDocumentPath = archive.FilePath;
            viewModel.StatusMessage = $"Invoice PDF downloaded to {archive.FilePath}.";
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = archive == null
                ? "Download failed."
                : $"Download failed after creating {archive.DirectoryPath}.";
            MessageBox.Show(
                this,
                ex.Message,
                "Download Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void PrintInvoiceButton_Click(object sender, RoutedEventArgs e)
    {
        viewModel.StatusMessage = "Opening print dialog...";

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            var html = InvoiceDocumentBuilder.BuildHtml(viewModel);
            await InvoiceBrowserPrintService.ShowPrintUiAsync(PrintWebView, html, timeout.Token);
            viewModel.StatusMessage = "Print dialog opened.";
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = "Print failed.";
            MessageBox.Show(
                this,
                ex.Message,
                "Print Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        await CheckForUpdatesAsync(showUpToDateStatus: true);
    }

    private void BillToTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            AdjustAutoHeightTextBox(textBox);
        }
    }

    private void BillToTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            AdjustAutoHeightTextBox(textBox);
        }
    }

    private void InvoiceNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            AdjustAutoHeightTextBox(textBox);
        }
    }

    private void InvoiceNumberTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            AdjustAutoHeightTextBox(textBox);
        }
    }

    private void ChargeDescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            AdjustAutoHeightTextBox(textBox);
        }
    }

    private void ChargeDescriptionTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            AdjustAutoHeightTextBox(textBox);
        }
    }

    private static void AdjustAutoHeightTextBox(TextBox textBox)
    {
        const double singleLineHeight = 38d;

        if (!textBox.IsLoaded)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(textBox.Text) || textBox.LineCount <= 1)
        {
            textBox.Height = singleLineHeight;
            return;
        }

        var width = textBox.ActualWidth;
        if (width <= 0)
        {
            return;
        }

        var padding = textBox.Padding;
        var border = textBox.BorderThickness;
        width -= padding.Left + padding.Right + border.Left + border.Right + 6;
        if (width <= 0)
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(textBox);
        var typeface = new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch);
        var text = string.IsNullOrWhiteSpace(textBox.Text) ? " " : textBox.Text + " ";

        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            textBox.FlowDirection,
            typeface,
            textBox.FontSize,
            Brushes.Black,
            dpi.PixelsPerDip)
        {
            MaxTextWidth = width
        };

        var height = formatted.Height + padding.Top + padding.Bottom + border.Top + border.Bottom + 6;
        if (!double.IsNaN(textBox.MaxHeight) && textBox.MaxHeight > 0)
        {
            height = Math.Min(height, textBox.MaxHeight);
        }

        height = Math.Max(height, singleLineHeight);
        textBox.Height = height;
    }

    private void ApplyCaptionTheme()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        TrySetDwmAttribute(handle, DwmwaUseImmersiveDarkModeLegacy, 1);
        TrySetDwmAttribute(handle, DwmwaUseImmersiveDarkMode, 1);
        TrySetDwmAttribute(handle, DwmwaCaptionColor, ToColorRef(Color.FromRgb(0x1F, 0x25, 0x30)));
        TrySetDwmAttribute(handle, DwmwaTextColor, ToColorRef(Color.FromRgb(0xF7, 0xF4, 0xEC)));
        TrySetDwmAttribute(handle, DwmwaBorderColor, ToColorRef(Color.FromRgb(0x8B, 0x5E, 0x1A)));
    }

    private static void TrySetDwmAttribute(IntPtr handle, int attribute, int value)
    {
        try
        {
            _ = DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private static int ToColorRef(Color color)
    {
        return color.R | (color.G << 8) | (color.B << 16);
    }

    private async Task ScheduleStartupUpdateCheckAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1.2));
            await Dispatcher.InvokeAsync(
                async () => await CheckForUpdatesAsync(showUpToDateStatus: false),
                DispatcherPriority.Background);
        }
        catch
        {
        }
    }

    private async Task CheckForUpdatesAsync(bool showUpToDateStatus)
    {
        AppUpdateInfo? update = null;

        try
        {
            using var checkTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            update = await UpdateService.CheckForUpdateAsync(checkTimeout.Token);
        }
        catch (Exception ex)
        {
            if (showUpToDateStatus)
            {
                viewModel.StatusMessage = $"Update check failed: {ex.Message}";
            }

            return;
        }

        if (update == null)
        {
            if (showUpToDateStatus)
            {
                var currentVersion = UpdateService.GetCurrentVersion().ToString(3);
                viewModel.StatusMessage = $"You're already on the latest version ({currentVersion}).";
            }

            return;
        }

        var prompt = BuildUpdatePrompt(update);
        var userChoice = MessageBox.Show(
            this,
            prompt,
            "Update Available",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (userChoice != MessageBoxResult.Yes)
        {
            viewModel.StatusMessage = $"Update {update.AvailableVersionDisplay} is available.";
            return;
        }

        try
        {
            viewModel.StatusMessage = $"Downloading update {update.AvailableVersionDisplay}...";
            using var downloadTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var packagePath = await UpdateService.DownloadPackageAsync(update, downloadTimeout.Token);

            if (!string.IsNullOrWhiteSpace(packagePath) &&
                UpdateService.TryLaunchInstalledUpdater(packagePath, AppContext.BaseDirectory, restartAfterUpdate: true))
            {
                viewModel.StatusMessage = $"Installing update {update.AvailableVersionDisplay}...";
                Application.Current.Shutdown();
                return;
            }

            if (UpdateService.TryOpenInstallerDownload(update))
            {
                viewModel.StatusMessage = $"Update {update.AvailableVersionDisplay} is ready to install.";
                return;
            }

            viewModel.StatusMessage = "Update download is ready, but the installer could not be opened.";
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Update failed: {ex.Message}";
        }
    }

    private string BuildUpdatePrompt(AppUpdateInfo update)
    {
        var lines = new List<string>
        {
            $"Version {update.AvailableVersionDisplay} is available.",
            $"Current version: {update.CurrentVersion.ToString(3)}."
        };

        if (update.PublishedAtUtc.HasValue)
        {
            lines.Add($"Published: {update.PublishedAtUtc.Value.LocalDateTime:MMMM d, yyyy h:mm tt}.");
        }

        if (!string.IsNullOrWhiteSpace(update.ReleaseNotes))
        {
            lines.Add(string.Empty);
            lines.Add(update.ReleaseNotes.Trim());
        }

        lines.Add(string.Empty);
        lines.Add("Install the update now?");
        return string.Join(Environment.NewLine, lines);
    }

    private static string GetReadyStatusMessage()
    {
        return $"Ready to download PDF invoices into {AppPaths.ArchiveRootDirectory}.";
    }
}
