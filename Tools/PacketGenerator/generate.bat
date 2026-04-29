@echo off
setlocal

set "HERE=%~dp0"
cd /d "%HERE%"

echo [PacketGenerator] Start
dotnet run --project PacketGenerator.csproj --configuration Release ^
  --server "..\..\Server\Server\Generated\FlatBuffer" ^
  --client "..\..\PocoPoachers\Assets\01. Scripts\Generated\FlatBuffer"

if errorlevel 1 (
    echo [Error] PacketGenerator failed.
    pause
    exit /b 1
)

echo [PacketGenerator] End
exit /b 0
