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
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(660, 320);
        MinimumSize = new Size(660, 320);
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor = ColorTranslator.FromHtml("#F7F4EC");
        Padding = new Padding(24);

        var titleLabel = new Label
        {
            Text = "Install DJ Electric Invoice Generator",
            Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#1F2530"),
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8)
        };

        var helperLabel = new Label
        {
            Text = "Install the app locally and choose whether to place a shortcut on the desktop.",
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ForeColor = ColorTranslator.FromHtml("#5B6472"),
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 20)
        };

        var pathLabel = new Label
        {
            Text = "Install location",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = ColorTranslator.FromHtml("#5B6472"),
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 6)
        };

        installPathTextBox = new TextBox
        {
            Text = options.InstallDirectory,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0)
        };

        browseButton = CreateSecondaryButton("Browse");
        browseButton.Width = 112;
        browseButton.Dock = DockStyle.Fill;
        browseButton.Click += BrowseButton_Click;

        desktopShortcutCheckBox = new CheckBox
        {
            Text = "Create desktop shortcut",
            Checked = true,
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = ColorTranslator.FromHtml("#1F2530"),
            Margin = new Padding(0, 14, 0, 0)
        };

        statusLabel = new Label
        {
            Text = File.Exists(options.PackagePath)
                ? "Ready to install."
                : $"Place {InstallService.PackageFileName} next to this setup app before installing.",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = ColorTranslator.FromHtml("#8B5E1A"),
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 16, 0)
        };

        installButton = CreatePrimaryButton("Install");
        installButton.Width = 148;
        installButton.Height = 42;
        installButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        installButton.Click += InstallButton_Click;

        AcceptButton = installButton;

        var pathPanel = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pathPanel.Controls.Add(installPathTextBox, 0, 0);
        pathPanel.Controls.Add(browseButton, 1, 0);

        var footerPanel = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footerPanel.Controls.Add(statusLabel, 0, 0);
        footerPanel.Controls.Add(installButton, 1, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(helperLabel, 0, 1);
        layout.Controls.Add(pathLabel, 0, 2);
        layout.Controls.Add(pathPanel, 0, 3);
        layout.Controls.Add(desktopShortcutCheckBox, 0, 4);
        layout.Controls.Add(footerPanel, 0, 5);

        Controls.Add(layout);
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

    private static Button CreatePrimaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            BackColor = ColorTranslator.FromHtml("#1F2530"),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            Padding = new Padding(16, 0, 16, 0),
            Margin = new Padding(0)
        };

        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#1F2530");
        button.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#2B3240");
        button.FlatAppearance.MouseDownBackColor = ColorTranslator.FromHtml("#141922");
        return button;
    }

    private static Button CreateSecondaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            BackColor = ColorTranslator.FromHtml("#F2E4C1"),
            ForeColor = ColorTranslator.FromHtml("#1F2530"),
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Margin = new Padding(0)
        };

        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#CDA86A");
        button.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#EAD9AF");
        button.FlatAppearance.MouseDownBackColor = ColorTranslator.FromHtml("#E0CB97");
        return button;
    }
}
