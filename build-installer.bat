@echo off
REM Simple installer builder for Azure TTS Batch Studio
REM This creates a Windows installer (.exe) for easy installation

echo ===============================================
echo  Azure TTS Batch Studio - Installer Builder
echo ===============================================
echo.

REM Check if Inno Setup is installed
if not exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    if not exist "C:\Program Files\Inno Setup 6\ISCC.exe" (
        echo ERROR: Inno Setup not found!
        echo.
        echo Please install Inno Setup from: https://jrsoftware.org/isinfo.php
        echo Then run this script again.
        echo.
        pause
        exit /b 1
    )
    set "INNO_SETUP=C:\Program Files\Inno Setup 6\ISCC.exe"
) else (
    set "INNO_SETUP=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

echo Inno Setup found. Building installer...
echo.

REM Build the application first if not already built
if not exist "publish\win-x64\AzureTtsBatchStudio.exe" (
    echo Application not built yet. Building first...
    call build.bat
    if %errorlevel% neq 0 (
        echo ERROR: Failed to build application
        pause
        exit /b 1
    )
)

REM Create installer output directory
if not exist "publish\installers" mkdir "publish\installers"

echo Creating Windows installer...
"%INNO_SETUP%" "setup.iss"

if %errorlevel% neq 0 (
    echo ERROR: Failed to create installer
    pause
    exit /b 1
)

echo.
echo ===============================================
echo  INSTALLER CREATED SUCCESSFULLY!
echo ===============================================
echo.
echo The installer has been created in: publish\installers\
echo.
echo For easy Windows 11 installation:
echo   1. Share the .exe installer file
echo   2. Users just double-click to install
echo   3. Application appears in Start Menu
echo.
echo Opening installer folder...
explorer "publish\installers"
echo.
pause