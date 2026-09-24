using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Unbound.Core;

namespace Unbound.UI;

public sealed class MainForm : Form
{
    private readonly ListBox _files = new();
    private readonly Label _status = new();
    private readonly Label _emptyState = new();

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

    public MainForm()
    {
        Text = "Unbound";
        ClientSize = new Size(840, 520);
        MinimumSize = new Size(740, 470);
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
        }
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(28, 24, 28, 22),
            BackColor = Bg
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildDropArea(), 0, 1);
        root.Controls.Add(BuildPrimaryActions(), 0, 2);
        root.Controls.Add(BuildHelperText(), 0, 3);
        root.Controls.Add(BuildFooter(), 0, 4);

        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Bg,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        // Let the brand use all remaining room while reserving a stable, DPI-safe
        // area for the Settings button. Docking the button directly in this cell
        // avoids the clipped sliver that could appear with the old nested panel.
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));

        var brand = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            Margin = new Padding(0)
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

        brand.Controls.Add(logo);
        brand.Controls.Add(title);
        brand.Controls.Add(subtitle);

        var settings = new Button
        {
            Text = "Settings",
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 5, 0, 33),
            BackColor = SurfaceButton,
            ForeColor = TextMain,
            Font = new Font("Segoe UI Semibold", 9f),
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            TabStop = false
        };

        settings.FlatAppearance.BorderSize = 0;
        settings.FlatAppearance.MouseOverBackColor = SurfaceHover;
        settings.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(SurfaceButton, 0.06f);
        settings.Click += (_, _) =>
        {
            using var dialog = new SettingsForm();
            dialog.ShowDialog(this);
        };

        header.Controls.Add(brand, 0, 0);
        header.Controls.Add(settings, 1, 0);
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

    private Control BuildHelperText()
    {
        return new Label
        {
            Text = "Force delete automatically unlocks locked items, retries deletion, then uses reboot scheduling if enabled in Settings.",
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
            Text = $"Unbound {Application.ProductVersion.Split('+')[0]}",
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
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 8, 4, 8),
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.3f),
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            TabStop = true
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = hoverColor;
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(color, 0.06f);
        return button;
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

        UserSettings settings = SettingsStore.Current;
        var results = new List<UnlockResult>();

        foreach (string path in selected)
        {
            results.Add(Unlocker.Unlock(
                path,
                new UnlockOptions(
                    ConfirmBeforeClosing: true,
                    ShowProcessDetails: settings.ShowLockingApplications,
                    ShowSummary: false,
                    ShowNoLocksMessage: false,
                    DialogTitle: "Unlock with Unbound"),
                this));
        }

        int noLocks = results.Count(x => x.LockingProcesses == 0);
        int unlocked = results.Count(x => x.Success && x.LockingProcesses > 0);
        int cancelled = results.Count(x => x.UserCancelled);
        int failed = results.Count - noLocks - unlocked - cancelled;
        int graceful = results.Sum(x => x.GracefullyClosed);
        int forced = results.Sum(x => x.ForceClosed);
        int protectedCount = results.Sum(x => x.ProtectedProcesses);

        _status.Text = $"Unlocked {unlocked}  •  No locks {noLocks}  •  Failed {failed + cancelled}";

        if (settings.ShowOperationSummary)
        {
            MessageBox.Show(
                this,
                "Unlock finished.\n\n" +
                $"Unlocked items: {unlocked}\n" +
                $"No locks detected: {noLocks}\n" +
                $"Cancelled: {cancelled}\n" +
                $"Could not fully unlock: {failed}\n\n" +
                $"Apps closed normally: {graceful}\n" +
                $"Apps force closed: {forced}\n" +
                $"Protected processes: {protectedCount}",
                "Unbound",
                MessageBoxButtons.OK,
                failed + cancelled == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
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
            $"Permanently delete {selected.Count} item(s)?\n\n" +
            "Force delete automatically attempts to unlock items that Windows reports as in use.\n\n" +
            "This bypasses the Recycle Bin and cannot be undone.",
            "Unbound",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (answer != DialogResult.Yes)
            return;

        UserSettings settings = SettingsStore.Current;
        DeleteBatchResult result = DeleteCoordinator.Execute(selected, settings, this);

        foreach (string path in result.CompletedPaths.ToList())
            _files.Items.Remove(path);

        UpdateQueueUi();
        _status.Text = result.ToStatusLine();

        if (settings.ShowOperationSummary)
        {
            MessageBox.Show(
                this,
                result.ToSummaryText(),
                "Unbound",
                MessageBoxButtons.OK,
                result.Failed + result.Cancelled == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
    }
}
