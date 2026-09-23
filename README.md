<p align="center">
  <img src="Assets/Logo.png" width="112" alt="Unbound logo">
</p>

<h1 align="center">Unbound</h1>

<p align="center">
  A small Windows utility for finding file locks, unlocking files and folders, and permanently removing stubborn items.
</p>

## What it does

- Drag files and folders into a clean local queue.
- Detect locking processes with the Windows Restart Manager API.
- Ask applications to close normally before offering force termination.
- Refuse to automatically terminate protected Windows processes.
- Force-delete files, folders, and read-only items.
- Optionally schedule locked items for deletion after reboot.
- Add **Unlock with Unbound** and **Force delete with Unbound** to Explorer for **every file and folder**.
- Install Explorer integration per-user without machine-wide registry changes.
- Run without telemetry, accounts, network requests, or a background service.

## Download

For normal use, download the current Windows package from the repository's **Releases** page and run `Unbound.exe`.

Prebuilt releases are self-contained; the .NET runtime does not need to be installed separately.

> Windows SmartScreen may warn about new unsigned community builds. See the signing section below.

## Build the EXE yourself

Requirements:

- Windows 10 or Windows 11
- .NET 8 SDK

The easiest build is:

```text
Build-Unbound.cmd
```

Or from Command Prompt:

```cmd
scripts\publish.cmd win-x64
```

The executable is created at:

```text
dist\win-x64\Unbound.exe
```

ARM64:

```cmd
scripts\publish.cmd win-arm64
```

You can also use PowerShell:

```powershell
.\scripts\publish.ps1
```

## Explorer integration

The Settings section in Unbound can install or remove Explorer integration. When installed, the release executable is copied to:

```text
%LOCALAPPDATA%\Unbound\Unbound.exe
```

That stable copy keeps the right-click commands working if the original downloaded EXE is later moved.

The per-user registry entries are:

```text
HKCU\Software\Classes\*\shell\UnboundUnlock
HKCU\Software\Classes\*\shell\UnboundDelete
HKCU\Software\Classes\Directory\shell\UnboundUnlock
HKCU\Software\Classes\Directory\shell\UnboundDelete
```

On Windows 11, classic shell commands can appear under **Show more options**.

The commands can also be managed from a terminal:

```cmd
Unbound.exe --install-menu
Unbound.exe --remove-menu
```

## Safety

**Force delete is permanent and bypasses the Recycle Bin.**

Unlocking can close applications. Unbound first requests a normal close. If an application does not close, Unbound asks again before force-terminating it. Unsaved work in that application can be lost.

Unbound intentionally refuses to automatically terminate several critical Windows processes. Protected files can still require administrator privileges.

Directory deletion does not recursively follow junctions/reparse points into another directory tree.

## Privacy

Unbound has no analytics, telemetry, accounts, advertising, or network requests. The app runs locally.

## Development and publishing

- Contribution guide: [CONTRIBUTING.md](CONTRIBUTING.md)
- Security policy: [SECURITY.md](SECURITY.md)
- Changelog: [CHANGELOG.md](CHANGELOG.md)
- GitHub release guide: [PUBLISHING.md](PUBLISHING.md)
- Maintainer release notes: [RELEASING.md](RELEASING.md)

## Windows SmartScreen / code signing

This source repository does not contain a private signing certificate. Unsigned builds can trigger SmartScreen while the executable has little reputation. Never commit a private certificate or password; if signing is added later, use repository secrets or another secure signing system.

## License

Released under the [MIT License](LICENSE).
