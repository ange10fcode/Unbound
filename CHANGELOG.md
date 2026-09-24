# Changelog

All notable changes to Unbound are documented here.

## [1.2.0] - 2026-09-23

### Added

- Dedicated **Settings** window with persistent per-user behavior preferences.
- Toggle to confirm before Force delete automatically closes locking applications.
- Toggle to show or hide locking application names and PIDs in confirmations.
- Toggle to show or hide final unlock/delete statistics dialogs.
- Delete-after-reboot preference moved into Settings.

### Changed

- **Force delete now performs the full workflow automatically:** normal delete → detect/unlock locking apps → retry delete → optional delete-after-reboot.
- Removed the old “Try Unlock first” dead-end and the extra “unlock and delete again?” step.
- App installation and Explorer integration controls moved off the main screen and into Settings.
- Main window is smaller and focused only on the queue and the four primary actions.
- Explorer right-click **Force delete with Unbound** now uses the same automatic unlock-and-retry workflow as the main app.
- Operation statistics are aggregated across multi-item actions instead of showing a separate final dialog for every file.

### Safety

- Force termination still requires explicit confirmation when an application refuses to close normally.
- Protected Windows processes and Unbound's own running/installed files remain guarded.

## [1.1.0] - 2026-09-23

### Added

- One-click per-user **Install** button in the app.
- One-click **Uninstall** button with self-cleanup after the running app exits.
- Start menu shortcut for installed builds.
- Windows **Settings → Apps** uninstall registration under the current user.
- `--install` and `--uninstall` command-line actions.
- Explorer association refresh after adding or removing right-click commands.
- Self-deletion protection for the running executable and installed app folder.

### Changed

- Installed copy now lives at `%LOCALAPPDATA%\Programs\Unbound\Unbound.exe`.
- Adding Explorer integration from portable mode automatically creates the stable per-user installation.
- Settings UI now shows separate App installation and Explorer integration states.
- Legacy `%LOCALAPPDATA%\Unbound` installs are cleaned up when possible.

### Fixed

- Prevents the app from force-deleting its own folder and leaving stale Explorer context-menu entries behind.
- Delete-after-reboot no longer escapes the error handler when the selected path is protected by Unbound's self-deletion guard.

## [1.0.0] - 2026-09-23

### Added

- Initial public release.
- Minimal dark Windows interface with the Unbound application icon.
- Artifact-free native flat action buttons and a compact Explorer settings card.
- File and folder drag-and-drop queue with multi-selection and queue removal.
- Restart Manager based lock detection.
- Graceful-close-first unlock flow with explicit confirmation before force termination.
- Protected Windows process guardrail.
- Force deletion with read-only attribute clearing.
- Optional delete-on-reboot scheduling.
- Per-user Explorer context-menu integration for all files and folders.
- Stable `%LOCALAPPDATA%\Unbound\Unbound.exe` Explorer copy.
- One-click `Build-Unbound.cmd` local release builder.
- GitHub issue/PR templates plus automated build and release workflows.
