using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Unbound.Core;

namespace Unbound.Shell;

public static class ShellCommandRouter
{
    public static bool TryHandle(string[] args)
    {
        if (args.Length == 0)
            return false;

        string command = args[0].Trim().ToLowerInvariant();

        switch (command)
        {
            case "--unlock":
                HandleUnlock(args.Skip(1));
                return true;

            case "--delete":
                HandleDelete(args.Skip(1));
                return true;

            case "--install":
                HandleInstall();
                return true;

            case "--uninstall":
                HandleUninstall();
                return true;

            case "--install-menu":
                HandleInstallMenu();
                return true;

            case "--remove-menu":
                ExplorerIntegration.Remove();
                return true;

            default:
                return false;
        }
    }

    private static void HandleUnlock(IEnumerable<string> paths)
    {
        List<string> targets = CleanPaths(paths).ToList();
        if (targets.Count == 0)
            return;

        UserSettings settings = SettingsStore.Current;
        var results = new List<UnlockResult>();

        foreach (string path in targets)
        {
            results.Add(Unlocker.Unlock(
                path,
                new UnlockOptions(
                    ConfirmBeforeClosing: true,
                    ShowProcessDetails: settings.ShowLockingApplications,
                    ShowSummary: false,
                    ShowNoLocksMessage: false,
                    DialogTitle: "Unlock with Unbound")));
        }

        if (!settings.ShowOperationSummary)
            return;

        int noLocks = results.Count(x => x.LockingProcesses == 0);
        int unlocked = results.Count(x => x.Success && x.LockingProcesses > 0);
        int cancelled = results.Count(x => x.UserCancelled);
        int failed = results.Count - noLocks - unlocked - cancelled;
        int graceful = results.Sum(x => x.GracefullyClosed);
        int forced = results.Sum(x => x.ForceClosed);
        int protectedCount = results.Sum(x => x.ProtectedProcesses);

        MessageBox.Show(
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

    private static void HandleDelete(IEnumerable<string> paths)
    {
        List<string> targets = CleanPaths(paths).ToList();
        if (targets.Count == 0)
            return;

        string summary = targets.Count == 1
            ? targets[0]
            : string.Join(Environment.NewLine, targets.Take(8).Select(x => $"• {x}")) +
              (targets.Count > 8 ? $"\n• …and {targets.Count - 8} more" : string.Empty);

        DialogResult answer = MessageBox.Show(
            $"Permanently delete {targets.Count} item(s)?\n\n{summary}\n\n" +
            "Force delete automatically attempts to unlock items that Windows reports as in use.\n\n" +
            "This cannot be undone.",
            "Unbound",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (answer != DialogResult.Yes)
            return;

        UserSettings settings = SettingsStore.Current;
        DeleteBatchResult result = DeleteCoordinator.Execute(targets, settings);

        if (settings.ShowOperationSummary)
        {
            MessageBox.Show(
                result.ToSummaryText(),
                "Unbound",
                MessageBoxButtons.OK,
                result.Failed + result.Cancelled == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
    }

    private static void HandleInstall()
    {
        try
        {
            string installedPath = AppInstallation.Install();
            MessageBox.Show(
                $"Unbound installed successfully.\n\nInstalled to:\n{installedPath}\n\nA Start menu shortcut and Explorer commands were added.",
                "Unbound",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Unbound",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void HandleUninstall()
    {
        DialogResult answer = MessageBox.Show(
            "Uninstall Unbound for this Windows user?\n\nThis removes the Start menu shortcut, Explorer right-click commands, Apps entry, and installed program files.",
            "Unbound",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes)
            return;

        try
        {
            bool exitRequired = AppInstallation.Uninstall();

            MessageBox.Show(
                exitRequired
                    ? "Unbound has been uninstalled. This window will now close so Windows can remove the final program file."
                    : "Unbound has been uninstalled.",
                "Unbound",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Unbound",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void HandleInstallMenu()
    {
        try
        {
            string installedPath = ExplorerIntegration.Install();
            MessageBox.Show(
                $"Explorer integration installed.\n\nRegistered copy:\n{installedPath}",
                "Unbound",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Unbound",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static IEnumerable<string> CleanPaths(IEnumerable<string> paths)
    {
        return paths
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().Trim('"'))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
