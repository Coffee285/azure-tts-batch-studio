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
mkdir %OUTPUT_DIR%\portable

echo.
echo [1/4] Building for Windows x64 (recommended for most PCs)...
dotnet publish %PROJECT_PATH% --configuration %CONFIG% --runtime win-x64 --self-contained true --output %OUTPUT_DIR%\win-x64 -p:PublishSingleFile=false -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true

if %errorlevel% neq 0 (
    echo ERROR: Failed to build x64 version
    pause
    exit /b 1
)

echo.
echo [2/4] Building for Windows x86 (for older 32-bit systems)...
dotnet publish %PROJECT_PATH% --configuration %CONFIG% --runtime win-x86 --self-contained true --output %OUTPUT_DIR%\win-x86 -p:PublishSingleFile=false -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true

echo.
echo [3/4] Building for Windows ARM64 (for ARM devices)...
dotnet publish %PROJECT_PATH% --configuration %CONFIG% --runtime win-arm64 --self-contained true --output %OUTPUT_DIR%\win-arm64 -p:PublishSingleFile=false -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true

echo.
echo [4/4] Creating portable packages for easy installation...

REM Create portable zip files using PowerShell for better compression
powershell -command "Compress-Archive -Path '%OUTPUT_DIR%\win-x64\*' -DestinationPath '%OUTPUT_DIR%\portable\AzureTtsBatchStudio-x64-portable.zip' -Force"
powershell -command "Compress-Archive -Path '%OUTPUT_DIR%\win-x86\*' -DestinationPath '%OUTPUT_DIR%\portable\AzureTtsBatchStudio-x86-portable.zip' -Force"
powershell -command "Compress-Archive -Path '%OUTPUT_DIR%\win-arm64\*' -DestinationPath '%OUTPUT_DIR%\portable\AzureTtsBatchStudio-arm64-portable.zip' -Force"

REM Create installation instructions
echo Azure TTS Batch Studio - Simple Installation for Windows 11> %OUTPUT_DIR%\portable\README-Windows11.txt
echo =========================================================> %OUTPUT_DIR%\portable\README-Windows11.txt
echo.>> %OUTPUT_DIR%\portable\README-Windows11.txt
echo EASY INSTALLATION STEPS:>> %OUTPUT_DIR%\portable\README-Windows11.txt
echo 1. Download the appropriate ZIP file for your system:>> %OUTPUT_DIR%\portable\README-Windows11.txt
echo    - AzureTtsBatchStudio-x64-portable.zip (for most modern PCs^)>> %OUTPUT_DIR%\portable\README-Windows11.txt
echo    - AzureTtsBatchStudio-x86-portable.zip (for older 32-bit systems^)>> %OUTPUT_DIR%\portable\README-Windows11.txt
echo.>> %OUTPUT_DIR%\portable\README-Windows11.txt
echo 2. Extract the ZIP file to any folder (like Desktop or C:\Programs^)>> %OUTPUT_DIR%\portable\README-Windows11.txt
echo.>> %OUTPUT_DIR%\portable\README-Windows11.txt
echo 3. Double-click on AzureTtsBatchStudio.exe to run>> %OUTPUT_DIR%\portable\README-Windows11.txt

echo.
echo ===============================================
echo  BUILD COMPLETED SUCCESSFULLY!
echo ===============================================
echo.
echo Ready-to-use packages created in: %OUTPUT_DIR%\portable\
echo.
echo For Windows 11 users:
echo   1. Go to: %OUTPUT_DIR%\portable\
echo   2. Download: AzureTtsBatchStudio-x64-portable.zip
echo   3. Extract and run AzureTtsBatchStudio.exe
echo.
echo Package sizes:
for %%f in (%OUTPUT_DIR%\portable\*.zip) do (
    echo   %%~nf: %%~zf bytes
)
echo.
echo Opening output folder...
explorer %OUTPUT_DIR%\portable
echo.
pause