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
- Force-delete in one flow: try normal deletion, automatically detect and unlock locking apps, retry deletion, then optionally schedule removal after reboot.
- Ask applications to close normally before offering force termination.
- Refuse to automatically terminate protected Windows processes.
- Force-delete files, folders, and read-only items.
- Configure auto-unlock confirmation, locking-process details, final statistics, and delete-after-reboot behavior in **Settings**.
- Install Unbound for the current Windows user with one click — no administrator prompt required.
- Add a Start menu shortcut and a normal **Settings → Apps** uninstall entry.
- Add **Unlock with Unbound** and **Force delete with Unbound** to Explorer for **every file and folder**.
- Uninstall cleanly from inside the app or from Windows Settings.
- Refuse to delete its own running or installed folder; use the Uninstall button instead.
- Run without telemetry, accounts, network requests, or a background service.

## Download

For normal use, download the current Windows package from the repository's **Releases** page and run `Unbound.exe`.

You can keep using it as a portable app, or open **Settings → Windows integration → Install** to install it for your Windows account. The installed copy is placed in:

```text
%LOCALAPPDATA%\Programs\Unbound\Unbound.exe
```

Installation adds a Start menu shortcut, a Windows Apps uninstall entry, and Explorer right-click commands. No administrator rights are required.

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

## Install and uninstall

Open **Settings** from the main window. The **Windows integration** section provides one-click per-user installation/uninstall and Explorer menu controls.

Install adds:

- `%LOCALAPPDATA%\Programs\Unbound\Unbound.exe`
- a Start menu shortcut,
- a normal Windows **Settings → Apps** uninstall entry,
- Explorer right-click commands for files and folders.

Uninstall removes those items and safely cleans up the running executable after the app exits. Unbound also blocks force-deleting its own executable or installation folder so Explorer entries cannot be left behind accidentally.

Command-line equivalents:

```cmd
Unbound.exe --install
Unbound.exe --uninstall
```

## Explorer integration

Explorer integration can also be added or removed independently from **Settings**. If you add Explorer integration while running the portable build, Unbound installs a stable per-user copy automatically so the right-click entries do not break when the downloaded EXE moves.

The per-user registry entries are:

```text
HKCU\Software\Classes\*\shell\UnboundUnlock
HKCU\Software\Classes\*\shell\UnboundDelete
HKCU\Software\Classes\Directory\shell\UnboundUnlock
HKCU\Software\Classes\Directory\shell\UnboundDelete
```

On Windows 11, classic shell commands can appear under **Show more options**.

Explorer commands can also be managed from a terminal:

```cmd
Unbound.exe --install-menu
Unbound.exe --remove-menu
```

## Behavior settings

Unbound v1.2 keeps the main window focused on files and actions. Open **Settings** to configure:

- **Ask before automatic unlock during Force delete** — when enabled, Unbound asks before closing locking apps after a delete failure.
- **Show locking application names and PIDs** — controls whether process details are shown in confirmation dialogs.
- **Show operation summary when finished** — toggles the final statistics dialog. The main status bar still updates either way.
- **Schedule stubborn items for deletion after reboot** — used only when immediate deletion still fails after unlocking.

Force delete always starts with a normal deletion attempt. If Windows reports the item as locked, Unbound automatically runs the unlock workflow and retries the delete. There is no separate “Unlock first” step.

Settings are stored per Windows user in `%APPDATA%\Unbound\settings.json`.

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
