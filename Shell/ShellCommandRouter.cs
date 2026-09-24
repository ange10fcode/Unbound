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
        foreach (string path in CleanPaths(paths))
            Unlocker.UnlockWithUi(path);
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
            $"Permanently delete {targets.Count} item(s)?\n\n{summary}\n\nThis cannot be undone.",
            "Unbound",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (answer != DialogResult.Yes)
            return;

        var failures = new List<(string Path, Exception Error)>();

        foreach (string path in targets)
        {
            try
            {
                FileTools.ForceDelete(path);
            }
            catch (Exception ex)
            {
                failures.Add((path, ex));
            }
        }

        if (failures.Count == 0)
            return;

        string failedText = string.Join(
            Environment.NewLine,
            failures.Take(6).Select(x => $"• {Path.GetFileName(x.Path)} — {x.Error.Message}"));

        DialogResult unlockAnswer = MessageBox.Show(
            $"{failures.Count} item(s) could not be deleted:\n\n{failedText}\n\nTry to unlock them and delete again?",
            "Unbound",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (unlockAnswer != DialogResult.Yes)
            return;

        int deletedAfterUnlock = 0;
        int stillFailed = 0;

        foreach ((string path, _) in failures)
        {
            Unlocker.UnlockWithUi(path);

            try
            {
                FileTools.ForceDelete(path);
                deletedAfterUnlock++;
            }
            catch
            {
                stillFailed++;
            }
        }

        MessageBox.Show(
            $"Retry finished.\n\nDeleted after unlock: {deletedAfterUnlock}\nStill failed: {stillFailed}",
            "Unbound",
            MessageBoxButtons.OK,
            stillFailed == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
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
