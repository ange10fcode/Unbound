@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo.
    echo ERROR: .NET 8 SDK was not found.
    echo Download it from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

call "%~dp0scripts\publish.cmd" win-x64
if errorlevel 1 (
    echo.
    echo The build failed. Read the error above.
    pause
    exit /b 1
)

echo.
echo Unbound.exe is ready.
echo Opening the output folder...
start "" "%~dp0dist\win-x64"

echo.
pause
