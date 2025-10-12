using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Nexor
{
    public enum UpdatePhase
    {
        WindowsUpdate,
        DriverUpdate,
        Cleanup,
        Completed
    }

    public class UpdateState
    {
        public UpdatePhase CurrentPhase { get; set; }
        public int UpdateCheckCount { get; set; }
        public int RestartCount { get; set; }
        public DateTime LastCheckTime { get; set; }
        public bool WaitingForRestart { get; set; }
    }

    public class WindowsUpdateStatus
    {
        public bool IsChecking { get; set; }
        public bool IsDownloading { get; set; }
        public bool IsInstalling { get; set; }
        public bool RebootRequired { get; set; }
        public bool NoUpdatesAvailable { get; set; }
        public int TotalUpdates { get; set; }
        public int UpdatesDownloaded { get; set; }
        public int UpdatesInstalled { get; set; }
        public int ProgressPercentage { get; set; }
        public string StatusMessage { get; set; }
    }

    public partial class FreshSetupPage : UserControl
    {
        private const string STATE_FILE = "nexor_state.json";
        private const string STARTUP_TASK_NAME = "NexorAutoResume";
        private const int MAX_UPDATE_PASSES = 10;
        private const int UPDATE_CHECK_INTERVAL = 15; // seconds

        private UpdateState _state;
        private DispatcherTimer _monitorTimer;
        private readonly string _currentLanguage;
        private bool _isProcessing = false;
        private CancellationTokenSource _cancellationSource;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        public FreshSetupPage(string language = "PT")
        {
            InitializeComponent();
            _currentLanguage = language;
            SetLanguage(language);

            LoadState();

            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Interval = TimeSpan.FromSeconds(UPDATE_CHECK_INTERVAL);
            _monitorTimer.Tick += MonitorTimer_Tick;

            if (_state.CurrentPhase != UpdatePhase.Completed && _state.WaitingForRestart)
            {
                Task.Delay(3000).ContinueWith(_ => Dispatcher.Invoke(() => ShowResumeDialog()));
            }
        }

        private void LoadState()
        {
            try
            {
                string statePath = Path.Combine(Path.GetTempPath(), STATE_FILE);
                if (File.Exists(statePath))
                {
                    string json = File.ReadAllText(statePath);
                    _state = System.Text.Json.JsonSerializer.Deserialize<UpdateState>(json);
                    AddLog($"📋 Loaded state: Phase={_state.CurrentPhase}, Pass={_state.UpdateCheckCount}, Restarts={_state.RestartCount}");
                }
                else
                {
                    _state = new UpdateState
                    {
                        CurrentPhase = UpdatePhase.WindowsUpdate,
                        UpdateCheckCount = 0,
                        RestartCount = 0,
                        LastCheckTime = DateTime.MinValue,
                        WaitingForRestart = false
                    };
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Error loading state: {ex.Message}");
                _state = new UpdateState
                {
                    CurrentPhase = UpdatePhase.WindowsUpdate,
                    UpdateCheckCount = 0,
                    RestartCount = 0,
                    LastCheckTime = DateTime.MinValue,
                    WaitingForRestart = false
                };
            }
        }

        private void SaveState()
        {
            try
            {
                string statePath = Path.Combine(Path.GetTempPath(), STATE_FILE);
                string json = System.Text.Json.JsonSerializer.Serialize(_state);
                File.WriteAllText(statePath, json);
                AddLog($"💾 State saved: Phase={_state.CurrentPhase}, Pass={_state.UpdateCheckCount}");
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Could not save state: {ex.Message}");
            }
        }

        private void ClearState()
        {
            try
            {
                string statePath = Path.Combine(Path.GetTempPath(), STATE_FILE);
                if (File.Exists(statePath))
                {
                    File.Delete(statePath);
                }
                _state = new UpdateState
                {
                    CurrentPhase = UpdatePhase.Completed,
                    UpdateCheckCount = 0,
                    RestartCount = 0,
                    LastCheckTime = DateTime.Now,
                    WaitingForRestart = false
                };
            }
            catch { }
        }

        private void ShowResumeDialog()
        {
            string phase = _state.CurrentPhase switch
            {
                UpdatePhase.WindowsUpdate => _currentLanguage == "PT" ? "Atualização do Windows" : "Windows Update",
                UpdatePhase.DriverUpdate => _currentLanguage == "PT" ? "Atualização de Drivers" : "Driver Update",
                UpdatePhase.Cleanup => _currentLanguage == "PT" ? "Limpeza do Sistema" : "System Cleanup",
                _ => ""
            };

            ShowCustomDialog(
                _currentLanguage == "PT" ? "Retomar Processo" : "Resume Process",
                _currentLanguage == "PT"
                    ? $"Processo detectado após reinício.\n\nFase: {phase}\nPasse: {_state.UpdateCheckCount}/{MAX_UPDATE_PASSES}\nReinícios: {_state.RestartCount}\n\nContinuar automaticamente?"
                    : $"Process detected after restart.\n\nPhase: {phase}\nPass: {_state.UpdateCheckCount}/{MAX_UPDATE_PASSES}\nRestarts: {_state.RestartCount}\n\nContinue automatically?",
                DialogType.Question,
                () =>
                {
                    _state.WaitingForRestart = false;
                    SaveState();
                    Task.Run(() => ContinueProcess());
                },
                () => ClearState()
            );
        }

        private void SetLanguage(string language)
        {
            if (language == "PT")
            {
                TxtTitle.Text = "Configuração Automática Windows 11";
                TxtSubtitle.Text = "Atualize e otimize automaticamente";
                TxtStatusTitle.Text = "Estado do Processo";
                TxtStatusDesc.Text = "Pronto para iniciar";
                BtnRunAll.Content = "🚀 Iniciar Processo Automático";

                TxtStep1Title.Text = "Atualizar Windows";
                TxtStep1Desc.Text = "Instalar todas as atualizações disponíveis";
                TxtStep1Status.Text = "⏳ Pendente";

                TxtStep2Title.Text = "Atualizar Drivers";
                TxtStep2Desc.Text = "Verificar e atualizar drivers";
                TxtStep2Status.Text = "⏳ Pendente";

                TxtStep3Title.Text = "Limpar Sistema";
                TxtStep3Desc.Text = "Remover arquivos desnecessários";
                TxtStep3Status.Text = "⏳ Pendente";

                TxtInfoTitle.Text = "ℹ️ Como Funciona";
                TxtInfo1.Text = "• O programa abre o Windows Update automaticamente";
                TxtInfo2.Text = "• Verifica, baixa e instala todas as atualizações";
                TxtInfo3.Text = "• Reinicia automaticamente quando necessário";
                TxtInfo4.Text = "• Processo totalmente automático até conclusão";
            }
            else
            {
                TxtTitle.Text = "Windows 11 Automatic Setup";
                TxtSubtitle.Text = "Update and optimize automatically";
                TxtStatusTitle.Text = "Process Status";
                TxtStatusDesc.Text = "Ready to start";
                BtnRunAll.Content = "🚀 Start Automatic Process";

                TxtStep1Title.Text = "Update Windows";
                TxtStep1Desc.Text = "Install all available updates";
                TxtStep1Status.Text = "⏳ Pending";

                TxtStep2Title.Text = "Update Drivers";
                TxtStep2Desc.Text = "Check and update drivers";
                TxtStep2Status.Text = "⏳ Pending";

                TxtStep3Title.Text = "Clean System";
                TxtStep3Desc.Text = "Remove unnecessary files";
                TxtStep3Status.Text = "⏳ Pending";

                TxtInfoTitle.Text = "ℹ️ How It Works";
                TxtInfo1.Text = "• Program opens Windows Update automatically";
                TxtInfo2.Text = "• Checks, downloads and installs all updates";
                TxtInfo3.Text = "• Restarts automatically when needed";
                TxtInfo4.Text = "• Fully automatic process until completion";
            }
        }

        private async void BtnRunAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
                return;

            ShowCustomDialog(
                _currentLanguage == "PT" ? "Iniciar Processo Automático" : "Start Automatic Process",
                _currentLanguage == "PT"
                    ? "O processo irá:\n\n1️⃣ Verificar e instalar TODAS as atualizações do Windows\n2️⃣ Reiniciar automaticamente quantas vezes necessário\n3️⃣ Atualizar drivers do sistema\n4️⃣ Limpar arquivos antigos e cache\n\n⚠️ Pode demorar 1-3 HORAS dependendo das atualizações\n⚠️ O PC vai REINICIAR VÁRIAS VEZES\n\n✅ O processo continua automaticamente após cada reinício\n\nDeseja continuar?"
                    : "The process will:\n\n1️⃣ Check and install ALL Windows updates\n2️⃣ Restart automatically as many times as needed\n3️⃣ Update system drivers\n4️⃣ Clean old files and cache\n\n⚠️ May take 1-3 HOURS depending on updates\n⚠️ PC will RESTART MULTIPLE TIMES\n\n✅ Process continues automatically after each restart\n\nDo you want to continue?",
                DialogType.Question,
                async () =>
                {
                    _isProcessing = true;
                    BtnRunAll.IsEnabled = false;
                    LogCard.Visibility = Visibility.Visible;

                    AddLog("═══════════════════════════════════════");
                    AddLog("🚀 " + (_currentLanguage == "PT"
                        ? "Iniciando processo automático..."
                        : "Starting automatic process..."));
                    AddLog("═══════════════════════════════════════\n");

                    _cancellationSource = new CancellationTokenSource();

                    await SetupAutoStart();
                    _state.CurrentPhase = UpdatePhase.WindowsUpdate;
                    _state.UpdateCheckCount = 0;
                    SaveState();

                    await StartProcess();
                },
                () =>
                {
                    AddLog("\n❌ " + (_currentLanguage == "PT"
                        ? "Processo cancelado pelo utilizador"
                        : "Process cancelled by user"));
                }
            );
        }

        private async Task SetupAutoStart()
        {
            try
            {
                string appPath = Process.GetCurrentProcess().MainModule.FileName;

                string taskScript = $@"
$Action = New-ScheduledTaskAction -Execute '{appPath}'
$Trigger = New-ScheduledTaskTrigger -AtLogOn -User '$env:USERNAME'
$Principal = New-ScheduledTaskPrincipal -UserId '$env:USERNAME' -LogonType Interactive -RunLevel Highest
$Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Hours 0) -StartWhenAvailable

Unregister-ScheduledTask -TaskName '{STARTUP_TASK_NAME}' -Confirm:$false -ErrorAction SilentlyContinue
Register-ScheduledTask -TaskName '{STARTUP_TASK_NAME}' -Action $Action -Trigger $Trigger -Principal $Principal -Settings $Settings -Force

Write-Output 'SUCCESS'
";

                var result = await RunPowerShellScript(taskScript);

                if (result.Contains("SUCCESS"))
                {
                    AddLog("✅ " + (_currentLanguage == "PT"
                        ? "Início automático configurado"
                        : "Auto-start configured"));
                }
                else
                {
                    AddLog("⚠️ " + (_currentLanguage == "PT"
                        ? "Erro ao configurar início automático"
                        : "Error configuring auto-start"));
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Setup auto-start error: {ex.Message}");
            }
        }

        private async Task RemoveAutoStart()
        {
            try
            {
                string script = $@"
Unregister-ScheduledTask -TaskName '{STARTUP_TASK_NAME}' -Confirm:$false -ErrorAction SilentlyContinue
Write-Output 'REMOVED'
";

                await RunPowerShellScript(script);
                AddLog("✅ " + (_currentLanguage == "PT"
                    ? "Início automático removido"
                    : "Auto-start removed"));
            }
            catch { }
        }

        private async Task StartProcess()
        {
            await ContinueProcess();
        }

        private async Task ContinueProcess()
        {
            switch (_state.CurrentPhase)
            {
                case UpdatePhase.WindowsUpdate:
                    await RunWindowsUpdate();
                    break;
                case UpdatePhase.DriverUpdate:
                    await RunDriverUpdate();
                    break;
                case UpdatePhase.Cleanup:
                    await RunCleanup();
                    break;
                case UpdatePhase.Completed:
                    await CompleteProcess();
                    break;
            }
        }

        private async Task RunWindowsUpdate()
        {
            AddLog("\n▶️ " + (_currentLanguage == "PT"
                ? $"FASE 1: Atualização do Windows (Passe {_state.UpdateCheckCount + 1}/{MAX_UPDATE_PASSES})"
                : $"PHASE 1: Windows Update (Pass {_state.UpdateCheckCount + 1}/{MAX_UPDATE_PASSES})"));

            UpdateStepStatus(1, _currentLanguage == "PT" ? "🔄 Em progresso..." : "🔄 In progress...", Brushes.Orange);
            AnimateStep(Step1Card, Step1Badge);
            ProgressStep1.Visibility = Visibility.Visible;

            try
            {
                AddLog("🔧 " + (_currentLanguage == "PT"
                    ? "Verificando serviço Windows Update..."
                    : "Checking Windows Update service..."));

                await EnsureWindowsUpdateServiceRunning();
                UpdateProgressBar(ProgressStep1, 5);

                AddLog("📂 " + (_currentLanguage == "PT"
                    ? "Abrindo Windows Update..."
                    : "Opening Windows Update..."));

                await OpenWindowsUpdateSettings();
                await Task.Delay(3000);
                UpdateProgressBar(ProgressStep1, 10);

                AddLog("🔍 " + (_currentLanguage == "PT"
                    ? "Clicando em 'Verificar atualizações'..."
                    : "Clicking 'Check for updates'..."));

                bool clicked = await ClickCheckForUpdatesButton();

                if (!clicked)
                {
                    AddLog("⚠️ " + (_currentLanguage == "PT"
                        ? "Não foi possível clicar automaticamente. Tentando método alternativo..."
                        : "Could not click automatically. Trying alternative method..."));

                    await TriggerWindowsUpdateCheck();
                }

                await Task.Delay(5000);
                UpdateProgressBar(ProgressStep1, 15);

                _state.UpdateCheckCount++;
                SaveState();

                AddLog("⏱️ " + (_currentLanguage == "PT"
                    ? "Monitorando progresso das atualizações..."
                    : "Monitoring update progress..."));

                _monitorTimer.Start();
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error: {ex.Message}");
                UpdateStepStatus(1, "❌ " + (_currentLanguage == "PT" ? "Erro" : "Error"), Brushes.Red);
            }
        }

        private async Task EnsureWindowsUpdateServiceRunning()
        {
            try
            {
                using (ServiceController sc = new ServiceController("wuauserv"))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        AddLog("▶️ Starting Windows Update service...");
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                        await Task.Delay(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Service check error: {ex.Message}");
            }
        }

        private async Task OpenWindowsUpdateSettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:windowsupdate",
                    UseShellExecute = true
                });

                await Task.Delay(2000);

                IntPtr hwnd = FindWindow(null, "Settings");
                if (hwnd != IntPtr.Zero)
                {
                    ShowWindow(hwnd, SW_RESTORE);
                    SetForegroundWindow(hwnd);
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Open settings error: {ex.Message}");
            }
        }

        private async Task<bool> ClickCheckForUpdatesButton()
        {
            try
            {
                string script = @"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$maxAttempts = 15
$attempt = 0
$found = $false

while ($attempt -lt $maxAttempts -and -not $found) {
    try {
        $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Children,
            [System.Windows.Automation.Condition]::TrueCondition
        )

        foreach ($window in $windows) {
            $windowName = $window.Current.Name
            if ($windowName -like '*Settings*' -or $windowName -like '*Configurações*') {
                
                $buttonNames = @(
                    'Check for updates',
                    'Verificar atualizações',
                    'Procurar atualizações'
                )

                foreach ($btnName in $buttonNames) {
                    $button = $window.FindFirst(
                        [System.Windows.Automation.TreeScope]::Descendants,
                        (New-Object System.Windows.Automation.AndCondition(
                            (New-Object System.Windows.Automation.PropertyCondition(
                                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                                [System.Windows.Automation.ControlType]::Button
                            )),
                            (New-Object System.Windows.Automation.PropertyCondition(
                                [System.Windows.Automation.AutomationElement]::NameProperty,
                                $btnName
                            ))
                        ))
                    )

                    if ($button) {
                        $invokePattern = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
                        $invokePattern.Invoke()
                        Write-Output 'BUTTON_CLICKED'
                        $found = $true
                        break
                    }
                }
                
                if ($found) { break }
            }
        }
    } catch {
    }

    if (-not $found) {
        Start-Sleep -Seconds 1
        $attempt++
    }
}

if (-not $found) {
    Write-Output 'BUTTON_NOT_FOUND'
}
";

                var result = await RunPowerShellScript(script);

                if (result.Contains("BUTTON_CLICKED"))
                {
                    AddLog("✅ " + (_currentLanguage == "PT"
                        ? "Botão clicado com sucesso!"
                        : "Button clicked successfully!"));
                    return true;
                }
                else
                {
                    AddLog("⚠️ " + (_currentLanguage == "PT"
                        ? "Botão não encontrado após 15 tentativas"
                        : "Button not found after 15 attempts"));
                    return false;
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Click button error: {ex.Message}");
                return false;
            }
        }

        private async Task TriggerWindowsUpdateCheck()
        {
            try
            {
                string script = @"
$updateSession = New-Object -ComObject Microsoft.Update.Session
$updateSearcher = $updateSession.CreateUpdateSearcher()

$searchResult = $updateSearcher.Search('IsInstalled=0')
Write-Output ""FOUND:$($searchResult.Updates.Count)""
";

                var result = await RunPowerShellScript(script);
                AddLog($"📊 {result.Trim()}");
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Trigger update error: {ex.Message}");
            }
        }

        private async void MonitorTimer_Tick(object sender, EventArgs e)
        {
            if (_cancellationSource?.Token.IsCancellationRequested == true)
            {
                _monitorTimer.Stop();
                return;
            }

            try
            {
                var status = await GetWindowsUpdateStatus();

                int baseProgress = 15 + ((_state.UpdateCheckCount - 1) * 10);
                int currentProgress = baseProgress + (status.ProgressPercentage / 10);
                UpdateProgressBar(ProgressStep1, Math.Min(currentProgress, 90));

                AddLog($"📊 {status.StatusMessage} ({status.ProgressPercentage}%)");

                if (status.RebootRequired)
                {
                    _monitorTimer.Stop();
                    AddLog("🔄 " + (_currentLanguage == "PT"
                        ? "REINÍCIO NECESSÁRIO - Aguardando instalação completa..."
                        : "REBOOT REQUIRED - Waiting for installation to complete..."));

                    await Task.Delay(10000);
                    await InitiateRestart();
                }
                else if (status.IsInstalling)
                {
                    AddLog("📥 " + (_currentLanguage == "PT"
                        ? $"Instalando atualizações... {status.UpdatesInstalled}/{status.TotalUpdates}"
                        : $"Installing updates... {status.UpdatesInstalled}/{status.TotalUpdates}"));
                }
                else if (status.IsDownloading)
                {
                    AddLog("⬇️ " + (_currentLanguage == "PT"
                        ? $"Baixando atualizações... {status.UpdatesDownloaded}/{status.TotalUpdates}"
                        : $"Downloading updates... {status.UpdatesDownloaded}/{status.TotalUpdates}"));
                }
                else if (status.IsChecking)
                {
                    AddLog("🔍 " + (_currentLanguage == "PT"
                        ? "Verificando atualizações..."
                        : "Checking for updates..."));
                }
                else if (status.NoUpdatesAvailable)
                {
                    _monitorTimer.Stop();

                    if (_state.UpdateCheckCount >= MAX_UPDATE_PASSES)
                    {
                        AddLog("✅ " + (_currentLanguage == "PT"
                            ? "Windows completamente atualizado! Máximo de passes atingido."
                            : "Windows fully updated! Maximum passes reached."));

                        await CompleteWindowsUpdate();
                    }
                    else
                    {
                        AddLog("🔄 " + (_currentLanguage == "PT"
                            ? "Verificando novamente... (pode haver mais atualizações)"
                            : "Checking again... (may have more updates)"));

                        await Task.Delay(5000);
                        await RunWindowsUpdate();
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Monitor error: {ex.Message}");
            }
        }

        private async Task<WindowsUpdateStatus> GetWindowsUpdateStatus()
        {
            string script = @"
$status = @{
    IsChecking = $false
    IsDownloading = $false
    IsInstalling = $false
    RebootRequired = $false
    NoUpdatesAvailable = $false
    TotalUpdates = 0
    UpdatesDownloaded = 0
    UpdatesInstalled = 0
    ProgressPercentage = 0
    StatusMessage = 'Unknown'
}

try {
    $rebootPending = $false
    $rebootKeys = @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired',
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending',
        'HKLM:\SOFTWARE\Microsoft\Updates\UpdateExeVolatile'
    )
    
    foreach ($key in $rebootKeys) {
        if (Test-Path $key) {
            $rebootPending = $true
            break
        }
    }

    if ($rebootPending) {
        $status.RebootRequired = $true
        $status.StatusMessage = 'Reboot Required'
        $status.ProgressPercentage = 100
    } else {
        $updateSession = New-Object -ComObject Microsoft.Update.Session
        $updateSearcher = $updateSession.CreateUpdateSearcher()

        $searchResult = $updateSearcher.Search('IsInstalled=0')
        $pendingUpdates = $searchResult.Updates
        
        $status.TotalUpdates = $pendingUpdates.Count

        if ($pendingUpdates.Count -eq 0) {
            $status.NoUpdatesAvailable = $true
            $status.StatusMessage = 'No Updates Available'
            $status.ProgressPercentage = 100
        } else {
            $downloading = 0
            $readyToInstall = 0
            
            foreach ($update in $pendingUpdates) {
                if ($update.IsDownloaded) {
                    $readyToInstall++
                } else {
                    $downloading++
                }
            }

            $status.UpdatesDownloaded = $readyToInstall
            
            if ($readyToInstall -gt 0) {
                $status.IsInstalling = $true
                $status.StatusMessage = 'Installing Updates'
                $status.ProgressPercentage = 50 + (($readyToInstall / $pendingUpdates.Count) * 50)
            } elseif ($downloading -gt 0) {
                $status.IsDownloading = $true
                $status.StatusMessage = 'Downloading Updates'
                $status.ProgressPercentage = 25
            } else {
                $status.IsChecking = $true
                $status.StatusMessage = 'Checking for Updates'
                $status.ProgressPercentage = 10
            }
        }
    }
} catch {
    $status.StatusMessage = 'Error: ' + $_.Exception.Message
}

$status | ConvertTo-Json -Compress
";

            try
            {
                var result = await RunPowerShellScript(script);
                var status = System.Text.Json.JsonSerializer.Deserialize<WindowsUpdateStatus>(result);
                return status ?? new WindowsUpdateStatus { StatusMessage = "Unknown", ProgressPercentage = 0 };
            }
            catch
            {
                return new WindowsUpdateStatus { StatusMessage = "Error checking status", ProgressPercentage = 0 };
            }
        }

        private async Task CompleteWindowsUpdate()
        {
            UpdateProgressBar(ProgressStep1, 100);
            UpdateStepStatus(1, "✅ " + (_currentLanguage == "PT" ? "Concluído" : "Completed"), Brushes.LightGreen);

            AddLog("✅ " + (_currentLanguage == "PT"
                ? "Fase de atualização do Windows concluída!"
                : "Windows update phase completed!"));

            _state.CurrentPhase = UpdatePhase.DriverUpdate;
            _state.UpdateCheckCount = 0;
            SaveState();

            await Task.Delay(2000);
            await RunDriverUpdate();
        }

        private async Task InitiateRestart()
        {
            _state.RestartCount++;
            _state.WaitingForRestart = true;
            SaveState();

            ShowCustomDialog(
                _currentLanguage == "PT" ? "Reinício Automático" : "Automatic Restart",
                _currentLanguage == "PT"
                    ? $"O Windows precisa reiniciar para continuar.\n\n📊 Progresso:\n• Passe: {_state.UpdateCheckCount}/{MAX_UPDATE_PASSES}\n• Reinício: #{_state.RestartCount}\n\n✅ O processo continuará automaticamente após o reinício.\n\n⏱️ O sistema reiniciará em 60 segundos.\n\nClique em 'Sim' para reiniciar agora ou 'Não' para cancelar."
                    : $"Windows needs to restart to continue.\n\n📊 Progress:\n• Pass: {_state.UpdateCheckCount}/{MAX_UPDATE_PASSES}\n• Restart: #{_state.RestartCount}\n\n✅ Process will continue automatically after restart.\n\n⏱️ System will restart in 60 seconds.\n\nClick 'Yes' to restart now or 'No' to cancel.",
                DialogType.Question,
                () =>
                {
                    AddLog("🔄 " + (_currentLanguage == "PT"
                        ? "Iniciando reinício..."
                        : "Initiating restart..."));

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown.exe",
                        Arguments = "/r /t 10 /c \"Nexor - Continuando processo de atualização\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                },
                () =>
                {
                    AddLog("❌ " + (_currentLanguage == "PT"
                        ? "Reinício cancelado. O processo foi interrompido."
                        : "Restart cancelled. Process interrupted."));

                    _state.WaitingForRestart = false;
                    SaveState();
                    _isProcessing = false;
                }
            );
        }

        private async Task RunDriverUpdate()
        {
            AddLog("\n▶️ " + (_currentLanguage == "PT"
                ? "FASE 2: Atualização de Drivers"
                : "PHASE 2: Driver Update"));

            UpdateStepStatus(2, _currentLanguage == "PT" ? "🔄 Em progresso..." : "🔄 In progress...", Brushes.Orange);
            AnimateStep(Step2Card, Step2Badge);
            ProgressStep2.Visibility = Visibility.Visible;

            try
            {
                UpdateProgressBar(ProgressStep2, 10);

                AddLog("🔍 " + (_currentLanguage == "PT"
                    ? "Verificando atualizações de drivers..."
                    : "Checking for driver updates..."));

                string driverScript = @"
try {
    $Session = New-Object -ComObject Microsoft.Update.Session
    $Searcher = $Session.CreateUpdateSearcher()
    
    $Searcher.ServiceID = '7971f918-a847-4430-9279-4a52d1efe18d'
    $Searcher.SearchScope = 1
    $Searcher.ServerSelection = 3
    
    $Result = $Searcher.Search(""IsInstalled=0 and Type='Driver'"")
    
    $driverCount = $Result.Updates.Count
    Write-Output ""DRIVERS_FOUND:$driverCount""
    
    if ($driverCount -gt 0) {
        $Downloader = $Session.CreateUpdateDownloader()
        $Downloader.Updates = $Result.Updates
        
        Write-Output 'DOWNLOADING_DRIVERS'
        $DownloadResult = $Downloader.Download()
        
        if ($DownloadResult.ResultCode -eq 2) {
            Write-Output 'DOWNLOAD_SUCCESS'
            
            $Installer = $Session.CreateUpdateInstaller()
            $Installer.Updates = $Result.Updates
            
            Write-Output 'INSTALLING_DRIVERS'
            $InstallResult = $Installer.Install()
            
            if ($InstallResult.ResultCode -eq 2) {
                Write-Output 'INSTALL_SUCCESS'
            } else {
                Write-Output ""INSTALL_FAILED:$($InstallResult.ResultCode)""
            }
        } else {
            Write-Output ""DOWNLOAD_FAILED:$($DownloadResult.ResultCode)""
        }
    } else {
        Write-Output 'NO_DRIVER_UPDATES'
    }
} catch {
    Write-Output ""ERROR:$($_.Exception.Message)""
}
";

                UpdateProgressBar(ProgressStep2, 30);

                var result = await RunPowerShellScript(driverScript);

                UpdateProgressBar(ProgressStep2, 50);

                if (result.Contains("NO_DRIVER_UPDATES"))
                {
                    AddLog("✅ " + (_currentLanguage == "PT"
                        ? "Todos os drivers estão atualizados"
                        : "All drivers are up to date"));
                }
                else if (result.Contains("INSTALL_SUCCESS"))
                {
                    AddLog("✅ " + (_currentLanguage == "PT"
                        ? "Drivers instalados com sucesso!"
                        : "Drivers installed successfully!"));
                }
                else if (result.Contains("DRIVERS_FOUND"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(result, @"DRIVERS_FOUND:(\d+)");
                    if (match.Success)
                    {
                        AddLog($"📦 " + (_currentLanguage == "PT"
                            ? $"Encontrados {match.Groups[1].Value} drivers para atualizar"
                            : $"Found {match.Groups[1].Value} drivers to update"));
                    }
                }

                UpdateProgressBar(ProgressStep2, 70);

                AddLog("🔧 " + (_currentLanguage == "PT"
                    ? "Verificando drivers através do Gestor de Dispositivos..."
                    : "Checking drivers through Device Manager..."));

                await ScanForHardwareChanges();

                UpdateProgressBar(ProgressStep2, 90);

                var problemDrivers = await CheckForProblemDevices();

                if (problemDrivers.Count > 0)
                {
                    AddLog("⚠️ " + (_currentLanguage == "PT"
                        ? $"Encontrados {problemDrivers.Count} dispositivos com problemas:"
                        : $"Found {problemDrivers.Count} devices with problems:"));

                    foreach (var driver in problemDrivers)
                    {
                        AddLog($"   • {driver}");
                    }

                    AddLog("💡 " + (_currentLanguage == "PT"
                        ? "Recomenda-se atualizar estes drivers manualmente se necessário."
                        : "Recommend updating these drivers manually if needed."));
                }

                UpdateProgressBar(ProgressStep2, 100);
                UpdateStepStatus(2, "✅ " + (_currentLanguage == "PT" ? "Concluído" : "Completed"), Brushes.LightGreen);

                AddLog("✅ " + (_currentLanguage == "PT"
                    ? "Fase de atualização de drivers concluída!"
                    : "Driver update phase completed!"));

                _state.CurrentPhase = UpdatePhase.Cleanup;
                SaveState();

                await Task.Delay(2000);
                await RunCleanup();
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error: {ex.Message}");
                UpdateStepStatus(2, "❌ " + (_currentLanguage == "PT" ? "Erro" : "Error"), Brushes.Red);

                _state.CurrentPhase = UpdatePhase.Cleanup;
                SaveState();
                await Task.Delay(2000);
                await RunCleanup();
            }
        }

        private async Task ScanForHardwareChanges()
        {
            try
            {
                string script = @"
Get-PnpDevice | ForEach-Object { 
    $device = $_
    try {
        $device | Invoke-CimMethod -MethodName Enable -ErrorAction SilentlyContinue
    } catch {}
}
pnputil /scan-devices
Write-Output 'SCAN_COMPLETE'
";

                var result = await RunPowerShellScript(script);

                if (result.Contains("SCAN_COMPLETE"))
                {
                    AddLog("✅ " + (_currentLanguage == "PT"
                        ? "Varredura de hardware concluída"
                        : "Hardware scan completed"));
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Hardware scan error: {ex.Message}");
            }
        }

        private async Task<List<string>> CheckForProblemDevices()
        {
            var problemDevices = new List<string>();

            try
            {
                string script = @"
Get-PnpDevice | Where-Object { $_.Status -ne 'OK' } | ForEach-Object {
    Write-Output ""PROBLEM:$($_.FriendlyName)|$($_.Status)""
}
";

                var result = await RunPowerShellScript(script);
                var lines = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (line.StartsWith("PROBLEM:"))
                    {
                        var parts = line.Substring(8).Split('|');
                        if (parts.Length >= 2)
                        {
                            problemDevices.Add($"{parts[0]} ({parts[1]})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Problem device check error: {ex.Message}");
            }

            return problemDevices;
        }

        private async Task RunCleanup()
        {
            AddLog("\n▶️ " + (_currentLanguage == "PT"
                ? "FASE 3: Limpeza do Sistema"
                : "PHASE 3: System Cleanup"));

            UpdateStepStatus(3, _currentLanguage == "PT" ? "🔄 Em progresso..." : "🔄 In progress...", Brushes.Orange);
            AnimateStep(Step3Card, Step3Badge);
            ProgressStep3.Visibility = Visibility.Visible;

            try
            {
                long totalFreed = 0;
                int filesDeleted = 0;

                UpdateProgressBar(ProgressStep3, 5);

                AddLog("🧹 " + (_currentLanguage == "PT"
                    ? "Limpando cache do Windows Update..."
                    : "Cleaning Windows Update cache..."));

                await StopWindowsUpdateService();
                await Task.Delay(2000);

                var (freed1, count1) = CleanDirectory(@"C:\Windows\SoftwareDistribution\Download");
                totalFreed += freed1;
                filesDeleted += count1;
                AddLog($"   ✓ {FormatBytes(freed1)} libertados, {count1} arquivos");

                await StartWindowsUpdateService();
                UpdateProgressBar(ProgressStep3, 15);

                AddLog("🧹 " + (_currentLanguage == "PT"
                    ? "Limpando arquivos temporários do Windows..."
                    : "Cleaning Windows temporary files..."));

                var (freed2, count2) = CleanDirectory(@"C:\Windows\Temp");
                totalFreed += freed2;
                filesDeleted += count2;
                AddLog($"   ✓ {FormatBytes(freed2)} libertados, {count2} arquivos");
                UpdateProgressBar(ProgressStep3, 25);

                AddLog("🧹 " + (_currentLanguage == "PT"
                    ? "Limpando arquivos temporários do utilizador..."
                    : "Cleaning user temporary files..."));

                var (freed3, count3) = CleanDirectory(Path.GetTempPath());
                totalFreed += freed3;
                filesDeleted += count3;
                AddLog($"   ✓ {FormatBytes(freed3)} libertados, {count3} arquivos");
                UpdateProgressBar(ProgressStep3, 35);

                AddLog("🧹 " + (_currentLanguage == "PT"
                    ? "Limpando Prefetch..."
                    : "Cleaning Prefetch..."));

                var (freed4, count4) = CleanDirectory(@"C:\Windows\Prefetch");
                totalFreed += freed4;
                filesDeleted += count4;
                AddLog($"   ✓ {FormatBytes(freed4)} libertados, {count4} arquivos");
                UpdateProgressBar(ProgressStep3, 45);

                AddLog("🧹 " + (_currentLanguage == "PT"
                    ? "Limpando logs do Windows..."
                    : "Cleaning Windows logs..."));

                var (freed5, count5) = CleanDirectory(@"C:\Windows\Logs");
                totalFreed += freed5;
                filesDeleted += count5;
                AddLog($"   ✓ {FormatBytes(freed5)} libertados, {count5} arquivos");
                UpdateProgressBar(ProgressStep3, 55);

                AddLog("🗑️ " + (_currentLanguage == "PT"
                    ? "Esvaziando Reciclagem..."
                    : "Emptying Recycle Bin..."));

                await RunPowerShellCommand("Clear-RecycleBin -Force -ErrorAction SilentlyContinue");
                UpdateProgressBar(ProgressStep3, 65);

                AddLog("🧹 " + (_currentLanguage == "PT"
                    ? "Executando limpeza de disco..."
                    : "Running disk cleanup..."));

                await RunDiskCleanup();
                UpdateProgressBar(ProgressStep3, 75);

                AddLog("🧹 " + (_currentLanguage == "PT"
                    ? "Removendo versões antigas do Windows (isto pode demorar)..."
                    : "Removing old Windows versions (this may take a while)..."));

                await RunDismCleanup();
                UpdateProgressBar(ProgressStep3, 95);

                AddLog("🧹 " + (_currentLanguage == "PT"
                    ? "Limpando cache do Windows Installer..."
                    : "Cleaning Windows Installer cache..."));

                var (freed6, count6) = CleanDirectory(@"C:\Windows\Installer\$PatchCache$");
                totalFreed += freed6;
                filesDeleted += count6;
                AddLog($"   ✓ {FormatBytes(freed6)} libertados, {count6} arquivos");

                UpdateProgressBar(ProgressStep3, 100);

                AddLog($"\n📊 " + (_currentLanguage == "PT"
                    ? $"TOTAL LIBERTADO: {FormatBytes(totalFreed)}"
                    : $"TOTAL FREED: {FormatBytes(totalFreed)}"));
                AddLog($"📊 " + (_currentLanguage == "PT"
                    ? $"ARQUIVOS REMOVIDOS: {filesDeleted}"
                    : $"FILES REMOVED: {filesDeleted}"));

                UpdateStepStatus(3, $"✅ " + (_currentLanguage == "PT" ? "Concluído" : "Completed") + $" ({FormatBytes(totalFreed)})", Brushes.LightGreen);

                AddLog("✅ " + (_currentLanguage == "PT"
                    ? "Fase de limpeza do sistema concluída!"
                    : "System cleanup phase completed!"));

                _state.CurrentPhase = UpdatePhase.Completed;
                SaveState();

                await Task.Delay(2000);
                await CompleteProcess();
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error: {ex.Message}");
                UpdateStepStatus(3, "❌ " + (_currentLanguage == "PT" ? "Erro" : "Error"), Brushes.Red);

                _state.CurrentPhase = UpdatePhase.Completed;
                SaveState();
                await Task.Delay(2000);
                await CompleteProcess();
            }
        }

        private async Task StopWindowsUpdateService()
        {
            try
            {
                using (ServiceController sc = new ServiceController("wuauserv"))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        AddLog("⏸️ Parando serviço Windows Update...");
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Stop service error: {ex.Message}");
            }
        }

        private async Task StartWindowsUpdateService()
        {
            try
            {
                using (ServiceController sc = new ServiceController("wuauserv"))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        AddLog("▶️ Reiniciando serviço Windows Update...");
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Start service error: {ex.Message}");
            }
        }

        private async Task RunDiskCleanup()
        {
            try
            {
                string script = @"
$SageSet = 'StateFlags0100'
$BaseKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\'

$Items = @(
    'Active Setup Temp Folders',
    'Downloaded Program Files',
    'Internet Cache Files',
    'Memory Dump Files',
    'Old ChkDsk Files',
    'Previous Installations',
    'Recycle Bin',
    'Setup Log Files',
    'System error memory dump files',
    'System error minidump files',
    'Temporary Files',
    'Temporary Setup Files',
    'Thumbnail Cache',
    'Update Cleanup',
    'Upgrade Discarded Files',
    'Windows Error Reporting Archive Files',
    'Windows Error Reporting Queue Files',
    'Windows Error Reporting System Archive Files',
    'Windows Error Reporting System Queue Files',
    'Windows Upgrade Log Files'
)

foreach ($Item in $Items) {
    $Key = $BaseKey + $Item
    if (Test-Path $Key) {
        Set-ItemProperty -Path $Key -Name $SageSet -Value 2 -Type DWord -ErrorAction SilentlyContinue
    }
}

Start-Process -FilePath 'cleanmgr.exe' -ArgumentList ""/sagerun:100"" -Wait -NoNewWindow
Write-Output 'CLEANUP_COMPLETE'
";

                var result = await RunPowerShellScript(script);

                if (result.Contains("CLEANUP_COMPLETE"))
                {
                    AddLog("   ✓ Limpeza de disco concluída");
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Disk cleanup error: {ex.Message}");
            }
        }

        private async Task RunDismCleanup()
        {
            try
            {
                AddLog("   • Executando DISM /StartComponentCleanup...");
                await RunCommand("Dism.exe", "/online /Cleanup-Image /StartComponentCleanup /ResetBase");

                await Task.Delay(2000);

                AddLog("   • Executando DISM /AnalyzeComponentStore...");
                await RunCommand("Dism.exe", "/online /Cleanup-Image /AnalyzeComponentStore");

                AddLog("   ✓ DISM cleanup concluído");
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ DISM cleanup error: {ex.Message}");
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async Task CompleteProcess()
        {
            AddLog("\n═══════════════════════════════════════");
            AddLog("🎉 " + (_currentLanguage == "PT"
                ? "PROCESSO CONCLUÍDO COM SUCESSO!"
                : "PROCESS COMPLETED SUCCESSFULLY!"));
            AddLog("═══════════════════════════════════════");
            AddLog($"📊 Passes de atualização: {_state.UpdateCheckCount}");
            AddLog($"🔄 Reinícios: {_state.RestartCount}");
            AddLog($"⏱️ Concluído em: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            await RemoveAutoStart();
            ClearState();

            Dispatcher.Invoke(() =>
            {
                TxtStatusDesc.Text = "✅ " + (_currentLanguage == "PT"
                    ? "Configuração completa!"
                    : "Setup complete!");
                BtnRunAll.IsEnabled = true;
                _isProcessing = false;

                AnimateCompletion();
            });

            ShowCustomNotification(
                _currentLanguage == "PT" ? "Sucesso Total!" : "Complete Success!",
                _currentLanguage == "PT"
                    ? $"🎉 Processo totalmente concluído!\n\n✅ Windows: Totalmente atualizado ({_state.UpdateCheckCount} passes)\n✅ Drivers: Verificados e atualizados\n✅ Sistema: Limpo e otimizado\n🔄 Reinícios automáticos: {_state.RestartCount}\n\n💡 Recomendação: Reinicie o PC uma última vez para garantir que todas as mudanças sejam aplicadas."
                    : $"🎉 Process fully completed!\n\n✅ Windows: Fully updated ({_state.UpdateCheckCount} passes)\n✅ Drivers: Checked and updated\n✅ System: Cleaned and optimized\n🔄 Automatic restarts: {_state.RestartCount}\n\n💡 Recommendation: Restart PC one last time to ensure all changes are applied.",
                NotificationType.Success
            );
        }

        private async Task<bool> RunCommand(string fileName, string arguments)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    var process = Process.Start(psi);
                    process?.WaitForExit();
                    return process?.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    AddLog($"⚠️ Command error: {ex.Message}");
                    return false;
                }
            });
        }

        private async Task<string> RunPowerShellCommand(string command)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    var process = Process.Start(psi);
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        return output;
                    }
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            });
        }

        private async Task<string> RunPowerShellScript(string script)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string tempFile = Path.Combine(Path.GetTempPath(), $"nexor_{Guid.NewGuid()}.ps1");
                    File.WriteAllText(tempFile, script, Encoding.UTF8);

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    var process = Process.Start(psi);
                    string output = string.Empty;
                    if (process != null)
                    {
                        output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                    }

                    try { File.Delete(tempFile); } catch { }

                    return output;
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            });
        }

        private (long, int) CleanDirectory(string path)
        {
            long bytesFreed = 0;
            int filesCount = 0;

            try
            {
                if (!Directory.Exists(path))
                    return (0, 0);

                DirectoryInfo di = new DirectoryInfo(path);

                foreach (FileInfo file in di.GetFiles())
                {
                    try
                    {
                        long fileSize = file.Length;
                        file.Delete();
                        bytesFreed += fileSize;
                        filesCount++;
                    }
                    catch { }
                }

                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    try
                    {
                        var (freed, count) = GetDirectorySizeAndCount(dir);
                        dir.Delete(true);
                        bytesFreed += freed;
                        filesCount += count;
                    }
                    catch { }
                }
            }
            catch { }

            return (bytesFreed, filesCount);
        }

        private (long, int) GetDirectorySizeAndCount(DirectoryInfo directory)
        {
            long size = 0;
            int count = 0;

            try
            {
                FileInfo[] files = directory.GetFiles();
                foreach (FileInfo file in files)
                {
                    size += file.Length;
                    count++;
                }

                DirectoryInfo[] subdirs = directory.GetDirectories();
                foreach (DirectoryInfo subdir in subdirs)
                {
                    var (subSize, subCount) = GetDirectorySizeAndCount(subdir);
                    size += subSize;
                    count += subCount;
                }
            }
            catch { }

            return (size, count);
        }

        private void UpdateStepStatus(int step, string text, Brush color)
        {
            Dispatcher.Invoke(() =>
            {
                switch (step)
                {
                    case 1:
                        TxtStep1Status.Text = text;
                        TxtStep1Status.Foreground = color;
                        break;
                    case 2:
                        TxtStep2Status.Text = text;
                        TxtStep2Status.Foreground = color;
                        break;
                    case 3:
                        TxtStep3Status.Text = text;
                        TxtStep3Status.Foreground = color;
                        break;
                }
            });
        }

        private void UpdateProgressBar(ProgressBar progressBar, double value)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = Math.Min(value, 100);
            });
        }

        private void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtLog.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";

                if (TxtLog?.Parent is ScrollViewer scrollViewer)
                {
                    scrollViewer.ScrollToEnd();
                }
            });
        }

        private void AnimateStep(Border card, Border badge)
        {
            Dispatcher.Invoke(() =>
            {
                var scaleTransform = new ScaleTransform(1, 1);
                card.RenderTransform = scaleTransform;
                card.RenderTransformOrigin = new Point(0.5, 0.5);

                var cardAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 1.02,
                    Duration = TimeSpan.FromMilliseconds(300),
                    AutoReverse = true,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, cardAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, cardAnimation);

                var badgeRotate = new RotateTransform(0);
                badge.RenderTransform = badgeRotate;
                badge.RenderTransformOrigin = new Point(0.5, 0.5);

                var rotateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromMilliseconds(800),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                badgeRotate.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
            });
        }

        private void AnimateCompletion()
        {
            Dispatcher.Invoke(() =>
            {
                var scaleTransform = new ScaleTransform(1, 1);
                ProgressCircleBorder.RenderTransform = scaleTransform;
                ProgressCircleBorder.RenderTransformOrigin = new Point(0.5, 0.5);

                var pulseAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 1.2,
                    Duration = TimeSpan.FromMilliseconds(400),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(3),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);

                var colorAnimation = new ColorAnimation
                {
                    To = Color.FromRgb(16, 185, 129),
                    Duration = TimeSpan.FromMilliseconds(500)
                };

                if (ProgressCircleBorder.Background is SolidColorBrush brush)
                {
                    brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                }
            });
        }

        private enum DialogType { Question, Warning, Error, Info }
        private enum NotificationType { Success, Error, Warning, Info }

        private void ShowCustomDialog(string title, string message, DialogType type, Action onYes, Action onNo)
        {
            Dispatcher.Invoke(() =>
            {
                DialogOverlay.Visibility = Visibility.Visible;
                DialogTitle.Text = title;
                DialogMessage.Text = message;

                switch (type)
                {
                    case DialogType.Question:
                        DialogIcon.Text = "❓";
                        DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                        DialogBtnYes.Visibility = Visibility.Visible;
                        DialogBtnNo.Visibility = onNo != null ? Visibility.Visible : Visibility.Collapsed;
                        DialogBtnOk.Visibility = Visibility.Collapsed;
                        break;
                    case DialogType.Warning:
                        DialogIcon.Text = "⚠️";
                        DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                        DialogBtnYes.Visibility = onYes != null ? Visibility.Visible : Visibility.Collapsed;
                        DialogBtnNo.Visibility = onNo != null ? Visibility.Visible : Visibility.Collapsed;
                        DialogBtnOk.Visibility = (onYes == null && onNo == null) ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case DialogType.Error:
                        DialogIcon.Text = "❌";
                        DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                        DialogBtnYes.Visibility = Visibility.Collapsed;
                        DialogBtnNo.Visibility = Visibility.Collapsed;
                        DialogBtnOk.Visibility = Visibility.Visible;
                        break;
                    case DialogType.Info:
                        DialogIcon.Text = "ℹ️";
                        DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                        DialogBtnYes.Visibility = Visibility.Collapsed;
                        DialogBtnNo.Visibility = Visibility.Collapsed;
                        DialogBtnOk.Visibility = Visibility.Visible;
                        break;
                }

                DialogBtnYes.Click -= DialogBtnYes_Click;
                DialogBtnNo.Click -= DialogBtnNo_Click;
                DialogBtnOk.Click -= DialogBtnOk_Click;

                if (onYes != null)
                {
                    DialogBtnYes.Click += (s, e) =>
                    {
                        AnimateDialogClose();
                        onYes?.Invoke();
                    };
                }

                if (onNo != null)
                {
                    DialogBtnNo.Click += (s, e) =>
                    {
                        AnimateDialogClose();
                        onNo?.Invoke();
                    };
                }

                DialogBtnOk.Click += (s, e) =>
                {
                    AnimateDialogClose();
                };

                AnimateDialogOpen();
            });
        }

        private void DialogBtnYes_Click(object sender, RoutedEventArgs e) { }
        private void DialogBtnNo_Click(object sender, RoutedEventArgs e) { }
        private void DialogBtnOk_Click(object sender, RoutedEventArgs e) { }

        private void ShowCustomNotification(string title, string message, NotificationType type)
        {
            Dispatcher.Invoke(() =>
            {
                NotificationTitle.Text = title;
                NotificationMessage.Text = message;

                switch (type)
                {
                    case NotificationType.Success:
                        NotificationIcon.Text = "✅";
                        NotificationBorder.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                        break;
                    case NotificationType.Error:
                        NotificationIcon.Text = "❌";
                        NotificationBorder.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                        break;
                    case NotificationType.Warning:
                        NotificationIcon.Text = "⚠️";
                        NotificationBorder.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                        break;
                    case NotificationType.Info:
                        NotificationIcon.Text = "ℹ️";
                        NotificationBorder.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                        break;
                }

                AnimateNotificationIn();

                Task.Run(async () =>
                {
                    await Task.Delay(8000);
                    Dispatcher.Invoke(() => AnimateNotificationOut());
                });
            });
        }

        private void AnimateDialogOpen()
        {
            DialogOverlay.Opacity = 0;
            DialogBox.RenderTransform = new ScaleTransform(0.7, 0.7);
            DialogBox.RenderTransformOrigin = new Point(0.5, 0.5);

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            DialogOverlay.BeginAnimation(OpacityProperty, fadeIn);

            var scaleX = new DoubleAnimation(0.7, 1, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };
            var scaleY = new DoubleAnimation(0.7, 1, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };

            ((ScaleTransform)DialogBox.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            ((ScaleTransform)DialogBox.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        }

        private void AnimateDialogClose()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => { DialogOverlay.Visibility = Visibility.Collapsed; };
            DialogOverlay.BeginAnimation(OpacityProperty, fadeOut);

            var scaleX = new DoubleAnimation(1, 0.7, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var scaleY = new DoubleAnimation(1, 0.7, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            ((ScaleTransform)DialogBox.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            ((ScaleTransform)DialogBox.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        }

        private void AnimateNotificationIn()
        {
            NotificationPanel.Visibility = Visibility.Visible;
            NotificationPanel.RenderTransform = new TranslateTransform(400, 0);

            var slideIn = new DoubleAnimation(400, 0, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            ((TranslateTransform)NotificationPanel.RenderTransform).BeginAnimation(TranslateTransform.XProperty, slideIn);
        }

        private void AnimateNotificationOut()
        {
            var slideOut = new DoubleAnimation(0, 400, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            slideOut.Completed += (s, e) => { NotificationPanel.Visibility = Visibility.Collapsed; };

            ((TranslateTransform)NotificationPanel.RenderTransform).BeginAnimation(TranslateTransform.XProperty, slideOut);
        }

        private async void BtnStep1_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
                return;

            _isProcessing = true;
            BtnStep1.IsEnabled = false;
            LogCard.Visibility = Visibility.Visible;

            AddLog("═══════════════════════════════════════");
            AddLog("🚀 " + (_currentLanguage == "PT"
                ? "Iniciando Atualização do Windows..."
                : "Starting Windows Update..."));
            AddLog("═══════════════════════════════════════\n");

            _cancellationSource = new CancellationTokenSource();
            await SetupAutoStart();
            _state.CurrentPhase = UpdatePhase.WindowsUpdate;
            _state.UpdateCheckCount = 0;
            SaveState();
            await RunWindowsUpdate();

            _isProcessing = false;
            BtnStep1.IsEnabled = true;
        }

        private async void BtnStep2_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
                return;

            _isProcessing = true;
            BtnStep2.IsEnabled = false;
            LogCard.Visibility = Visibility.Visible;

            AddLog("═══════════════════════════════════════");
            AddLog("🚀 " + (_currentLanguage == "PT"
                ? "Iniciando Atualização de Drivers..."
                : "Starting Driver Update..."));
            AddLog("═══════════════════════════════════════\n");

            _state.CurrentPhase = UpdatePhase.DriverUpdate;
            SaveState();
            await RunDriverUpdate();

            _isProcessing = false;
            BtnStep2.IsEnabled = true;
        }

        private async void BtnStep3_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
                return;

            _isProcessing = true;
            BtnStep3.IsEnabled = false;
            LogCard.Visibility = Visibility.Visible;

            AddLog("═══════════════════════════════════════");
            AddLog("🚀 " + (_currentLanguage == "PT"
                ? "Iniciando Limpeza do Sistema..."
                : "Starting System Cleanup..."));
            AddLog("═══════════════════════════════════════\n");

            _state.CurrentPhase = UpdatePhase.Cleanup;
            SaveState();
            await RunCleanup();

            _isProcessing = false;
            BtnStep3.IsEnabled = true;
        }
    }
}