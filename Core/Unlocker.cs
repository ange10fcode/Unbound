using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Unbound.Core;

public sealed record UnlockOptions(
    bool ConfirmBeforeClosing,
    bool ShowProcessDetails,
    bool ShowSummary,
    bool ShowNoLocksMessage,
    string DialogTitle);

public sealed record UnlockResult(
    bool Success,
    bool UserCancelled,
    int LockingProcesses,
    int GracefullyClosed,
    int ForceClosed,
    int Failed,
    int ProtectedProcesses)
{
    public int ClosedProcesses => GracefullyClosed + ForceClosed;
}

public static class Unlocker
{
    private const int ErrorMoreData = 234;
    private const int MaxDirectoryResources = 1024;

    private static readonly HashSet<string> ProtectedProcessNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "System",
            "Registry",
            "smss",
            "csrss",
            "wininit",
            "winlogon",
            "services",
            "lsass",
            "fontdrvhost",
            "svchost",
            "dwm",
            "explorer",
            "MsMpEng",
            "SearchIndexer",
            "SearchHost"
        };

    [StructLayout(LayoutKind.Sequential)]
    private struct RmUniqueProcess
    {
        public int ProcessId;
        public FileTime ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RmProcessInfo
    {
        public RmUniqueProcess Process;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string AppName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string ServiceShortName;

        public uint ApplicationType;
        public uint AppStatus;
        public uint SessionId;

        [MarshalAs(UnmanagedType.Bool)]
        public bool Restartable;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;

        public long ToLong() => ((long)HighDateTime << 32) | LowDateTime;
    }

    private sealed record LockingProcessInfo(int ProcessId, long StartTimeFileTime, string DisplayName);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(
        out uint sessionHandle,
        int sessionFlags,
        StringBuilder sessionKey);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint sessionHandle);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(
        uint sessionHandle,
        uint fileCount,
        string[] fileNames,
        uint applicationCount,
        IntPtr applications,
        uint serviceCount,
        string[]? serviceNames);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(
        uint sessionHandle,
        out uint processInfoNeeded,
        ref uint processInfoCount,
        [In, Out] RmProcessInfo[]? affectedApps,
        ref uint rebootReasons);

    public static UnlockResult UnlockWithUi(string path, IWin32Window? owner = null)
    {
        UserSettings settings = SettingsStore.Current;
        return Unlock(
            path,
            new UnlockOptions(
                ConfirmBeforeClosing: true,
                ShowProcessDetails: settings.ShowLockingApplications,
                ShowSummary: settings.ShowOperationSummary,
                ShowNoLocksMessage: true,
                DialogTitle: "Unlock with Unbound"),
            owner);
    }

    public static UnlockResult Unlock(
        string path,
        UnlockOptions options,
        IWin32Window? owner = null)
    {
        List<LockingProcessInfo> lockingProcesses = FindLockingProcesses(path);

        if (lockingProcesses.Count == 0)
        {
            if (options.ShowSummary && options.ShowNoLocksMessage)
            {
                MessageBox.Show(
                    owner,
                    $"No locking application was found.\n\n{path}",
                    "Unbound",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return new UnlockResult(
                Success: true,
                UserCancelled: false,
                LockingProcesses: 0,
                GracefullyClosed: 0,
                ForceClosed: 0,
                Failed: 0,
                ProtectedProcesses: 0);
        }

        var protectedProcesses = new List<LockingProcessInfo>();
        var closableProcesses = new List<LockingProcessInfo>();

        foreach (LockingProcessInfo info in lockingProcesses)
        {
            string processName = TryGetProcessName(info.ProcessId) ?? info.DisplayName;
            if (ProtectedProcessNames.Contains(processName))
                protectedProcesses.Add(info);
            else
                closableProcesses.Add(info);
        }

        if (closableProcesses.Count == 0)
        {
            if (options.ShowSummary)
            {
                MessageBox.Show(
                    owner,
                    "The item is locked only by protected Windows processes. " +
                    "Unbound will not terminate those processes automatically.",
                    "Unbound",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return new UnlockResult(
                Success: false,
                UserCancelled: false,
                LockingProcesses: lockingProcesses.Count,
                GracefullyClosed: 0,
                ForceClosed: 0,
                Failed: 0,
                ProtectedProcesses: protectedProcesses.Count);
        }

        if (options.ConfirmBeforeClosing)
        {
            string processDescription = options.ShowProcessDetails
                ? string.Join(Environment.NewLine, closableProcesses.Select(p => $"• {FormatProcess(p)}"))
                : $"{closableProcesses.Count} application(s) are using this item.";

            string protectedNote = protectedProcesses.Count == 0
                ? string.Empty
                : $"\n\nProtected Windows processes detected: {protectedProcesses.Count}. " +
                  "They will not be terminated.";

            DialogResult approval = MessageBox.Show(
                owner,
                processDescription +
                "\n\nUnbound will first ask the locking application(s) to close normally." +
                protectedNote,
                options.DialogTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (approval != DialogResult.Yes)
            {
                return new UnlockResult(
                    Success: false,
                    UserCancelled: true,
                    LockingProcesses: lockingProcesses.Count,
                    GracefullyClosed: 0,
                    ForceClosed: 0,
                    Failed: closableProcesses.Count,
                    ProtectedProcesses: protectedProcesses.Count);
            }
        }

        var stillRunning = new List<LockingProcessInfo>();
        int gracefullyClosed = 0;

        foreach (LockingProcessInfo info in closableProcesses)
        {
            Process? process = TryOpenMatchingProcess(info);
            if (process is null)
                continue;

            using (process)
            {
                try
                {
                    if (process.HasExited)
                        continue;

                    bool closeRequested = process.CloseMainWindow();
                    if (closeRequested && process.WaitForExit(2500))
                    {
                        gracefullyClosed++;
                        continue;
                    }

                    if (!process.HasExited)
                        stillRunning.Add(info);
                }
                catch
                {
                    stillRunning.Add(info);
                }
            }
        }

        int forceClosed = 0;
        int failed = 0;

        if (stillRunning.Count > 0)
        {
            string forceDescription = options.ShowProcessDetails
                ? string.Join(Environment.NewLine, stillRunning.Select(p => $"• {FormatProcess(p)}"))
                : $"{stillRunning.Count} application(s) did not close normally.";

            DialogResult forceApproval = MessageBox.Show(
                owner,
                forceDescription +
                "\n\nForce terminate the remaining application(s)? Unsaved work may be lost.",
                "Force unlock",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (forceApproval == DialogResult.Yes)
            {
                foreach (LockingProcessInfo info in stillRunning)
                {
                    Process? process = TryOpenMatchingProcess(info);
                    if (process is null)
                        continue;

                    using (process)
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill(entireProcessTree: true);
                                process.WaitForExit(3000);
                            }

                            forceClosed++;
                        }
                        catch
                        {
                            failed++;
                        }
                    }
                }
            }
            else
            {
                failed += stillRunning.Count;
            }
        }

        bool success = failed == 0 && protectedProcesses.Count == 0;
        var result = new UnlockResult(
            Success: success,
            UserCancelled: false,
            LockingProcesses: lockingProcesses.Count,
            GracefullyClosed: gracefullyClosed,
            ForceClosed: forceClosed,
            Failed: failed,
            ProtectedProcesses: protectedProcesses.Count);

        if (options.ShowSummary)
        {
            MessageBox.Show(
                owner,
                BuildSummary(result),
                "Unbound",
                MessageBoxButtons.OK,
                success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        return result;
    }

    public static string BuildSummary(UnlockResult result)
    {
        return
            "Unlock finished.\n\n" +
            $"Locking apps detected: {result.LockingProcesses}\n" +
            $"Closed normally: {result.GracefullyClosed}\n" +
            $"Force closed: {result.ForceClosed}\n" +
            $"Could not close: {result.Failed}\n" +
            $"Protected: {result.ProtectedProcesses}";
    }

    private static List<LockingProcessInfo> FindLockingProcesses(string path)
    {
        string[] resources = BuildResourceList(path).ToArray();
        if (resources.Length == 0)
            return new List<LockingProcessInfo>();

        int startResult = RmStartSession(out uint sessionHandle, 0, new StringBuilder(64));
        if (startResult != 0)
            return new List<LockingProcessInfo>();

        try
        {
            int registerResult = RmRegisterResources(
                sessionHandle,
                (uint)resources.Length,
                resources,
                0,
                IntPtr.Zero,
                0,
                null);

            if (registerResult != 0)
                return new List<LockingProcessInfo>();

            uint needed = 0;
            uint count = 0;
            uint rebootReasons = 0;

            int listResult = RmGetList(
                sessionHandle,
                out needed,
                ref count,
                null,
                ref rebootReasons);

            if (listResult != ErrorMoreData || needed == 0)
                return new List<LockingProcessInfo>();

            count = needed;
            var info = new RmProcessInfo[(int)count];

            listResult = RmGetList(
                sessionHandle,
                out needed,
                ref count,
                info,
                ref rebootReasons);

            if (listResult != 0)
                return new List<LockingProcessInfo>();

            return info
                .Take((int)count)
                .Select(x => new LockingProcessInfo(
                    x.Process.ProcessId,
                    x.Process.ProcessStartTime.ToLong(),
                    string.IsNullOrWhiteSpace(x.AppName) ? $"PID {x.Process.ProcessId}" : x.AppName))
                .Where(x => x.ProcessId != Environment.ProcessId)
                .GroupBy(x => x.ProcessId)
                .Select(g => g.First())
                .ToList();
        }
        finally
        {
            RmEndSession(sessionHandle);
        }
    }

    private static IEnumerable<string> BuildResourceList(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            yield break;

        path = Path.GetFullPath(path);

        if (File.Exists(path))
        {
            yield return path;
            yield break;
        }

        if (!Directory.Exists(path))
            yield break;

        int count = 0;
        var stack = new Stack<string>();
        stack.Push(path);

        while (stack.Count > 0 && count < MaxDirectoryResources)
        {
            string current = stack.Pop();

            IEnumerable<string> entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(current);
            }
            catch
            {
                continue;
            }

            foreach (string entry in entries)
            {
                if (count >= MaxDirectoryResources)
                    yield break;

                FileAttributes attributes;
                try
                {
                    attributes = File.GetAttributes(entry);
                }
                catch
                {
                    continue;
                }

                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    if (!attributes.HasFlag(FileAttributes.ReparsePoint))
                        stack.Push(entry);
                    continue;
                }

                yield return entry;
                count++;
            }
        }
    }

    private static Process? TryOpenMatchingProcess(LockingProcessInfo info)
    {
        try
        {
            Process process = Process.GetProcessById(info.ProcessId);

            long currentStart = process.StartTime.ToUniversalTime().ToFileTimeUtc();
            if (currentStart != info.StartTimeFileTime)
            {
                process.Dispose();
                return null;
            }

            return process;
        }
        catch
        {
            return null;
        }
    }

    private static string FormatProcess(LockingProcessInfo info)
    {
        string name = TryGetProcessName(info.ProcessId) ?? info.DisplayName;
        if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            name += ".exe";
        return $"{name}  (PID {info.ProcessId})";
    }

    private static string? TryGetProcessName(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }
}
