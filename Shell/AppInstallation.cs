using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using Unbound.Core;

namespace Unbound.Shell;

public static class AppInstallation
{
    private const string UninstallKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Unbound";

    public static string InstallDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "Unbound");

    public static string InstalledExecutablePath =>
        Path.Combine(InstallDirectory, "Unbound.exe");

    public static string LegacyInstallDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Unbound");

    public static string StartMenuShortcutPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            "Unbound.lnk");

    public static bool IsInstalled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(UninstallKeyPath);
        return key is not null && File.Exists(InstalledExecutablePath);
    }

    public static bool IsCurrentProcessInstalled()
    {
        return PathsEqual(Application.ExecutablePath, InstalledExecutablePath);
    }

    public static string Install(bool installExplorerIntegration = true)
    {
        EnsurePublishedSingleFile();

        Directory.CreateDirectory(InstallDirectory);

        string currentExecutable = Path.GetFullPath(Application.ExecutablePath);
        string installedExecutable = Path.GetFullPath(InstalledExecutablePath);

        if (!PathsEqual(currentExecutable, installedExecutable))
        {
            try
            {
                File.Copy(currentExecutable, installedExecutable, overwrite: true);
            }
            catch (IOException ex)
            {
                throw new IOException(
                    "Unbound could not update the installed copy. Close other Unbound windows and try again.",
                    ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException(
                    "Windows blocked Unbound from writing to the per-user install folder.",
                    ex);
            }
        }

        CreateStartMenuShortcut();
        WriteUninstallEntry();

        if (installExplorerIntegration)
            ExplorerIntegration.Install(installedExecutable);

        TryRemoveLegacyInstallDirectory();
        return installedExecutable;
    }

    /// <summary>
    /// Removes registration immediately. Returns true when the current process is
    /// running from the installed folder and must exit so a temporary cleanup script
    /// can remove the executable itself.
    /// </summary>
    public static bool Uninstall()
    {
        ExplorerIntegration.Remove();
        RemoveStartMenuShortcut();
        RemoveUninstallEntry();

        bool currentInsideInstall = IsPathInsideDirectory(Application.ExecutablePath, InstallDirectory);
        bool currentInsideLegacy = IsPathInsideDirectory(Application.ExecutablePath, LegacyInstallDirectory);

        if (currentInsideInstall || currentInsideLegacy)
        {
            LaunchCleanupScript(
                Environment.ProcessId,
                InstallDirectory,
                LegacyInstallDirectory,
                SettingsStore.SettingsDirectory);

            return true;
        }

        TryDeleteDirectory(InstallDirectory);
        TryDeleteDirectory(LegacyInstallDirectory);
        TryDeleteDirectory(SettingsStore.SettingsDirectory);
        return false;
    }

    public static bool IsPathInsideInstallDirectory(string path)
    {
        return IsPathInsideDirectory(path, InstallDirectory) ||
               IsPathInsideDirectory(path, LegacyInstallDirectory);
    }

    private static void EnsurePublishedSingleFile()
    {
        string currentExecutable = Path.GetFullPath(Application.ExecutablePath);
        string siblingDll = Path.ChangeExtension(currentExecutable, ".dll");

        if (!File.Exists(siblingDll))
            return;

        throw new InvalidOperationException(
            "This looks like a development build. Build the self-contained single-file release first, then run that Unbound.exe and click Install.");
    }

    private static void WriteUninstallEntry()
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallKeyPath, writable: true)
            ?? throw new InvalidOperationException("Could not create the Windows uninstall entry.");

        string exe = InstalledExecutablePath;
        string quotedExe = $"\"{exe}\"";

        key.SetValue("DisplayName", "Unbound", RegistryValueKind.String);
        key.SetValue("DisplayVersion", Application.ProductVersion.Split('+')[0], RegistryValueKind.String);
        key.SetValue("DisplayIcon", $"{quotedExe},0", RegistryValueKind.String);
        key.SetValue("Publisher", "Unbound contributors", RegistryValueKind.String);
        key.SetValue("InstallLocation", InstallDirectory, RegistryValueKind.String);
        key.SetValue("UninstallString", $"{quotedExe} --uninstall", RegistryValueKind.String);
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    private static void RemoveUninstallEntry()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(UninstallKeyPath, throwOnMissingSubKey: false);
        }
        catch
        {
            // Best effort. The UI reports the remaining installation state after this call.
        }
    }

    private static void CreateStartMenuShortcut()
    {
        string? directory = Path.GetDirectoryName(StartMenuShortcutPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        Type shellLinkType = Type.GetTypeFromCLSID(
            new Guid("00021401-0000-0000-C000-000000000046"),
            throwOnError: true)!;

        object shellLinkObject = Activator.CreateInstance(shellLinkType)
            ?? throw new InvalidOperationException("Windows could not create a Start menu shortcut.");

        try
        {
            var link = (IShellLinkW)shellLinkObject;
            link.SetPath(InstalledExecutablePath);
            link.SetWorkingDirectory(InstallDirectory);
            link.SetDescription("Unbound file unlocker and force-delete utility");
            link.SetIconLocation(InstalledExecutablePath, 0);
            link.SetShowCmd(1);

            ((IPersistFile)link).Save(StartMenuShortcutPath, false);
        }
        finally
        {
            if (Marshal.IsComObject(shellLinkObject))
                Marshal.FinalReleaseComObject(shellLinkObject);
        }
    }

    private static void RemoveStartMenuShortcut()
    {
        try
        {
            if (File.Exists(StartMenuShortcutPath))
                File.Delete(StartMenuShortcutPath);
        }
        catch
        {
            // Best effort cleanup.
        }
    }

    private static void TryRemoveLegacyInstallDirectory()
    {
        if (IsPathInsideDirectory(Application.ExecutablePath, LegacyInstallDirectory))
            return;

        TryDeleteDirectory(LegacyInstallDirectory);
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // A stale directory is harmless. A later uninstall can retry cleanup.
        }
    }

    private static void LaunchCleanupScript(int processId, params string[] directories)
    {
        string scriptPath = Path.Combine(
            Path.GetTempPath(),
            $"Unbound-uninstall-{Guid.NewGuid():N}.cmd");

        var script = new StringBuilder();
        script.AppendLine("@echo off");
        script.AppendLine("setlocal EnableExtensions");
        script.AppendLine("set /a waitCount=0");
        script.AppendLine(":wait_for_unbound");
        script.AppendLine($"tasklist /FI \"PID eq {processId}\" 2>NUL | find \"{processId}\" >NUL");
        script.AppendLine("if errorlevel 1 goto remove_files");
        script.AppendLine("set /a waitCount+=1");
        script.AppendLine("if %waitCount% GEQ 30 goto remove_files");
        script.AppendLine(">NUL 2>&1 timeout /t 1 /nobreak");
        script.AppendLine("goto wait_for_unbound");
        script.AppendLine(":remove_files");

        for (int index = 0; index < directories.Length; index++)
        {
            string fullPath = Path.GetFullPath(directories[index]);
            string label = index.ToString();
            script.AppendLine("set /a removeCount=0");
            script.AppendLine($":retry_remove_{label}");
            script.AppendLine($"rmdir /s /q \"{fullPath}\" 2>NUL");
            script.AppendLine($"if not exist \"{fullPath}\" goto removed_{label}");
            script.AppendLine("set /a removeCount+=1");
            script.AppendLine($"if %removeCount% GEQ 10 goto removed_{label}");
            script.AppendLine(">NUL 2>&1 timeout /t 1 /nobreak");
            script.AppendLine($"goto retry_remove_{label}");
            script.AppendLine($":removed_{label}");
        }

        script.AppendLine("del /q \"%~f0\" >NUL 2>&1");

        File.WriteAllText(scriptPath, script.ToString(), Encoding.ASCII);

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/d /c \"\"{scriptPath}\"\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPathInsideDirectory(string path, string directory)
    {
        try
        {
            string fullPath = Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            string fullDirectory = Path.GetFullPath(directory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return fullPath.Equals(fullDirectory, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(fullDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}
