using System;
using System.Drawing;
using System.Windows.Forms;
using Unbound.Core;
using Unbound.Shell;

namespace Unbound.UI;

public sealed class SettingsForm : Form
{
    private readonly Label _installStatus = new();
    private readonly Label _explorerStatus = new();
    private readonly Button _installButton = new();
    private readonly Button _explorerButton = new();

    private static readonly Color Bg = Color.FromArgb(15, 17, 21);
    private static readonly Color Surface = Color.FromArgb(24, 27, 33);
    private static readonly Color SurfaceHover = Color.FromArgb(31, 35, 43);
    private static readonly Color SurfaceButton = Color.FromArgb(35, 39, 48);
    private static readonly Color Border = Color.FromArgb(49, 55, 66);
    private static readonly Color TextMain = Color.FromArgb(242, 244, 247);
    private static readonly Color TextDim = Color.FromArgb(151, 158, 173);
    private static readonly Color TextFaint = Color.FromArgb(98, 106, 120);
    private static readonly Color Green = Color.FromArgb(88, 191, 126);

    public SettingsForm()
    {
        Text = "Unbound settings";
        ClientSize = new Size(620, 610);
        MinimumSize = new Size(620, 610);
        MaximumSize = new Size(620, 610);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Bg;
        ForeColor = TextMain;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;

        TryApplyAppIcon();
        BuildUi();

        Shown += (_, _) =>
        {
            Win11Style.Apply(Handle);
            RefreshIntegrationState();
        };
    }

    private void TryApplyAppIcon()
    {
        try
        {
            Icon? icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (icon is not null)
                Icon = icon;
        }
        catch
        {
        }
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(26, 22, 26, 20),
            BackColor = Bg
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 258));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildBehaviorCard(), 0, 1);
        root.Controls.Add(BuildIntegrationCard(), 0, 2);
        root.Controls.Add(BuildSettingsPathLabel(), 0, 3);
        root.Controls.Add(BuildFooter(), 0, 4);

        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg
        };

        var title = new Label
        {
            Text = "Settings",
            AutoSize = true,
            Location = new Point(0, 0),
            Font = new Font("Segoe UI Semibold", 18, FontStyle.Bold),
            ForeColor = TextMain
        };

        var subtitle = new Label
        {
            Text = "Choose how Unbound behaves and manage Windows integration.",
            AutoSize = true,
            Location = new Point(2, 35),
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextDim
        };

        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        return panel;
    }

    private Control BuildBehaviorCard()
    {
        var card = MakeCard();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Surface,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        layout.Controls.Add(MakeSectionTitle("Behavior"), 0, 0);

        UserSettings settings = SettingsStore.Current;

        layout.Controls.Add(MakeOption(
            "Ask before automatic unlock during Force delete",
            "When off, Force delete immediately attempts to close locking apps after the delete confirmation.",
            settings.ConfirmAutomaticUnlock,
            value =>
            {
                SettingsStore.Current.ConfirmAutomaticUnlock = value;
                SettingsStore.Save();
            }), 0, 1);

        layout.Controls.Add(MakeOption(
            "Show locking application names and PIDs",
            "Include the detected apps in unlock and force-termination confirmations.",
            settings.ShowLockingApplications,
            value =>
            {
                SettingsStore.Current.ShowLockingApplications = value;
                SettingsStore.Save();
            }), 0, 2);

        layout.Controls.Add(MakeOption(
            "Show operation summary when finished",
            "Display the final delete/unlock statistics in a dialog. The status bar is always updated.",
            settings.ShowOperationSummary,
            value =>
            {
                SettingsStore.Current.ShowOperationSummary = value;
                SettingsStore.Save();
            }), 0, 3);

        layout.Controls.Add(MakeOption(
            "Schedule stubborn items for deletion after reboot",
            "If immediate delete still fails after unlocking, ask Windows to remove the item on the next reboot.",
            settings.DeleteAfterReboot,
            value =>
            {
                SettingsStore.Current.DeleteAfterReboot = value;
                SettingsStore.Save();
            }), 0, 4);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildIntegrationCard()
    {
        var card = MakeCard();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Surface,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        layout.Controls.Add(MakeSectionTitle("Windows integration"), 0, 0);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 2);

        layout.Controls.Add(BuildIntegrationInfo("App installation", _installStatus), 0, 1);
        ConfigureButton(_installButton, "Install");
        _installButton.Click += (_, _) => ToggleInstallation();
        layout.Controls.Add(_installButton, 1, 1);

        layout.Controls.Add(BuildIntegrationInfo("Explorer right-click menu", _explorerStatus), 0, 2);
        ConfigureButton(_explorerButton, "Add");
        _explorerButton.Click += (_, _) => ToggleExplorerMenu();
        layout.Controls.Add(_explorerButton, 1, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildSettingsPathLabel()
    {
        return new Label
        {
            Text = $"Settings are saved per Windows user in {SettingsStore.SettingsPath}",
            Dock = DockStyle.Fill,
            ForeColor = TextFaint,
            Font = new Font("Segoe UI", 8.2f),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Bg,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));

        var defaults = new Button
        {
            Text = "Restore defaults",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 8, 4)
        };
        StyleButton(defaults);
        defaults.Click += (_, _) => RestoreDefaults();

        var close = new Button
        {
            Text = "Close",
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 4, 0, 4),
            DialogResult = DialogResult.OK
        };
        StyleButton(close);
        close.Click += (_, _) => Close();

        footer.Controls.Add(defaults, 0, 0);
        footer.Controls.Add(close, 2, 0);
        return footer;
    }

    private RoundedPanel MakeCard()
    {
        return new RoundedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            BorderColor = Border,
            Radius = 10,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 4, 0, 10)
        };
    }

    private Label MakeSectionTitle(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = TextMain,
            Font = new Font("Segoe UI Semibold", 10.5f),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private Control MakeOption(
        string title,
        string description,
        bool initialValue,
        Action<bool> changed)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            Margin = new Padding(0)
        };

        var check = new CheckBox
        {
            Checked = initialValue,
            AutoSize = true,
            Location = new Point(0, 8),
            BackColor = Surface,
            ForeColor = TextMain,
            Text = title,
            Font = new Font("Segoe UI Semibold", 9.2f)
        };

        var help = new Label
        {
            Text = description,
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Location = new Point(22, 31),
            BackColor = Surface,
            ForeColor = TextDim,
            Font = new Font("Segoe UI", 8.3f)
        };

        check.CheckedChanged += (_, _) => changed(check.Checked);

        panel.Controls.Add(check);
        panel.Controls.Add(help);
        return panel;
    }

    private Control BuildIntegrationInfo(string title, Label statusLabel)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            Margin = new Padding(0)
        };

        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            Location = new Point(0, 4),
            Font = new Font("Segoe UI Semibold", 9.3f),
            ForeColor = TextMain
        };

        statusLabel.AutoSize = true;
        statusLabel.Location = new Point(0, 29);
        statusLabel.Font = new Font("Segoe UI", 8.3f);
        statusLabel.ForeColor = TextDim;

        panel.Controls.Add(titleLabel);
        panel.Controls.Add(statusLabel);
        return panel;
    }

    private void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(12, 9, 0, 9);
        StyleButton(button);
    }

    private void StyleButton(Button button)
    {
        button.BackColor = SurfaceButton;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI Semibold", 9f);
        button.Cursor = Cursors.Hand;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = SurfaceHover;
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(SurfaceButton, 0.06f);
        button.UseVisualStyleBackColor = false;

        // The footer buttons are mouse actions only. Removing focus cues prevents
        // the tiny accent/focus remnants that could appear beneath them.
        button.TabStop = false;
    }

    private void RefreshIntegrationState()
    {
        bool installed = AppInstallation.IsInstalled();
        _installStatus.Text = installed
            ? "●  Installed for this Windows user"
            : "Portable mode";
        _installStatus.ForeColor = installed ? Green : TextDim;
        _installButton.Text = installed ? "Uninstall" : "Install";

        bool explorerInstalled = ExplorerIntegration.IsInstalled();
        _explorerStatus.Text = explorerInstalled
            ? "●  Available for files and folders"
            : "Not installed";
        _explorerStatus.ForeColor = explorerInstalled ? Green : TextDim;
        _explorerButton.Text = explorerInstalled ? "Remove" : "Add";
    }

    private void ToggleInstallation()
    {
        try
        {
            if (AppInstallation.IsInstalled())
            {
                DialogResult result = MessageBox.Show(
                    this,
                    "Uninstall Unbound for this Windows user?\n\nThis removes the Start menu shortcut, Explorer commands, Windows Apps entry, and installed program files.",
                    "Unbound",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                bool exitRequired = AppInstallation.Uninstall();
                RefreshIntegrationState();

                MessageBox.Show(
                    this,
                    exitRequired
                        ? "Unbound has been uninstalled. The app will now close so Windows can remove the final program file."
                        : "Unbound has been uninstalled.",
                    "Unbound",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                if (exitRequired)
                    Application.Exit();

                return;
            }

            string installedPath = AppInstallation.Install();
            RefreshIntegrationState();

            MessageBox.Show(
                this,
                "Unbound is installed for your Windows account.\n\n" +
                $"Installed to:\n{installedPath}\n\n" +
                "A Start menu shortcut, Windows Apps uninstall entry, and Explorer right-click commands were added.",
                "Unbound",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Unbound", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ToggleExplorerMenu()
    {
        try
        {
            if (ExplorerIntegration.IsInstalled())
            {
                DialogResult result = MessageBox.Show(
                    this,
                    "Remove Unbound from the Explorer right-click menu?",
                    "Unbound",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                ExplorerIntegration.Remove();
            }
            else
            {
                string installedPath = ExplorerIntegration.Install();
                MessageBox.Show(
                    this,
                    "Unbound is now available when you right-click any file or folder.\n\n" +
                    $"Registered copy:\n{installedPath}\n\n" +
                    "On Windows 11, the commands can appear under ‘Show more options’.",
                    "Unbound",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            RefreshIntegrationState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Unbound", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RestoreDefaults()
    {
        DialogResult answer = MessageBox.Show(
            this,
            "Restore Unbound behavior settings to their defaults?",
            "Unbound",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes)
            return;

        SettingsStore.Reset();
        Close();

        MessageBox.Show(
            Owner,
            "Default settings restored. Reopen Settings to review them.",
            "Unbound",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
