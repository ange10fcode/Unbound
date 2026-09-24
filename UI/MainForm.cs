using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Unbound.Core;
using Unbound.Shell;

namespace Unbound.UI;

public sealed class MainForm : Form
{
    private readonly ListBox _files = new();
    private readonly Label _status = new();
    private readonly Label _emptyState = new();
    private readonly Label _installStatus = new();
    private readonly Label _explorerStatus = new();
    private readonly CheckBox _rebootDelete = new();
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
    private static readonly Color Blue = Color.FromArgb(55, 120, 230);
    private static readonly Color BlueHover = Color.FromArgb(65, 133, 242);
    private static readonly Color Red = Color.FromArgb(211, 55, 67);
    private static readonly Color RedHover = Color.FromArgb(226, 67, 80);
    private static readonly Color Green = Color.FromArgb(88, 191, 126);

    public MainForm()
    {
        Text = "Unbound";
        ClientSize = new Size(840, 640);
        MinimumSize = new Size(740, 570);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Bg;
        ForeColor = TextMain;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        AllowDrop = true;
        KeyPreview = true;

        TryApplyAppIcon();
        BuildUi();
        SetupDragDrop(this);
        SetupDragDrop(_files);
        SetupDragDrop(_emptyState);

        Shown += (_, _) =>
        {
            Win11Style.Apply(Handle);
            RefreshSettingsState();
            UpdateQueueUi();
        };

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete)
            {
                RemoveSelectedFromQueue();
                e.Handled = true;
            }
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
            // The executable already contains the project icon.
        }
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(28, 24, 28, 22),
            BackColor = Bg
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 154));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildDropArea(), 0, 1);
        root.Controls.Add(BuildPrimaryActions(), 0, 2);
        root.Controls.Add(BuildSettingsCard(), 0, 3);
        root.Controls.Add(BuildHelperText(), 0, 4);
        root.Controls.Add(BuildFooter(), 0, 5);

        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg
        };

        var logo = new PictureBox
        {
            Size = new Size(46, 46),
            Location = new Point(0, 3),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };

        try
        {
            Icon? icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (icon is not null)
                logo.Image = icon.ToBitmap();
        }
        catch
        {
            // Header still works without a runtime icon extraction.
        }

        var title = new Label
        {
            Text = "UNBOUND",
            AutoSize = true,
            Location = new Point(59, -2),
            Font = new Font("Segoe UI Semibold", 21, FontStyle.Bold),
            ForeColor = TextMain
        };

        var subtitle = new Label
        {
            Text = "Unlock or permanently remove stubborn files and folders",
            AutoSize = true,
            Location = new Point(61, 37),
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = TextDim
        };

        header.Controls.Add(logo);
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        return header;
    }

    private Control BuildDropArea()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            BorderColor = Border,
            Radius = 12,
            Padding = new Padding(14),
            Margin = new Padding(0, 2, 0, 12)
        };

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface
        };

        _files.Dock = DockStyle.Fill;
        _files.BackColor = Surface;
        _files.ForeColor = TextMain;
        _files.BorderStyle = BorderStyle.None;
        _files.Font = new Font("Segoe UI", 10);
        _files.IntegralHeight = false;
        _files.SelectionMode = SelectionMode.MultiExtended;
        _files.HorizontalScrollbar = true;

        _files.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete)
            {
                RemoveSelectedFromQueue();
                e.Handled = true;
            }
        };

        var queueMenu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Font = new Font("Segoe UI", 9.5f)
        };

        queueMenu.Items.Add("Remove from queue", null, (_, _) => RemoveSelectedFromQueue());
        queueMenu.Items.Add("Clear queue", null, (_, _) =>
        {
            _files.Items.Clear();
            UpdateQueueUi();
        });
        _files.ContextMenuStrip = queueMenu;

        _emptyState.Text = "Drop files or folders here\n\nor click to browse files";
        _emptyState.Dock = DockStyle.Fill;
        _emptyState.TextAlign = ContentAlignment.MiddleCenter;
        _emptyState.ForeColor = TextDim;
        _emptyState.BackColor = Surface;
        _emptyState.Font = new Font("Segoe UI", 10.5f);
        _emptyState.Cursor = Cursors.Hand;
        _emptyState.Click += (_, _) => AddFiles();

        content.Controls.Add(_files);
        content.Controls.Add(_emptyState);
        _emptyState.BringToFront();

        card.Controls.Add(content);
        return card;
    }

    private Control BuildPrimaryActions()
    {
        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));

        Button addFile = MakeButton("Add file", SurfaceButton, SurfaceHover);
        Button addFolder = MakeButton("Add folder", SurfaceButton, SurfaceHover);
        Button remove = MakeButton("Remove", SurfaceButton, SurfaceHover);
        Button unlock = MakeButton("Unlock", Blue, BlueHover);
        Button delete = MakeButton("Force delete", Red, RedHover);

        addFile.Click += (_, _) => AddFiles();
        addFolder.Click += (_, _) => AddFolder();
        remove.Click += (_, _) => RemoveSelectedFromQueue();
        unlock.Click += (_, _) => UnlockSelected();
        delete.Click += (_, _) => DeleteSelected();

        actions.Controls.Add(addFile, 0, 0);
        actions.Controls.Add(addFolder, 1, 0);
        actions.Controls.Add(remove, 2, 0);
        actions.Controls.Add(unlock, 3, 0);
        actions.Controls.Add(delete, 4, 0);
        return actions;
    }

    private Control BuildSettingsCard()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            BorderColor = Border,
            Radius = 10,
            Padding = new Padding(14, 10, 14, 9),
            Margin = new Padding(0, 2, 0, 8)
        };

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
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 32));

        var installInfo = BuildSettingsInfo(
            "App installation",
            _installStatus);

        ConfigureButton(
            _installButton,
            "Install",
            SurfaceButton,
            SurfaceHover,
            margin: new Padding(10, 3, 0, 5));
        _installButton.Click += (_, _) => ToggleInstallation();

        var explorerInfo = BuildSettingsInfo(
            "Explorer right-click menu",
            _explorerStatus);

        ConfigureButton(
            _explorerButton,
            "Add",
            SurfaceButton,
            SurfaceHover,
            margin: new Padding(10, 3, 0, 5));
        _explorerButton.Click += (_, _) => ToggleExplorerMenu();

        _rebootDelete.Text = "Delete locked items after reboot if immediate deletion fails";
        _rebootDelete.AutoSize = true;
        _rebootDelete.Anchor = AnchorStyles.Left;
        _rebootDelete.BackColor = Surface;
        _rebootDelete.ForeColor = TextDim;
        _rebootDelete.Margin = new Padding(0, 3, 0, 0);
        _rebootDelete.Font = new Font("Segoe UI", 9f);

        layout.Controls.Add(installInfo, 0, 0);
        layout.Controls.Add(_installButton, 1, 0);
        layout.Controls.Add(explorerInfo, 0, 1);
        layout.Controls.Add(_explorerButton, 1, 1);
        layout.Controls.Add(_rebootDelete, 0, 2);
        layout.SetColumnSpan(_rebootDelete, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildSettingsInfo(string title, Label statusLabel)
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
            Location = new Point(0, 1),
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = TextMain
        };

        statusLabel.AutoSize = true;
        statusLabel.Location = new Point(0, 25);
        statusLabel.Font = new Font("Segoe UI", 8.7f);
        statusLabel.ForeColor = TextDim;

        panel.Controls.Add(titleLabel);
        panel.Controls.Add(statusLabel);
        return panel;
    }

    private Control BuildHelperText()
    {
        return new Label
        {
            Text = "Ctrl/Shift selects multiple  •  Delete removes items from the queue  •  Force delete bypasses the Recycle Bin",
            Dock = DockStyle.Fill,
            ForeColor = TextFaint,
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Margin = new Padding(0)
        };

        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _status.Text = "Ready";
        _status.ForeColor = TextDim;
        _status.Dock = DockStyle.Fill;
        _status.Font = new Font("Segoe UI", 8.6f);
        _status.TextAlign = ContentAlignment.MiddleLeft;

        var version = new Label
        {
            Text = $"Unbound {Application.ProductVersion}",
            ForeColor = TextFaint,
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Font = new Font("Segoe UI", 8.3f)
        };

        footer.Controls.Add(_status, 0, 0);
        footer.Controls.Add(version, 1, 0);
        return footer;
    }

    private static Button MakeButton(string text, Color color, Color hoverColor)
    {
        var button = new Button();
        ConfigureButton(button, text, color, hoverColor, new Padding(4, 8, 4, 8));
        return button;
    }

    private static void ConfigureButton(
        Button button,
        string text,
        Color color,
        Color hoverColor,
        Padding margin)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Margin = margin;
        button.BackColor = color;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI Semibold", 9.3f);
        button.Cursor = Cursors.Hand;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = hoverColor;
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(color, 0.06f);
        button.UseVisualStyleBackColor = false;
        button.TabStop = true;
    }

    private void AddFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Add files to Unbound"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        foreach (string path in dialog.FileNames)
            AddPath(path);
    }

    private void AddFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Add folder to Unbound"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            AddPath(dialog.SelectedPath);
    }

    private void AddPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        string normalized;
        try
        {
            normalized = Path.GetFullPath(path);
        }
        catch
        {
            return;
        }

        bool alreadyQueued = _files.Items
            .Cast<string>()
            .Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));

        if (!alreadyQueued)
            _files.Items.Add(normalized);

        UpdateQueueUi();
    }

    private void RemoveSelectedFromQueue()
    {
        if (_files.SelectedItems.Count == 0)
            return;

        object[] selected = _files.SelectedItems.Cast<object>().ToArray();
        foreach (object item in selected)
            _files.Items.Remove(item);

        UpdateQueueUi();
    }

    private void UpdateQueueUi()
    {
        _emptyState.Visible = _files.Items.Count == 0;
        _status.Text = _files.Items.Count == 0
            ? "Ready"
            : $"{_files.Items.Count} item(s) queued";
    }

    private void SetupDragDrop(Control control)
    {
        control.AllowDrop = true;

        control.DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        };

        control.DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] dropped)
                return;

            foreach (string path in dropped)
                AddPath(path);
        };
    }

    private void RefreshSettingsState()
    {
        RefreshInstallationState();
        RefreshExplorerState();
    }

    private void RefreshInstallationState()
    {
        bool installed = AppInstallation.IsInstalled();

        _installStatus.Text = installed
            ? "●  Installed — Start menu and Windows Apps entry are ready"
            : "Portable mode — install for Start menu access and clean uninstall";

        _installStatus.ForeColor = installed ? Green : TextDim;
        _installButton.Text = installed ? "Uninstall" : "Install";
        _installButton.BackColor = SurfaceButton;
        _installButton.FlatAppearance.MouseOverBackColor = SurfaceHover;
        _installButton.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(SurfaceButton, 0.06f);
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
                _status.Text = "Unbound uninstalled";
                RefreshSettingsState();

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
            _status.Text = "Unbound installed";
            RefreshSettingsState();

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
            MessageBox.Show(
                this,
                ex.Message,
                "Unbound",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void RefreshExplorerState()
    {
        bool installed = ExplorerIntegration.IsInstalled();

        _explorerStatus.Text = installed
            ? "●  Installed — available for every file and folder"
            : "Not installed — adds Unlock and Force delete to Explorer";

        _explorerStatus.ForeColor = installed ? Green : TextDim;
        _explorerButton.Text = installed ? "Remove" : "Add";
        _explorerButton.BackColor = SurfaceButton;
        _explorerButton.FlatAppearance.MouseOverBackColor = SurfaceHover;
        _explorerButton.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(SurfaceButton, 0.06f);
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
                _status.Text = "Explorer integration removed";
            }
            else
            {
                string installedPath = ExplorerIntegration.Install();
                _status.Text = "Explorer integration installed";

                MessageBox.Show(
                    this,
                    "Unbound is now available when you right-click any file or folder.\n\n" +
                    $"Explorer copy:\n{installedPath}\n\n" +
                    "On Windows 11, the commands can appear under “Show more options”.",
                    "Unbound",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            RefreshSettingsState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Unbound",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private List<string> GetActionTargets()
    {
        return (_files.SelectedItems.Count > 0
                ? _files.SelectedItems.Cast<string>()
                : _files.Items.Cast<string>())
            .ToList();
    }

    private void UnlockSelected()
    {
        List<string> selected = GetActionTargets();
        if (selected.Count == 0)
        {
            _status.Text = "Nothing selected";
            return;
        }

        foreach (string path in selected)
            Unlocker.UnlockWithUi(path, this);

        _status.Text = "Unlock operation finished";
    }

    private void DeleteSelected()
    {
        List<string> selected = GetActionTargets();
        if (selected.Count == 0)
        {
            _status.Text = "Nothing selected";
            return;
        }

        DialogResult answer = MessageBox.Show(
            this,
            $"Permanently delete {selected.Count} item(s)?\n\nThis bypasses the Recycle Bin and cannot be undone.",
            "Unbound",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (answer != DialogResult.Yes)
            return;

        int deleted = 0;
        int scheduled = 0;
        var failures = new List<(string Path, string Message)>();

        foreach (string path in selected)
        {
            try
            {
                FileTools.ForceDelete(path);
                _files.Items.Remove(path);
                deleted++;
            }
            catch (Exception ex)
            {
                bool scheduledForReboot = false;
                string failureMessage = ex.Message;

                if (_rebootDelete.Checked)
                {
                    try
                    {
                        scheduledForReboot = FileTools.ScheduleDelete(path);
                    }
                    catch (Exception scheduleEx)
                    {
                        failureMessage = scheduleEx.Message;
                    }
                }

                if (scheduledForReboot)
                {
                    _files.Items.Remove(path);
                    scheduled++;
                }
                else
                {
                    failures.Add((path, failureMessage));
                }
            }
        }

        UpdateQueueUi();
        _status.Text = $"Deleted {deleted}  •  Reboot {scheduled}  •  Failed {failures.Count}";

        if (failures.Count == 0)
            return;

        string details = string.Join(
            Environment.NewLine,
            failures.Take(6).Select(x => $"• {Path.GetFileName(x.Path)} — {x.Message}"));

        if (failures.Count > 6)
            details += $"{Environment.NewLine}• …and {failures.Count - 6} more";

        MessageBox.Show(
            this,
            $"Some items could not be deleted:\n\n{details}\n\nTry Unlock first, or enable delete-after-reboot.",
            "Unbound",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}
