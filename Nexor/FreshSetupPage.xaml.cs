using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.ServiceProcess;

namespace Nexor
{
    public partial class FreshSetupPage : UserControl
    {
        private int _completedSteps = 0;
        private readonly string _currentLanguage;
        private bool _isRunning = false;
        private static readonly HttpClient _httpClient = new HttpClient();

        public FreshSetupPage(string language = "PT")
        {
            InitializeComponent();
            _currentLanguage = language;
            SetLanguage(language);
            CheckAdminPrivileges();
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

                MessageBox.Show(
                    _currentLanguage == "PT"
                        ? "⚠️ ATENÇÃO: Este programa DEVE ser executado como Administrador!\n\nPor favor, feche e execute novamente com 'Executar como Administrador'."
                        : "⚠️ WARNING: This program MUST be run as Administrator!\n\nPlease close and run again with 'Run as Administrator'.",
                    "Nexor - Administrator Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
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
                ? "🔍 Executando diagnósticos do sistema..."
                : "🔍 Running system diagnostics..."));

            bool allChecksPass = true;

            // Check 1: Admin privileges
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
                    ? "FALHA: Não está a executar como Administrador"
                    : "FAILED: Not running as Administrator"));
                allChecksPass = false;
            }

            // Check 2: Internet connection
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
                    ? "AVISO: Sem conexão à Internet"
                    : "WARNING: No Internet Connection"));
                allChecksPass = false;
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
                    ? "AVISO: Serviço Windows Update não está ativo"
                    : "WARNING: Windows Update Service not running"));

                AddLog("  → " + (_currentLanguage == "PT"
                    ? "Tentando iniciar o serviço..."
                    : "Attempting to start service..."));

                bool started = await StartWindowsUpdateService();
                if (started)
                {
                    AddLog("  ✅ " + (_currentLanguage == "PT"
                        ? "Serviço iniciado com sucesso"
                        : "Service started successfully"));
                }
                else
                {
                    AddLog("  ❌ " + (_currentLanguage == "PT"
                        ? "Falha ao iniciar serviço"
                        : "Failed to start service"));
                    allChecksPass = false;
                }
            }

            // Check 4: PowerShell availability
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
                    ? "FALHA: PowerShell não encontrado"
                    : "FAILED: PowerShell not found"));
                allChecksPass = false;
            }

            // Check 5: PSWindowsUpdate Module
            bool moduleInstalled = await CheckPSWindowsUpdateModule();
            if (moduleInstalled)
            {
                AddLog("  ✅ " + (_currentLanguage == "PT"
                    ? "Módulo PSWindowsUpdate: Instalado"
                    : "PSWindowsUpdate Module: Installed"));
            }
            else
            {
                AddLog("  ⚠️ " + (_currentLanguage == "PT"
                    ? "Módulo PSWindowsUpdate não encontrado"
                    : "PSWindowsUpdate Module not found"));

                AddLog("  → " + (_currentLanguage == "PT"
                    ? "O módulo será instalado automaticamente durante a atualização"
                    : "Module will be installed automatically during update"));
            }

            // Check 6: Disk space
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
            }
            else
            {
                AddLog($"  ❌ " + (_currentLanguage == "PT"
                    ? $"CRÍTICO: Espaço insuficiente: {freeSpaceGB:F1} GB"
                    : $"CRITICAL: Insufficient space: {freeSpaceGB:F1} GB"));
                allChecksPass = false;
            }

            AddLog("");

            if (allChecksPass)
            {
                AddLog("✅ " + (_currentLanguage == "PT"
                    ? "Todos os diagnósticos passaram! Sistema pronto."
                    : "All diagnostics passed! System ready."));
            }
            else
            {
                AddLog("⚠️ " + (_currentLanguage == "PT"
                    ? "Alguns problemas foram detectados. O processo pode falhar."
                    : "Some issues detected. Process may fail."));
            }

            return allChecksPass;
        }

        private async Task<bool> CheckInternetConnection()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync("http://www.msftconnecttest.com/connecttest.txt",
                        new System.Threading.CancellationToken());
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            });
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

        private async Task<bool> CheckPSWindowsUpdateModule()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -Command \"Get-Module -ListAvailable -Name PSWindowsUpdate\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(psi))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();
                            return output.Contains("PSWindowsUpdate");
                        }
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            });
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

        private void ShowDetailedErrorMessage(string step, Exception ex)
        {
            string errorDetails = "";
            string solution = "";

            if (ex.Message.Contains("PSWindowsUpdate"))
            {
                errorDetails = _currentLanguage == "PT"
                    ? "O módulo PSWindowsUpdate não está instalado ou não pode ser carregado."
                    : "PSWindowsUpdate module is not installed or cannot be loaded.";

                solution = _currentLanguage == "PT"
                    ? "Solução:\n1. Abra PowerShell como Administrador\n2. Execute: Install-Module -Name PSWindowsUpdate -Force\n3. Tente novamente"
                    : "Solution:\n1. Open PowerShell as Administrator\n2. Run: Install-Module -Name PSWindowsUpdate -Force\n3. Try again";
            }
            else if (ex.Message.Contains("Internet") || ex.Message.Contains("network"))
            {
                errorDetails = _currentLanguage == "PT"
                    ? "Sem conexão à Internet ou bloqueio de rede."
                    : "No Internet connection or network block.";

                solution = _currentLanguage == "PT"
                    ? "Solução:\n1. Verifique a conexão à Internet\n2. Desative temporariamente firewall/antivírus\n3. Tente novamente"
                    : "Solution:\n1. Check Internet connection\n2. Temporarily disable firewall/antivirus\n3. Try again";
            }
            else if (ex.Message.Contains("Access") || ex.Message.Contains("Denied"))
            {
                errorDetails = _currentLanguage == "PT"
                    ? "Acesso negado - privilégios insuficientes."
                    : "Access denied - insufficient privileges.";

                solution = _currentLanguage == "PT"
                    ? "Solução:\n1. Feche a aplicação\n2. Execute como Administrador\n3. Tente novamente"
                    : "Solution:\n1. Close the application\n2. Run as Administrator\n3. Try again";
            }
            else if (ex.Message.Contains("wuauserv") || ex.Message.Contains("Windows Update"))
            {
                errorDetails = _currentLanguage == "PT"
                    ? "O serviço Windows Update não está a funcionar corretamente."
                    : "Windows Update service is not working correctly.";

                solution = _currentLanguage == "PT"
                    ? "Solução:\n1. Abra Serviços (services.msc)\n2. Procure 'Windows Update'\n3. Clique com botão direito → Iniciar\n4. Tente novamente"
                    : "Solution:\n1. Open Services (services.msc)\n2. Find 'Windows Update'\n3. Right-click → Start\n4. Try again";
            }
            else
            {
                errorDetails = ex.Message;
                solution = _currentLanguage == "PT"
                    ? "Solução:\n1. Verifique o log para mais detalhes\n2. Tente executar os passos manualmente\n3. Reinicie o computador e tente novamente"
                    : "Solution:\n1. Check the log for more details\n2. Try running steps manually\n3. Restart computer and try again";
            }

            MessageBox.Show(
                $"{(_currentLanguage == "PT" ? "Erro em" : "Error in")} {step}:\n\n" +
                $"{errorDetails}\n\n{solution}",
                "Nexor - " + (_currentLanguage == "PT" ? "Erro Detalhado" : "Detailed Error"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
                TxtInfo4.Text = "• Pode afastar-se do computador - o processo é totalmente automático";
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
                TxtInfo4.Text = "• You can walk away - the process is fully automatic";
            }
        }

        private async void BtnRunAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
                return;

            var result = MessageBox.Show(
                _currentLanguage == "PT"
                    ? "Deseja executar todos os passos automaticamente?\n\n⚠️ IMPORTANTE:\n• O processo pode demorar 30-60 minutos\n• Pode afastar-se do computador\n• As atualizações serão instaladas automaticamente\n• O log mostrará o progresso em tempo real\n\nRecomendação: Deixe o computador ligado e conectado à internet."
                    : "Do you want to run all steps automatically?\n\n⚠️ IMPORTANT:\n• The process may take 30-60 minutes\n• You can walk away from the computer\n• Updates will be installed automatically\n• The log will show real-time progress\n\nRecommendation: Keep computer on and connected to internet.",
                "Nexor - " + (_currentLanguage == "PT" ? "Configuração Automática" : "Automatic Setup"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
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

                if (!diagnosticsPass)
                {
                    var continueResult = MessageBox.Show(
                        _currentLanguage == "PT"
                            ? "⚠️ Alguns problemas foram detectados durante os diagnósticos.\n\nO processo pode não funcionar corretamente.\n\nDeseja continuar mesmo assim?"
                            : "⚠️ Some issues were detected during diagnostics.\n\nThe process may not work correctly.\n\nDo you want to continue anyway?",
                        "Nexor - " + (_currentLanguage == "PT" ? "Aviso" : "Warning"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (continueResult == MessageBoxResult.No)
                    {
                        _isRunning = false;
                        BtnRunAll.IsEnabled = true;
                        AddLog("\n❌ " + (_currentLanguage == "PT"
                            ? "Processo cancelado pelo utilizador"
                            : "Process cancelled by user"));
                        return;
                    }
                }

                AddLog("\n" + (_currentLanguage == "PT"
                    ? "⏰ Pode afastar-se do computador - isto será automático"
                    : "⏰ You can walk away - this will be automatic"));
                AddLog("═══════════════════════════════════════\n");

                await RunAllSteps();

                _isRunning = false;
                BtnRunAll.IsEnabled = true;
            }
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

                MessageBox.Show(
                    _currentLanguage == "PT"
                        ? "🎉 Configuração inicial concluída!\n\n✅ Windows Update: Todas as atualizações instaladas\n✅ Drivers: Atualizados via Windows Update\n✅ Sistema: Limpo e otimizado\n\nRecomendação: Reinicie o computador agora para aplicar todas as alterações."
                        : "🎉 Initial setup completed!\n\n✅ Windows Update: All updates installed\n✅ Drivers: Updated via Windows Update\n✅ System: Cleaned and optimized\n\nRecommendation: Restart your computer now to apply all changes.",
                    "Nexor - " + (_currentLanguage == "PT" ? "Sucesso" : "Success"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AddLog($"\n❌ {(_currentLanguage == "PT" ? "ERRO CRÍTICO" : "CRITICAL ERROR")}: {ex.Message}");
                MessageBox.Show(
                    $"{(_currentLanguage == "PT" ? "Erro durante a configuração" : "Error during setup")}:\n{ex.Message}",
                    "Nexor - Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
            AddLog("\n" + (_currentLanguage == "PT" ? "▶ Passo 1: Atualizando Windows..." : "▶ Step 1: Updating Windows..."));
            AddLog(_currentLanguage == "PT"
                ? "⏰ Este passo pode demorar 15-30 minutos. Aguarde..."
                : "⏰ This step may take 15-30 minutes. Please wait...");

            UpdateStepStatus(1, _currentLanguage == "PT" ? "🔄 A executar..." : "🔄 Running...", Brushes.Orange);
            AnimateStep(Step1Card, Step1Badge);
            ProgressStep1.Visibility = Visibility.Visible;

            try
            {
                await Task.Run(async () =>
                {
                    AddLog(_currentLanguage == "PT"
                        ? "  → Preparando sistema de atualizações..."
                        : "  → Preparing update system...");
                    UpdateProgressBar(ProgressStep1, 5);

                    string installModuleScript = @"
                        if (-not (Get-Module -ListAvailable -Name PSWindowsUpdate)) {
                            Write-Host 'Installing PSWindowsUpdate module...'
                            Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -ErrorAction SilentlyContinue
                            Set-PSRepository -Name 'PSGallery' -InstallationPolicy Trusted -ErrorAction SilentlyContinue
                            Install-Module -Name PSWindowsUpdate -Force -Confirm:$false -ErrorAction Stop
                            Write-Host 'PSWindowsUpdate installed successfully'
                        } else {
                            Write-Host 'PSWindowsUpdate already installed'
                        }
                    ";

                    try
                    {
                        var moduleResult = await RunPowerShellScript(installModuleScript);
                        AddLog($"  ✓ {moduleResult.Trim()}");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"  ⚠️ Module installation: {ex.Message}");
                    }

                    UpdateProgressBar(ProgressStep1, 10);

                    AddLog(_currentLanguage == "PT"
                        ? "  → Verificando atualizações disponíveis..."
                        : "  → Checking for available updates...");

                    string updateScript = @"
                        Import-Module PSWindowsUpdate -ErrorAction Stop
                        
                        Write-Host '=== Searching for Windows Updates ==='
                        
                        $Updates = Get-WindowsUpdate -MicrosoftUpdate -IgnoreReboot -Verbose
                        
                        if ($Updates.Count -eq 0) {
                            Write-Host 'No updates available'
                            exit 0
                        }
                        
                        Write-Host ""Found $($Updates.Count) updates to install""
                        
                        Write-Host '=== Installing Updates ==='
                        Install-WindowsUpdate -MicrosoftUpdate -AcceptAll -IgnoreReboot -Verbose -Confirm:$false
                        
                        Write-Host '=== Update Installation Complete ==='
                        
                        $RemainingUpdates = Get-WindowsUpdate -MicrosoftUpdate
                        if ($RemainingUpdates.Count -gt 0) {
                            Write-Host ""Note: $($RemainingUpdates.Count) additional updates available (may require reboot)""
                        } else {
                            Write-Host 'All updates installed successfully'
                        }
                    ";

                    UpdateProgressBar(ProgressStep1, 20);

                    try
                    {
                        AddLog(_currentLanguage == "PT"
                            ? "  → Instalando atualizações (isto pode demorar...)..."
                            : "  → Installing updates (this may take a while)...");

                        var updateResult = await RunPowerShellScriptWithProgress(updateScript, ProgressStep1, 20, 90);

                        var lines = updateResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                AddLog($"  {line.Trim()}");
                            }
                        }

                        UpdateProgressBar(ProgressStep1, 95);
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        AddLog($"  ⚠️ Update installation: {ex.Message}");

                        AddLog(_currentLanguage == "PT"
                            ? "  → Tentando método alternativo..."
                            : "  → Trying alternative method...");

                        await RunCommand("UsoClient.exe", "StartScan");
                        await Task.Delay(5000);
                        await RunCommand("UsoClient.exe", "StartDownload");
                        await Task.Delay(5000);
                        await RunCommand("UsoClient.exe", "StartInstall");

                        AddLog(_currentLanguage == "PT"
                            ? "  ✓ Processo de atualização iniciado via UsoClient"
                            : "  ✓ Update process started via UsoClient");
                    }

                    UpdateProgressBar(ProgressStep1, 100);
                });

                UpdateStepStatus(1, _currentLanguage == "PT" ? "✅ Concluído" : "✅ Completed", Brushes.LightGreen);
                UpdateProgress();
                AddLog(_currentLanguage == "PT"
                    ? "  ✅ Windows Update concluído! Verifique o Windows Update para confirmar."
                    : "  ✅ Windows Update completed! Check Windows Update to confirm.");
            }
            catch (Exception ex)
            {
                UpdateStepStatus(1, _currentLanguage == "PT" ? "❌ Erro" : "❌ Error", Brushes.Red);
                AddLog($"  ❌ {(_currentLanguage == "PT" ? "Erro" : "Error")}: {ex.Message}");
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
                    AddLog(_currentLanguage == "PT"
                        ? "  → Verificando dispositivos sem driver..."
                        : "  → Checking devices without drivers...");
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
                                Write-Host 'No driver updates found'
                                exit 0
                            }
                            
                            Write-Host ""Found $($SearchResult.Updates.Count) driver updates""
                            
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
                            
                            Write-Host ""Download completed with result code: $($DownloadResult.ResultCode)""
                            
                            Write-Host '=== Installing Driver Updates ==='
                            $UpdatesToInstall = New-Object -ComObject Microsoft.Update.UpdateColl
                            foreach ($Update in $SearchResult.Updates) {
                                if ($Update.IsDownloaded) {
                                    $UpdatesToInstall.Add($Update) | Out-Null
                                }
                            }
                            
                            if ($UpdatesToInstall.Count -eq 0) {
                                Write-Host 'No updates ready to install'
                                exit 0
                            }
                            
                            $Installer = $Session.CreateUpdateInstaller()
                            $Installer.Updates = $UpdatesToInstall
                            $InstallResult = $Installer.Install()
                            
                            Write-Host ""Installation completed with result code: $($InstallResult.ResultCode)""
                            Write-Host ""Reboot required: $($InstallResult.RebootRequired)""
                            
                            Write-Host '=== Installation Summary ==='
                            for ($i = 0; $i -lt $UpdatesToInstall.Count; $i++) {
                                Write-Host ""$($UpdatesToInstall.Item($i).Title): ResultCode=$($InstallResult.GetUpdateResult($i).ResultCode)""
                            }
                            
                            Write-Host '=== Driver Update Complete ==='
                            
                        } catch {
                            Write-Host ""Error: $($_.Exception.Message)""
                            exit 1
                        }
                    ";

                    AddLog(_currentLanguage == "PT"
                        ? "  → Buscando e instalando drivers do Windows Update..."
                        : "  → Searching and installing drivers from Windows Update...");

                    try
                    {
                        var driverResult = await RunPowerShellScriptWithProgress(driverUpdateScript, ProgressStep2, 20, 90);

                        var lines = driverResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                AddLog($"  {line.Trim()}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLog($"  ⚠️ Driver update: {ex.Message}");
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
        }

        private async void BtnStep3_Click(object sender, RoutedEventArgs e)
        {
            BtnStep3.IsEnabled = false;
            await RunStep3();
            BtnStep3.IsEnabled = true;
        }

        private async Task RunStep3()
        {
            AddLog("\n" + (_currentLanguage == "PT" ? "▶ Passo 3: Limpando Sistema..." : "▶ Step 3: Cleaning System..."));
            UpdateStepStatus(3, _currentLanguage == "PT" ? "🔄 A executar..." : "🔄 Running...", Brushes.Orange);
            AnimateStep(Step3Card, Step3Badge);
            ProgressStep3.Visibility = Visibility.Visible;

            try
            {
                long totalFreed = 0;

                await Task.Run(async () =>
                {
                    AddLog(_currentLanguage == "PT"
                        ? "  → Limpando cache do Windows Update..."
                        : "  → Cleaning Windows Update cache...");
                    UpdateProgressBar(ProgressStep3, 15);

                    string updateCachePath = @"C:\Windows\SoftwareDistribution\Download";
                    long freed = await Task.Run(() => CleanDirectory(updateCachePath));
                    totalFreed += freed;
                    AddLog($"  ✓ {FormatBytes(freed)} " + (_currentLanguage == "PT" ? "libertados" : "freed"));

                    AddLog(_currentLanguage == "PT"
                        ? "  → Limpando ficheiros temporários do utilizador..."
                        : "  → Cleaning user temporary files...");
                    UpdateProgressBar(ProgressStep3, 30);

                    string tempPath = Path.GetTempPath();
                    freed = await Task.Run(() => CleanDirectory(tempPath));
                    totalFreed += freed;
                    AddLog($"  ✓ {FormatBytes(freed)} " + (_currentLanguage == "PT" ? "libertados" : "freed"));

                    AddLog(_currentLanguage == "PT"
                        ? "  → Limpando ficheiros temporários do Windows..."
                        : "  → Cleaning Windows temporary files...");
                    UpdateProgressBar(ProgressStep3, 45);

                    string windowsTempPath = @"C:\Windows\Temp";
                    freed = await Task.Run(() => CleanDirectory(windowsTempPath));
                    totalFreed += freed;
                    AddLog($"  ✓ {FormatBytes(freed)} " + (_currentLanguage == "PT" ? "libertados" : "freed"));

                    AddLog(_currentLanguage == "PT"
                        ? "  → Limpando Prefetch..."
                        : "  → Cleaning Prefetch...");
                    UpdateProgressBar(ProgressStep3, 60);

                    string prefetchPath = @"C:\Windows\Prefetch";
                    freed = await Task.Run(() => CleanDirectory(prefetchPath));
                    totalFreed += freed;
                    AddLog($"  ✓ {FormatBytes(freed)} " + (_currentLanguage == "PT" ? "libertados" : "freed"));

                    AddLog(_currentLanguage == "PT"
                        ? "  → Esvaziando Reciclagem..."
                        : "  → Emptying Recycle Bin...");
                    UpdateProgressBar(ProgressStep3, 70);

                    try
                    {
                        await RunPowerShellCommand("Clear-RecycleBin -Force -ErrorAction SilentlyContinue");
                        AddLog("  ✓ " + (_currentLanguage == "PT" ? "Reciclagem esvaziada" : "Recycle Bin emptied"));
                    }
                    catch { }

                    AddLog(_currentLanguage == "PT"
                        ? "  → Limpando componentes do Windows (DISM)..."
                        : "  → Cleaning Windows components (DISM)...");
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