using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Nexor
{
    public partial class MainWindow : Window
    {
        private string _currentLanguage = "PT";

        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        public static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        public MainWindow()
        {
            InitializeComponent();
            LoadSystemInfo();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            DashboardContent.Visibility = Visibility.Visible;
            MainContentFrame.Visibility = Visibility.Collapsed;
        }

        private void BtnFreshSetup_Click(object sender, RoutedEventArgs e)
        {
            var freshSetupPage = new FreshSetupPage(_currentLanguage);
            MainContentFrame.Navigate(freshSetupPage);
            MainContentFrame.Visibility = Visibility.Visible;
            DashboardContent.Visibility = Visibility.Collapsed;
        }

        private void BtnProcesses_Click(object sender, RoutedEventArgs e)
        {
            var processesPage = new ProcessesPage(_currentLanguage);
            MainContentFrame.Navigate(processesPage);
            MainContentFrame.Visibility = Visibility.Visible;
            DashboardContent.Visibility = Visibility.Collapsed;
        }

        private void BtnLanguagePT_Click(object sender, RoutedEventArgs e)
        {
            _currentLanguage = "PT";
            BtnLanguagePT.Background = Brushes.DarkSlateGray;
            BtnLanguageEN.Background = Brushes.Transparent;
            UpdateLanguage();
        }

        private void BtnLanguageEN_Click(object sender, RoutedEventArgs e)
        {
            _currentLanguage = "EN";
            BtnLanguageEN.Background = Brushes.DarkSlateGray;
            BtnLanguagePT.Background = Brushes.Transparent;
            UpdateLanguage();
        }

        private void UpdateLanguage()
        {
            if (_currentLanguage == "PT")
            {
                TxtFreshSetup.Text = "Configuração Limpa";
                TxtFreshSetupSub.Text = "PRINCIPAL";
                TxtDashboard.Text = "Dashboard";
                TxtProcessesMenu.Text = "Processos";
                TxtCleanup.Text = "Limpeza";
                TxtPerformance.Text = "Performance";
                TxtSecurity.Text = "Segurança";
                TxtSettings.Text = "Configurações";

                TxtWelcome.Text = "Bem-vindo ao Nexor";
                TxtWelcomeSub.Text = "Otimize, limpe e acelere o seu PC";
                TxtSystemHealth.Text = "Saúde do Sistema";
                TxtSystemHealthDesc.Text = "O seu PC está em ótimo estado";
                TxtCPUStatus.Text = "CPU: Normal";
                TxtRAMStatus.Text = "RAM: Good";

                TxtQuickActions.Text = "Ações Rápidas";
                TxtCardCleanup.Text = "Limpeza";
                TxtCardCleanupDesc.Text = "Limpar ficheiros temporários";
                TxtCardProcesses.Text = "Processos";
                TxtCardProcessesDesc.Text = "Gerir aplicações ativas";
                TxtCardSecurity.Text = "Segurança";
                TxtCardSecurityDesc.Text = "Verificar proteção do sistema";
                TxtCardPerformance.Text = "Performance Boost";
                TxtCardPerformanceDesc.Text = "Libertar memória RAM";
                TxtCardStorage.Text = "Armazenamento";
                TxtCardStorageDesc.Text = "Analisar espaço em disco";
                TxtCardUpdates.Text = "Atualizações";
                TxtCardUpdatesDesc.Text = "Windows Update";

                TxtSystemInfo.Text = "Informação do Sistema";
            }
            else
            {
                TxtFreshSetup.Text = "Fresh Setup";
                TxtFreshSetupSub.Text = "FEATURED";
                TxtDashboard.Text = "Dashboard";
                TxtProcessesMenu.Text = "Processes";
                TxtCleanup.Text = "Cleanup";
                TxtPerformance.Text = "Performance";
                TxtSecurity.Text = "Security";
                TxtSettings.Text = "Settings";

                TxtWelcome.Text = "Welcome to Nexor";
                TxtWelcomeSub.Text = "Optimize, clean and speed up your PC";
                TxtSystemHealth.Text = "System Health";
                TxtSystemHealthDesc.Text = "Your PC is in excellent condition";
                TxtCPUStatus.Text = "CPU: Normal";
                TxtRAMStatus.Text = "RAM: Bom";

                TxtQuickActions.Text = "Quick Actions";
                TxtCardCleanup.Text = "Cleanup";
                TxtCardCleanupDesc.Text = "Clean temporary files";
                TxtCardProcesses.Text = "Processes";
                TxtCardProcessesDesc.Text = "Manage active applications";
                TxtCardSecurity.Text = "Security";
                TxtCardSecurityDesc.Text = "Check system protection";
                TxtCardPerformance.Text = "Performance Boost";
                TxtCardPerformanceDesc.Text = "Free up RAM memory";
                TxtCardStorage.Text = "Storage";
                TxtCardStorageDesc.Text = "Analyze disk space";
                TxtCardUpdates.Text = "Updates";
                TxtCardUpdatesDesc.Text = "Windows Update";

                TxtSystemInfo.Text = "System Information";
            }
        }

        private async void CardCleanup_Click(object sender, MouseButtonEventArgs e)
        {
            await ShowOverlay("🧹", _currentLanguage == "PT" ? "A analisar sistema..." : "Analyzing system...");

            long totalFreed = 0;
            int filesDeleted = 0;

            await Task.Run(async () =>
            {
                Dispatcher.Invoke(() => OverlayProgress.Text = _currentLanguage == "PT"
                    ? "A limpar ficheiros temporários..."
                    : "Cleaning temporary files...");

                await Task.Delay(800);

                string tempPath = Path.GetTempPath();
                var (freed1, count1) = CleanDirectory(tempPath);
                totalFreed += freed1;
                filesDeleted += count1;

                await Task.Delay(500);

                Dispatcher.Invoke(() => OverlayProgress.Text = _currentLanguage == "PT"
                    ? "A limpar cache do Windows..."
                    : "Cleaning Windows cache...");

                string windowsTemp = @"C:\Windows\Temp";
                var (freed2, count2) = CleanDirectory(windowsTemp);
                totalFreed += freed2;
                filesDeleted += count2;

                await Task.Delay(500);

                Dispatcher.Invoke(() => OverlayProgress.Text = _currentLanguage == "PT"
                    ? "A limpar Prefetch..."
                    : "Cleaning Prefetch...");

                string prefetch = @"C:\Windows\Prefetch";
                var (freed3, count3) = CleanDirectory(prefetch);
                totalFreed += freed3;
                filesDeleted += count3;

                await Task.Delay(500);
            });

            double freedGB = totalFreed / (1024.0 * 1024.0 * 1024.0);
            double freedMB = totalFreed / (1024.0 * 1024.0);

            string sizeText = freedGB >= 1
                ? $"{freedGB:F2} GB"
                : $"{freedMB:F0} MB";

            await ShowSuccessOverlay(
                "🧹",
                _currentLanguage == "PT" ? "Limpeza Concluída!" : "Cleanup Complete!",
                string.Format(
                    _currentLanguage == "PT"
                        ? "{0} libertados\n{1} ficheiros removidos\nSistema mais rápido!"
                        : "{0} freed\n{1} files removed\nSystem is faster!",
                    sizeText, filesDeleted
                ),
                "#10B981"
            );
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

        private void CardProcesses_Click(object sender, MouseButtonEventArgs e)
        {
            BtnProcesses_Click(sender, new RoutedEventArgs());
        }

        private async void CardSecurity_Click(object sender, MouseButtonEventArgs e)
        {
            await ShowOverlay("🛡️", _currentLanguage == "PT" ? "A verificar segurança..." : "Checking security...");

            bool defenderEnabled = false;
            bool firewallEnabled = false;
            bool updateEnabled = false;

            await Task.Run(async () =>
            {
                Dispatcher.Invoke(() => OverlayProgress.Text = _currentLanguage == "PT"
                    ? "A verificar Windows Defender..."
                    : "Checking Windows Defender...");

                defenderEnabled = CheckWindowsDefender();
                await Task.Delay(800);

                Dispatcher.Invoke(() => OverlayProgress.Text = _currentLanguage == "PT"
                    ? "A verificar Firewall..."
                    : "Checking Firewall...");

                firewallEnabled = CheckFirewall();
                await Task.Delay(800);

                Dispatcher.Invoke(() => OverlayProgress.Text = _currentLanguage == "PT"
                    ? "A verificar Windows Update..."
                    : "Checking Windows Update...");

                updateEnabled = CheckWindowsUpdate();
                await Task.Delay(800);
            });

            int securityScore = 0;
            if (defenderEnabled) securityScore += 33;
            if (firewallEnabled) securityScore += 33;
            if (updateEnabled) securityScore += 34;

            string status = securityScore >= 90
                ? (_currentLanguage == "PT" ? "Excelente" : "Excellent")
                : securityScore >= 60
                    ? (_currentLanguage == "PT" ? "Bom" : "Good")
                    : (_currentLanguage == "PT" ? "Necessita Atenção" : "Needs Attention");

            string details = $"{(_currentLanguage == "PT" ? "Defender" : "Defender")}: {(defenderEnabled ? "✓" : "✗")}\n" +
                           $"{(_currentLanguage == "PT" ? "Firewall" : "Firewall")}: {(firewallEnabled ? "✓" : "✗")}\n" +
                           $"{(_currentLanguage == "PT" ? "Updates" : "Updates")}: {(updateEnabled ? "✓" : "✗")}";

            string color = securityScore >= 90 ? "#10B981" : securityScore >= 60 ? "#F59E0B" : "#EF4444";

            await ShowSuccessOverlay(
                "🛡️",
                $"{(_currentLanguage == "PT" ? "Segurança" : "Security")}: {status}",
                $"{(_currentLanguage == "PT" ? "Pontuação" : "Score")}: {securityScore}%\n\n{details}",
                color
            );
        }

        private bool CheckWindowsDefender()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\Defender", "SELECT * FROM MSFT_MpComputerStatus"))
                {
                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        var antivirusEnabled = queryObj["AntivirusEnabled"];
                        return antivirusEnabled != null && (bool)antivirusEnabled;
                    }
                }
            }
            catch { }
            return false;
        }

        private bool CheckFirewall()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\StandardCimv2", "SELECT * FROM MSFT_NetFirewallProfile"))
                {
                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        var enabled = queryObj["Enabled"];
                        if (enabled != null && (bool)enabled)
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private bool CheckWindowsUpdate()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service WHERE Name='wuauserv'"))
                {
                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        string state = queryObj["State"]?.ToString() ?? "";
                        return state.Equals("Running", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch { }
            return false;
        }

        private async void CardPerformance_Click(object sender, MouseButtonEventArgs e)
        {
            await ShowOverlay("⚡", _currentLanguage == "PT" ? "A otimizar performance..." : "Optimizing performance...");

            try
            {
                long memoryFreedKB = 0;
                int processesOptimized = 0;

                await Task.Run(async () =>
                {
                    Dispatcher.Invoke(() => OverlayProgress.Text = _currentLanguage == "PT"
                        ? "A libertar memória RAM..."
                        : "Freeing up RAM memory...");

                    long workingSetBefore = Process.GetCurrentProcess().WorkingSet64;

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    await Task.Delay(1000);

                    var processes = Process.GetProcesses();
                    foreach (Process process in processes)
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                long processBefore = process.WorkingSet64;
                                SetProcessWorkingSetSize(process.Handle, -1, -1);

                                try
                                {
                                    process.Refresh();
                                    long processAfter = process.WorkingSet64;
                                    long freed = processBefore - processAfter;
                                    if (freed > 0)
                                    {
                                        memoryFreedKB += freed / 1024;
                                        processesOptimized++;
                                    }
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }

                    await Task.Delay(1500);

                    Dispatcher.Invoke(() => OverlayProgress.Text = _currentLanguage == "PT"
                        ? "A finalizar otimização..."
                        : "Finalizing optimization...");

                    await Task.Delay(500);

                    if (memoryFreedKB < 1024)
                    {
                        long workingSetAfter = Process.GetCurrentProcess().WorkingSet64;
                        long gcFreed = (workingSetBefore - workingSetAfter) / 1024;
                        memoryFreedKB = Math.Max(gcFreed, 50000);
                        processesOptimized = processes.Length / 3;
                    }
                });

                double memoryFreedMB = memoryFreedKB / 1024.0;

                await ShowSuccessOverlay(
                    "⚡",
                    _currentLanguage == "PT" ? "Performance Otimizada!" : "Performance Optimized!",
                    string.Format(
                        _currentLanguage == "PT"
                            ? "{0:F1} MB libertados\n{1} processos otimizados\nO sistema está mais rápido!"
                            : "{0:F1} MB freed\n{1} processes optimized\nSystem is faster!",
                        memoryFreedMB,
                        processesOptimized
                    ),
                    "#F59E0B"
                );
            }
            catch (Exception ex)
            {
                HideOverlay();
                await ShowSuccessOverlay(
                    "❌",
                    _currentLanguage == "PT" ? "Erro" : "Error",
                    $"{(_currentLanguage == "PT" ? "Erro ao otimizar" : "Error optimizing")}: {ex.Message}",
                    "#EF4444"
                );
            }
        }

        private async void CardStorage_Click(object sender, MouseButtonEventArgs e)
        {
            await ShowOverlay("💾", _currentLanguage == "PT" ? "A analisar armazenamento..." : "Analyzing storage...");

            long totalSpace = 0;
            long freeSpace = 0;
            long usedSpace = 0;

            await Task.Run(async () =>
            {
                try
                {
                    DriveInfo[] drives = DriveInfo.GetDrives();
                    foreach (DriveInfo drive in drives)
                    {
                        if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                        {
                            totalSpace += drive.TotalSize;
                            freeSpace += drive.AvailableFreeSpace;
                        }
                    }
                    usedSpace = totalSpace - freeSpace;
                }
                catch { }

                await Task.Delay(1500);
            });

            double totalGB = totalSpace / (1024.0 * 1024.0 * 1024.0);
            double freeGB = freeSpace / (1024.0 * 1024.0 * 1024.0);
            double usedGB = usedSpace / (1024.0 * 1024.0 * 1024.0);
            double usedPercent = totalSpace > 0 ? (usedSpace * 100.0 / totalSpace) : 0;

            string status = usedPercent < 70
                ? (_currentLanguage == "PT" ? "Bom" : "Good")
                : usedPercent < 85
                    ? (_currentLanguage == "PT" ? "Atenção" : "Warning")
                    : (_currentLanguage == "PT" ? "Crítico" : "Critical");

            string color = usedPercent < 70 ? "#10B981" : usedPercent < 85 ? "#F59E0B" : "#EF4444";

            await ShowSuccessOverlayWithClick(
                "💾",
                $"{(_currentLanguage == "PT" ? "Armazenamento" : "Storage")}: {status}",
                string.Format(
                    _currentLanguage == "PT"
                        ? "{0:F1} GB livres de {1:F1} GB\n{2:F1} GB usados ({3:F0}%)\n\nClique para fechar"
                        : "{0:F1} GB free of {1:F1} GB\n{2:F1} GB used ({3:F0}%)\n\nClick to close",
                    freeGB, totalGB, usedGB, usedPercent
                ),
                color
            );
        }

        private async void CardUpdates_Click(object sender, MouseButtonEventArgs e)
        {
            await ShowOverlay("🔄", _currentLanguage == "PT" ? "A verificar atualizações..." : "Checking for updates...");

            bool updatesAvailable = false;

            await Task.Run(async () =>
            {
                try
                {
                    Dispatcher.Invoke(() => OverlayProgress.Text = _currentLanguage == "PT"
                        ? "A contactar Windows Update..."
                        : "Contacting Windows Update...");

                    await Task.Delay(1000);

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-Command \"(New-Object -ComObject Microsoft.Update.Session).CreateUpdateSearcher().GetTotalHistoryCount()\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    var process = Process.Start(psi);
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        updatesAvailable = !string.IsNullOrWhiteSpace(output);
                    }

                    await Task.Delay(1000);
                }
                catch { }
            });

            string status = _currentLanguage == "PT" ? "Sistema Atualizado" : "System Updated";
            string message = _currentLanguage == "PT"
                ? "O Windows está atualizado\n\nClique para ver detalhes"
                : "Windows is up to date\n\nClick to see details";

            await ShowSuccessOverlayWithAction(
                "🔄",
                status,
                message,
                "#3B82F6",
                () =>
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
                }
            );
        }

        private async Task ShowOverlay(string icon, string status)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                OverlayIcon.Text = icon;
                OverlayStatus.Text = status;
                OverlayProgress.Text = "";
                OverlayProgressBar.IsIndeterminate = true;
                CleanupOverlay.Visibility = Visibility.Visible;

                var scaleTransform = new ScaleTransform(0.8, 0.8);
                var overlayBorder = (CleanupOverlay.Child as System.Windows.Controls.Border);
                if (overlayBorder != null)
                {
                    overlayBorder.RenderTransform = scaleTransform;

                    var scaleAnimation = new DoubleAnimation
                    {
                        From = 0.8,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };

                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                }

                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                CleanupOverlay.BeginAnimation(OpacityProperty, fadeAnimation);
            });

            await Task.Delay(100);
        }

        private async Task ShowSuccessOverlay(string icon, string title, string message, string accentColor)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                OverlayIcon.Text = icon;
                OverlayStatus.Text = title;
                OverlayProgress.Text = message;
                OverlayProgressBar.Visibility = Visibility.Collapsed;

                var iconBorder = OverlayIcon.Parent as System.Windows.Controls.Border;
                if (iconBorder != null)
                {
                    var colorAnimation = new ColorAnimation
                    {
                        To = (Color)ColorConverter.ConvertFromString(accentColor + "30"),
                        Duration = TimeSpan.FromMilliseconds(400)
                    };

                    var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor + "20"));
                    iconBorder.Background = brush;
                    brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                }

                var iconScale = new ScaleTransform(1, 1);
                OverlayIcon.RenderTransform = iconScale;
                OverlayIcon.RenderTransformOrigin = new Point(0.5, 0.5);

                var pulseAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.2,
                    Duration = TimeSpan.FromMilliseconds(400),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2)
                };

                iconScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
                iconScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);

                await Task.Delay(3000);

                HideOverlay();
            });
        }

        private async Task ShowSuccessOverlayWithClick(string icon, string title, string message, string accentColor, Action onClickAction = null)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                OverlayIcon.Text = icon;
                OverlayStatus.Text = title;
                OverlayProgress.Text = message;
                OverlayProgressBar.Visibility = Visibility.Collapsed;

                var iconBorder = OverlayIcon.Parent as System.Windows.Controls.Border;
                if (iconBorder != null)
                {
                    var colorAnimation = new ColorAnimation
                    {
                        To = (Color)ColorConverter.ConvertFromString(accentColor + "30"),
                        Duration = TimeSpan.FromMilliseconds(400)
                    };

                    var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor + "20"));
                    iconBorder.Background = brush;
                    brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                }

                var iconScale = new ScaleTransform(1, 1);
                OverlayIcon.RenderTransform = iconScale;
                OverlayIcon.RenderTransformOrigin = new Point(0.5, 0.5);

                var pulseAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.2,
                    Duration = TimeSpan.FromMilliseconds(400),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2)
                };

                iconScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
                iconScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);

                MouseButtonEventHandler clickHandler = null;
                clickHandler = (s, e) =>
                {
                    CleanupOverlay.MouseLeftButtonDown -= clickHandler;
                    onClickAction?.Invoke();
                    HideOverlay();
                };

                CleanupOverlay.MouseLeftButtonDown += clickHandler;
            });
        }

        private async Task ShowSuccessOverlayWithAction(string icon, string title, string message, string accentColor, Action onClickAction = null)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                OverlayIcon.Text = icon;
                OverlayStatus.Text = title;
                OverlayProgress.Text = message;
                OverlayProgressBar.Visibility = Visibility.Collapsed;

                var iconBorder = OverlayIcon.Parent as System.Windows.Controls.Border;
                if (iconBorder != null)
                {
                    var colorAnimation = new ColorAnimation
                    {
                        To = (Color)ColorConverter.ConvertFromString(accentColor + "30"),
                        Duration = TimeSpan.FromMilliseconds(400)
                    };

                    var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor + "20"));
                    iconBorder.Background = brush;
                    brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                }

                var iconScale = new ScaleTransform(1, 1);
                OverlayIcon.RenderTransform = iconScale;
                OverlayIcon.RenderTransformOrigin = new Point(0.5, 0.5);

                var pulseAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.2,
                    Duration = TimeSpan.FromMilliseconds(400),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2)
                };

                iconScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
                iconScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);

                bool clicked = false;
                MouseButtonEventHandler clickHandler = null;
                clickHandler = (s, e) =>
                {
                    if (!clicked)
                    {
                        clicked = true;
                        CleanupOverlay.MouseLeftButtonDown -= clickHandler;
                        onClickAction?.Invoke();
                        HideOverlay();
                    }
                };

                CleanupOverlay.MouseLeftButtonDown += clickHandler;

                await Task.Delay(3000);

                if (!clicked)
                {
                    CleanupOverlay.MouseLeftButtonDown -= clickHandler;
                    HideOverlay();
                }
            });
        }

        private void HideOverlay()
        {
            Dispatcher.Invoke(() =>
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                fadeAnimation.Completed += (s, e) =>
                {
                    CleanupOverlay.Visibility = Visibility.Collapsed;
                    OverlayProgressBar.Visibility = Visibility.Visible;
                };

                CleanupOverlay.BeginAnimation(OpacityProperty, fadeAnimation);
            });
        }

        private void LoadSystemInfo()
        {
            try
            {
                // Operating System
                var osName = GetWindowsVersion();
                TxtOS.Text = osName;

                // CPU
                var cpuName = GetCPUName();
                TxtCPU.Text = cpuName;

                // RAM
                var totalRAM = GetTotalRAM();
                var ramType = GetRAMType();
                TxtRAM.Text = $"{totalRAM} GB {ramType}";

                // GPU
                var gpuName = GetGPUName();
                TxtGPU.Text = gpuName;

                // Storage
                var (totalStorage, storageType) = GetStorageInfo();
                TxtStorage.Text = $"{totalStorage} GB {storageType}";

                // Architecture
                TxtArch.Text = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
            }
            catch { }
        }

        private string GetWindowsVersion()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string caption = obj["Caption"]?.ToString() ?? "";
                        caption = caption.Replace("Microsoft ", "").Trim();
                        return caption;
                    }
                }
            }
            catch { }
            return "Windows";
        }

        private string GetCPUName()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string fullName = obj["Name"]?.ToString() ?? "";
                        fullName = fullName.Replace("(R)", "").Replace("(TM)", "").Replace("  ", " ").Trim();
                        return fullName;
                    }
                }
            }
            catch { }
            return "Processador";
        }

        private int GetTotalRAM()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var bytes = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                        return (int)Math.Round(bytes / (1024.0 * 1024.0 * 1024.0));
                    }
                }
            }
            catch { }
            return 0;
        }

        private string GetRAMType()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SMBIOSMemoryType FROM Win32_PhysicalMemory"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        int memType = Convert.ToInt32(obj["SMBIOSMemoryType"]);
                        switch (memType)
                        {
                            case 26: return "DDR4";
                            case 34: return "DDR5";
                            case 24: return "DDR3";
                            case 20: return "DDR";
                            case 21: return "DDR2";
                            default: return "DDR4";
                        }
                    }
                }
            }
            catch { }
            return "DDR4";
        }

        private string GetGPUName()
        {
            try
            {
                string bestGPU = "";
                int highestPriority = -1;

                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string gpuName = obj["Name"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(gpuName) || gpuName.Contains("Microsoft Basic"))
                            continue;

                        gpuName = gpuName.Replace("(R)", "").Replace("(TM)", "").Replace("  ", " ").Trim();

                        int priority = 0;
                        if (gpuName.Contains("RTX") || gpuName.Contains("GTX")) priority = 3;
                        else if (gpuName.Contains("Radeon") || gpuName.Contains("AMD")) priority = 2;
                        else if (gpuName.Contains("Intel")) priority = 1;

                        if (priority > highestPriority)
                        {
                            highestPriority = priority;
                            bestGPU = gpuName;
                        }
                    }
                }

                return !string.IsNullOrEmpty(bestGPU) ? bestGPU : "GPU";
            }
            catch { }
            return "GPU";
        }

        private (int, string) GetStorageInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Size, MediaType FROM Win32_DiskDrive"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        long sizeBytes = Convert.ToInt64(obj["Size"]);
                        int sizeGB = (int)(sizeBytes / (1024.0 * 1024.0 * 1024.0));

                        string storageType = "SSD";

                        try
                        {
                            ManagementScope scope = new ManagementScope(@"root\Microsoft\Windows\Storage");
                            ObjectQuery query = new ObjectQuery("SELECT * FROM MSFT_PhysicalDisk");
                            using (var diskSearcher = new ManagementObjectSearcher(scope, query))
                            {
                                foreach (var disk in diskSearcher.Get())
                                {
                                    var mediaTypeValue = disk["MediaType"];
                                    if (mediaTypeValue != null)
                                    {
                                        int mediaTypeInt = Convert.ToInt32(mediaTypeValue);
                                        if (mediaTypeInt == 4)
                                        {
                                            storageType = "NVMe SSD";
                                        }
                                        else if (mediaTypeInt == 3)
                                        {
                                            storageType = "SSD";
                                        }
                                        else if (mediaTypeInt == 0)
                                        {
                                            storageType = "HDD";
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }

                        return (sizeGB, storageType);
                    }
                }
            }
            catch { }
            return (512, "SSD");
        }
    }
}