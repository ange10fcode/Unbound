@echo off
setlocal EnableExtensions

set "RUNTIME=%~1"
if "%RUNTIME%"=="" set "RUNTIME=win-x64"

if /I not "%RUNTIME%"=="win-x64" if /I not "%RUNTIME%"=="win-arm64" (
    echo Unsupported runtime: %RUNTIME%
    echo Use win-x64 or win-arm64.
    exit /b 2
)

set "ROOT=%~dp0.."
set "OUTPUT=%ROOT%\dist\%RUNTIME%"
set "ZIP=%ROOT%\dist\Unbound-%RUNTIME%.zip"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERROR: .NET SDK not found in PATH.
    exit /b 1
)

if exist "%OUTPUT%" rmdir /s /q "%OUTPUT%"
if exist "%ZIP%" del /q "%ZIP%"
mkdir "%OUTPUT%" >nul 2>nul

pushd "%ROOT%"

echo.
echo [1/3] Restoring...
dotnet restore Unbound.csproj
if errorlevel 1 goto :fail

echo.
echo [2/3] Publishing %RUNTIME%...
dotnet publish Unbound.csproj -c Release -r %RUNTIME% --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o "%OUTPUT%"
if errorlevel 1 goto :fail

copy /y LICENSE "%OUTPUT%\LICENSE" >nul
copy /y README.md "%OUTPUT%\README.md" >nul

for %%F in ("%OUTPUT%\Unbound.exe") do set "EXESIZE=%%~zF"
if not exist "%OUTPUT%\Unbound.exe" goto :fail

echo.
echo [3/3] Creating release ZIP...
powershell -NoProfile -Command "Compress-Archive -Path '%OUTPUT%\*' -DestinationPath '%ZIP%' -Force"
if errorlevel 1 (
    echo Warning: EXE built successfully, but the ZIP could not be created.
)

echo.
echo ========================================
echo Unbound build complete
echo ========================================
echo EXE: %OUTPUT%\Unbound.exe
echo ZIP: %ZIP%

echo.
popd
exit /b 0

:fail
echo.
echo Build failed.
popd
exit /b 1
