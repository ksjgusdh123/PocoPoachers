@echo off
setlocal

set "HERE=%~dp0"
cd /d "%HERE%"

echo Run TableGenerator...
dotnet run --project TableGenerator.csproj --configuration Release

if errorlevel 1 (
    echo [Error] TableGenerator failed.
    pause
    exit /b 1
)

echo ==================================
exit /b 0
