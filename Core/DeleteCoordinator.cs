using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Unbound.Core;

public enum DeleteOutcome
{
    DeletedDirectly,
    DeletedAfterUnlock,
    ScheduledForReboot,
    Failed,
    Cancelled
}

public sealed record DeleteItemResult(
    string Path,
    DeleteOutcome Outcome,
    string? ErrorMessage,
    UnlockResult? UnlockResult)
{
    public bool Completed =>
        Outcome is DeleteOutcome.DeletedDirectly or
            DeleteOutcome.DeletedAfterUnlock or
            DeleteOutcome.ScheduledForReboot;
}

public sealed class DeleteBatchResult
{
    public DeleteBatchResult(IReadOnlyList<DeleteItemResult> items)
    {
        Items = items;
    }

    public IReadOnlyList<DeleteItemResult> Items { get; }

    public int DeletedDirectly => Items.Count(x => x.Outcome == DeleteOutcome.DeletedDirectly);
    public int DeletedAfterUnlock => Items.Count(x => x.Outcome == DeleteOutcome.DeletedAfterUnlock);
    public int ScheduledForReboot => Items.Count(x => x.Outcome == DeleteOutcome.ScheduledForReboot);
    public int Failed => Items.Count(x => x.Outcome == DeleteOutcome.Failed);
    public int Cancelled => Items.Count(x => x.Outcome == DeleteOutcome.Cancelled);

    public int GracefullyClosed => Items.Sum(x => x.UnlockResult?.GracefullyClosed ?? 0);
    public int ForceClosed => Items.Sum(x => x.UnlockResult?.ForceClosed ?? 0);
    public int ProtectedProcesses => Items.Sum(x => x.UnlockResult?.ProtectedProcesses ?? 0);

    public IEnumerable<string> CompletedPaths =>
        Items.Where(x => x.Completed).Select(x => x.Path);

    public string ToStatusLine() =>
        $"Deleted {DeletedDirectly + DeletedAfterUnlock}  •  Reboot {ScheduledForReboot}  •  Failed {Failed + Cancelled}";

    public string ToSummaryText()
    {
        string text =
            "Force delete finished.\n\n" +
            $"Deleted immediately: {DeletedDirectly}\n" +
            $"Deleted after unlock: {DeletedAfterUnlock}\n" +
            $"Scheduled for reboot: {ScheduledForReboot}\n" +
            $"Failed: {Failed}\n" +
            $"Cancelled: {Cancelled}\n\n" +
            $"Apps closed normally: {GracefullyClosed}\n" +
            $"Apps force closed: {ForceClosed}\n" +
            $"Protected processes: {ProtectedProcesses}";

        List<DeleteItemResult> failures = Items
            .Where(x => x.Outcome is DeleteOutcome.Failed or DeleteOutcome.Cancelled)
            .Take(5)
            .ToList();

        if (failures.Count > 0)
        {
            text += "\n\nNot deleted:\n" + string.Join(
                Environment.NewLine,
                failures.Select(x => $"• {System.IO.Path.GetFileName(x.Path)} — {x.ErrorMessage ?? "Unknown error"}"));
        }

        return text;
    }
}

public static class DeleteCoordinator
{
    public static DeleteBatchResult Execute(
        IEnumerable<string> paths,
        UserSettings settings,
        IWin32Window? owner = null)
    {
        var results = new List<DeleteItemResult>();

        foreach (string path in paths)
            results.Add(DeleteOne(path, settings, owner));

        return new DeleteBatchResult(results);
    }

    private static DeleteItemResult DeleteOne(
        string path,
        UserSettings settings,
        IWin32Window? owner)
    {
        try
        {
            DeletionGuard.ThrowIfProtected(path);
        }
        catch (Exception guardError)
        {
            return new DeleteItemResult(
                path,
                DeleteOutcome.Failed,
                guardError.Message,
                null);
        }

        try
        {
            FileTools.ForceDelete(path);
            return new DeleteItemResult(path, DeleteOutcome.DeletedDirectly, null, null);
        }
        catch (Exception firstError)
        {
            UnlockResult unlockResult = Unlocker.Unlock(
                path,
                new UnlockOptions(
                    ConfirmBeforeClosing: settings.ConfirmAutomaticUnlock,
                    ShowProcessDetails: settings.ShowLockingApplications,
                    ShowSummary: false,
                    ShowNoLocksMessage: false,
                    DialogTitle: "Force delete with Unbound"),
                owner);

            if (unlockResult.UserCancelled)
            {
                if (settings.DeleteAfterReboot && TrySchedule(path))
                {
                    return new DeleteItemResult(
                        path,
                        DeleteOutcome.ScheduledForReboot,
                        null,
                        unlockResult);
                }

                return new DeleteItemResult(
                    path,
                    DeleteOutcome.Cancelled,
                    "Automatic unlock was cancelled.",
                    unlockResult);
            }

            try
            {
                FileTools.ForceDelete(path);
                return new DeleteItemResult(
                    path,
                    DeleteOutcome.DeletedAfterUnlock,
                    null,
                    unlockResult);
            }
            catch (Exception retryError)
            {
                if (settings.DeleteAfterReboot && TrySchedule(path))
                {
                    return new DeleteItemResult(
                        path,
                        DeleteOutcome.ScheduledForReboot,
                        null,
                        unlockResult);
                }

                string message = string.IsNullOrWhiteSpace(retryError.Message)
                    ? firstError.Message
                    : retryError.Message;

                return new DeleteItemResult(
                    path,
                    DeleteOutcome.Failed,
                    message,
                    unlockResult);
            }
        }
    }

    private static bool TrySchedule(string path)
    {
        try
        {
            return FileTools.ScheduleDelete(path);
        }
        catch
        {
            return false;
        }
    }
}
