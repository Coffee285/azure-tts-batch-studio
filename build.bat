@echo off
REM Build script for Azure TTS Batch Studio
REM This script builds the application for Windows 11 and makes it easy to install

echo ===============================================
echo  Azure TTS Batch Studio - Windows 11 Builder
echo ===============================================
echo.

REM Check if .NET is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found! 
    echo Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo .NET SDK found. Building application...
echo.

REM Set variables
set PROJECT_PATH=AzureTtsBatchStudio\AzureTtsBatchStudio.csproj
set OUTPUT_DIR=publish
set CONFIG=Release

REM Clean previous builds
if exist %OUTPUT_DIR% (
    echo Cleaning previous builds...
    rmdir /s /q %OUTPUT_DIR%
)
mkdir %OUTPUT_DIR%

echo.
echo Building for Windows x64...
dotnet publish %PROJECT_PATH% --configuration %CONFIG% --runtime win-x64 --self-contained true --output %OUTPUT_DIR%\win-x64 -p:PublishSingleFile=false -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true

if %errorlevel% neq 0 (
    echo ERROR: Failed to build x64 version
    pause
    exit /b 1
)

echo.
echo ===============================================
echo  BUILD COMPLETED SUCCESSFULLY!
echo ===============================================
echo.
echo Build output: %OUTPUT_DIR%\win-x64\
echo.
echo To run the application:
echo   1. Go to: %OUTPUT_DIR%\win-x64\
echo   2. Double-click AzureTtsBatchStudio.exe
echo.
echo Opening output folder...
explorer %OUTPUT_DIR%\win-x64
echo.
pause