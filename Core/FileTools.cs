using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Unbound.Core;

public static class FileTools
{
    private const int MoveFileDelayUntilReboot = 0x4;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(
        string lpExistingFileName,
        string? lpNewFileName,
        int dwFlags);

    public static void ForceDelete(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty.", nameof(path));

        path = Path.GetFullPath(path);
        DeletionGuard.ThrowIfProtected(path);

        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(path);
        }
        catch (FileNotFoundException)
        {
            return;
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }

        bool isDirectory = attributes.HasFlag(FileAttributes.Directory);
        bool isReparsePoint = attributes.HasFlag(FileAttributes.ReparsePoint);

        if (!isDirectory)
        {
            ClearAttributes(path);
            File.Delete(path);
            return;
        }

        if (isReparsePoint)
        {
            ClearAttributes(path);
            Directory.Delete(path, false);
            return;
        }

        DeleteDirectoryTree(path);
    }

    public static bool ScheduleDelete(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        path = Path.GetFullPath(path);
        DeletionGuard.ThrowIfProtected(path);

        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(path);
        }
        catch
        {
            return false;
        }

        if (!attributes.HasFlag(FileAttributes.Directory) ||
            attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            TryClearAttributes(path);
            return MoveFileEx(path, null, MoveFileDelayUntilReboot);
        }

        bool success = true;
        var directories = new List<string>();

        try
        {
            foreach (string entry in EnumerateTreeWithoutFollowingReparsePoints(path, directories))
            {
                TryClearAttributes(entry);
                if (!MoveFileEx(entry, null, MoveFileDelayUntilReboot))
                    success = false;
            }

            foreach (string directory in directories.OrderByDescending(x => x.Length))
            {
                TryClearAttributes(directory);
                if (!MoveFileEx(directory, null, MoveFileDelayUntilReboot))
                    success = false;
            }

            TryClearAttributes(path);
            if (!MoveFileEx(path, null, MoveFileDelayUntilReboot))
                success = false;
        }
        catch
        {
            return false;
        }

        return success;
    }

    private static void DeleteDirectoryTree(string directory)
    {
        foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
        {
            FileAttributes attributes = File.GetAttributes(entry);
            bool isDirectory = attributes.HasFlag(FileAttributes.Directory);
            bool isReparsePoint = attributes.HasFlag(FileAttributes.ReparsePoint);

            if (!isDirectory)
            {
                ClearAttributes(entry);
                File.Delete(entry);
                continue;
            }

            if (isReparsePoint)
            {
                ClearAttributes(entry);
                Directory.Delete(entry, false);
                continue;
            }

            DeleteDirectoryTree(entry);
        }

        ClearAttributes(directory);
        Directory.Delete(directory, false);
    }

    private static IEnumerable<string> EnumerateTreeWithoutFollowingReparsePoints(
        string root,
        List<string> directories)
    {
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            string current = stack.Pop();

            foreach (string entry in Directory.EnumerateFileSystemEntries(current))
            {
                FileAttributes attributes = File.GetAttributes(entry);
                bool isDirectory = attributes.HasFlag(FileAttributes.Directory);
                bool isReparsePoint = attributes.HasFlag(FileAttributes.ReparsePoint);

                if (!isDirectory || isReparsePoint)
                {
                    yield return entry;
                    continue;
                }

                directories.Add(entry);
                stack.Push(entry);
            }
        }
    }

    private static void ClearAttributes(string path)
    {
        File.SetAttributes(path, FileAttributes.Normal);
    }

    private static void TryClearAttributes(string path)
    {
        try
        {
            ClearAttributes(path);
        }
        catch
        {
            // Scheduling may still succeed even if an attribute cannot be changed.
        }
    }
}
