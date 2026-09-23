param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Output = Join-Path $RepoRoot "dist\$Runtime"

if (Test-Path $Output) {
    Remove-Item $Output -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

Push-Location $RepoRoot
try {
    dotnet restore .\Unbound.csproj
    dotnet publish .\Unbound.csproj `
        -c Release `
        -r $Runtime `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $Output

    Copy-Item .\LICENSE $Output
    Copy-Item .\README.md $Output

    $Zip = Join-Path $RepoRoot "dist\Unbound-$Runtime.zip"
    if (Test-Path $Zip) {
        Remove-Item $Zip -Force
    }

    Compress-Archive -Path "$Output\*" -DestinationPath $Zip

    Write-Host ""
    Write-Host "Release build complete:" -ForegroundColor Green
    Write-Host "  EXE: $Output\Unbound.exe"
    Write-Host "  ZIP: $Zip"
}
finally {
    Pop-Location
}
