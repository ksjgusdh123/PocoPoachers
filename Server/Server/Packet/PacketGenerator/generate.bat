@echo off
setlocal enabledelayedexpansion
echo.

set "HERE=%~dp0"
set "SCHEMAS=%HERE%Schemas"
set "OUT=%HERE%Generated"
set "CLIENT_OUT=%HERE%..\..\..\..\PocoPoachers\Assets\01. Scripts\Packet\Generated"
set "FLATC_EXE=%HERE%flatc.exe"

if not exist "%FLATC_EXE%" (
    echo [Error] flatc.exe not found in %HERE%
    pause
    exit /b 1
)


echo Run FlatBuffers Generator...
echo Server C# output: %OUT%
echo Client C# output: %CLIENT_OUT%
echo.

if not exist "%OUT%" mkdir "%OUT%"
if not exist "%CLIENT_OUT%" mkdir "%CLIENT_OUT%"

pushd "%SCHEMAS%"
for /r %%f in (*.fbs) do (
    echo %%~nxf
    "%FLATC_EXE%" --csharp -I "%SCHEMAS%" -o "%OUT%" "%%f"
    if errorlevel 1 (
        echo [Error] Failed to compile ^(server^): %%~nxf
        popd
        pause
        exit /b 1
    )
    "%FLATC_EXE%" --csharp -I "%SCHEMAS%" -o "%CLIENT_OUT%" "%%f"
    if errorlevel 1 (
        echo [Error] Failed to compile ^(client^): %%~nxf
        popd
        pause
        exit /b 1
    )
)
popd

echo ==================================
exit /b 0