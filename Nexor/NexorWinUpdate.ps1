<#
.SYNOPSIS
    Nexor - Complete Windows 11 Fresh Setup Script (C# Compatible)
.DESCRIPTION
    Enhanced version with:
    - Clean, organized console output
    - Better update detection with forced rechecks
    - Device Manager driver detection and updates
    - Improved cleanup with service handling
    - Multiple verification passes
    - Designed to run from WPF C# application
.NOTES
    Run from WPF app with admin privileges already granted
#>

param(
    [switch]$Silent = $false
)

$ErrorActionPreference = "Continue"

# ============================================
# CONFIGURATION
# ============================================
$nexorDir = "$env:ProgramData\Nexor"
$stateFile = "$nexorDir\state.json"
$maxUpdateRounds = 15
$maxReboots = 10

# ============================================
# CONSOLE UI HELPERS (ASCII ONLY)
# ============================================
function Write-Header {
    param([string]$Text)
    if (-not $Silent) {
        Write-Host ""
        Write-Host "====================================================================" -ForegroundColor Cyan
        Write-Host " $Text" -ForegroundColor Cyan
        Write-Host "====================================================================" -ForegroundColor Cyan
        Write-Host ""
    }
}

function Write-Step {
    param(
        [string]$Message,
        [switch]$NoNewLine
    )
    if (-not $Silent) {
        if ($NoNewLine) {
            Write-Host "  > $Message" -ForegroundColor White -NoNewline
        } else {
            Write-Host "  > $Message" -ForegroundColor White
        }
    }
}

function Write-Success {
    param([string]$Message)
    if (-not $Silent) {
        Write-Host "  [OK] $Message" -ForegroundColor Green
    }
}

function Write-Info {
    param([string]$Message)
    if (-not $Silent) {
        Write-Host "      $Message" -ForegroundColor Gray
    }
}

function Write-Warn {
    param([string]$Message)
    if (-not $Silent) {
        Write-Host "  [!] $Message" -ForegroundColor Yellow
    }
}

function Write-Err {
    param([string]$Message)
    if (-not $Silent) {
        Write-Host "  [X] $Message" -ForegroundColor Red
    }
}

# ============================================
# LOGGING (Silent background logging)
# ============================================
function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "Info"
    )
    
    $timestamp = Get-Date -Format "HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    Add-Content -Path "$nexorDir\nexor.log" -Value $logMessage -ErrorAction SilentlyContinue
}

# ============================================
# STATE MANAGEMENT
# ============================================
function Get-State {
    if (Test-Path $stateFile) {
        try {
            $json = Get-Content $stateFile -Raw | ConvertFrom-Json
            return @{
                Phase = $json.Phase
                UpdateRound = $json.UpdateRound
                DriverRound = $json.DriverRound
                StartTime = $json.StartTime
                RebootCount = $json.RebootCount
                LogFile = $json.LogFile
                UpdateLog = @($json.UpdateLog)
                DriverLog = @($json.DriverLog)
                CleanupLog = @($json.CleanupLog)
                FreeSpaceBefore = $json.FreeSpaceBefore
                LastUpdateCheck = $json.LastUpdateCheck
            }
        } catch {
            Write-Log "Error loading state, creating new" "Warning"
        }
    }
    
    return @{
        Phase = -1
        UpdateRound = 0
        DriverRound = 0
        StartTime = (Get-Date).ToString('o')
        RebootCount = 0
        LogFile = "$env:USERPROFILE\Desktop\Nexor_Report_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
        UpdateLog = @()
        DriverLog = @()
        CleanupLog = @()
        FreeSpaceBefore = 0
        LastUpdateCheck = ""
    }
}

function Save-State($state) {
    if (-not (Test-Path $nexorDir)) {
        New-Item -Path $nexorDir -ItemType Directory -Force | Out-Null
    }
    $state | ConvertTo-Json -Depth 10 | Out-File -FilePath $stateFile -Encoding UTF8 -Force
}

function Test-RebootRequired {
    $reboot = $false
    
    if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired") { 
        $reboot = $true 
        Write-Log "Reboot detected: Windows Update flag" "Info"
    }
    
    if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending") { 
        $reboot = $true 
        Write-Log "Reboot detected: Component Based Servicing" "Info"
    }
    
    try {
        $regKey = Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager" -Name PendingFileRenameOperations -ErrorAction SilentlyContinue
        if ($regKey) { 
            $reboot = $true 
            Write-Log "Reboot detected: Pending file operations" "Info"
        }
    } catch {}
    
    try {
        $activeComputer = (Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName").ComputerName
        $pendingComputer = (Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName").ComputerName
        if ($activeComputer -ne $pendingComputer) { 
            $reboot = $true 
            Write-Log "Reboot detected: Computer name change pending" "Info"
        }
    } catch {}
    
    return $reboot
}

# ============================================
# INITIAL SETUP (Phase -1)
# ============================================
function Initialize-Environment {
    Write-Header "NEXOR - Windows 11 Fresh Setup"
    Write-Step "Initializing environment..."
    
    if (-not (Test-Path $nexorDir)) {
        New-Item -Path $nexorDir -ItemType Directory -Force | Out-Null
    }
    
    Write-Step "Configuring NuGet provider..." -NoNewLine
    try {
        $nugetProviders = Get-PackageProvider -ListAvailable -Name NuGet -ErrorAction SilentlyContinue
        
        if ($nugetProviders) {
            $latestNuget = $nugetProviders | Sort-Object Version -Descending | Select-Object -First 1
            Write-Host " Found (v$($latestNuget.Version))" -ForegroundColor Green
            Write-Log "NuGet provider already installed: v$($latestNuget.Version)" "Info"
        } else {
            Write-Host " Installing..." -ForegroundColor Yellow
            Install-PackageProvider -Name NuGet -Force -Confirm:$false -ErrorAction Stop | Out-Null
            Write-Host "`r  Configuring NuGet provider... Done" -ForegroundColor Green
            Write-Log "NuGet provider installed" "Success"
        }
    } catch {
        Write-Host " Warning" -ForegroundColor Yellow
        Write-Log "Error with NuGet provider: $_" "Warning"
        Write-Info "NuGet issues may occur, but script will continue"
    }
    
    Write-Step "Configuring PowerShell Gallery..." -NoNewLine
    try {
        if ((Get-PSRepository -Name PSGallery).InstallationPolicy -ne 'Trusted') {
            Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction Stop
        }
        Write-Host " Done" -ForegroundColor Green
        Write-Log "PSGallery configured" "Success"
    } catch {
        Write-Host " Failed" -ForegroundColor Red
        Write-Log "Error configuring PSGallery: $_" "Error"
        return $false
    }
    
    Write-Step "Installing PSWindowsUpdate module..." -NoNewLine
    try {
        if (-not (Get-Module -ListAvailable -Name PSWindowsUpdate)) {
            Install-Module -Name PSWindowsUpdate -Force -Confirm:$false -AllowClobber -Scope AllUsers -ErrorAction Stop | Out-Null
        }
        Remove-Module PSWindowsUpdate -Force -ErrorAction SilentlyContinue
        Import-Module PSWindowsUpdate -Force -ErrorAction Stop
        Write-Host " Done" -ForegroundColor Green
        Write-Log "PSWindowsUpdate module installed" "Success"
    } catch {
        Write-Host " Failed" -ForegroundColor Red
        Write-Log "Error installing PSWindowsUpdate: $_" "Error"
        return $false
    }
    
    Write-Success "Initialization complete"
    return $true
}

# ============================================
# REBOOT HANDLER
# ============================================
function Invoke-SystemReboot($state, $reason) {
    $state.RebootCount++
    Save-State $state
    
    Write-Host ""
    Write-Warn "System restart required: $reason"
    Write-Info "Reboot $($state.RebootCount)/$maxReboots"
    Write-Host ""
    
    for ($i = 15; $i -gt 0; $i--) {
        Write-Host "`r  Restarting in $i seconds... (Press Ctrl+C to cancel)" -NoNewline -ForegroundColor Yellow
        Start-Sleep -Seconds 1
    }
    Write-Host ""
    
    Write-Log "Rebooting: $reason (Count: $($state.RebootCount))" "Info"
    Restart-Computer -Force
    exit 0
}

# ============================================
# PHASE 0: WINDOWS UPDATES
# ============================================
function Install-WindowsUpdates($state) {
    Write-Header "PHASE 1: Windows Updates (Round $($state.UpdateRound + 1)/$maxUpdateRounds)"
    
    try {
        Remove-Module PSWindowsUpdate -Force -ErrorAction SilentlyContinue
        Import-Module PSWindowsUpdate -Force -ErrorAction Stop
        
        if ($state.UpdateRound -gt 0 -and ($state.UpdateRound % 3) -eq 0) {
            Write-Step "Resetting Windows Update components..." -NoNewLine
            Stop-Service wuauserv, bits, cryptsvc, msiserver -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 3
            Start-Service wuauserv, bits, cryptsvc, msiserver -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 5
            Write-Host " Done" -ForegroundColor Green
        }
        
        Write-Step "Searching for updates (this may take a few minutes)..."
        Write-Host ""
        
        $updates = @()
        
        try {
            $updates += Get-WindowsUpdate -MicrosoftUpdate -ErrorAction Stop
        } catch {
            Write-Log "Method 1 failed: $_" "Warning"
        }
        
        try {
            $updates += Get-WindowsUpdate -MicrosoftUpdate -IsHidden:$false -ErrorAction SilentlyContinue
        } catch {}
        
        $updates = $updates | Sort-Object -Property KB -Unique
        
        if ($updates.Count -eq 0) {
            Write-Info "No updates found, performing final verification..."
            Start-Sleep -Seconds 3
            
            $updateSession = New-Object -ComObject Microsoft.Update.Session
            $updateSearcher = $updateSession.CreateUpdateSearcher()
            $searchResult = $updateSearcher.Search("IsInstalled=0 and Type='Software' and IsHidden=0")
            
            if ($searchResult.Updates.Count -eq 0) {
                Write-Success "All Windows updates installed!"
                $state.LastUpdateCheck = (Get-Date).ToString('o')
                Write-Log "All updates complete" "Success"
                return $true
            } else {
                Write-Warn "Found $($searchResult.Updates.Count) additional update(s) via COM API"
                Write-Log "COM API found additional updates: $($searchResult.Updates.Count)" "Warning"
            }
        }
        
        if ($updates.Count -gt 0) {
            Write-Success "Found $($updates.Count) update(s)"
            Write-Host ""
            
            $counter = 0
            foreach ($update in $updates) {
                $counter++
                $updateTitle = $update.Title
                if ($update.KBArticleIDs) {
                    $updateTitle += " (KB$($update.KBArticleIDs))"
                }
                $state.UpdateLog += $updateTitle
                Write-Info "[$counter/$($updates.Count)] $updateTitle"
                Write-Log "Found update: $updateTitle" "Info"
            }
            
            Write-Host ""
            Write-Step "Installing updates..."
            Install-WindowsUpdate -MicrosoftUpdate -AcceptAll -IgnoreReboot -ErrorAction Stop | Out-Null
            Write-Success "Updates installed successfully"
            Write-Log "Updates installed: $($updates.Count)" "Success"
        }
        
        $state.UpdateRound++
        Save-State $state
        
        Start-Sleep -Seconds 5
        if (Test-RebootRequired) {
            Invoke-SystemReboot $state "Windows Updates"
        }
        
        if ($state.UpdateRound -ge $maxUpdateRounds) {
            Write-Warn "Maximum update rounds reached"
            Write-Log "Max rounds reached: $maxUpdateRounds" "Warning"
            return $true
        }
        
        return $false
        
    } catch {
        Write-Err "Error during Windows Update: $_"
        Write-Log "Update error: $_" "Error"
        
        if ($state.UpdateRound -lt 3) {
            $state.UpdateRound++
            Save-State $state
            return $false
        }
        
        return $true
    }
}

# ============================================
# PHASE 1: DRIVER UPDATES
# ============================================
function Install-DriverUpdates($state) {
    Write-Header "PHASE 2: Driver Updates (Round $($state.DriverRound + 1))"
    
    try {
        Import-Module PSWindowsUpdate -Force -ErrorAction SilentlyContinue
        
        Write-Step "Scanning Device Manager..."
        $problemDevices = Get-WmiObject Win32_PnPEntity | Where-Object { 
            $_.ConfigManagerErrorCode -ne 0
        }
        
        if ($problemDevices) {
            Write-Warn "Found $($problemDevices.Count) device(s) with issues"
            foreach ($device in $problemDevices) {
                Write-Info "[!] $($device.Name) (Error: $($device.ConfigManagerErrorCode))"
                $state.DriverLog += "Issue: $($device.Name)"
                Write-Log "Device issue: $($device.Name)" "Warning"
            }
            
            Write-Step "Attempting to resolve device issues..." -NoNewLine
            Start-Process "pnputil.exe" -ArgumentList "/scan-devices" -Wait -NoNewWindow -WindowStyle Hidden -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 5
            Write-Host " Done" -ForegroundColor Green
        } else {
            Write-Success "All devices functioning correctly"
        }
        
        Write-Host ""
        Write-Step "Searching for driver updates..."
        $driverUpdates = Get-WindowsUpdate -MicrosoftUpdate -UpdateType Driver -ErrorAction SilentlyContinue
        
        if ($driverUpdates -and $driverUpdates.Count -gt 0) {
            Write-Success "Found $($driverUpdates.Count) driver update(s)"
            Write-Host ""
            
            $counter = 0
            foreach ($driver in $driverUpdates) {
                $counter++
                $state.DriverLog += $driver.Title
                Write-Info "[$counter/$($driverUpdates.Count)] $($driver.Title)"
                Write-Log "Driver update: $($driver.Title)" "Info"
            }
            
            Write-Host ""
            Write-Step "Installing driver updates..."
            Install-WindowsUpdate -MicrosoftUpdate -UpdateType Driver -AcceptAll -IgnoreReboot -ErrorAction SilentlyContinue | Out-Null
            Write-Success "Driver updates installed"
            Write-Log "Drivers installed: $($driverUpdates.Count)" "Success"
            
            $state.DriverRound++
            Save-State $state
            
            Start-Sleep -Seconds 5
            if (Test-RebootRequired) {
                Invoke-SystemReboot $state "Driver Updates"
            }
            
            if ($state.DriverRound -lt 3) {
                return $false
            }
        } else {
            Write-Success "No driver updates available"
            Write-Log "No driver updates found" "Info"
        }
        
        Write-Host ""
        Write-Step "Final device scan..." -NoNewLine
        Start-Process "pnputil.exe" -ArgumentList "/scan-devices" -Wait -NoNewWindow -WindowStyle Hidden -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
        Write-Host " Done" -ForegroundColor Green
        
        $problemDevicesAfter = Get-WmiObject Win32_PnPEntity | Where-Object { 
            $_.ConfigManagerErrorCode -ne 0
        }
        
        if ($problemDevicesAfter) {
            Write-Warn "Remaining devices with issues: $($problemDevicesAfter.Count)"
            foreach ($device in $problemDevicesAfter) {
                Write-Info "[!] $($device.Name)"
            }
        } else {
            Write-Success "All devices verified and working"
        }
        
        Save-State $state
        Write-Log "Driver phase complete" "Success"
        return $true
        
    } catch {
        Write-Err "Error updating drivers: $_"
        Write-Log "Driver error: $_" "Error"
        return $true
    }
}

# ============================================
# PHASE 2: SYSTEM CLEANUP
# ============================================
function Invoke-SystemCleanup($state) {
    Write-Header "PHASE 3: System Cleanup"
    
    $driveBefore = Get-PSDrive C | Select-Object Used, Free
    $state.FreeSpaceBefore = [math]::Round($driveBefore.Free / 1GB, 2)
    Write-Info "Free space before cleanup: $($state.FreeSpaceBefore) GB"
    Write-Host ""
    
    # Windows Update Cache
    Write-Step "Cleaning Windows Update cache..."
    try {
        Write-Info "Stopping Windows Update services..."
        for ($i = 1; $i -le 3; $i++) {
            Stop-Service wuauserv, bits, cryptsvc -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
        }
        
        $wuService = Get-Service wuauserv
        if ($wuService.Status -eq 'Running') {
            Write-Info "Force stopping services..."
            Stop-Process -Name svchost -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 3
        }
        
        $updateCache = "$env:SystemRoot\SoftwareDistribution\Download"
        if (Test-Path $updateCache) {
            $size = [math]::Round(((Get-ChildItem $updateCache -Recurse -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum) / 1MB, 2)
            Get-ChildItem $updateCache -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            
            Start-Sleep -Seconds 2
            $remainingFiles = Get-ChildItem $updateCache -Force -ErrorAction SilentlyContinue
            if ($remainingFiles.Count -eq 0) {
                $state.CleanupLog += "Update Cache: $size MB"
                Write-Success "Cleaned $size MB from update cache"
                Write-Log "Update cache cleaned: $size MB" "Success"
            } else {
                $state.CleanupLog += "Update Cache: $size MB (Partial)"
                Write-Warn "Partially cleaned ($($remainingFiles.Count) files in use)"
                Write-Log "Update cache partially cleaned" "Warning"
            }
        } else {
            Write-Info "Update cache already empty"
        }
        
        Write-Info "Restarting Windows Update services..."
        Start-Service wuauserv, bits, cryptsvc -ErrorAction SilentlyContinue
        
    } catch {
        Write-Warn "Could not fully clean update cache: $_"
        Write-Log "Update cache error: $_" "Warning"
    }
    
    Write-Host ""
    
    # Temporary Files
    Write-Step "Cleaning temporary files..."
    $tempPaths = @(
        "$env:TEMP",
        "$env:SystemRoot\Temp",
        "$env:SystemRoot\Prefetch",
        "$env:LOCALAPPDATA\Temp"
    )
    
    $totalTemp = 0
    foreach ($path in $tempPaths) {
        try {
            if (Test-Path $path) {
                $size = [math]::Round(((Get-ChildItem $path -Recurse -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum) / 1MB, 2)
                $totalTemp += $size
                Remove-Item "$path\*" -Recurse -Force -ErrorAction SilentlyContinue
            }
        } catch {}
    }
    
    if ($totalTemp -gt 0) {
        $state.CleanupLog += "Temp Files: $totalTemp MB"
        Write-Success "Cleaned $totalTemp MB of temporary files"
        Write-Log "Temp files cleaned: $totalTemp MB" "Success"
    } else {
        Write-Info "No temporary files to clean"
    }
    
    # Recycle Bin
    Write-Step "Emptying Recycle Bin..." -NoNewLine
    try {
        Clear-RecycleBin -Force -ErrorAction Stop
        $state.CleanupLog += "Recycle Bin: Emptied"
        Write-Host " Done" -ForegroundColor Green
        Write-Log "Recycle bin emptied" "Success"
    } catch {
        Write-Host " Skipped" -ForegroundColor Yellow
    }
    
    # Windows.old
    Write-Step "Checking for Windows.old..."
    $windowsOld = "C:\Windows.old"
    if (Test-Path $windowsOld) {
        try {
            $size = [math]::Round(((Get-ChildItem $windowsOld -Recurse -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum) / 1GB, 2)
            Write-Info "Taking ownership and removing..."
            cmd /c "takeown /F C:\Windows.old\* /R /A /D Y" 2>&1 | Out-Null
            cmd /c "icacls C:\Windows.old\*.* /T /grant administrators:F" 2>&1 | Out-Null
            Remove-Item $windowsOld -Recurse -Force -ErrorAction SilentlyContinue
            
            $state.CleanupLog += "Windows.old: $size GB"
            Write-Success "Removed $size GB (Windows.old)"
            Write-Log "Windows.old removed: $size GB" "Success"
        } catch {
            Write-Warn "Could not remove Windows.old"
            Write-Log "Windows.old removal failed" "Warning"
        }
    } else {
        Write-Info "No Windows.old folder found"
    }
    
    Write-Host ""
    
    # DISM Cleanup
    Write-Step "Running DISM cleanup (may take several minutes)..."
    try {
        $dism = Start-Process dism.exe -ArgumentList "/Online /Cleanup-Image /StartComponentCleanup /ResetBase /Quiet" -Wait -PassThru -NoNewWindow -WindowStyle Hidden
        if ($dism.ExitCode -eq 0) {
            $state.CleanupLog += "DISM Cleanup: Success"
            Write-Success "DISM cleanup completed"
            Write-Log "DISM cleanup success" "Success"
        }
    } catch {
        Write-Warn "DISM cleanup failed: $_"
        Write-Log "DISM error: $_" "Warning"
    }
    
    # Storage Sense
    Write-Step "Running Storage Sense..." -NoNewLine
    try {
        Start-Process cleanmgr.exe -ArgumentList "/autoclean" -Wait -NoNewWindow -WindowStyle Hidden -ErrorAction SilentlyContinue
        $state.CleanupLog += "Storage Sense: Executed"
        Write-Host " Done" -ForegroundColor Green
        Write-Log "Storage Sense executed" "Success"
    } catch {
        Write-Host " Skipped" -ForegroundColor Yellow
    }
    
    Write-Host ""
    
    $driveAfter = Get-PSDrive C | Select-Object Used, Free
    $freeSpaceAfter = [math]::Round($driveAfter.Free / 1GB, 2)
    $spaceFreed = [math]::Round($freeSpaceAfter - $state.FreeSpaceBefore, 2)
    
    $state.CleanupLog += "Total Space Freed: $spaceFreed GB"
    Write-Info "Free space after cleanup: $freeSpaceAfter GB"
    Write-Success "Total space freed: $spaceFreed GB"
    Write-Log "Cleanup complete. Freed: $spaceFreed GB" "Success"
    
    Save-State $state
    return $true
}

# ============================================
# PHASE 3: GENERATE REPORT & CLEANUP
# ============================================
function Complete-Setup($state) {
    Write-Header "PHASE 4: Generating Report"
    
    $reportContent = @"
================================================================================
                    NEXOR - WINDOWS 11 SETUP REPORT
================================================================================

Generated: $(Get-Date -Format 'MMMM dd, yyyy - HH:mm:ss')
Started: $([DateTime]::Parse($state.StartTime).ToString('MMMM dd, yyyy - HH:mm:ss'))
Computer: $env:COMPUTERNAME
User: $env:USERNAME

================================================================================
                            SUMMARY STATISTICS
================================================================================

Windows Updates:          $($state.UpdateLog.Count) installed
Driver Updates:           $($state.DriverLog.Count) installed
Cleanup Operations:       $($state.CleanupLog.Count) completed
System Reboots:           $($state.RebootCount)
Update Rounds:            $($state.UpdateRound)
Driver Rounds:            $($state.DriverRound)

================================================================================
                        WINDOWS UPDATES INSTALLED
================================================================================

"@

    if ($state.UpdateLog.Count -gt 0) {
        foreach ($update in $state.UpdateLog) {
            $reportContent += "[+] $update`r`n"
        }
    } else {
        $reportContent += "No updates were installed (system was up to date)`r`n"
    }

    $reportContent += @"

================================================================================
                        DRIVER UPDATES INSTALLED
================================================================================

"@

    if ($state.DriverLog.Count -gt 0) {
        foreach ($driver in $state.DriverLog) {
            $reportContent += "[+] $driver`r`n"
        }
    } else {
        $reportContent += "No driver updates were available`r`n"
    }

    $reportContent += @"

================================================================================
                        CLEANUP OPERATIONS
================================================================================

"@

    if ($state.CleanupLog.Count -gt 0) {
        foreach ($cleanup in $state.CleanupLog) {
            $reportContent += "[+] $cleanup`r`n"
        }
    } else {
        $reportContent += "No cleanup was performed`r`n"
    }

    $reportContent += @"

================================================================================
                      NEXOR - SETUP COMPLETE
================================================================================
"@

    $reportPath = $state.LogFile
    try {
        $reportContent | Out-File -FilePath $reportPath -Encoding UTF8 -Force
        Write-Success "Report saved to: $reportPath"
        Write-Log "Report saved: $reportPath" "Success"
    } catch {
        $reportPath = "$env:TEMP\Nexor_Report_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
        $reportContent | Out-File -FilePath $reportPath -Encoding UTF8 -Force
        Write-Warn "Report saved to temp: $reportPath"
        Write-Log "Report saved to temp: $reportPath" "Warning"
    }
    
    if (Test-RebootRequired) {
        Write-Host ""
        Write-Warn "Final system restart required"
        Write-Info "Report saved to: $reportPath"
        Write-Host ""
        
        for ($i = 20; $i -gt 0; $i--) {
            Write-Host "`r  Restarting in $i seconds... (Press Ctrl+C to cancel)" -NoNewline -ForegroundColor Yellow
            Start-Sleep -Seconds 1
        }
        Write-Host ""
        Restart-Computer -Force
    } else {
        Write-Host ""
        Write-Success "Setup complete!"
        Write-Info "Report saved to: $reportPath"
        Write-Log "Setup complete without reboot" "Success"
        
        Start-Sleep -Seconds 2
        Remove-Item $nexorDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# ============================================
# MAIN EXECUTION
# ============================================
try {
    Clear-Host
    $state = Get-State
    
    # Phase -1: Initialization
    if ($state.Phase -eq -1) {
        if (-not (Initialize-Environment)) {
            Write-Err "Initialization failed!"
            Write-Log "Initialization failed" "Error"
            Read-Host "`nPress Enter to exit"
            exit 1
        }
        $state.Phase = 0
        Save-State $state
        Write-Host ""
        Write-Info "Press any key to start..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    }
    
    # Phase 0: Windows Updates (loop until complete)
    if ($state.Phase -eq 0) {
        $updatesComplete = Install-WindowsUpdates $state
        
        if ($updatesComplete) {
            Write-Host ""
            Write-Success "Windows Updates phase complete!"
            Write-Log "Windows Updates complete" "Success"
            $state.Phase = 1
            $state.UpdateRound = 0
            Save-State $state
            
            Write-Host ""
            Write-Info "Continuing to driver updates in 3 seconds..."
            Start-Sleep -Seconds 3
        } else {
            Save-State $state
            Write-Host ""
            Write-Info "Searching for more updates in 3 seconds..."
            Start-Sleep -Seconds 3
            
            & $PSCommandPath -Silent:$Silent
            exit 0
        }
    }
    
    # Phase 1: Driver Updates (loop until complete)
    if ($state.Phase -eq 1) {
        $driversComplete = Install-DriverUpdates $state
        
        if ($driversComplete) {
            Write-Host ""
            Write-Success "Driver Updates phase complete!"
            Write-Log "Driver Updates complete" "Success"
            $state.Phase = 2
            Save-State $state
            
            Write-Host ""
            Write-Info "Continuing to cleanup in 3 seconds..."
            Start-Sleep -Seconds 3
        } else {
            Save-State $state
            Write-Host ""
            Write-Info "Checking for more drivers in 3 seconds..."
            Start-Sleep -Seconds 3
            
            & $PSCommandPath -Silent:$Silent
            exit 0
        }
    }
    
    # Phase 2: System Cleanup
    if ($state.Phase -eq 2) {
        Invoke-SystemCleanup $state | Out-Null
        $state.Phase = 3
        Save-State $state
        
        Write-Host ""
        Write-Info "Continuing to final verification in 3 seconds..."
        Start-Sleep -Seconds 3
    }
    
    # Phase 3: Final verification and report
    if ($state.Phase -eq 3) {
        Write-Header "FINAL VERIFICATION"
        
        Write-Step "Performing final update check..."
        try {
            Import-Module PSWindowsUpdate -Force -ErrorAction Stop
            $finalCheck = Get-WindowsUpdate -MicrosoftUpdate -ErrorAction SilentlyContinue
            
            if ($finalCheck -and $finalCheck.Count -gt 0) {
                Write-Warn "$($finalCheck.Count) update(s) still available"
                Write-Info "You may need to run Windows Update manually"
                Write-Host ""
                
                foreach ($update in $finalCheck) {
                    Write-Info "[!] $($update.Title)"
                }
                Write-Log "Final check found updates: $($finalCheck.Count)" "Warning"
            } else {
                Write-Success "All updates verified and installed"
                Write-Log "All updates verified" "Success"
            }
        } catch {
            Write-Warn "Final check completed with warnings"
            Write-Log "Final check warning: $_" "Warning"
        }
        
        Write-Host ""
        Write-Step "Performing final device check..."
        $finalDeviceCheck = Get-WmiObject Win32_PnPEntity | Where-Object { 
            $_.ConfigManagerErrorCode -ne 0
        }
        
        if ($finalDeviceCheck) {
            Write-Warn "$($finalDeviceCheck.Count) device(s) still have issues"
            Write-Info "These may require manual driver installation"
            Write-Host ""
            
            foreach ($device in $finalDeviceCheck) {
                Write-Info "[!] $($device.Name)"
            }
            Write-Log "Devices with issues: $($finalDeviceCheck.Count)" "Warning"
        } else {
            Write-Success "All devices verified and working"
            Write-Log "All devices verified" "Success"
        }
        
        Write-Host ""
        Complete-Setup $state
    }
    
    Write-Host ""
    Write-Header "NEXOR SETUP COMPLETED"
    Write-Log "Setup completed successfully" "Success"
    
    if (-not $Silent) {
        Write-Host ""
        Write-Info "Press any key to exit..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    }
    
} catch {
    Write-Host ""
    Write-Err "Critical error occurred"
    Write-Err $_.Exception.Message
    Write-Log "Critical error: $_ | $($_.ScriptStackTrace)" "Error"
    
    if (-not $Silent) {
        Write-Host ""
        Read-Host "Press Enter to exit"
    }
    exit 1
}