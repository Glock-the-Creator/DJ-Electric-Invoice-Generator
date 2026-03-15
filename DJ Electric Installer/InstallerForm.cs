using System.Diagnostics;
using System.Drawing;

namespace DJ_Electric_Invoice_Generator_Installer;

internal sealed class InstallerForm : Form
{
    private readonly InstallerOptions options;
    private readonly TextBox installPathTextBox;
    private readonly CheckBox desktopShortcutCheckBox;
    private readonly Label statusLabel;
    private readonly Button installButton;
    private readonly Button browseButton;

    public InstallerForm(InstallerOptions options)
    {
        this.options = options;

        Text = InstallService.ProductName;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(520, 250);
        MinimumSize = new Size(520, 250);
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = ColorTranslator.FromHtml("#F7F4EC");

        var titleLabel = new Label
        {
            Text = "Install DJ Electric Invoice Generator",
            Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#1F2530"),
            AutoSize = true,
            Location = new Point(24, 20)
        };

        var helperLabel = new Label
        {
            Text = "Install the app locally and choose whether to place a shortcut on the desktop.",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = ColorTranslator.FromHtml("#5B6472"),
            AutoSize = false,
            Size = new Size(460, 36),
            Location = new Point(24, 58)
        };

        var pathLabel = new Label
        {
            Text = "Install location",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = ColorTranslator.FromHtml("#5B6472"),
            AutoSize = true,
            Location = new Point(24, 102)
        };

        installPathTextBox = new TextBox
        {
            Text = options.InstallDirectory,
            Location = new Point(24, 124),
            Size = new Size(364, 27)
        };

        browseButton = new Button
        {
            Text = "Browse",
            Location = new Point(396, 122),
            Size = new Size(90, 30),
            BackColor = ColorTranslator.FromHtml("#F2E4C1"),
            FlatStyle = FlatStyle.Flat
        };
        browseButton.FlatAppearance.BorderSize = 0;
        browseButton.Click += BrowseButton_Click;

        desktopShortcutCheckBox = new CheckBox
        {
            Text = "Create desktop shortcut",
            Checked = true,
            AutoSize = true,
            Location = new Point(24, 166),
            ForeColor = ColorTranslator.FromHtml("#1F2530")
        };

        statusLabel = new Label
        {
            Text = File.Exists(options.PackagePath)
                ? "Ready to install."
                : $"Place {InstallService.PackageFileName} next to this setup app before installing.",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = ColorTranslator.FromHtml("#8B5E1A"),
            AutoSize = false,
            Size = new Size(460, 36),
            Location = new Point(24, 192)
        };

        installButton = new Button
        {
            Text = "Install",
            Location = new Point(376, 205),
            Size = new Size(110, 34),
            BackColor = ColorTranslator.FromHtml("#1F2530"),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        installButton.FlatAppearance.BorderSize = 0;
        installButton.Click += InstallButton_Click;

        Controls.Add(titleLabel);
        Controls.Add(helperLabel);
        Controls.Add(pathLabel);
        Controls.Add(installPathTextBox);
        Controls.Add(browseButton);
        Controls.Add(desktopShortcutCheckBox);
        Controls.Add(statusLabel);
        Controls.Add(installButton);
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = installPathTextBox.Text,
            Description = "Choose where DJ Electric Invoice Generator should be installed."
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            installPathTextBox.Text = dialog.SelectedPath;
        }
    }

    private async void InstallButton_Click(object? sender, EventArgs e)
    {
        installButton.Enabled = false;
        browseButton.Enabled = false;
        statusLabel.Text = "Installing...";

        try
        {
            var installPath = string.IsNullOrWhiteSpace(installPathTextBox.Text)
                ? InstallService.DefaultInstallDirectory
                : installPathTextBox.Text.Trim();

            var result = await Task.Run(() => InstallService.Install(options.PackagePath, installPath, desktopShortcutCheckBox.Checked));
            statusLabel.Text = $"Installed to {result.InstallDirectory}.";

            var openNow = MessageBox.Show(
                this,
                $"Installed successfully.{Environment.NewLine}{Environment.NewLine}Open the app now?",
                InstallService.ProductName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (openNow == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = result.MainExecutablePath,
                    WorkingDirectory = Path.GetDirectoryName(result.MainExecutablePath),
                    UseShellExecute = true
                });
            }

            Close();
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Install failed.";
            MessageBox.Show(
                this,
                ex.Message,
                "Install Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            installButton.Enabled = true;
            browseButton.Enabled = true;
        }
    }
}
