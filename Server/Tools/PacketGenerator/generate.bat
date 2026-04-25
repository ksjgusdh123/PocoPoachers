@echo off
setlocal

set "HERE=%~dp0"
cd /d "%HERE%"

echo Run PacketGenerator...
dotnet run --project PacketGenerator.csproj --configuration Release

if errorlevel 1 (
    echo [Error] PacketGenerator failed.
    pause
    exit /b 1
)

echo ==================================
exit /b 0
