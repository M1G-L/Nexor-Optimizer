using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Nexor
{
    public class StatusTracker
    {
        public string CurrentAction { get; set; } = "";
        public string CurrentSubAction { get; set; } = "";
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public bool IsAnimating { get; set; } = true;

        public void Update(string action, string subAction = "")
        {
            CurrentAction = action;
            CurrentSubAction = subAction;
            LastUpdate = DateTime.Now;
        }
    }
    public partial class FreshSetupPage : UserControl
    {
        private int _completedSteps = 0;
        private readonly string _currentLanguage;
        private bool _isRunning = false;
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string UPDATE_STATE_FILE = "nexor_update_state.txt";
        private const string UPDATE_PASS_KEY = "UpdatePass";
        private int _currentUpdatePass = 0;
        private const int MAX_UPDATE_PASSES = 3;
        private StatusTracker _statusTracker = new StatusTracker();
        private DispatcherTimer _statusAnimationTimer;

        public FreshSetupPage(string language = "PT")
        {
            InitializeComponent();
            _currentLanguage = language;
            SetLanguage(language);
            // Initialize the status animation timer
            _statusAnimationTimer = new DispatcherTimer();
            _statusAnimationTimer.Interval = TimeSpan.FromMilliseconds(500);
            _statusAnimationTimer.Tick += StatusAnimationTimer_Tick;
            CheckAdminPrivileges();
            CheckForPendingUpdates();
        }

        private void StatusAnimationTimer_Tick(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int dotCount = (int)((DateTime.Now.Millisecond / 250) % 4);
                string dots = new string('.', dotCount);

                string displayText = _statusTracker.CurrentAction + dots;
                if (!string.IsNullOrEmpty(_statusTracker.CurrentSubAction))
                {
                    displayText = _statusTracker.CurrentSubAction + dots;
                }

                TxtLiveStatus.Text = displayText;
            });
        }

        private void UpdateLiveStatus(string action, string subAction = "")
        {
            _statusTracker.Update(action, subAction);

            Dispatcher.Invoke(() =>
            {
                TxtLiveStatus.Visibility = Visibility.Visible;
            });

            AddLog($"\n🔄 {action}");
            if (!string.IsNullOrEmpty(subAction))
            {
                AddLog($"   → {subAction}");
            }
        }

        private void CheckForPendingUpdates()
        {
            try
            {
                string stateFile = Path.Combine(Path.GetTempPath(), UPDATE_STATE_FILE);
                if (File.Exists(stateFile))
                {
                    string[] lines = File.ReadAllLines(stateFile);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith(UPDATE_PASS_KEY + "="))
                        {
                            int.TryParse(line.Split('=')[1], out _currentUpdatePass);
                            break;
                        }
                    }

                    if (_currentUpdatePass > 0 && _currentUpdatePass < MAX_UPDATE_PASSES)
                    {
                        ShowCustomDialog(
                            _currentLanguage == "PT" ? "Atualização Pendente" : "Pending Update",
                            _currentLanguage == "PT"
                                ? $"Foi detectada uma atualização em progresso (Passo {_currentUpdatePass}/{MAX_UPDATE_PASSES}).\n\nDeseja continuar o processo de atualização?"
                                : $"An update in progress was detected (Pass {_currentUpdatePass}/{MAX_UPDATE_PASSES}).\n\nDo you want to continue the update process?",
                            DialogType.Question,
                            () =>
                            {
                                Task.Run(async () =>
                                {
                                    await Task.Delay(500);
                                    Dispatcher.Invoke(() => BtnRunAll_Click(null, null));
                                });
                            },
                            () => ClearUpdateState()
                        );
                    }
                    else if (_currentUpdatePass >= MAX_UPDATE_PASSES)
                    {
                        ClearUpdateState();
                        ShowCustomNotification(
                            _currentLanguage == "PT" ? "Processo Concluído" : "Process Completed",
                            _currentLanguage == "PT"
                                ? "Processo de atualização concluído após múltiplas verificações!"
                                : "Update process completed after multiple checks!",
                            NotificationType.Success
                        );
                    }
                }
            }
            catch { }
        }

        private void SaveUpdateState(int pass)
        {
            try
            {
                string stateFile = Path.Combine(Path.GetTempPath(), UPDATE_STATE_FILE);
                File.WriteAllText(stateFile, $"{UPDATE_PASS_KEY}={pass}\n");
            }
            catch { }
        }

        private void ClearUpdateState()
        {
            try
            {
                string stateFile = Path.Combine(Path.GetTempPath(), UPDATE_STATE_FILE);
                if (File.Exists(stateFile))
                {
                    File.Delete(stateFile);
                }
                _currentUpdatePass = 0;
            }
            catch { }
        }

        private void CheckAdminPrivileges()
        {
            bool isAdmin = new System.Security.Principal.WindowsPrincipal(
                System.Security.Principal.WindowsIdentity.GetCurrent())
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                AddLog("⚠️ " + (_currentLanguage == "PT"
                    ? "AVISO CRÍTICO: Não está a executar como Administrador. A configuração FALHARÁ!"
                    : "CRITICAL WARNING: Not running as Administrator. Setup WILL FAIL!"));

                ShowCustomDialog(
                    "Nexor - Administrator Required",
                    _currentLanguage == "PT"
                        ? "⚠️ ATENÇÃO: Este programa DEVE ser executado como Administrador!\n\nPor favor, feche e execute novamente com 'Executar como Administrador'."
                        : "⚠️ WARNING: This program MUST be run as Administrator!\n\nPlease close and run again with 'Run as Administrator'.",
                    DialogType.Warning,
                    null,
                    null
                );
            }
            else
            {
                AddLog("✓ " + (_currentLanguage == "PT"
                    ? "A executar com privilégios de Administrador"
                    : "Running with Administrator privileges"));
            }
        }

        private async Task<bool> PerformSystemDiagnostics()
        {
            AddLog("\n" + (_currentLanguage == "PT"
                ? "🔍 Executando diagnósticos e correções automáticas do sistema..."
                : "🔍 Running system diagnostics and automatic fixes..."));

            bool allChecksPass = true;

            // Check 1: Administrator Privileges
            bool isAdmin = new System.Security.Principal.WindowsPrincipal(
                System.Security.Principal.WindowsIdentity.GetCurrent())
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

            if (isAdmin)
            {
                AddLog("  ✅ " + (_currentLanguage == "PT"
                    ? "Privilégios de Administrador: OK"
                    : "Administrator Privileges: OK"));
            }
            else
            {
                AddLog("  ❌ " + (_currentLanguage == "PT"
                    ? "FALHA CRÍTICA: Não está a executar como Administrador - NÃO PODE SER CORRIGIDO AUTOMATICAMENTE"
                    : "CRITICAL FAILURE: Not running as Administrator - CANNOT BE FIXED AUTOMATICALLY"));
                allChecksPass = false;
            }

            // Check 2: Internet Connection
            bool hasInternet = await CheckInternetConnection();
            if (hasInternet)
            {
                AddLog("  ✅ " + (_currentLanguage == "PT"
                    ? "Conexão à Internet: OK"
                    : "Internet Connection: OK"));
            }
            else
            {
                AddLog("  ⚠️ " + (_currentLanguage == "PT"
                    ? "AVISO: Sem conexão à Internet - Por favor, conecte-se à internet"
                    : "WARNING: No Internet Connection - Please connect to internet"));

                AddLog("  → " + (_currentLanguage == "PT"
                    ? "Aguardando 10 segundos e verificando novamente..."
                    : "Waiting 10 seconds and checking again..."));

                await Task.Delay(10000);
                hasInternet = await CheckInternetConnection();

                if (hasInternet)
                {
                    AddLog("  ✅ " + (_currentLanguage == "PT"
                        ? "Conexão à Internet restaurada!"
                        : "Internet Connection restored!"));
                }
                else
                {
                    AddLog("  ⚠️ " + (_currentLanguage == "PT"
                        ? "Ainda sem conexão - O processo pode falhar"
                        : "Still no connection - Process may fail"));
                    allChecksPass = false;
                }
            }

            // Check 3: Windows Update Service
            bool wuServiceRunning = await CheckWindowsUpdateService();
            if (wuServiceRunning)
            {
                AddLog("  ✅ " + (_currentLanguage == "PT"
                    ? "Serviço Windows Update: Ativo"
                    : "Windows Update Service: Running"));
            }
            else
            {
                AddLog("  ⚠️ " + (_currentLanguage == "PT"
                    ? "Serviço Windows Update não está ativo"
                    : "Windows Update Service not running"));

                AddLog("  → " + (_currentLanguage == "PT"
                    ? "CORRIGINDO: Iniciando o serviço..."
                    : "FIXING: Starting service..."));

                bool started = await StartWindowsUpdateService();
                if (started)
                {
                    AddLog("  ✅ " + (_currentLanguage == "PT"
                        ? "Serviço iniciado com sucesso - PROBLEMA CORRIGIDO"
                        : "Service started successfully - PROBLEM FIXED"));
                }
                else
                {
                    AddLog("  ❌ " + (_currentLanguage == "PT"
                        ? "Falha ao iniciar serviço - Tentando reparação avançada..."
                        : "Failed to start service - Trying advanced repair..."));

                    bool repaired = await RepairWindowsUpdateService();
                    if (repaired)
                    {
                        AddLog("  ✅ " + (_currentLanguage == "PT"
                            ? "Serviço reparado e iniciado - PROBLEMA CORRIGIDO"
                            : "Service repaired and started - PROBLEM FIXED"));
                    }
                    else
                    {
                        AddLog("  ❌ " + (_currentLanguage == "PT"
                            ? "Falha na reparação - Pode afetar o processo"
                            : "Repair failed - May affect process"));
                        allChecksPass = false;
                    }
                }
            }

            // Check 4: Related Services
            await EnsureRelatedServicesRunning();

            // Check 5: PowerShell
            bool psAvailable = CheckPowerShellAvailable();
            if (psAvailable)
            {
                AddLog("  ✅ " + (_currentLanguage == "PT"
                    ? "PowerShell: Disponível"
                    : "PowerShell: Available"));
            }
            else
            {
                AddLog("  ❌ " + (_currentLanguage == "PT"
                    ? "FALHA CRÍTICA: PowerShell não encontrado - Sistema comprometido"
                    : "CRITICAL FAILURE: PowerShell not found - System compromised"));
                allChecksPass = false;
            }

            // Check 6: Disk Space
            long freeSpace = GetSystemDriveFreeSpace();
            double freeSpaceGB = freeSpace / (1024.0 * 1024.0 * 1024.0);

            if (freeSpaceGB > 20)
            {
                AddLog($"  ✅ " + (_currentLanguage == "PT"
                    ? $"Espaço em disco: {freeSpaceGB:F1} GB disponível"
                    : $"Disk Space: {freeSpaceGB:F1} GB available"));
            }
            else if (freeSpaceGB > 10)
            {
                AddLog($"  ⚠️ " + (_currentLanguage == "PT"
                    ? $"AVISO: Pouco espaço em disco: {freeSpaceGB:F1} GB"
                    : $"WARNING: Low disk space: {freeSpaceGB:F1} GB"));

                AddLog("  → " + (_currentLanguage == "PT"
                    ? "CORRIGINDO: Executando limpeza rápida de disco..."
                    : "FIXING: Running quick disk cleanup..."));

                await QuickDiskCleanup();

                freeSpace = GetSystemDriveFreeSpace();
                freeSpaceGB = freeSpace / (1024.0 * 1024.0 * 1024.0);

                if (freeSpaceGB > 15)
                {
                    AddLog($"  ✅ " + (_currentLanguage == "PT"
                        ? $"Espaço liberado! Agora: {freeSpaceGB:F1} GB - PROBLEMA MELHORADO"
                        : $"Space freed! Now: {freeSpaceGB:F1} GB - PROBLEM IMPROVED"));
                }
                else
                {
                    AddLog($"  ⚠️ " + (_currentLanguage == "PT"
                        ? $"Ainda pouco espaço: {freeSpaceGB:F1} GB - Considere liberar mais espaço manualmente"
                        : $"Still low space: {freeSpaceGB:F1} GB - Consider freeing more space manually"));
                }
            }
            else
            {
                AddLog($"  ❌ " + (_currentLanguage == "PT"
                    ? $"CRÍTICO: Espaço insuficiente: {freeSpaceGB:F1} GB"
                    : $"CRITICAL: Insufficient space: {freeSpaceGB:F1} GB"));

                AddLog("  → " + (_currentLanguage == "PT"
                    ? "CORRIGINDO: Executando limpeza agressiva de disco..."
                    : "FIXING: Running aggressive disk cleanup..."));

                await AggressiveDiskCleanup();

                freeSpace = GetSystemDriveFreeSpace();
                freeSpaceGB = freeSpace / (1024.0 * 1024.0 * 1024.0);

                if (freeSpaceGB > 10)
                {
                    AddLog($"  ✅ " + (_currentLanguage == "PT"
                        ? $"Espaço suficiente liberado: {freeSpaceGB:F1} GB - PROBLEMA CORRIGIDO"
                        : $"Sufficient space freed: {freeSpaceGB:F1} GB - PROBLEM FIXED"));
                }
                else
                {
                    AddLog($"  ❌ " + (_currentLanguage == "PT"
                        ? $"Ainda insuficiente: {freeSpaceGB:F1} GB - REQUER INTERVENÇÃO MANUAL"
                        : $"Still insufficient: {freeSpaceGB:F1} GB - REQUIRES MANUAL INTERVENTION"));
                    allChecksPass = false;
                }
            }

            // Check 7: Windows Update Registry Settings
            await FixWindowsUpdateRegistry();

            // Check 8: Network Configuration
            await CheckAndFixNetworkConfiguration();

            AddLog("");

            if (allChecksPass)
            {
                AddLog("✅ " + (_currentLanguage == "PT"
                    ? "Todos os diagnósticos passaram! Sistema pronto e otimizado."
                    : "All diagnostics passed! System ready and optimized."));
            }
            else
            {
                AddLog("⚠️ " + (_currentLanguage == "PT"
                    ? "Alguns problemas não puderam ser corrigidos automaticamente. O processo continuará, mas pode haver falhas."
                    : "Some issues could not be fixed automatically. Process will continue, but failures may occur."));
            }

            return allChecksPass;
        }

        private async Task<bool> RepairWindowsUpdateService()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string repairScript = @"
                        Write-Host 'Stopping Windows Update services...'
                        Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue
                        Stop-Service -Name cryptSvc -Force -ErrorAction SilentlyContinue
                        Stop-Service -Name bits -Force -ErrorAction SilentlyContinue
                        Stop-Service -Name msiserver -Force -ErrorAction SilentlyContinue
                        
                        Start-Sleep -Seconds 3
                        
                        Write-Host 'Resetting service configurations...'
                        sc.exe config wuauserv start= auto
                        sc.exe config cryptSvc start= auto
                        sc.exe config bits start= auto
                        
                        Write-Host 'Clearing Windows Update cache...'
                        Remove-Item -Path 'C:\Windows\SoftwareDistribution\Download\*' -Recurse -Force -ErrorAction SilentlyContinue
                        
                        Start-Sleep -Seconds 2
                        
                        Write-Host 'Starting Windows Update services...'
                        Start-Service -Name wuauserv -ErrorAction SilentlyContinue
                        Start-Service -Name cryptSvc -ErrorAction SilentlyContinue
                        Start-Service -Name bits -ErrorAction SilentlyContinue
                        
                        Start-Sleep -Seconds 2
                        
                        $wuService = Get-Service -Name wuauserv
                        if ($wuService.Status -eq 'Running') {
                            Write-Host 'SERVICE_REPAIR_SUCCESS'
                        } else {
                            Write-Host 'SERVICE_REPAIR_FAILED'
                        }
                    ";

                    var result = await RunPowerShellScript(repairScript);
                    return result.Contains("SERVICE_REPAIR_SUCCESS");
                }
                catch
                {
                    return false;
                }
            });
        }

        private async Task EnsureRelatedServicesRunning()
        {
            string[] requiredServices = { "cryptSvc", "bits", "TrustedInstaller" };

            foreach (string serviceName in requiredServices)
            {
                try
                {
                    using (var service = new ServiceController(serviceName))
                    {
                        if (service.Status != ServiceControllerStatus.Running)
                        {
                            AddLog($"  → " + (_currentLanguage == "PT"
                                ? $"CORRIGINDO: Iniciando serviço {serviceName}..."
                                : $"FIXING: Starting service {serviceName}..."));

                            service.Start();
                            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

                            AddLog($"  ✅ " + (_currentLanguage == "PT"
                                ? $"Serviço {serviceName} iniciado - PROBLEMA CORRIGIDO"
                                : $"Service {serviceName} started - PROBLEM FIXED"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"  ⚠️ " + (_currentLanguage == "PT"
                        ? $"Não foi possível iniciar {serviceName}: {ex.Message}"
                        : $"Could not start {serviceName}: {ex.Message}"));
                }
            }
        }

        private async Task QuickDiskCleanup()
        {
            try
            {
                await Task.Run(() =>
                {
                    string tempPath = Path.GetTempPath();
                    CleanDirectory(tempPath);

                    string windowsTempPath = @"C:\Windows\Temp";
                    CleanDirectory(windowsTempPath);
                });

                AddLog("  ✓ " + (_currentLanguage == "PT"
                    ? "Limpeza rápida concluída"
                    : "Quick cleanup completed"));
            }
            catch { }
        }

        private async Task AggressiveDiskCleanup()
        {
            try
            {
                await Task.Run(async () =>
                {
                    string tempPath = Path.GetTempPath();
                    CleanDirectory(tempPath);

                    string windowsTempPath = @"C:\Windows\Temp";
                    CleanDirectory(windowsTempPath);

                    string updateCachePath = @"C:\Windows\SoftwareDistribution\Download";
                    CleanDirectory(updateCachePath);

                    string prefetchPath = @"C:\Windows\Prefetch";
                    CleanDirectory(prefetchPath);

                    try
                    {
                        await RunPowerShellCommand("Clear-RecycleBin -Force -ErrorAction SilentlyContinue");
                    }
                    catch { }

                    try
                    {
                        await RunCommand("cleanmgr.exe", "/sagerun:1");
                    }
                    catch { }
                });

                AddLog("  ✓ " + (_currentLanguage == "PT"
                    ? "Limpeza agressiva concluída"
                    : "Aggressive cleanup completed"));
            }
            catch { }
        }

        private async Task FixWindowsUpdateRegistry()
        {
            try
            {
                AddLog("  → " + (_currentLanguage == "PT"
                    ? "Verificando e corrigindo configurações de registro do Windows Update..."
                    : "Checking and fixing Windows Update registry settings..."));

                string registryFixScript = @"
                    try {
                        $WUPath = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'
                        $AUPath = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'
                        
                        if (Test-Path $WUPath) {
                            Remove-Item -Path $WUPath -Recurse -Force -ErrorAction SilentlyContinue
                            Write-Host 'Removed restrictive Windows Update policies'
                        }
                        
                        Write-Host 'REGISTRY_FIX_SUCCESS'
                    } catch {
                        Write-Host 'REGISTRY_FIX_FAILED'
                    }
                ";

                var result = await RunPowerShellScript(registryFixScript);

                if (result.Contains("REGISTRY_FIX_SUCCESS"))
                {
                    AddLog("  ✅ " + (_currentLanguage == "PT"
                        ? "Configurações de registro corrigidas"
                        : "Registry settings fixed"));
                }
            }
            catch { }
        }

        private async Task CheckAndFixNetworkConfiguration()
        {
            try
            {
                AddLog("  → " + (_currentLanguage == "PT"
                    ? "Verificando configuração de rede..."
                    : "Checking network configuration..."));

                string networkFixScript = @"
                    try {
                        ipconfig /flushdns | Out-Null
                        Write-Host 'DNS cache flushed'
                        
                        netsh winsock reset | Out-Null
                        Write-Host 'Winsock reset'
                        
                        Write-Host 'NETWORK_FIX_SUCCESS'
                    } catch {
                        Write-Host 'NETWORK_FIX_FAILED'
                    }
                ";

                var result = await RunPowerShellScript(networkFixScript);

                if (result.Contains("NETWORK_FIX_SUCCESS"))
                {
                    AddLog("  ✅ " + (_currentLanguage == "PT"
                        ? "Configuração de rede otimizada"
                        : "Network configuration optimized"));
                }
            }
            catch { }
        }

        private async Task<bool> CheckInternetConnection()
        {
            try
            {
                using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    var response = await _httpClient.GetAsync("http://www.msftconnecttest.com/connecttest.txt", cts.Token);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                try
                {
                    using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        var response = await _httpClient.GetAsync("http://www.google.com", cts.Token);
                        return response.IsSuccessStatusCode;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        private async Task<bool> CheckWindowsUpdateService()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var service = new ServiceController("wuauserv"))
                    {
                        return service.Status == ServiceControllerStatus.Running;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        private async Task<bool> StartWindowsUpdateService()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var service = new ServiceController("wuauserv"))
                    {
                        if (service.Status != ServiceControllerStatus.Running)
                        {
                            service.Start();
                            service.WaitForStatus(ServiceControllerStatus.Running,
                                TimeSpan.FromSeconds(30));
                            return true;
                        }
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        private bool CheckPowerShellAvailable()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Version",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        process.WaitForExit(5000);
                        return process.ExitCode == 0;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private long GetSystemDriveFreeSpace()
        {
            try
            {
                DriveInfo systemDrive = new DriveInfo("C");
                return systemDrive.AvailableFreeSpace;
            }
            catch
            {
                return 0;
            }
        }

        private void SetLanguage(string language)
        {
            if (language == "PT")
            {
                TxtTitle.Text = "Configuração Inicial Windows 11";
                TxtSubtitle.Text = "Otimize o seu sistema após instalação limpa";
                TxtStatusTitle.Text = "Estado do Sistema";
                TxtStatusDesc.Text = "Pronto para configuração inicial";
                BtnRunAll.Content = "🚀 Executar Configuração Completa";

                TxtStep1Title.Text = "Atualizar Windows";
                TxtStep1Desc.Text = "Instalar todas as atualizações disponíveis do Windows 11";
                TxtStep1Status.Text = "⏳ Pendente";

                TxtStep2Title.Text = "Atualizar Drivers";
                TxtStep2Desc.Text = "Verificar e instalar drivers mais recentes para hardware";
                TxtStep2Status.Text = "⏳ Pendente";

                TxtStep3Title.Text = "Limpar Sistema";
                TxtStep3Desc.Text = "Remover ficheiros temporários, caches e versões antigas do Windows";
                TxtStep3Status.Text = "⏳ Pendente";

                TxtInfoTitle.Text = "ℹ️ Informação Importante";
                TxtInfo1.Text = "• Este processo pode demorar 30-60 minutos dependendo das atualizações disponíveis";
                TxtInfo2.Text = "• É OBRIGATÓRIO executar como Administrador";
                TxtInfo3.Text = "• Mantenha o computador ligado e conectado à internet";
                TxtInfo4.Text = "• O sistema pode reiniciar automaticamente várias vezes";
            }
            else
            {
                TxtTitle.Text = "Fresh Windows 11 Setup";
                TxtSubtitle.Text = "Optimize your system after clean installation";
                TxtStatusTitle.Text = "System Status";
                TxtStatusDesc.Text = "Ready for initial setup";
                BtnRunAll.Content = "🚀 Run Complete Setup";

                TxtStep1Title.Text = "Update Windows";
                TxtStep1Desc.Text = "Install all available Windows 11 updates";
                TxtStep1Status.Text = "⏳ Pending";

                TxtStep2Title.Text = "Update Drivers";
                TxtStep2Desc.Text = "Check and install latest hardware drivers";
                TxtStep2Status.Text = "⏳ Pending";

                TxtStep3Title.Text = "Clean System";
                TxtStep3Desc.Text = "Remove temporary files, caches and old Windows versions";
                TxtStep3Status.Text = "⏳ Pending";

                TxtInfoTitle.Text = "ℹ️ Important Information";
                TxtInfo1.Text = "• This process may take 30-60 minutes depending on available updates";
                TxtInfo2.Text = "• Running as Administrator is REQUIRED";
                TxtInfo3.Text = "• Keep computer on and connected to internet";
                TxtInfo4.Text = "• The system may restart automatically several times";
            }
        }

        private async void BtnRunAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
                return;

            ShowCustomDialog(
                _currentLanguage == "PT" ? "Configuração Automática" : "Automatic Setup",
                _currentLanguage == "PT"
                    ? "Deseja executar todos os passos automaticamente?\n\n⚠️ IMPORTANTE:\n• O processo pode demorar 30-90 minutos\n• O computador pode reiniciar AUTOMATICAMENTE várias vezes\n• As atualizações serão instaladas em múltiplos passes\n• Problemas detectados serão CORRIGIDOS AUTOMATICAMENTE\n• O log mostrará o progresso em tempo real\n\nRecomendação: Deixe o computador ligado e conectado à internet."
                    : "Do you want to run all steps automatically?\n\n⚠️ IMPORTANT:\n• The process may take 30-90 minutes\n• The computer may RESTART AUTOMATICALLY several times\n• Updates will be installed in multiple passes\n• Detected problems will be FIXED AUTOMATICALLY\n• The log will show real-time progress\n\nRecommendation: Keep computer on and connected to internet.",
                DialogType.Question,
                async () =>
                {
                    _isRunning = true;
                    BtnRunAll.IsEnabled = false;
                    LogCard.Visibility = Visibility.Visible;

                    AddLog("═══════════════════════════════════════");
                    AddLog(_currentLanguage == "PT"
                        ? "🚀 Iniciando configuração automática completa..."
                        : "🚀 Starting complete automatic setup...");
                    AddLog("═══════════════════════════════════════");

                    bool diagnosticsPass = await PerformSystemDiagnostics();

                    AddLog("\n" + (_currentLanguage == "PT"
                        ? "⏰ O processo será totalmente automático, incluindo reinicializações"
                        : "⏰ The process will be fully automatic, including restarts"));
                    AddLog("═══════════════════════════════════════\n");

                    await RunAllSteps();

                    _isRunning = false;
                    BtnRunAll.IsEnabled = true;
                },
                () =>
                {
                    AddLog("\n❌ " + (_currentLanguage == "PT"
                        ? "Processo cancelado pelo utilizador"
                        : "Process cancelled by user"));
                }
            );
        }

        private async Task RunAllSteps()
        {
            try
            {
                await RunStep1();
                await Task.Delay(3000);

                await RunStep2();
                await Task.Delay(3000);

                await RunStep3();

                AddLog("\n═══════════════════════════════════════");
                AddLog(_currentLanguage == "PT"
                    ? "✅ CONFIGURAÇÃO CONCLUÍDA COM SUCESSO!"
                    : "✅ SETUP COMPLETED SUCCESSFULLY!");
                AddLog("═══════════════════════════════════════");

                AnimateCompletion();
                ClearUpdateState();

                ShowCustomNotification(
                    _currentLanguage == "PT" ? "Sucesso" : "Success",
                    _currentLanguage == "PT"
                        ? "🎉 Configuração inicial concluída!\n\n✅ Windows Update: Todas as atualizações instaladas\n✅ Drivers: Atualizados via Windows Update\n✅ Sistema: Limpo e otimizado\n\nRecomendação: Reinicie o computador agora para aplicar todas as alterações."
                        : "🎉 Initial setup completed!\n\n✅ Windows Update: All updates installed\n✅ Drivers: Updated via Windows Update\n✅ System: Cleaned and optimized\n\nRecommendation: Restart your computer now to apply all changes.",
                    NotificationType.Success
                );
            }
            catch (Exception ex)
            {
                AddLog($"\n❌ {(_currentLanguage == "PT" ? "ERRO CRÍTICO" : "CRITICAL ERROR")}: {ex.Message}");
                ShowCustomNotification(
                    "Error",
                    $"{(_currentLanguage == "PT" ? "Erro durante a configuração" : "Error during setup")}:\n{ex.Message}",
                    NotificationType.Error
                );
            }
        }

        private async void BtnStep1_Click(object sender, RoutedEventArgs e)
        {
            BtnStep1.IsEnabled = false;
            await RunStep1();
            BtnStep1.IsEnabled = true;
        }

        private async Task RunStep1()
        {
            _statusAnimationTimer.Start();

            AddLog("\n" + (_currentLanguage == "PT" ? "▶ Passo 1: Atualizando Windows..." : "▶ Step 1: Updating Windows..."));
            AddLog(_currentLanguage == "PT"
                ? "⏰ Este passo pode demorar 15-45 minutos. Aguarde..."
                : "⏰ This step may take 15-45 minutes. Please wait...");

            UpdateStepStatus(1, _currentLanguage == "PT" ? "🔄 A executar..." : "🔄 Running...", Brushes.Orange);
            AnimateStep(Step1Card, Step1Badge);
            ProgressStep1.Visibility = Visibility.Visible;

            try
            {
                await Task.Run(async () =>
                {
                    UpdateLiveStatus(
                        _currentLanguage == "PT"
                            ? "Reiniciando componentes do Windows Update"
                            : "Restarting Windows Update components");

                    await ResetWindowsUpdateComponents();

                    UpdateLiveStatus(
                        _currentLanguage == "PT"
                            ? "Preparando sistema de atualizações"
                            : "Preparing update system");

                    UpdateProgressBar(ProgressStep1, 10);

                    string installModuleScript = @"
                try {
                    if (-not (Get-Module -ListAvailable -Name PSWindowsUpdate)) {
                        Write-Host 'Installing PSWindowsUpdate module...'
                        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
                        Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -ErrorAction SilentlyContinue | Out-Null
                        Set-PSRepository -Name 'PSGallery' -InstallationPolicy Trusted -ErrorAction SilentlyContinue
                        Install-Module -Name PSWindowsUpdate -Force -Confirm:$false -AllowClobber -ErrorAction Stop
                        Write-Host 'PSWindowsUpdate installed successfully'
                    } else {
                        Write-Host 'PSWindowsUpdate already installed'
                        Import-Module PSWindowsUpdate -Force
                    }
                } catch {
                    Write-Host ""Error installing PSWindowsUpdate: $($_.Exception.Message)""
                    exit 1
                }
            ";

                    try
                    {
                        UpdateLiveStatus(
                            _currentLanguage == "PT"
                                ? "Instalando módulo PSWindowsUpdate"
                                : "Installing PSWindowsUpdate module");

                        var moduleResult = await RunPowerShellScript(installModuleScript);
                        AddLog($"  ✓ {moduleResult.Trim()}");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"  ⚠️ Module installation: {ex.Message}");
                    }

                    UpdateProgressBar(ProgressStep1, 15);

                    int updatePass = _currentUpdatePass > 0 ? _currentUpdatePass : 1;
                    bool updatesFound = true;

                    while (updatesFound && updatePass <= MAX_UPDATE_PASSES)
                    {
                        AddLog($"\n  🔄 " + (_currentLanguage == "PT"
                            ? $"Passo de atualização {updatePass}/{MAX_UPDATE_PASSES}"
                            : $"Update pass {updatePass}/{MAX_UPDATE_PASSES}"));

                        SaveUpdateState(updatePass);

                        UpdateLiveStatus(
                            _currentLanguage == "PT"
                                ? $"Verificando atualizações (passo {updatePass})"
                                : $"Checking updates (pass {updatePass})");

                        string checkUpdatesScript = @"
                    Import-Module PSWindowsUpdate -ErrorAction Stop
                    $Updates = Get-WindowsUpdate -MicrosoftUpdate -IgnoreReboot -ErrorAction SilentlyContinue
                    if ($Updates.Count -eq 0) {
                        Write-Host 'NO_UPDATES_FOUND'
                    } else {
                        Write-Host ""UPDATES_FOUND:$($Updates.Count)""
                        foreach ($Update in $Updates) {
                            Write-Host ""  - $($Update.Title)""
                        }
                    }
                ";

                        var checkResult = await RunPowerShellScript(checkUpdatesScript);

                        if (checkResult.Contains("NO_UPDATES_FOUND"))
                        {
                            AddLog("  ✅ " + (_currentLanguage == "PT"
                                ? "Nenhuma atualização disponível"
                                : "No updates available"));
                            updatesFound = false;
                            break;
                        }
                        else if (checkResult.Contains("UPDATES_FOUND:"))
                        {
                            var lines = checkResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    AddLog($"  {line.Trim()}");
                                }
                            }

                            UpdateProgressBar(ProgressStep1, 20 + (updatePass - 1) * 20);

                            UpdateLiveStatus(
                                _currentLanguage == "PT"
                                    ? $"Instalando atualizações (passo {updatePass})"
                                    : $"Installing updates (pass {updatePass})",
                                _currentLanguage == "PT"
                                    ? "Isto pode demorar alguns minutos..."
                                    : "This may take a few minutes...");

                            string installUpdatesScript = @"
                        Import-Module PSWindowsUpdate -ErrorAction Stop
                        
                        Write-Host '=== Installing Windows Updates ==='
                        
                        try {
                            Install-WindowsUpdate -MicrosoftUpdate -AcceptAll -IgnoreReboot -Verbose -Confirm:$false -ErrorAction Stop
                            Write-Host 'UPDATE_INSTALL_SUCCESS'
                        } catch {
                            Write-Host ""UPDATE_INSTALL_ERROR: $($_.Exception.Message)""
                            exit 1
                        }
                        
                        Write-Host '=== Checking for remaining updates ==='
                        $Remaining = Get-WindowsUpdate -MicrosoftUpdate -ErrorAction SilentlyContinue
                        if ($Remaining.Count -gt 0) {
                            Write-Host ""REMAINING_UPDATES:$($Remaining.Count)""
                        } else {
                            Write-Host 'ALL_UPDATES_INSTALLED'
                        }
                    ";

                            try
                            {
                                var installResult = await RunPowerShellScriptWithProgress(
                                    installUpdatesScript,
                                    ProgressStep1,
                                    20 + (updatePass - 1) * 20,
                                    40 + (updatePass - 1) * 20);

                                var installLines = installResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var line in installLines)
                                {
                                    if (!string.IsNullOrWhiteSpace(line) && !line.Contains("WARNING:"))
                                    {
                                        AddLog($"  {line.Trim()}");
                                    }
                                }

                                if (installResult.Contains("REMAINING_UPDATES:"))
                                {
                                    AddLog("  ⚠️ " + (_currentLanguage == "PT"
                                        ? "Atualizações adicionais disponíveis. Será necessário outro passo."
                                        : "Additional updates available. Another pass will be needed."));

                                    if (updatePass < MAX_UPDATE_PASSES)
                                    {
                                        bool rebootRequired = await CheckRebootRequired();

                                        if (rebootRequired)
                                        {
                                            UpdateLiveStatus(
                                                _currentLanguage == "PT"
                                                    ? "Reinicialização necessária"
                                                    : "Restart required");

                                            AddLog("\n  🔄 " + (_currentLanguage == "PT"
                                                ? "Reinicialização necessária. Preparando reinício automático..."
                                                : "Reboot required. Preparing automatic restart..."));

                                            _statusAnimationTimer.Stop();
                                            TxtLiveStatus.Visibility = Visibility.Collapsed;

                                            ShowCustomDialog(
                                                _currentLanguage == "PT" ? "Reinicialização Necessária" : "Restart Required",
                                                _currentLanguage == "PT"
                                                    ? $"É necessário reiniciar o computador para continuar as atualizações.\n\nPasso {updatePass}/{MAX_UPDATE_PASSES} concluído.\n\nDeseja reiniciar AGORA? O processo continuará automaticamente após o reinício."
                                                    : $"Computer restart required to continue updates.\n\nPass {updatePass}/{MAX_UPDATE_PASSES} completed.\n\nRestart NOW? The process will continue automatically after restart.",
                                                DialogType.Question,
                                                async () =>
                                                {
                                                    SaveUpdateState(updatePass + 1);
                                                    await ScheduleStartup();
                                                    RestartComputer();
                                                },
                                                null
                                            );
                                            return;
                                        }
                                    }

                                    updatePass++;
                                    await Task.Delay(5000);
                                }
                                else if (installResult.Contains("ALL_UPDATES_INSTALLED"))
                                {
                                    AddLog("  ✅ " + (_currentLanguage == "PT"
                                        ? "Todas as atualizações instaladas!"
                                        : "All updates installed!"));
                                    updatesFound = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                AddLog($"  ⚠️ Error during installation: {ex.Message}");

                                UpdateLiveStatus(
                                    _currentLanguage == "PT"
                                        ? "Tentando método alternativo"
                                        : "Trying alternative method");

                                await RunCommand("UsoClient.exe", "StartScan");
                                await Task.Delay(5000);
                                await RunCommand("UsoClient.exe", "StartDownload");
                                await Task.Delay(10000);
                                await RunCommand("UsoClient.exe", "StartInstall");

                                updatesFound = false;
                            }
                        }
                        else
                        {
                            AddLog("  ⚠️ " + (_currentLanguage == "PT"
                                ? "Não foi possível verificar atualizações"
                                : "Unable to check for updates"));

                            UpdateLiveStatus(
                                _currentLanguage == "PT"
                                    ? "Usando método alternativo"
                                    : "Using alternative method");

                            await RunCommand("UsoClient.exe", "StartScan");
                            await Task.Delay(5000);
                            await RunCommand("UsoClient.exe", "StartDownload");
                            await Task.Delay(10000);
                            await RunCommand("UsoClient.exe", "StartInstall");

                            updatesFound = false;
                        }
                    }

                    if (updatePass > MAX_UPDATE_PASSES)
                    {
                        AddLog("\n  ℹ️ " + (_currentLanguage == "PT"
                            ? $"Número máximo de passes ({MAX_UPDATE_PASSES}) atingido. Verifique o Windows Update manualmente para confirmar."
                            : $"Maximum number of passes ({MAX_UPDATE_PASSES}) reached. Check Windows Update manually to confirm."));
                    }

                    UpdateProgressBar(ProgressStep1, 95);

                    UpdateLiveStatus(
                        _currentLanguage == "PT"
                            ? "Verificação final de atualizações"
                            : "Final update check");

                    var finalCheck = await RunPowerShellScript(@"
                Import-Module PSWindowsUpdate -ErrorAction SilentlyContinue
                $Final = Get-WindowsUpdate -MicrosoftUpdate -ErrorAction SilentlyContinue
                if ($Final.Count -eq 0) {
                    Write-Host 'SYSTEM_UP_TO_DATE'
                } else {
                    Write-Host ""UPDATES_STILL_AVAILABLE:$($Final.Count)""
                }
            ");

                    if (finalCheck.Contains("SYSTEM_UP_TO_DATE"))
                    {
                        AddLog("  ✅ " + (_currentLanguage == "PT"
                            ? "Sistema totalmente atualizado!"
                            : "System fully up to date!"));
                    }
                    else if (finalCheck.Contains("UPDATES_STILL_AVAILABLE:"))
                    {
                        AddLog("  ⚠️ " + (_currentLanguage == "PT"
                            ? "Algumas atualizações ainda disponíveis. Podem requerer reinicialização manual."
                            : "Some updates still available. May require manual restart."));
                    }

                    UpdateProgressBar(ProgressStep1, 100);
                });

                UpdateStepStatus(1, _currentLanguage == "PT" ? "✅ Concluído" : "✅ Completed", Brushes.LightGreen);
                UpdateProgress();
                AddLog(_currentLanguage == "PT"
                    ? "  ✅ Processo de atualização do Windows concluído!"
                    : "  ✅ Windows update process completed!");
            }
            catch (Exception ex)
            {
                UpdateStepStatus(1, _currentLanguage == "PT" ? "❌ Erro" : "❌ Error", Brushes.Red);
                AddLog($"  ❌ {(_currentLanguage == "PT" ? "Erro" : "Error")}: {ex.Message}");
            }
            finally
            {
                _statusAnimationTimer.Stop();
                Dispatcher.Invoke(() =>
                {
                    TxtLiveStatus.Visibility = Visibility.Collapsed;
                });
            }
        }

        private async Task ResetWindowsUpdateComponents()
        {
            try
            {
                string resetScript = @"
                    Write-Host 'Stopping Windows Update services...'
                    Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue
                    Stop-Service -Name cryptSvc -Force -ErrorAction SilentlyContinue
                    Stop-Service -Name bits -Force -ErrorAction SilentlyContinue
                    Stop-Service -Name msiserver -Force -ErrorAction SilentlyContinue
                    
                    Start-Sleep -Seconds 2
                    
                    Write-Host 'Starting Windows Update services...'
                    Start-Service -Name wuauserv -ErrorAction SilentlyContinue
                    Start-Service -Name cryptSvc -ErrorAction SilentlyContinue
                    Start-Service -Name bits -ErrorAction SilentlyContinue
                    
                    Write-Host 'Services restarted successfully'
                ";

                await RunPowerShellScript(resetScript);
                AddLog("  ✓ " + (_currentLanguage == "PT"
                    ? "Componentes do Windows Update reiniciados"
                    : "Windows Update components reset"));
            }
            catch
            {
                AddLog("  ⚠️ " + (_currentLanguage == "PT"
                    ? "Aviso ao reiniciar componentes"
                    : "Warning resetting components"));
            }
        }

        private async Task<bool> CheckRebootRequired()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"))
                    {
                        return key != null;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        private async Task ScheduleStartup()
        {
            try
            {
                string appPath = Process.GetCurrentProcess().MainModule.FileName;
                string taskScript = $@"
                    $Action = New-ScheduledTaskAction -Execute '{appPath}'
                    $Trigger = New-ScheduledTaskTrigger -AtLogOn
                    $Principal = New-ScheduledTaskPrincipal -UserId '$env:USERNAME' -LogonType Interactive -RunLevel Highest
                    $Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
                    
                    Register-ScheduledTask -TaskName 'NexorAutoResume' -Action $Action -Trigger $Trigger -Principal $Principal -Settings $Settings -Force
                    
                    Write-Host 'Startup task scheduled'
                ";

                await RunPowerShellScript(taskScript);
                AddLog("  ✓ " + (_currentLanguage == "PT"
                    ? "Reinício automático configurado"
                    : "Auto-resume configured"));
            }
            catch (Exception ex)
            {
                AddLog($"  ⚠️ Schedule startup: {ex.Message}");
            }
        }

        private void RestartComputer()
        {
            try
            {
                ShowCustomNotification(
                    _currentLanguage == "PT" ? "Reiniciando..." : "Restarting...",
                    _currentLanguage == "PT"
                        ? "O computador irá reiniciar em 10 segundos..."
                        : "Computer will restart in 10 seconds...",
                    NotificationType.Info
                );

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/r /t 10 /c \"Nexor - Reiniciando para continuar atualizações\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                ShowCustomNotification(
                    "Error",
                    $"{(_currentLanguage == "PT" ? "Erro ao reiniciar" : "Error restarting")}: {ex.Message}",
                    NotificationType.Error
                );
            }
        }

        private async void BtnStep2_Click(object sender, RoutedEventArgs e)
        {
            BtnStep2.IsEnabled = false;
            await RunStep2();
            BtnStep2.IsEnabled = true;
        }

        private async Task RunStep2()
        {
            _statusAnimationTimer.Start();

            AddLog("\n" + (_currentLanguage == "PT" ? "▶ Passo 2: Atualizando Drivers..." : "▶ Step 2: Updating Drivers..."));
            AddLog(_currentLanguage == "PT"
                ? "⏰ Este passo pode demorar 10-20 minutos. Aguarde..."
                : "⏰ This step may take 10-20 minutes. Please wait...");

            UpdateStepStatus(2, _currentLanguage == "PT" ? "🔄 A executar..." : "🔄 Running...", Brushes.Orange);
            AnimateStep(Step2Card, Step2Badge);
            ProgressStep2.Visibility = Visibility.Visible;

            try
            {
                await Task.Run(async () =>
                {
                    UpdateLiveStatus(
                        _currentLanguage == "PT"
                            ? "Verificando dispositivos sem driver"
                            : "Checking devices without drivers");

                    UpdateProgressBar(ProgressStep2, 10);

                    try
                    {
                        await RunCommand("pnputil.exe", "/scan-devices");
                        AddLog(_currentLanguage == "PT"
                            ? "  ✓ Scan de dispositivos concluído"
                            : "  ✓ Device scan completed");
                    }
                    catch { }

                    UpdateProgressBar(ProgressStep2, 20);

                    UpdateLiveStatus(
                        _currentLanguage == "PT"
                            ? "Buscando atualizações de drivers"
                            : "Searching for driver updates");

                    string driverUpdateScript = @"
                Write-Host '=== Searching for Driver Updates ==='
                
                try {
                    $Session = New-Object -ComObject Microsoft.Update.Session
                    $Searcher = $Session.CreateUpdateSearcher()
                    
                    $Searcher.ServiceID = '7971f918-a847-4430-9279-4a52d1efe18d'
                    $Searcher.SearchScope = 1
                    $Searcher.ServerSelection = 3
                    
                    Write-Host 'Searching for driver updates...'
                    $SearchResult = $Searcher.Search(""IsInstalled=0 and Type='Driver' and IsHidden=0"")
                    
                    if ($SearchResult.Updates.Count -eq 0) {
                        Write-Host 'NO_DRIVERS_FOUND'
                        exit 0
                    }
                    
                    Write-Host ""DRIVERS_FOUND:$($SearchResult.Updates.Count)""
                    
                    foreach ($Update in $SearchResult.Updates) {
                        Write-Host ""  - $($Update.Title)""
                    }
                    
                    Write-Host '=== Downloading Driver Updates ==='
                    $UpdatesToDownload = New-Object -ComObject Microsoft.Update.UpdateColl
                    foreach ($Update in $SearchResult.Updates) {
                        $UpdatesToDownload.Add($Update) | Out-Null
                    }
                    
                    $Downloader = $Session.CreateUpdateDownloader()
                    $Downloader.Updates = $UpdatesToDownload
                    $DownloadResult = $Downloader.Download()
                    
                    Write-Host ""Download result: $($DownloadResult.ResultCode)""
                    
                    Write-Host '=== Installing Driver Updates ==='
                    $UpdatesToInstall = New-Object -ComObject Microsoft.Update.UpdateColl
                    foreach ($Update in $SearchResult.Updates) {
                        if ($Update.IsDownloaded) {
                            $UpdatesToInstall.Add($Update) | Out-Null
                        }
                    }
                    
                    if ($UpdatesToInstall.Count -eq 0) {
                        Write-Host 'NO_DRIVERS_TO_INSTALL'
                        exit 0
                    }
                    
                    $Installer = $Session.CreateUpdateInstaller()
                    $Installer.Updates = $UpdatesToInstall
                    $InstallResult = $Installer.Install()
                    
                    Write-Host ""Installation result: $($InstallResult.ResultCode)""
                    Write-Host ""Reboot required: $($InstallResult.RebootRequired)""
                    
                    Write-Host 'DRIVER_UPDATE_SUCCESS'
                    
                } catch {
                    Write-Host ""DRIVER_UPDATE_ERROR: $($_.Exception.Message)""
                    exit 1
                }
            ";

                    try
                    {
                        UpdateLiveStatus(
                            _currentLanguage == "PT"
                                ? "Instalando drivers"
                                : "Installing drivers",
                            _currentLanguage == "PT"
                                ? "Isto pode levar alguns minutos..."
                                : "This may take a few minutes...");

                        var driverResult = await RunPowerShellScriptWithProgress(driverUpdateScript, ProgressStep2, 20, 90);

                        var lines = driverResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                AddLog($"  {line.Trim()}");
                            }
                        }

                        if (driverResult.Contains("NO_DRIVERS_FOUND"))
                        {
                            AddLog("  ✅ " + (_currentLanguage == "PT"
                                ? "Todos os drivers estão atualizados"
                                : "All drivers are up to date"));
                        }
                        else if (driverResult.Contains("DRIVER_UPDATE_SUCCESS"))
                        {
                            AddLog("  ✅ " + (_currentLanguage == "PT"
                                ? "Drivers atualizados com sucesso"
                                : "Drivers updated successfully"));
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLog($"  ⚠️ Driver update: {ex.Message}");

                        UpdateLiveStatus(
                            _currentLanguage == "PT"
                                ? "Tentando método alternativo"
                                : "Trying alternative method");

                        string altDriverScript = @"
                    Import-Module PSWindowsUpdate -ErrorAction Stop
                    Get-WindowsUpdate -MicrosoftUpdate -UpdateType Driver -AcceptAll -Install -IgnoreReboot -Verbose
                    Write-Host 'Alternative driver update completed'
                ";

                        try
                        {
                            var altResult = await RunPowerShellScript(altDriverScript);
                            AddLog($"  ✓ {altResult.Trim()}");
                        }
                        catch
                        {
                            AddLog("  ⚠️ " + (_currentLanguage == "PT"
                                ? "Método alternativo falhou. Verifique manualmente."
                                : "Alternative method failed. Check manually."));
                        }
                    }

                    UpdateProgressBar(ProgressStep2, 100);

                    AddLog(_currentLanguage == "PT"
                        ? "  ✓ Processo de atualização de drivers concluído"
                        : "  ✓ Driver update process completed");
                });

                UpdateStepStatus(2, _currentLanguage == "PT" ? "✅ Concluído" : "✅ Completed", Brushes.LightGreen);
                UpdateProgress();
            }
            catch (Exception ex)
            {
                UpdateStepStatus(2, _currentLanguage == "PT" ? "❌ Erro" : "❌ Error", Brushes.Red);
                AddLog($"  ❌ {(_currentLanguage == "PT" ? "Erro" : "Error")}: {ex.Message}");
            }
            finally
            {
                _statusAnimationTimer.Stop();
                TxtLiveStatus.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnStep3_Click(object sender, RoutedEventArgs e)
        {
            BtnStep3.IsEnabled = false;
            await RunStep3();
            BtnStep3.IsEnabled = true;
        }

        private async Task RunStep3()
        {
            _statusAnimationTimer.Start();

            AddLog("\n" + (_currentLanguage == "PT" ? "▶ Passo 3: Limpando Sistema..." : "▶ Step 3: Cleaning System..."));
            UpdateStepStatus(3, _currentLanguage == "PT" ? "🔄 A executar..." : "🔄 Running...", Brushes.Orange);
            AnimateStep(Step3Card, Step3Badge);
            ProgressStep3.Visibility = Visibility.Visible;

            try
            {
                long totalFreed = 0;

                await Task.Run(async () =>
                {
                    UpdateLiveStatus(
                        _currentLanguage == "PT"
                            ? "Limpando cache do Windows Update"
                            : "Cleaning Windows Update cache");

                    UpdateProgressBar(ProgressStep3, 15);

                    string updateCachePath = @"C:\Windows\SoftwareDistribution\Download";
                    long freed = await Task.Run(() => CleanDirectory(updateCachePath));
                    totalFreed += freed;
                    AddLog($"  ✓ {FormatBytes(freed)} " + (_currentLanguage == "PT" ? "libertados" : "freed"));

                    UpdateLiveStatus(
                        _currentLanguage == "PT"
                            ? "Limpando ficheiros temporários do utilizador"
                            : "Cleaning user temporary files");

                    UpdateProgressBar(ProgressStep3, 30);

                    string tempPath = Path.GetTempPath();
                    freed = await Task.Run(() => CleanDirectory(tempPath));
                    totalFreed += freed;
                    AddLog($"  ✓ {FormatBytes(freed)} " + (_currentLanguage == "PT" ? "libertados" : "freed"));

                    UpdateLiveStatus(
                        _currentLanguage == "PT"
                            ? "Limpando ficheiros temporários do Windows"
                            : "Cleaning Windows temporary files");

                    UpdateProgressBar(ProgressStep3, 45);

                    string windowsTempPath = @"C:\Windows\Temp";
                    freed = await Task.Run(() => CleanDirectory(windowsTempPath));
                    totalFreed += freed;
                    AddLog($"  ✓ {FormatBytes(freed)} " + (_currentLanguage == "PT" ? "libertados" : "freed"));

                    UpdateLiveStatus(
                        _currentLanguage == "PT"
                            ? "Limpando Prefetch"
                            : "Cleaning Prefetch");

                    UpdateProgressBar(ProgressStep3, 60);

                    string prefetchPath = @"C:\Windows\Prefetch";
                    freed = await Task.Run(() => CleanDirectory(prefetchPath));
                    totalFreed += freed;
                    AddLog($"  ✓ {FormatBytes(freed)} " + (_currentLanguage == "PT" ? "libertados" : "freed"));

                    UpdateLiveStatus(
                        _currentLanguage == "PT"
                            ? "Esvaziando Reciclagem"
                            : "Emptying Recycle Bin");

                    UpdateProgressBar(ProgressStep3, 70);

                    try
                    {
                        await RunPowerShellCommand("Clear-RecycleBin -Force -ErrorAction SilentlyContinue");
                        AddLog("  ✓ " + (_currentLanguage == "PT" ? "Reciclagem esvaziada" : "Recycle Bin emptied"));
                    }
                    catch { }

                    UpdateLiveStatus(
                        _currentLanguage == "PT"
                            ? "Limpando componentes do Windows"
                            : "Cleaning Windows components");

                    UpdateProgressBar(ProgressStep3, 85);

                    await ExecuteAdvancedDiskCleanup();

                    UpdateProgressBar(ProgressStep3, 100);
                });

                double freedGB = totalFreed / (1024.0 * 1024.0 * 1024.0);
                string message = $"✅ {(_currentLanguage == "PT" ? "Concluído" : "Completed")} ({freedGB:F2} GB)";

                UpdateStepStatus(3, message, Brushes.LightGreen);
                UpdateProgress();
                AddLog($"  ✓ " + (_currentLanguage == "PT"
                    ? $"Limpeza concluída! Total libertado: {freedGB:F2} GB"
                    : $"Cleanup completed! Total freed: {freedGB:F2} GB"));
            }
            catch (Exception ex)
            {
                UpdateStepStatus(3, _currentLanguage == "PT" ? "❌ Erro" : "❌ Error", Brushes.Red);
                AddLog($"  ❌ {(_currentLanguage == "PT" ? "Erro" : "Error")}: {ex.Message}");
            }
            finally
            {
                _statusAnimationTimer.Stop();
                TxtLiveStatus.Visibility = Visibility.Collapsed;
            }
        }

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

        private async Task<string> RunPowerShellScriptWithProgress(string script, ProgressBar progressBar, double startProgress, double endProgress)
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
                        var outputBuilder = new System.Text.StringBuilder();
                        double progressRange = endProgress - startProgress;
                        int linesRead = 0;

                        while (!process.StandardOutput.EndOfStream)
                        {
                            string? line = process.StandardOutput.ReadLine();
                            if (!string.IsNullOrEmpty(line))
                            {
                                outputBuilder.AppendLine(line);
                                linesRead++;

                                double progress = startProgress + (progressRange * Math.Min(linesRead / 20.0, 1.0));
                                UpdateProgressBar(progressBar, progress);
                            }
                        }

                        process.WaitForExit();
                        output = outputBuilder.ToString();
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

        private async Task ExecuteAdvancedDiskCleanup()
        {
            try
            {
                AddLog(_currentLanguage == "PT"
                    ? "  → Executando limpeza avançada de componentes..."
                    : "  → Running advanced component cleanup...");

                await RunCommand("Dism.exe", "/online /Cleanup-Image /StartComponentCleanup /ResetBase");

                await Task.Delay(2000);

                AddLog(_currentLanguage == "PT"
                    ? "  ✓ Limpeza de componentes concluída"
                    : "  ✓ Component cleanup completed");
            }
            catch (Exception ex)
            {
                AddLog($"  ⚠️ DISM Cleanup: {ex.Message}");
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

        private void UpdateProgress()
        {
            _completedSteps++;
            int percentage = (_completedSteps * 100) / 3;

            Dispatcher.Invoke(() =>
            {
                TxtProgress.Text = $"{percentage}%";
                ProgressBarMain.Value = percentage;

                if (percentage >= 100)
                {
                    TxtStatusDesc.Text = _currentLanguage == "PT"
                        ? "✅ Configuração concluída!"
                        : "✅ Setup completed!";
                }
                else
                {
                    TxtStatusDesc.Text = _currentLanguage == "PT"
                        ? $"A processar... ({_completedSteps}/3 passos)"
                        : $"Processing... ({_completedSteps}/3 steps)";
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
    }
}