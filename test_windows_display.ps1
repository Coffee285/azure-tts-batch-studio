# Windows Test Script for Azure TTS Batch Studio
# This script helps diagnose window display issues

Write-Host "Testing Azure TTS Batch Studio Window Display..." -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

# Check .NET version
Write-Host "Checking .NET version..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET Version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ ERROR: .NET not found or not installed" -ForegroundColor Red
    exit 1
}

# Change to the project directory
$projectPath = "AzureTtsBatchStudio"
if (Test-Path $projectPath) {
    Set-Location $projectPath
    Write-Host "✓ Found project directory: $projectPath" -ForegroundColor Green
} else {
    Write-Host "✗ ERROR: Could not find project directory '$projectPath'" -ForegroundColor Red
    Write-Host "Please run this script from the root of the repository." -ForegroundColor Yellow
    exit 1
}

# Build the application first
Write-Host "`nBuilding application..." -ForegroundColor Yellow
try {
    $buildOutput = dotnet build 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Build successful" -ForegroundColor Green
    } else {
        Write-Host "✗ Build failed:" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ ERROR: Failed to build application" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "`nStarting application with debug output..." -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Yellow

# Run the application and capture output
try {
    # Start the application
    $process = Start-Process -FilePath "dotnet" -ArgumentList "run" -PassThru -RedirectStandardOutput "output.log" -RedirectStandardError "error.log"
    
    Write-Host "✓ Application started. PID: $($process.Id)" -ForegroundColor Green
    Write-Host "Waiting for window to appear (timeout: 15 seconds)..." -ForegroundColor Yellow
    
    # Wait a bit for the application to start
    Start-Sleep -Seconds 15
    
    # Check if process is still running
    if (!$process.HasExited) {
        Write-Host "`n🎉 SUCCESS: Application is running!" -ForegroundColor Green
        Write-Host "Process ID: $($process.Id)" -ForegroundColor Green
        Write-Host "If you can see the 'Azure TTS Batch Studio' window, the fix worked!" -ForegroundColor Green
        Write-Host "The window should be 1200x800 pixels and centered on your screen." -ForegroundColor Green
        
        # Kill the process after showing success
        Write-Host "`nStopping application..." -ForegroundColor Yellow
        try {
            $process.Kill()
        } catch {
            Write-Host "WARNING: Could not kill process. It may have already exited." -ForegroundColor Yellow
        }
        $process.WaitForExit()
    } else {
        Write-Host "`n❌ Application exited with code: $($process.ExitCode)" -ForegroundColor Red
        Write-Host "This indicates the application encountered an error during startup." -ForegroundColor Red
    }
}
catch {
    Write-Host "`n❌ ERROR: Failed to start application" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# Show output logs
Write-Host "`n=========================================" -ForegroundColor Yellow
Write-Host "Application Output:" -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Yellow

if (Test-Path "output.log") {
    $output = Get-Content "output.log"
    if ($output) {
        $output | ForEach-Object { Write-Host $_ -ForegroundColor Cyan }
    } else {
        Write-Host "(No standard output)" -ForegroundColor Gray
    }
    Remove-Item "output.log"
} else {
    Write-Host "(No output log file)" -ForegroundColor Gray
}

if (Test-Path "error.log") {
    $errorOutput = Get-Content "error.log"
    if ($errorOutput) {
        Write-Host "`nError Output:" -ForegroundColor Red
        $errorOutput | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    }
    Remove-Item "error.log"
}

Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "Test completed!" -ForegroundColor Green
Write-Host "If the window appeared and you could see the Azure TTS Batch Studio interface," -ForegroundColor Green
Write-Host "then the fix was successful! ✓" -ForegroundColor Green
Write-Host "`nIf the window did not appear, please:" -ForegroundColor Yellow
Write-Host "1. Share the output above with the development team" -ForegroundColor Yellow
Write-Host "2. Check if any antivirus software is blocking the application" -ForegroundColor Yellow
Write-Host "3. Try running as Administrator" -ForegroundColor Yellow
Write-Host "4. Ensure you have the latest .NET 8 runtime installed" -ForegroundColor Yellow