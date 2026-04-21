@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

set "HERE=%~dp0"
set "SCHEMAS=%HERE%Schemas"
set "OUT=%HERE%\Generated"
set "FLATC_EXE=%HERE%flatc.exe"

if not exist "%FLATC_EXE%" (
    echo [Error] flatc.exe not found in %HERE%
    pause
    exit /b 1
)

if not exist "%OUT%" mkdir "%OUT%"

pushd "%SCHEMAS%"
for /r %%f in (*.fbs) do (
    echo %%~nxf
    "%FLATC_EXE%" --csharp -I "%SCHEMAS%" -o "%OUT%" "%%f"
    if errorlevel 1 (
        echo [Error] Failed to compile: %%~nxf
        popd
        pause
        exit /b 1
    )
)
popd

echo ==================================

pause
exit /b 0