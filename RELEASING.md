# Releasing Unbound

1. Update the version in `Unbound.csproj`.
2. Add the release notes to `CHANGELOG.md`.
3. Commit the changes.
4. Create and push a version tag:

```powershell
git tag v1.1.0
git push origin v1.1.0
```

The `release.yml` GitHub Actions workflow builds self-contained single-file `win-x64` and `win-arm64` packages and attaches them to a GitHub Release automatically.
