# Windows Update Installation Script
# Requires Administrator privileges

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Please right-click and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Windows Update Installation Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Install PSWindowsUpdate module if not already installed
if (-not (Get-Module -ListAvailable -Name PSWindowsUpdate)) {
    Write-Host "Installing PSWindowsUpdate module..." -ForegroundColor Yellow
    try {
        Install-Module -Name PSWindowsUpdate -Force -Confirm:$false -ErrorAction Stop
        Write-Host "PSWindowsUpdate module installed successfully." -ForegroundColor Green
    }
    catch {
        Write-Host "ERROR: Failed to install PSWindowsUpdate module." -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }
}

# Import the module
Write-Host "Importing PSWindowsUpdate module..." -ForegroundColor Yellow
Import-Module PSWindowsUpdate

Write-Host ""
Write-Host "Checking for Windows updates..." -ForegroundColor Yellow
Write-Host ""

try {
    # Get available updates
    $updates = Get-WindowsUpdate -AcceptAll -IgnoreReboot
    
    if ($updates.Count -eq 0) {
        Write-Host "No updates available. System is up to date." -ForegroundColor Green
        Write-Host "No restart required." -ForegroundColor Green
        exit 0
    }
    
    Write-Host "Found $($updates.Count) update(s) available:" -ForegroundColor Cyan
    foreach ($update in $updates) {
        Write-Host "  - $($update.Title)" -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "Starting update installation..." -ForegroundColor Yellow
    Write-Host "This may take a while. Please do not close this window." -ForegroundColor Yellow
    Write-Host ""
    
    # Install updates with progress
    Install-WindowsUpdate -AcceptAll -AutoReboot:$false -Verbose
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "All updates installed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "The computer will restart in 30 seconds..." -ForegroundColor Yellow
    Write-Host "Press Ctrl+C to cancel the restart." -ForegroundColor Yellow
    
    # Countdown before restart
    for ($i = 30; $i -gt 0; $i--) {
        Write-Host "`rRestarting in $i seconds... " -NoNewline -ForegroundColor Yellow
        Start-Sleep -Seconds 1
    }
    
    Write-Host ""
    Write-Host "Restarting now..." -ForegroundColor Red
    Restart-Computer -Force
}
catch {
    Write-Host ""
    Write-Host "ERROR: An error occurred during the update process." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}