using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace DJ_Electric_Invoice_Generator.Services;

public static class InvoiceBrowserPrintService
{
    public static async Task ShowPrintUiAsync(WebView2 webView, string html, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webView);
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        await webView.EnsureCoreWebView2Async();
        ConfigureWebView(webView.CoreWebView2);
        await NavigateToHtmlAsync(webView, html, cancellationToken);
        await WaitForDocumentReadyAsync(webView, cancellationToken);
        webView.CoreWebView2.ShowPrintUI();
    }

    private static void ConfigureWebView(CoreWebView2 coreWebView)
    {
        var settings = coreWebView.Settings;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreDevToolsEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false;
        settings.IsStatusBarEnabled = false;
        settings.IsZoomControlEnabled = false;
    }

    private static Task NavigateToHtmlAsync(WebView2 webView, string html, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var completionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<CoreWebView2NavigationCompletedEventArgs>? handler = null;
        handler = (_, args) =>
        {
            webView.NavigationCompleted -= handler;

            if (args.IsSuccess)
            {
                completionSource.TrySetResult(null);
                return;
            }

            completionSource.TrySetException(
                new InvalidOperationException($"Unable to load the invoice for printing ({args.WebErrorStatus})."));
        };

        webView.NavigationCompleted += handler;
        webView.NavigateToString(html);
        return completionSource.Task;
    }

    private static async Task WaitForDocumentReadyAsync(WebView2 webView, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await webView.ExecuteScriptAsync(
            """
            (async () => {
                if (document.fonts && document.fonts.ready) {
                    await document.fonts.ready;
                }

                return document.readyState;
            })();
            """);

        await Task.Delay(150, cancellationToken);
    }
}
