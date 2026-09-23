# Contributing

Thanks for improving Unbound.

## Development setup

1. Install the .NET 8 SDK on Windows.
2. Clone the repository.
3. Run `dotnet restore`.
4. Run `dotnet build` or open `Unbound.sln` in Visual Studio 2022+.

## Pull requests

- Keep changes focused.
- Do not add telemetry, advertising, or network dependencies without discussing them first.
- Preserve explicit confirmation before destructive deletion or force-terminating another process.
- Avoid automatically terminating protected Windows processes.
- Test Explorer integration on both files and folders when changing shell code.
- Update `CHANGELOG.md` for user-visible changes.

## Style

The repository includes `.editorconfig`; use it as the baseline formatting style.
