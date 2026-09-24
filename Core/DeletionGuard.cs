using System;
using System.IO;
using System.Windows.Forms;
using Unbound.Shell;

namespace Unbound.Core;

internal static class DeletionGuard
{
    public static void ThrowIfProtected(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        string target = Path.GetFullPath(path);
        string currentExecutable = Path.GetFullPath(Application.ExecutablePath);
        string installedExecutable = Path.GetFullPath(AppInstallation.InstalledExecutablePath);

        if (PathsEqual(target, currentExecutable) ||
            PathsEqual(target, installedExecutable))
        {
            throw new InvalidOperationException(
                "Unbound cannot delete its own executable. Use the Uninstall button in Unbound instead.");
        }

        if (!Directory.Exists(target))
            return;

        if (IsInside(currentExecutable, target) ||
            IsInside(installedExecutable, target) ||
            PathsEqual(target, AppInstallation.InstallDirectory) ||
            PathsEqual(target, AppInstallation.LegacyInstallDirectory))
        {
            throw new InvalidOperationException(
                "Unbound will not delete a folder that contains the running or installed app. Use the Uninstall button instead so Explorer entries and app files are removed safely.");
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInside(string path, string directory)
    {
        string fullPath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullDirectory = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return fullPath.Equals(fullDirectory, StringComparison.OrdinalIgnoreCase) ||
               fullPath.StartsWith(fullDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
