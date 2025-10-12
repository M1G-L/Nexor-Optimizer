using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
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
    }

    public partial class FreshSetupPage : UserControl
    {
        private const string STATE_FILE = "nexor_state.json";
        private const string STARTUP_TASK_NAME = "NexorAutoResume";
        private const int MAX_UPDATE_CHECKS = 5;
        private const int CHECK_INTERVAL_SECONDS = 30;

        private UpdateState _state;
        private DispatcherTimer _updateCheckTimer;
        private readonly string _currentLanguage;
        private bool _isProcessing = false;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public FreshSetupPage(string language = "PT")
        {
            InitializeComponent();
            _currentLanguage = language;
            SetLanguage(language);

            LoadState();
            CheckAdminPrivileges();

            // Initialize timer for checking Windows Update status
            _updateCheckTimer = new DispatcherTimer();
            _updateCheckTimer.Interval = TimeSpan.FromSeconds(CHECK_INTERVAL_SECONDS);
            _updateCheckTimer.Tick += UpdateCheckTimer_Tick;

            // If we resumed from restart, continue the process
            if (_state.CurrentPhase != UpdatePhase.Completed)
            {
                ShowResumeDialog();
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
                }
                else
                {
                    _state = new UpdateState
                    {
                        CurrentPhase = UpdatePhase.WindowsUpdate,
                        UpdateCheckCount = 0,
                        RestartCount = 0,
                        LastCheckTime = DateTime.MinValue
                    };
                }
            }
            catch
            {
                _state = new UpdateState
                {
                    CurrentPhase = UpdatePhase.WindowsUpdate,
                    UpdateCheckCount = 0,
                    RestartCount = 0,
                    LastCheckTime = DateTime.MinValue
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
                    LastCheckTime = DateTime.Now
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
                    ? $"Foi detectado um processo em andamento.\n\nFase: {phase}\nVerificações: {_state.UpdateCheckCount}/{MAX_UPDATE_CHECKS}\nReinícios: {_state.RestartCount}\n\nDeseja continuar?"
                    : $"A process in progress was detected.\n\nPhase: {phase}\nChecks: {_state.UpdateCheckCount}/{MAX_UPDATE_CHECKS}\nRestarts: {_state.RestartCount}\n\nDo you want to continue?",
                DialogType.Question,
                () => Task.Run(() => ContinueProcess()),
                () => ClearState()
            );
        }

        private void CheckAdminPrivileges()
        {
            bool isAdmin = new System.Security.Principal.WindowsPrincipal(
                System.Security.Principal.WindowsIdentity.GetCurrent())
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                AddLog("⚠️ " + (_currentLanguage == "PT"
                    ? "AVISO: Não está a executar como Administrador!"
                    : "WARNING: Not running as Administrator!"));

                ShowCustomDialog(
                    "Administrator Required",
                    _currentLanguage == "PT"
                        ? "Este programa DEVE ser executado como Administrador!\n\nFeche e execute novamente com 'Executar como Administrador'."
                        : "This program MUST be run as Administrator!\n\nClose and run again with 'Run as Administrator'.",
                    DialogType.Warning,
                    null,
                    null
                );
            }
            else
            {
                AddLog("✅ " + (_currentLanguage == "PT"
                    ? "A executar com privilégios de Administrador"
                    : "Running with Administrator privileges"));
            }
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
                TxtStep1Desc.Text = "Abre Windows Update e instala todas as atualizações";
                TxtStep1Status.Text = "⏳ Pendente";

                TxtStep2Title.Text = "Atualizar Drivers";
                TxtStep2Desc.Text = "Atualiza todos os drivers do Gestor de Dispositivos";
                TxtStep2Status.Text = "⏳ Pendente";

                TxtStep3Title.Text = "Limpar Sistema";
                TxtStep3Desc.Text = "Remove versões antigas do Windows e arquivos desnecessários";
                TxtStep3Status.Text = "⏳ Pendente";

                TxtInfoTitle.Text = "ℹ️ Como Funciona";
                TxtInfo1.Text = "• O programa abre o Windows Update e monitora o progresso";
                TxtInfo2.Text = "• Quando necessário, reinicia automaticamente o computador";
                TxtInfo3.Text = "• Após reiniciar, o processo continua automaticamente";
                TxtInfo4.Text = "• Todo o processo é totalmente automático até conclusão";
            }
            else
            {
                TxtTitle.Text = "Windows 11 Automatic Setup";
                TxtSubtitle.Text = "Update and optimize automatically";
                TxtStatusTitle.Text = "Process Status";
                TxtStatusDesc.Text = "Ready to start";
                BtnRunAll.Content = "🚀 Start Automatic Process";

                TxtStep1Title.Text = "Update Windows";
                TxtStep1Desc.Text = "Opens Windows Update and installs all updates";
                TxtStep1Status.Text = "⏳ Pending";

                TxtStep2Title.Text = "Update Drivers";
                TxtStep2Desc.Text = "Updates all drivers from Device Manager";
                TxtStep2Status.Text = "⏳ Pending";

                TxtStep3Title.Text = "Clean System";
                TxtStep3Desc.Text = "Removes old Windows versions and unnecessary files";
                TxtStep3Status.Text = "⏳ Pending";

                TxtInfoTitle.Text = "ℹ️ How It Works";
                TxtInfo1.Text = "• The program opens Windows Update and monitors progress";
                TxtInfo2.Text = "• When needed, automatically restarts the computer";
                TxtInfo3.Text = "• After restart, process continues automatically";
                TxtInfo4.Text = "• Entire process is fully automatic until completion";
            }
        }

        private async void BtnRunAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
                return;

            ShowCustomDialog(
                _currentLanguage == "PT" ? "Iniciar Processo Automático" : "Start Automatic Process",
                _currentLanguage == "PT"
                    ? "O processo irá:\n\n1️⃣ Abrir o Windows Update e verificar atualizações\n2️⃣ Instalar todas as atualizações disponíveis\n3️⃣ Reiniciar automaticamente quando necessário\n4️⃣ Atualizar todos os drivers\n5️⃣ Limpar arquivos antigos\n\n⚠️ O computador pode reiniciar VÁRIAS VEZES.\n⏱️ Duração estimada: 30-90 minutos\n\nDeseja continuar?"
                    : "The process will:\n\n1️⃣ Open Windows Update and check for updates\n2️⃣ Install all available updates\n3️⃣ Restart automatically when needed\n4️⃣ Update all drivers\n5️⃣ Clean old files\n\n⚠️ Computer may restart MULTIPLE TIMES.\n⏱️ Estimated duration: 30-90 minutes\n\nDo you want to continue?",
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

                    await SetupAutoStart();
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
                    $Trigger = New-ScheduledTaskTrigger -AtLogOn
                    $Principal = New-ScheduledTaskPrincipal -UserId '$env:USERNAME' -LogonType Interactive -RunLevel Highest
                    $Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Hours 0)
                    
                    Unregister-ScheduledTask -TaskName '{STARTUP_TASK_NAME}' -Confirm:$false -ErrorAction SilentlyContinue
                    Register-ScheduledTask -TaskName '{STARTUP_TASK_NAME}' -Action $Action -Trigger $Trigger -Principal $Principal -Settings $Settings -Force
                    
                    Write-Host 'AUTO_START_CONFIGURED'
                ";

                var result = await RunPowerShellScript(taskScript);

                if (result.Contains("AUTO_START_CONFIGURED"))
                {
                    AddLog("✅ " + (_currentLanguage == "PT"
                        ? "Início automático configurado"
                        : "Auto-start configured"));
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Setup auto-start: {ex.Message}");
            }
        }

        private async Task RemoveAutoStart()
        {
            try
            {
                string script = $@"
                    Unregister-ScheduledTask -TaskName '{STARTUP_TASK_NAME}' -Confirm:$false -ErrorAction SilentlyContinue
                    Write-Host 'AUTO_START_REMOVED'
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
                ? "FASE 1: Atualização do Windows"
                : "PHASE 1: Windows Update"));

            UpdateStepStatus(1, _currentLanguage == "PT" ? "🔄 Em progresso..." : "🔄 In progress...", Brushes.Orange);
            AnimateStep(Step1Card, Step1Badge);
            ProgressStep1.Visibility = Visibility.Visible;

            try
            {
                // Open Windows Update settings
                AddLog("📂 " + (_currentLanguage == "PT"
                    ? "Abrindo Windows Update..."
                    : "Opening Windows Update..."));

                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:windowsupdate",
                    UseShellExecute = true
                });

                await Task.Delay(3000);

                // Click "Check for updates" button
                AddLog("🔍 " + (_currentLanguage == "PT"
                    ? "Iniciando verificação de atualizações..."
                    : "Starting update check..."));

                await ClickCheckForUpdatesButton();

                // Start monitoring
                _state.UpdateCheckCount++;
                SaveState();

                UpdateProgressBar(ProgressStep1, 10);

                AddLog("⏱️ " + (_currentLanguage == "PT"
                    ? "Monitorando progresso das atualizações..."
                    : "Monitoring update progress..."));

                _updateCheckTimer.Start();
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error: {ex.Message}");
                UpdateStepStatus(1, "❌ " + (_currentLanguage == "PT" ? "Erro" : "Error"), Brushes.Red);
            }
        }

        private async Task ClickCheckForUpdatesButton()
        {
            try
            {
                // Use UI Automation to click the "Check for updates" button
                string script = @"
                    Add-Type -AssemblyName UIAutomationClient
                    Add-Type -AssemblyName UIAutomationTypes

                    $max_attempts = 10
                    $attempt = 0

                    while ($attempt -lt $max_attempts) {
                        try {
                            $settingsWindow = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
                                [System.Windows.Automation.TreeScope]::Children,
                                (New-Object System.Windows.Automation.PropertyCondition(
                                    [System.Windows.Automation.AutomationElement]::ClassNameProperty,
                                    'ApplicationFrameWindow'
                                ))
                            )

                            if ($settingsWindow) {
                                $button = $settingsWindow.FindFirst(
                                    [System.Windows.Automation.TreeScope]::Descendants,
                                    (New-Object System.Windows.Automation.AndCondition(
                                        (New-Object System.Windows.Automation.PropertyCondition(
                                            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                                            [System.Windows.Automation.ControlType]::Button
                                        )),
                                        (New-Object System.Windows.Automation.PropertyCondition(
                                            [System.Windows.Automation.AutomationElement]::NameProperty,
                                            'Check for updates'
                                        ))
                                    ))
                                )

                                if ($button) {
                                    $invokePattern = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
                                    $invokePattern.Invoke()
                                    Write-Host 'BUTTON_CLICKED'
                                    exit 0
                                }
                            }
                        } catch {}

                        Start-Sleep -Seconds 1
                        $attempt++
                    }

                    Write-Host 'BUTTON_NOT_FOUND'
                ";

                var result = await RunPowerShellScript(script);

                if (result.Contains("BUTTON_CLICKED"))
                {
                    AddLog("✅ " + (_currentLanguage == "PT"
                        ? "Verificação iniciada"
                        : "Check started"));
                }
                else
                {
                    AddLog("⚠️ " + (_currentLanguage == "PT"
                        ? "Não foi possível clicar automaticamente. Clique manualmente em 'Verificar atualizações'."
                        : "Could not click automatically. Please click 'Check for updates' manually."));
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Auto-click: {ex.Message}");
            }
        }

        private async void UpdateCheckTimer_Tick(object sender, EventArgs e)
        {
            if (_state.CurrentPhase != UpdatePhase.WindowsUpdate)
            {
                _updateCheckTimer.Stop();
                return;
            }

            try
            {
                var status = await CheckWindowsUpdateStatus();

                UpdateProgressBar(ProgressStep1, Math.Min(20 + (_state.UpdateCheckCount * 10), 80));

                AddLog($"📊 Status: {status.Status}");

                if (status.RestartRequired)
                {
                    _updateCheckTimer.Stop();
                    AddLog("🔄 " + (_currentLanguage == "PT"
                        ? "Reinício necessário..."
                        : "Restart required..."));

                    await InitiateRestart();
                }
                else if (status.IsComplete)
                {
                    _updateCheckTimer.Stop();

                    if (_state.UpdateCheckCount >= MAX_UPDATE_CHECKS || status.NoUpdatesAvailable)
                    {
                        AddLog("✅ " + (_currentLanguage == "PT"
                            ? "Windows totalmente atualizado!"
                            : "Windows fully updated!"));

                        UpdateProgressBar(ProgressStep1, 100);
                        UpdateStepStatus(1, "✅ " + (_currentLanguage == "PT" ? "Concluído" : "Completed"), Brushes.LightGreen);

                        // Move to next phase
                        _state.CurrentPhase = UpdatePhase.DriverUpdate;
                        _state.UpdateCheckCount = 0;
                        SaveState();

                        await Task.Delay(2000);
                        await RunDriverUpdate();
                    }
                    else
                    {
                        AddLog("🔄 " + (_currentLanguage == "PT"
                            ? "Verificando novamente por atualizações..."
                            : "Checking again for updates..."));

                        await Task.Delay(5000);
                        await ClickCheckForUpdatesButton();
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Monitor: {ex.Message}");
            }
        }

        private async Task<(string Status, bool RestartRequired, bool IsComplete, bool NoUpdatesAvailable)> CheckWindowsUpdateStatus()
        {
            string script = @"
                try {
                    $UpdateSession = New-Object -ComObject Microsoft.Update.Session
                    $UpdateSearcher = $UpdateSession.CreateUpdateSearcher()

                    $PendingUpdates = $UpdateSearcher.Search('IsInstalled=0').Updates
                    $InstallingUpdates = $UpdateSearcher.Search('IsInstalled=0 and RebootRequired=0').Updates

                    # Check if restart is required
                    $RebootRequired = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired' -ErrorAction SilentlyContinue) -ne $null

                    if ($RebootRequired) {
                        Write-Host 'STATUS:RESTART_REQUIRED'
                    } elseif ($PendingUpdates.Count -eq 0) {
                        Write-Host 'STATUS:NO_UPDATES'
                    } elseif ($InstallingUpdates.Count -gt 0) {
                        Write-Host 'STATUS:INSTALLING'
                    } else {
                        Write-Host 'STATUS:DOWNLOADING'
                    }
                } catch {
                    Write-Host 'STATUS:CHECKING'
                }
            ";

            var result = await RunPowerShellScript(script);

            bool restartRequired = result.Contains("RESTART_REQUIRED");
            bool noUpdates = result.Contains("NO_UPDATES");
            bool isComplete = noUpdates && !restartRequired;

            string status = "Checking...";
            if (restartRequired)
                status = _currentLanguage == "PT" ? "Reinício necessário" : "Restart required";
            else if (noUpdates)
                status = _currentLanguage == "PT" ? "Sem atualizações" : "No updates";
            else if (result.Contains("INSTALLING"))
                status = _currentLanguage == "PT" ? "Instalando..." : "Installing...";
            else if (result.Contains("DOWNLOADING"))
                status = _currentLanguage == "PT" ? "Baixando..." : "Downloading...";

            return (status, restartRequired, isComplete, noUpdates);
        }

        private async Task InitiateRestart()
        {
            _state.RestartCount++;
            SaveState();

            ShowCustomDialog(
                _currentLanguage == "PT" ? "Reinício Necessário" : "Restart Required",
                _currentLanguage == "PT"
                    ? $"O computador precisa reiniciar para continuar.\n\nReinício #{_state.RestartCount}\nVerificação #{_state.UpdateCheckCount}/{MAX_UPDATE_CHECKS}\n\nO processo continuará automaticamente após o reinício."
                    : $"Computer needs to restart to continue.\n\nRestart #{_state.RestartCount}\nCheck #{_state.UpdateCheckCount}/{MAX_UPDATE_CHECKS}\n\nProcess will continue automatically after restart.",
                DialogType.Info,
                () =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown.exe",
                        Arguments = "/r /t 30 /c \"Nexor - Continuando atualizações\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                },
                null
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
                // Open Device Manager
                AddLog("📂 " + (_currentLanguage == "PT"
                    ? "Abrindo Gestor de Dispositivos..."
                    : "Opening Device Manager..."));

                Process.Start(new ProcessStartInfo
                {
                    FileName = "devmgmt.msc",
                    UseShellExecute = true,
                    Verb = "runas"
                });

                await Task.Delay(2000);

                UpdateProgressBar(ProgressStep2, 20);

                // Use pnputil to update drivers
                AddLog("🔍 " + (_currentLanguage == "PT"
                    ? "Verificando drivers desatualizados..."
                    : "Checking for outdated drivers..."));

                await RunCommand("pnputil.exe", "/scan-devices");

                UpdateProgressBar(ProgressStep2, 40);

                // Attempt to update via Windows Update for drivers
                AddLog("📥 " + (_currentLanguage == "PT"
                    ? "Baixando atualizações de drivers..."
                    : "Downloading driver updates..."));

                string driverScript = @"
                    try {
                        $Session = New-Object -ComObject Microsoft.Update.Session
                        $Searcher = $Session.CreateUpdateSearcher()
                        $Searcher.ServiceID = '7971f918-a847-4430-9279-4a52d1efe18d'
                        $Searcher.SearchScope = 1
                        $Searcher.ServerSelection = 3
                        
                        $Result = $Searcher.Search(""IsInstalled=0 and Type='Driver'"")
                        
                        if ($Result.Updates.Count -eq 0) {
                            Write-Host 'NO_DRIVER_UPDATES'
                        } else {
                            Write-Host ""FOUND_DRIVERS:$($Result.Updates.Count)""
                        }
                    } catch {
                        Write-Host 'DRIVER_CHECK_ERROR'
                    }
                ";

                var result = await RunPowerShellScript(driverScript);

                UpdateProgressBar(ProgressStep2, 70);

                if (result.Contains("NO_DRIVER_UPDATES"))
                {
                    AddLog("✅ " + (_currentLanguage == "PT"
                        ? "Todos os drivers estão atualizados"
                        : "All drivers are up to date"));
                }
                else
                {
                    AddLog("⚠️ " + (_currentLanguage == "PT"
                        ? "Atualize os drivers manualmente através do Gestor de Dispositivos se necessário"
                        : "Update drivers manually through Device Manager if needed"));
                }

                UpdateProgressBar(ProgressStep2, 100);
                UpdateStepStatus(2, "✅ " + (_currentLanguage == "PT" ? "Concluído" : "Completed"), Brushes.LightGreen);

                // Move to cleanup phase
                _state.CurrentPhase = UpdatePhase.Cleanup;
                SaveState();

                await Task.Delay(2000);
                await RunCleanup();
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error: {ex.Message}");
                UpdateStepStatus(2, "❌ " + (_currentLanguage == "PT" ? "Erro" : "Error"), Brushes.Red);
            }
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

                // Clean Windows Update cache
                AddLog("🧹 " + (_currentLanguage == "PT"
                    ? "Limpando cache do Windows Update..."
                    : "Cleaning Windows Update cache..."));

                UpdateProgressBar(ProgressStep3, 15);
                totalFreed += CleanDirectory(@"C:\Windows\SoftwareDistribution\Download");

                // Clean temp files
                AddLog("🧹 " + (_currentLanguage == "PT"
                    ? "Limpando arquivos temporários..."
                    : "Cleaning temporary files..."));

                UpdateProgressBar(ProgressStep3, 30);
                totalFreed += CleanDirectory(Path.GetTempPath());
                totalFreed += CleanDirectory(@"C:\Windows\Temp");

                // Clean prefetch
                AddLog("🧹 " + (_currentLanguage == "PT"
                    ? "Limpando Prefetch..."
                    : "Cleaning Prefetch..."));

                UpdateProgressBar(ProgressStep3, 50);
                totalFreed += CleanDirectory(@"C:\Windows\Prefetch");

                // Empty Recycle Bin
                AddLog("🗑️ " + (_currentLanguage == "PT"
                    ? "Esvaziando Reciclagem..."
                    : "Emptying Recycle Bin..."));

                UpdateProgressBar(ProgressStep3, 65);
                await RunPowerShellCommand("Clear-RecycleBin -Force -ErrorAction SilentlyContinue");

                // Run DISM cleanup
                AddLog("🧹 " + (_currentLanguage == "PT"
                    ? "Removendo versões antigas do Windows..."
                    : "Removing old Windows versions..."));

                UpdateProgressBar(ProgressStep3, 80);
                await RunCommand("Dism.exe", "/online /Cleanup-Image /StartComponentCleanup /ResetBase");

                UpdateProgressBar(ProgressStep3, 100);

                double freedGB = totalFreed / (1024.0 * 1024.0 * 1024.0);
                AddLog($"✅ " + (_currentLanguage == "PT"
                    ? $"Limpeza concluída! {freedGB:F2} GB libertados"
                    : $"Cleanup completed! {freedGB:F2} GB freed"));

                UpdateStepStatus(3, $"✅ " + (_currentLanguage == "PT" ? "Concluído" : "Completed") + $" ({freedGB:F2} GB)", Brushes.LightGreen);

                // Move to completed phase
                _state.CurrentPhase = UpdatePhase.Completed;
                SaveState();

                await Task.Delay(2000);
                await CompleteProcess();
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error: {ex.Message}");
                UpdateStepStatus(3, "❌ " + (_currentLanguage == "PT" ? "Erro" : "Error"), Brushes.Red);
            }
        }

        private async Task CompleteProcess()
        {
            AddLog("\n═══════════════════════════════════════");
            AddLog("🎉 " + (_currentLanguage == "PT"
                ? "PROCESSO CONCLUÍDO COM SUCESSO!"
                : "PROCESS COMPLETED SUCCESSFULLY!"));
            AddLog("═══════════════════════════════════════");

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
                _currentLanguage == "PT" ? "Sucesso" : "Success",
                _currentLanguage == "PT"
                    ? $"🎉 Processo concluído!\n\n✅ Windows: Totalmente atualizado\n✅ Drivers: Verificados\n✅ Sistema: Limpo e otimizado\n🔄 Reinícios: {_state.RestartCount}\n\nRecomendação: Reinicie o computador uma última vez."
                    : $"🎉 Process completed!\n\n✅ Windows: Fully updated\n✅ Drivers: Checked\n✅ System: Cleaned and optimized\n🔄 Restarts: {_state.RestartCount}\n\nRecommendation: Restart computer one last time.",
                NotificationType.Success
            );
        }

        // Helper methods
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
                        Verb = "runas",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    var process = Process.Start(psi);
                    process?.WaitForExit();
                    return process?.ExitCode == 0;
                }
                catch
                {
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
                        CreateNoWindow = true,
                        Verb = "runas"
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
                    File.WriteAllText(tempFile, script);

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        Verb = "runas"
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

        private long CleanDirectory(string path)
        {
            long bytesFreed = 0;

            try
            {
                if (!Directory.Exists(path))
                    return 0;

                DirectoryInfo di = new DirectoryInfo(path);

                foreach (FileInfo file in di.GetFiles())
                {
                    try
                    {
                        long fileSize = file.Length;
                        file.Delete();
                        bytesFreed += fileSize;
                    }
                    catch { }
                }

                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    try
                    {
                        bytesFreed += GetDirectorySize(dir);
                        dir.Delete(true);
                    }
                    catch { }
                }
            }
            catch { }

            return bytesFreed;
        }

        private long GetDirectorySize(DirectoryInfo directory)
        {
            long size = 0;

            try
            {
                FileInfo[] files = directory.GetFiles();
                foreach (FileInfo file in files)
                {
                    size += file.Length;
                }

                DirectoryInfo[] subdirs = directory.GetDirectories();
                foreach (DirectoryInfo subdir in subdirs)
                {
                    size += GetDirectorySize(subdir);
                }
            }
            catch { }

            return size;
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
                progressBar.Value = value;
            });
        }

        private void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtLog.Text += $"{DateTime.Now:HH:mm:ss} {message}\n";

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
                    To = ((SolidColorBrush)Resources["AccentGreen"]).Color,
                    Duration = TimeSpan.FromMilliseconds(500)
                };

                ProgressCircleBorder.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            });
        }

        // Dialog and Notification methods
        private enum DialogType
        {
            Question,
            Warning,
            Error,
            Info
        }

        private enum NotificationType
        {
            Success,
            Error,
            Warning,
            Info
        }

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
                        DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212));
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
                    await Task.Delay(5000);
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

        // Individual step button handlers (for manual testing - hidden by default)
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

            await SetupAutoStart();
            _state.CurrentPhase = UpdatePhase.WindowsUpdate;
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