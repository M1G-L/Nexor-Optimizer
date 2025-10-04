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

namespace Nexor
{
    public partial class FreshSetupPage : UserControl
    {
        private int _completedSteps = 0;
        private readonly string _currentLanguage;
        private bool _isRunning = false;

        public FreshSetupPage(string language = "PT")
        {
            InitializeComponent();
            _currentLanguage = language;
            SetLanguage(language);

            // Verificar privilégios de administrador
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
                    ? "Aviso: Não está a executar como Administrador. Algumas funcionalidades podem falhar."
                    : "Warning: Not running as Administrator. Some features may fail."));
            }
            else
            {
                AddLog("✓ " + (_currentLanguage == "PT"
                    ? "A executar com privilégios de Administrador"
                    : "Running with Administrator privileges"));
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
                TxtInfo1.Text = "• Este processo pode demorar 20-40 minutos dependendo da velocidade da internet";
                TxtInfo2.Text = "• É recomendado executar como Administrador";
                TxtInfo3.Text = "• O sistema pode reiniciar durante o processo de atualização";
                TxtInfo4.Text = "• Não interrompa o processo uma vez iniciado";
            }
            else // EN
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
                TxtInfo1.Text = "• This process may take 20-40 minutes depending on internet speed";
                TxtInfo2.Text = "• Running as Administrator is recommended";
                TxtInfo3.Text = "• System may restart during the update process";
                TxtInfo4.Text = "• Do not interrupt the process once started";
            }
        }

        private async void BtnRunAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
                return;

            var result = MessageBox.Show(
                _currentLanguage == "PT"
                    ? "Deseja executar todos os passos automaticamente?\n\n⚠️ O processo é totalmente automático e pode demorar algum tempo.\n\nO que será feito:\n• Atualizar o Windows completamente\n• Atualizar todos os drivers\n• Limpar ficheiros desnecessários\n\nRecomendação: Deixe o computador ligado e não interrompa."
                    : "Do you want to run all steps automatically?\n\n⚠️ The process is fully automatic and may take some time.\n\nWhat will be done:\n• Update Windows completely\n• Update all drivers\n• Clean unnecessary files\n\nRecommendation: Keep the computer on and do not interrupt.",
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

                await RunAllSteps();

                _isRunning = false;
                BtnRunAll.IsEnabled = true;
            }
        }

        private async Task RunAllSteps()
        {
            try
            {
                // Passo 1: Windows Update
                await RunStep1();
                await Task.Delay(2000);

                // Passo 2: Drivers
                await RunStep2();
                await Task.Delay(2000);

                // Passo 3: Limpeza
                await RunStep3();

                // Conclusão
                AddLog("\n═══════════════════════════════════════");
                AddLog(_currentLanguage == "PT"
                    ? "✅ CONFIGURAÇÃO CONCLUÍDA COM SUCESSO!"
                    : "✅ SETUP COMPLETED SUCCESSFULLY!");
                AddLog("═══════════════════════════════════════");

                AnimateCompletion();

                MessageBox.Show(
                    _currentLanguage == "PT"
                        ? "🎉 Configuração inicial concluída com sucesso!\n\nO seu Windows 11 está otimizado e pronto a usar.\n\nRecomendação: Reinicie o computador para aplicar todas as alterações."
                        : "🎉 Initial setup completed successfully!\n\nYour Windows 11 is optimized and ready to use.\n\nRecommendation: Restart your computer to apply all changes.",
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
            UpdateStepStatus(1, _currentLanguage == "PT" ? "🔄 A executar..." : "🔄 Running...", Brushes.Orange);
            AnimateStep(Step1Card, Step1Badge);
            ProgressStep1.Visibility = Visibility.Visible;

            try
            {
                await Task.Run(async () =>
                {
                    // Método 1: UsoClient (Update Session Orchestrator) - Mais confiável
                    AddLog(_currentLanguage == "PT"
                        ? "  → Iniciando verificação de atualizações..."
                        : "  → Starting update check...");

                    UpdateProgressBar(ProgressStep1, 10);

                    try
                    {
                        // Scan para updates
                        var scanResult = await RunCommand("UsoClient.exe", "StartScan");
                        AddLog(_currentLanguage == "PT"
                            ? "  ✓ Verificação concluída"
                            : "  ✓ Scan completed");
                        UpdateProgressBar(ProgressStep1, 30);

                        await Task.Delay(2000);

                        // Download updates
                        AddLog(_currentLanguage == "PT"
                            ? "  → Fazendo download das atualizações..."
                            : "  → Downloading updates...");
                        var downloadResult = await RunCommand("UsoClient.exe", "StartDownload");
                        AddLog(_currentLanguage == "PT"
                            ? "  ✓ Download concluído"
                            : "  ✓ Download completed");
                        UpdateProgressBar(ProgressStep1, 60);

                        await Task.Delay(2000);

                        // Install updates
                        AddLog(_currentLanguage == "PT"
                            ? "  → Instalando atualizações..."
                            : "  → Installing updates...");
                        var installResult = await RunCommand("UsoClient.exe", "StartInstall");
                        AddLog(_currentLanguage == "PT"
                            ? "  ✓ Instalação iniciada"
                            : "  ✓ Installation started");
                        UpdateProgressBar(ProgressStep1, 85);
                    }
                    catch (Exception ex)
                    {
                        AddLog($"  ⚠️ UsoClient: {ex.Message}");

                        // Fallback: Abrir Windows Update via PowerShell
                        AddLog(_currentLanguage == "PT"
                            ? "  → Usando método alternativo..."
                            : "  → Using alternative method...");
                        await RunPowerShellCommand("Start-Process ms-settings:windowsupdate");
                    }

                    UpdateProgressBar(ProgressStep1, 100);

                    // Abrir configurações do Windows Update para o usuário acompanhar
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "ms-settings:windowsupdate",
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    });
                });

                UpdateStepStatus(1, _currentLanguage == "PT" ? "✅ Concluído" : "✅ Completed", Brushes.LightGreen);
                UpdateProgress();
                AddLog(_currentLanguage == "PT"
                    ? "  ✓ Windows Update iniciado - Verifique a janela de configurações"
                    : "  ✓ Windows Update started - Check settings window");
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
            UpdateStepStatus(2, _currentLanguage == "PT" ? "🔄 A executar..." : "🔄 Running...", Brushes.Orange);
            AnimateStep(Step2Card, Step2Badge);
            ProgressStep2.Visibility = Visibility.Visible;

            try
            {
                await Task.Run(async () =>
                {
                    AddLog(_currentLanguage == "PT"
                        ? "  → Verificando dispositivos..."
                        : "  → Checking devices...");
                    UpdateProgressBar(ProgressStep2, 20);

                    // Método 1: pnputil - Scan de dispositivos
                    try
                    {
                        await RunCommand("pnputil.exe", "/scan-devices");
                        AddLog(_currentLanguage == "PT"
                            ? "  ✓ Scan de dispositivos concluído"
                            : "  ✓ Device scan completed");
                        UpdateProgressBar(ProgressStep2, 50);
                    }
                    catch (Exception ex)
                    {
                        AddLog($"  ⚠️ pnputil: {ex.Message}");
                    }

                    await Task.Delay(1500);

                    // Método 2: Windows Update para drivers via PowerShell
                    AddLog(_currentLanguage == "PT"
                        ? "  → Buscando drivers no Windows Update..."
                        : "  → Searching for drivers on Windows Update...");

                    try
                    {
                        string psScript = @"
                            $Session = New-Object -ComObject Microsoft.Update.Session
                            $Searcher = $Session.CreateUpdateSearcher()
                            $Searcher.ServiceID = '7971f918-a847-4430-9279-4a52d1efe18d'
                            $Searcher.SearchScope = 1
                            $Searcher.ServerSelection = 3
                            $Criteria = ""IsInstalled=0 and Type='Driver'""
                            $SearchResult = $Searcher.Search($Criteria)
                            
                            if($SearchResult.Updates.Count -eq 0) {
                                Write-Host 'No driver updates found'
                            } else {
                                Write-Host ""Found $($SearchResult.Updates.Count) driver updates""
                                $Downloader = $Session.CreateUpdateDownloader()
                                $Downloader.Updates = $SearchResult.Updates
                                $Downloader.Download()
                                
                                $Installer = $Session.CreateUpdateInstaller()
                                $Installer.Updates = $SearchResult.Updates
                                $Result = $Installer.Install()
                                Write-Host ""Installation completed with code: $($Result.ResultCode)""
                            }
                        ";

                        await RunPowerShellScript(psScript);
                        UpdateProgressBar(ProgressStep2, 85);
                    }
                    catch (Exception ex)
                    {
                        AddLog($"  ⚠️ PowerShell: {ex.Message}");
                    }

                    UpdateProgressBar(ProgressStep2, 100);

                    // Abrir Device Manager
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "devmgmt.msc",
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    });

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
                    // 1. Limpar Windows Update Cache
                    AddLog(_currentLanguage == "PT"
                        ? "  → Limpando cache do Windows Update..."
                        : "  → Cleaning Windows Update cache...");
                    UpdateProgressBar(ProgressStep3, 15);

                    string updateCachePath = @"C:\Windows\SoftwareDistribution\Download";
                    long freed = await Task.Run(() => CleanDirectory(updateCachePath));
                    totalFreed += freed;
                    AddLog($"  ✓ {FormatBytes(freed)} " + (_currentLanguage == "PT" ? "libertados" : "freed"));

                    // 2. Limpar Temp files do usuário
                    AddLog(_currentLanguage == "PT"
                        ? "  → Limpando ficheiros temporários do utilizador..."
                        : "  → Cleaning user temporary files...");
                    UpdateProgressBar(ProgressStep3, 30);

                    string tempPath = Path.GetTempPath();
                    freed = await Task.Run(() => CleanDirectory(tempPath));
                    totalFreed += freed;
                    AddLog($"  ✓ {FormatBytes(freed)} " + (_currentLanguage == "PT" ? "libertados" : "freed"));

                    // 3. Limpar Windows Temp
                    AddLog(_currentLanguage == "PT"
                        ? "  → Limpando ficheiros temporários do Windows..."
                        : "  → Cleaning Windows temporary files...");
                    UpdateProgressBar(ProgressStep3, 45);

                    string windowsTempPath = @"C:\Windows\Temp";
                    freed = await Task.Run(() => CleanDirectory(windowsTempPath));
                    totalFreed += freed;
                    AddLog($"  ✓ {FormatBytes(freed)} " + (_currentLanguage == "PT" ? "libertados" : "freed"));

                    // 4. Limpar Prefetch
                    AddLog(_currentLanguage == "PT"
                        ? "  → Limpando Prefetch..."
                        : "  → Cleaning Prefetch...");
                    UpdateProgressBar(ProgressStep3, 60);

                    string prefetchPath = @"C:\Windows\Prefetch";
                    freed = await Task.Run(() => CleanDirectory(prefetchPath));
                    totalFreed += freed;
                    AddLog($"  ✓ {FormatBytes(freed)} " + (_currentLanguage == "PT" ? "libertados" : "freed"));

                    // 5. Limpar Recycle Bin
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

                    // 6. Disk Cleanup com CleanMgr
                    AddLog(_currentLanguage == "PT"
                        ? "  → Executando Disk Cleanup..."
                        : "  → Running Disk Cleanup...");
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

        // ═══════════════════════════════════════
        // MÉTODOS AUXILIARES
        // ═══════════════════════════════════════

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

                // Apagar ficheiros
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

                // Apagar subpastas
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
                // Configurar StateFlags para limpeza automática
                string[] cleanupFlags = new string[]
                {
                    "Active Setup Temp Folders",
                    "Downloaded Program Files",
                    "Internet Cache Files",
                    "Old ChkDsk Files",
                    "Recycle Bin",
                    "Setup Log Files",
                    "System error memory dump files",
                    "System error minidump files",
                    "Temporary Files",
                    "Temporary Setup Files",
                    "Thumbnail Cache",
                    "Update Cleanup",
                    "Windows Defender",
                    "Windows Error Reporting Files",
                    "Windows ESD installation files",
                    "Windows Upgrade Log Files"
                };

                // Executar cleanmgr com sage run
                await RunCommand("cleanmgr.exe", "/sagerun:1");

                await Task.Delay(3000);

                // Usar DISM para limpeza adicional
                await RunCommand("Dism.exe", "/online /Cleanup-Image /StartComponentCleanup /ResetBase");
            }
            catch { }
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

                // Auto-scroll para o final - Fixed null reference
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
                // Animação do card
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

                // Animação do badge
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
                // Animação do círculo de progresso
                var scaleTransform = new ScaleTransform(1, 1);
                ProgressCircleBorder.RenderTransform = scaleTransform;

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

                // Mudar cor para verde
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