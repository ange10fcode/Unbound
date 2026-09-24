# Publishing Unbound on GitHub

This repository is already laid out as a public open-source project: source code, MIT license, README, changelog, contributing/security docs, issue templates, pull-request template, and Windows build/release workflows are included.

## 1. Test a local release build

On Windows, install the **.NET 8 SDK**, then double-click:

```text
Build-Unbound.cmd
```

or run:

```cmd
scripts\publish.cmd win-x64
```

The main executable will be:

```text
dist\win-x64\Unbound.exe
```

Before publishing, test at least:

- adding a file and a folder,
- drag and drop,
- Unlock on a file you intentionally have open in another non-critical app,
- Force delete on a disposable test file/folder,
- one-click app installation and uninstall,
- Explorer installation and removal,
- right-click commands on both a file and a folder.

## 2. Create the GitHub repository

On GitHub, create a new repository named `Unbound`.

Recommended settings:

- Visibility: **Public** if you want it open source.
- Do **not** ask GitHub to generate a README, `.gitignore`, or license; this project already contains them.

## 3. Push the source

Open Terminal/Git Bash in this folder and run:

```bash
git init
git add .
git commit -m "Initial release of Unbound"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/Unbound.git
git push -u origin main
```

Replace `YOUR_USERNAME` with your GitHub username.

### Updating an existing Unbound repository

If `origin` is already configured and you are pushing this v1.2.0 update, copy these files over your existing local repository, then run:

```bash
git add .
git commit -m "Make Force delete auto-unlock and move controls to Settings"
git push
```

After the Build workflow passes, create the release tag:

```bash
git tag v1.2.0
git push origin v1.2.0
```

## 4. Check GitHub Actions

Open the repository's **Actions** tab. The `Build` workflow should compile `win-x64` and `win-arm64` on GitHub's Windows runner.

If that workflow passes, you have an independent clean build of the repository.

## 5. Create the v1.2.0 release

The release workflow is tag-driven. Run:

```bash
git tag v1.2.0
git push origin v1.2.0
```

GitHub Actions will build the release and create a GitHub Release with:

```text
Unbound-win-x64.zip
Unbound-win-arm64.zip
```

## 6. Recommended repository settings

After the first push:

- Add a short description, for example: `A small Windows utility for unlocking and force-deleting stubborn files and folders.`
- Add topics such as `windows`, `file-unlocker`, `winforms`, `dotnet`, `utility`.
- Enable **Issues** if you want bug reports and feature requests.
- Under **Settings → Security**, enable private vulnerability reporting if available.
- Protect `main` later if other people start contributing.

## Signing and SmartScreen

Public unsigned executables can trigger Microsoft SmartScreen, especially when they are new and have little reputation. Do not put a private code-signing certificate or password in the repository. If you add code signing later, store signing secrets in GitHub Actions secrets.
