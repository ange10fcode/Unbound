using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Unbound.Shell;

public static class ExplorerIntegration
{
    private const string FileBase = @"Software\Classes\*\shell";
    private const string FolderBase = @"Software\Classes\Directory\shell";

    private static string StableInstallDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Unbound");

    private static string StableExecutablePath =>
        Path.Combine(StableInstallDirectory, "Unbound.exe");

    public static bool IsInstalled()
    {
        using RegistryKey? fileUnlock = Registry.CurrentUser.OpenSubKey($@"{FileBase}\UnboundUnlock");
        using RegistryKey? fileDelete = Registry.CurrentUser.OpenSubKey($@"{FileBase}\UnboundDelete");
        using RegistryKey? folderUnlock = Registry.CurrentUser.OpenSubKey($@"{FolderBase}\UnboundUnlock");
        using RegistryKey? folderDelete = Registry.CurrentUser.OpenSubKey($@"{FolderBase}\UnboundDelete");

        return fileUnlock is not null &&
               fileDelete is not null &&
               folderUnlock is not null &&
               folderDelete is not null;
    }

    public static string Install()
    {
        RemoveLegacyKeys();

        string registeredExecutable = EnsureStableExecutable();

        AddMenu(
            $@"{FileBase}\UnboundUnlock",
            "Unlock with Unbound",
            $"\"{registeredExecutable}\" --unlock \"%1\"",
            registeredExecutable);

        AddMenu(
            $@"{FileBase}\UnboundDelete",
            "Force delete with Unbound",
            $"\"{registeredExecutable}\" --delete \"%1\"",
            registeredExecutable);

        AddMenu(
            $@"{FolderBase}\UnboundUnlock",
            "Unlock with Unbound",
            $"\"{registeredExecutable}\" --unlock \"%1\"",
            registeredExecutable);

        AddMenu(
            $@"{FolderBase}\UnboundDelete",
            "Force delete with Unbound",
            $"\"{registeredExecutable}\" --delete \"%1\"",
            registeredExecutable);

        return registeredExecutable;
    }

    public static void Remove()
    {
        DeleteTree($@"{FileBase}\UnboundUnlock");
        DeleteTree($@"{FileBase}\UnboundDelete");
        DeleteTree($@"{FolderBase}\UnboundUnlock");
        DeleteTree($@"{FolderBase}\UnboundDelete");
        RemoveLegacyKeys();
    }

    private static string EnsureStableExecutable()
    {
        string currentExecutable = Application.ExecutablePath;
        string currentFullPath = Path.GetFullPath(currentExecutable);
        string stableFullPath = Path.GetFullPath(StableExecutablePath);

        if (string.Equals(currentFullPath, stableFullPath, StringComparison.OrdinalIgnoreCase))
            return stableFullPath;

        // Framework-dependent/dev builds need their adjacent DLLs, so do not copy
        // only the apphost executable. Published single-file releases have no sibling
        // Unbound.dll and can safely be copied to a stable Explorer location.
        string siblingDll = Path.ChangeExtension(currentFullPath, ".dll");
        if (File.Exists(siblingDll))
            return currentFullPath;

        Directory.CreateDirectory(StableInstallDirectory);

        try
        {
            File.Copy(currentFullPath, stableFullPath, overwrite: true);
        }
        catch (IOException ex)
        {
            throw new IOException(
                "Unbound could not update its Explorer copy. Close any other Unbound windows or Explorer actions and try again.",
                ex);
        }

        return stableFullPath;
    }

    private static void AddMenu(
        string keyPath,
        string title,
        string command,
        string iconPath)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true)
            ?? throw new InvalidOperationException($"Could not create registry key: {keyPath}");

        key.SetValue(string.Empty, title, RegistryValueKind.String);
        key.SetValue("Icon", $"\"{iconPath}\",0", RegistryValueKind.String);
        key.SetValue("Position", "Top", RegistryValueKind.String);
        key.SetValue("MultiSelectModel", "Player", RegistryValueKind.String);

        using RegistryKey commandKey = key.CreateSubKey("command", writable: true)
            ?? throw new InvalidOperationException($"Could not create command key: {keyPath}\\command");

        commandKey.SetValue(string.Empty, command, RegistryValueKind.String);
    }

    private static void RemoveLegacyKeys()
    {
        string[] legacyKeys =
        {
            @"Software\Classes\*\shell\MiniForceDelete",
            @"Software\Classes\*\shell\MiniForceUnlock",
            @"Software\Classes\Directory\shell\MiniForceDelete",
            @"Software\Classes\Directory\shell\MiniForceUnlock"
        };

        foreach (string key in legacyKeys)
            DeleteTree(key);
    }

    private static void DeleteTree(string path)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
        }
        catch
        {
            // Best-effort cleanup. The caller can reinstall if a stale key remains.
        }
    }
}
