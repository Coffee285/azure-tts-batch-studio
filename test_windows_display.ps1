# Windows Test Script for Azure TTS Batch Studio
# This script helps diagnose window display issues

Write-Host "Testing Azure TTS Batch Studio Window Display..." -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

# Change to the project directory
$projectPath = "AzureTtsBatchStudio"
if (Test-Path $projectPath) {
    Set-Location $projectPath
    Write-Host "Found project directory: $projectPath" -ForegroundColor Green
} else {
    Write-Host "ERROR: Could not find project directory '$projectPath'" -ForegroundColor Red
    Write-Host "Please run this script from the root of the repository." -ForegroundColor Yellow
    exit 1
}

Write-Host "`nStarting application with debug output..." -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Yellow

# Run the application and capture output
try {
    # Start the application
    $process = Start-Process -FilePath "dotnet" -ArgumentList "run" -PassThru -RedirectStandardOutput "output.log" -RedirectStandardError "error.log"
    
    Write-Host "Application started. PID: $($process.Id)" -ForegroundColor Green
    Write-Host "Waiting for window to appear (timeout: 10 seconds)..." -ForegroundColor Yellow
    
    # Wait a bit for the application to start
    Start-Sleep -Seconds 10
    
    # Check if process is still running
    if (!$process.HasExited) {
        Write-Host "`nSUCCESS: Application is running!" -ForegroundColor Green
        Write-Host "Process ID: $($process.Id)" -ForegroundColor Green
        Write-Host "If you can see the window, the fix worked!" -ForegroundColor Green
        
        # Kill the process after showing success
        Write-Host "`nStopping application..." -ForegroundColor Yellow
        try {
            $process.Kill()
        } catch {
            Write-Host "WARNING: Could not kill process. It may have already exited." -ForegroundColor Yellow
        }
        $process.WaitForExit()
    } else {
        Write-Host "`nApplication exited with code: $($process.ExitCode)" -ForegroundColor Red
    }
}
catch {
    Write-Host "`nERROR: Failed to start application" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# Show output logs
Write-Host "`n=========================================" -ForegroundColor Yellow
Write-Host "Application Output:" -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Yellow

if (Test-Path "output.log") {
    Get-Content "output.log" | ForEach-Object { Write-Host $_ -ForegroundColor Cyan }
    Remove-Item "output.log"
}

if (Test-Path "error.log") {
    Write-Host "`nError Output:" -ForegroundColor Red
    Get-Content "error.log" | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    Remove-Item "error.log"
}

Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "Test completed!" -ForegroundColor Green
Write-Host "If the window appeared, the fix was successful." -ForegroundColor Green
Write-Host "If not, please share the output above for further diagnosis." -ForegroundColor Yellow