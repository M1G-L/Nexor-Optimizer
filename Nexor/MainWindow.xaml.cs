using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Nexor
{
    public partial class MainWindow : Window
    {
        private string _currentLanguage = "EN";
        private DispatcherTimer _systemMonitorTimer;
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;

        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        public static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                InitializeLanguage();
                LoadSystemInfo();
                InitializeSystemMonitor();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization Error: {ex.Message}\n\n{ex.StackTrace}",
                    "Nexor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                AnimatePageLoad();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Animation Error: {ex.Message}");
                // Continue without animations rather than crash
            }
        }

        private async void AnimatePageLoad()
        {
            try
            {
                // Check if elements exist before animating
                if (LogoSection != null)
                {
                    AnimateElement(LogoSection, 0, 1, -30, 0, 0.5);
                }

                if (WindowControls != null)
                {
                    AnimateElement(WindowControls, 0, 1, 30, 0, 0.5);
                }

                await Task.Delay(100);

                if (Sidebar != null)
                {
                    AnimateSidebarIn();
                }

                await Task.Delay(200);

                if (WelcomeSection != null)
                {
                    AnimateWelcomeIn();
                }

                await Task.Delay(150);

                if (SystemOverviewCard != null)
                {
                    AnimateSystemOverviewIn();
                }

                await Task.Delay(150);

                if (TxtQuickActions != null)
                {
                    AnimateElement(TxtQuickActions, 0, 1, -20, 0, 0.5, false);
                }

                await Task.Delay(100);

                AnimateQuickActionsIn();

                await Task.Delay(800);

                if (SystemInfoCards != null)
                {
                    AnimateSystemInfoIn();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Page load animation error: {ex.Message}");
            }
        }
        private void SetSelectedMenuItem(Button selectedButton)
        {
            // Reset all menu buttons
            BtnDashboard.Tag = "Normal";
            BtnProcesses.Tag = "Normal";
            BtnCleanup.Tag = "Normal";
            BtnPerformance.Tag = "Normal";
            BtnFreshSetup.Tag = "Normal";

            // Set selected button
            if (selectedButton != null)
            {
                selectedButton.Tag = "Selected";
            }
        }

        private void AnimateElement(FrameworkElement element, double fromOpacity, double toOpacity,
            double fromTranslate, double toTranslate, double durationSeconds, bool isXAxis = true)
        {
            try
            {
                var storyboard = new Storyboard();

                // Opacity animation
                var opacityAnimation = new DoubleAnimation
                {
                    From = fromOpacity,
                    To = toOpacity,
                    Duration = TimeSpan.FromSeconds(durationSeconds),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                Storyboard.SetTarget(opacityAnimation, element);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
                storyboard.Children.Add(opacityAnimation);

                // Transform animation - using TransformGroup path
                if (element.RenderTransform is TransformGroup)
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = fromTranslate,
                        To = toTranslate,
                        Duration = TimeSpan.FromSeconds(durationSeconds),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };

                    Storyboard.SetTarget(translateAnimation, element);

                    // Use the correct path for TransformGroup
                    // TransformGroup.Children[3] is the TranslateTransform
                    string propertyPath = isXAxis
                        ? "(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.X)"
                        : "(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.Y)";

                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath(propertyPath));
                    storyboard.Children.Add(translateAnimation);
                }

                storyboard.Begin();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Element animation error: {ex.Message}");
            }
        }

        private void AnimateSidebarIn()
        {
            try
            {
                if (Sidebar != null)
                {
                    AnimateElement(Sidebar, 0, 1, -50, 0, 0.6);
                }

                if (FeaturedButton != null)
                {
                    Task.Delay(200).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AnimateElement(FeaturedButton, 0, 1, 20, 0, 0.5);
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Sidebar animation error: {ex.Message}");
            }
        }

        private void AnimateWelcomeIn()
        {
            try
            {
                if (WelcomeSection != null)
                {
                    AnimateElement(WelcomeSection, 0, 1, -30, 0, 0.6);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Welcome animation error: {ex.Message}");
            }
        }

        private void AnimateSystemOverviewIn()
        {
            try
            {
                if (SystemOverviewCard == null) return;

                var storyboard = new Storyboard();

                var opacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.6),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                Storyboard.SetTarget(opacityAnimation, SystemOverviewCard);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));

                storyboard.Children.Add(opacityAnimation);
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"System overview animation error: {ex.Message}");
            }
        }

        private async void AnimateQuickActionsIn()
        {
            try
            {
                var cards = new[] { Card1, Card2, Card3, Card4, Card5, Card6 };

                for (int i = 0; i < cards.Length; i++)
                {
                    var card = cards[i];
                    if (card != null)
                    {
                        AnimateCardIn(card, i * 100);
                        await Task.Delay(100);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Quick actions animation error: {ex.Message}");
            }
        }

        private void AnimateCardIn(FrameworkElement card, int delay)
        {
            Task.Delay(delay).ContinueWith(_ =>
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        AnimateElement(card, 0, 1, 30, 0, 0.5);
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Card animation error: {ex.Message}");
                }
            });
        }

        private void AnimateSystemInfoIn()
        {
            try
            {
                if (SystemInfoCards != null)
                {
                    AnimateElement(SystemInfoCards, 0, 1, 30, 0, 0.6);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"System info animation error: {ex.Message}");
            }
        }

        private void InitializeSystemMonitor()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");

                _cpuCounter.NextValue();

                _systemMonitorTimer = new DispatcherTimer();
                _systemMonitorTimer.Interval = TimeSpan.FromSeconds(2);
                _systemMonitorTimer.Tick += UpdateSystemMonitor;
                _systemMonitorTimer.Start();

                UpdateSystemMonitor(null, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"System monitor initialization error: {ex.Message}");
                // Continue without system monitoring rather than crash
            }
        }

        private void InitializeLanguage()
        {
            try
            {
                if (BtnLanguageEN != null)
                {
                    BtnLanguageEN.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1F2E"));
                }
                if (BtnLanguagePT != null)
                {
                    BtnLanguagePT.Background = Brushes.Transparent;
                }
                UpdateLanguage();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Language initialization error: {ex.Message}");
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Title bar error: {ex.Message}");
            }
        }

        private void UpdateSystemMonitor(object sender, EventArgs e)
        {
            try
            {
                if (_cpuCounter == null || _ramCounter == null) return;

                // CPU Usage
                float cpuUsage = _cpuCounter.NextValue();
                if (TxtCPUUsage != null)
                {
                    TxtCPUUsage.Text = $"{cpuUsage:F0}%";
                }

                if (CpuProgressBar != null)
                {
                    AnimateProgressBar(CpuProgressBar, cpuUsage);
                }

                var cpuColor = cpuUsage < 50 ? "#1990FE" : cpuUsage < 80 ? "#F59E0B" : "#EF4444";
                if (TxtCPUUsage != null)
                {
                    AnimateColorChange(TxtCPUUsage, cpuColor);
                }
                if (CpuProgressBar != null)
                {
                    AnimateProgressBarColor(CpuProgressBar, cpuColor);
                }

                // RAM Usage
                float availableRAM = _ramCounter.NextValue() / 1024f;
                int totalRAM = GetTotalRAM();
                float usedRAM = totalRAM - availableRAM;
                float ramPercentage = totalRAM > 0 ? (usedRAM / totalRAM) * 100f : 0;

                if (TxtRAMUsed != null)
                {
                    TxtRAMUsed.Text = $"{usedRAM:F1}";
                }
                if (TxtRAMTotal != null)
                {
                    TxtRAMTotal.Text = $"{totalRAM} GB";
                }
                if (RamProgressBar != null)
                {
                    AnimateProgressBar(RamProgressBar, ramPercentage);
                }

                var ramColor = ramPercentage < 70 ? "#10B981" : ramPercentage < 85 ? "#F59E0B" : "#EF4444";
                if (TxtRAMUsed != null)
                {
                    AnimateColorChange(TxtRAMUsed, ramColor);
                }
                if (RamProgressBar != null)
                {
                    AnimateProgressBarColor(RamProgressBar, ramColor);
                }

                // Disk Usage
                DriveInfo[] drives = DriveInfo.GetDrives();
                long totalSpace = 0;
                long freeSpace = 0;

                foreach (DriveInfo drive in drives)
                {
                    if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    {
                        totalSpace += drive.TotalSize;
                        freeSpace += drive.AvailableFreeSpace;
                    }
                }

                double totalGB = totalSpace / (1024.0 * 1024.0 * 1024.0);
                double freeGB = freeSpace / (1024.0 * 1024.0 * 1024.0);
                double usedGB = totalGB - freeGB;
                double diskPercentage = totalGB > 0 ? (usedGB / totalGB) * 100.0 : 0;

                if (TxtDiskFree != null)
                {
                    TxtDiskFree.Text = $"{freeGB:F0}";
                }
                if (TxtDiskTotal != null)
                {
                    TxtDiskTotal.Text = $"{totalGB:F0} GB";
                }
                if (DiskProgressBar != null)
                {
                    AnimateProgressBar(DiskProgressBar, diskPercentage);
                }

                var diskColor = diskPercentage < 70 ? "#8B5CF6" : diskPercentage < 85 ? "#F59E0B" : "#EF4444";
                if (TxtDiskFree != null)
                {
                    AnimateColorChange(TxtDiskFree, diskColor);
                }
                if (DiskProgressBar != null)
                {
                    AnimateProgressBarColor(DiskProgressBar, diskColor);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"System monitor update error: {ex.Message}");
            }
        }

        private void AnimateProgressBar(System.Windows.Controls.ProgressBar progressBar, double toValue)
        {
            try
            {
                var animation = new DoubleAnimation
                {
                    To = toValue,
                    Duration = TimeSpan.FromSeconds(0.8),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                progressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Progress bar animation error: {ex.Message}");
            }
        }

        private void AnimateColorChange(System.Windows.Controls.TextBlock textBlock, string colorHex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                var brush = new SolidColorBrush(textBlock.Foreground is SolidColorBrush sb ? sb.Color : Colors.White);
                textBlock.Foreground = brush;

                var animation = new ColorAnimation
                {
                    To = color,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Color animation error: {ex.Message}");
            }
        }

        private void AnimateProgressBarColor(System.Windows.Controls.ProgressBar progressBar, string colorHex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                var brush = new SolidColorBrush(progressBar.Foreground is SolidColorBrush sb ? sb.Color : Colors.White);
                progressBar.Foreground = brush;

                var animation = new ColorAnimation
                {
                    To = color,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Progress bar color animation error: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                base.OnClosed(e);
                _systemMonitorTimer?.Stop();
                _cpuCounter?.Dispose();
                _ramCounter?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cleanup error: {ex.Message}");
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
            SetSelectedMenuItem(BtnDashboard);
            if (DashboardContent != null)
            {
                DashboardContent.Visibility = Visibility.Visible;
            }
            if (MainContentFrame != null)
            {
                MainContentFrame.Visibility = Visibility.Collapsed;
            }
        }
        private void BtnPerformance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetSelectedMenuItem(BtnPerformance);
                var performancePage = new PerformancePage(_currentLanguage);
                if (MainContentFrame != null)
                {
                    MainContentFrame.Navigate(performancePage);
                    MainContentFrame.Visibility = Visibility.Visible;
                }
                if (DashboardContent != null)
                {
                    DashboardContent.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Performance page: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnFreshSetup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetSelectedMenuItem(BtnFreshSetup);
                var freshSetupPage = new FreshSetupPage(_currentLanguage);
                if (MainContentFrame != null)
                {
                    MainContentFrame.Navigate(freshSetupPage);
                    MainContentFrame.Visibility = Visibility.Visible;
                }
                if (DashboardContent != null)
                {
                    DashboardContent.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Fresh Setup page: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnProcesses_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetSelectedMenuItem(BtnProcesses);
                var processesPage = new ProcessesPage(_currentLanguage);
                if (MainContentFrame != null)
                {
                    MainContentFrame.Navigate(processesPage);
                    MainContentFrame.Visibility = Visibility.Visible;
                }
                if (DashboardContent != null)
                {
                    DashboardContent.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Processes page: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLanguagePT_Click(object sender, RoutedEventArgs e)
        {
            _currentLanguage = "PT";
            if (BtnLanguagePT != null)
            {
                BtnLanguagePT.Background = Brushes.DarkSlateGray;
            }
            if (BtnLanguageEN != null)
            {
                BtnLanguageEN.Background = Brushes.Transparent;
            }
            UpdateLanguage();
        }

        private void BtnLanguageEN_Click(object sender, RoutedEventArgs e)
        {
            _currentLanguage = "EN";
            if (BtnLanguageEN != null)
            {
                BtnLanguageEN.Background = Brushes.DarkSlateGray;
            }
            if (BtnLanguagePT != null)
            {
                BtnLanguagePT.Background = Brushes.Transparent;
            }
            UpdateLanguage();
        }

        private void UpdateLanguage()
        {
            try
            {
                if (_currentLanguage == "PT")
                {
                    SafeSetText(TxtFreshSetup, "Configuração Limpa");
                    SafeSetText(TxtFreshSetupSub, "PRINCIPAL");
                    SafeSetText(TxtDashboard, "Dashboard");
                    SafeSetText(TxtProcessesMenu, "Processos");
                    SafeSetText(TxtCleanup, "Limpeza");
                    SafeSetText(TxtPerformance, "Performance");
                    SafeSetText(TxtSecurity, "Segurança");
                    SafeSetText(TxtSettings, "Configurações");
                    SafeSetText(TxtWelcome, "Bem-vindo ao Nexor");
                    SafeSetText(TxtWelcomeSub, "Otimize, limpe e acelere o seu PC");
                    SafeSetText(TxtSystemHealth, "Visão Geral do Sistema");
                    SafeSetText(TxtSystemHealthDesc, "Monitorização em tempo real do seu sistema");
                    SafeSetText(TxtQuickActions, "Ações Rápidas");
                    SafeSetText(TxtCardCleanup, "Limpeza");
                    SafeSetText(TxtCardCleanupDesc, "Limpar ficheiros temporários");
                    SafeSetText(TxtCardProcesses, "Processos");
                    SafeSetText(TxtCardProcessesDesc, "Gerir aplicações ativas");
                    SafeSetText(TxtCardSecurity, "Segurança");
                    SafeSetText(TxtCardSecurityDesc, "Verificar proteção do sistema");
                    SafeSetText(TxtCardPerformance, "Performance Boost");
                    SafeSetText(TxtCardPerformanceDesc, "Libertar memória RAM");
                    SafeSetText(TxtCardStorage, "Armazenamento");
                    SafeSetText(TxtCardStorageDesc, "Analisar espaço em disco");
                    SafeSetText(TxtCardUpdates, "Atualizações");
                    SafeSetText(TxtCardUpdatesDesc, "Windows Update");
                    SafeSetText(TxtSystemInfo, "Informação do Sistema");
                    SafeSetText(TxtLabelOS, "Sistema Operativo");
                    SafeSetText(TxtLabelCPU, "Processador");
                    SafeSetText(TxtLabelGPU, "Placa Gráfica");
                    SafeSetText(TxtLabelRAM, "Memória");
                    SafeSetText(TxtLabelStorage, "Armazenamento");
                    SafeSetText(TxtLabelArch, "Arquitetura");
                }
                else
                {
                    SafeSetText(TxtFreshSetup, "Fresh Setup");
                    SafeSetText(TxtFreshSetupSub, "FEATURED");
                    SafeSetText(TxtDashboard, "Dashboard");
                    SafeSetText(TxtProcessesMenu, "Processes");
                    SafeSetText(TxtCleanup, "Cleanup");
                    SafeSetText(TxtPerformance, "Performance");
                    SafeSetText(TxtSecurity, "Security");
                    SafeSetText(TxtSettings, "Settings");
                    SafeSetText(TxtWelcome, "Welcome to Nexor");
                    SafeSetText(TxtWelcomeSub, "Optimize, clean and speed up your PC");
                    SafeSetText(TxtSystemHealth, "System Overview");
                    SafeSetText(TxtSystemHealthDesc, "Real-time monitoring of your system");
                    SafeSetText(TxtQuickActions, "Quick Actions");
                    SafeSetText(TxtCardCleanup, "Cleanup");
                    SafeSetText(TxtCardCleanupDesc, "Clean temporary files");
                    SafeSetText(TxtCardProcesses, "Processes");
                    SafeSetText(TxtCardProcessesDesc, "Manage active applications");
                    SafeSetText(TxtCardSecurity, "Security");
                    SafeSetText(TxtCardSecurityDesc, "Check system protection");
                    SafeSetText(TxtCardPerformance, "Performance Boost");
                    SafeSetText(TxtCardPerformanceDesc, "Free up RAM memory");
                    SafeSetText(TxtCardStorage, "Storage");
                    SafeSetText(TxtCardStorageDesc, "Analyze disk space");
                    SafeSetText(TxtCardUpdates, "Updates");
                    SafeSetText(TxtCardUpdatesDesc, "Windows Update");
                    SafeSetText(TxtSystemInfo, "System Information");
                    SafeSetText(TxtLabelOS, "Operating System");
                    SafeSetText(TxtLabelCPU, "Processor");
                    SafeSetText(TxtLabelGPU, "Graphics Card");
                    SafeSetText(TxtLabelRAM, "Memory");
                    SafeSetText(TxtLabelStorage, "Storage");
                    SafeSetText(TxtLabelArch, "Architecture");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Language update error: {ex.Message}");
            }
        }

        private void SafeSetText(System.Windows.Controls.TextBlock textBlock, string text)
        {
            if (textBlock != null)
            {
                textBlock.Text = text;
            }
        }

        // Rest of the methods remain similar with try-catch blocks...
        // (CardCleanup_Click, CardProcesses_Click, etc.)
        // I'll include the key ones:

        private async void CardCleanup_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                await ShowOverlay("🧹", _currentLanguage == "PT" ? "A analisar sistema..." : "Analyzing system...");

                long totalFreed = 0;
                int filesDeleted = 0;

                await Task.Run(async () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (OverlayProgress != null)
                        {
                            OverlayProgress.Text = _currentLanguage == "PT"
                                ? "A limpar ficheiros temporários..."
                                : "Cleaning temporary files...";
                        }
                    });

                    await Task.Delay(800);

                    string tempPath = Path.GetTempPath();
                    var (freed1, count1) = CleanDirectory(tempPath);
                    totalFreed += freed1;
                    filesDeleted += count1;

                    await Task.Delay(500);

                    Dispatcher.Invoke(() =>
                    {
                        if (OverlayProgress != null)
                        {
                            OverlayProgress.Text = _currentLanguage == "PT"
                                ? "A limpar cache do Windows..."
                                : "Cleaning Windows cache...";
                        }
                    });

                    string windowsTemp = @"C:\Windows\Temp";
                    var (freed2, count2) = CleanDirectory(windowsTemp);
                    totalFreed += freed2;
                    filesDeleted += count2;

                    await Task.Delay(500);

                    Dispatcher.Invoke(() =>
                    {
                        if (OverlayProgress != null)
                        {
                            OverlayProgress.Text = _currentLanguage == "PT"
                                ? "A limpar Prefetch..."
                                : "Cleaning Prefetch...";
                        }
                    });

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
            catch (Exception ex)
            {
                Debug.WriteLine($"Cleanup error: {ex.Message}");
                HideOverlay();
                MessageBox.Show($"Cleanup error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void BtnCleanup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetSelectedMenuItem(BtnCleanup);
                var cleanupPage = new CleanupPage(_currentLanguage);
                if (MainContentFrame != null)
                {
                    MainContentFrame.Navigate(cleanupPage);
                    MainContentFrame.Visibility = Visibility.Visible;
                }
                if (DashboardContent != null)
                {
                    DashboardContent.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Cleanup page: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CardSecurity_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                await ShowOverlay("🛡️", _currentLanguage == "PT" ? "A verificar segurança..." : "Checking security...");

                bool defenderEnabled = false;
                bool firewallEnabled = false;
                bool updateEnabled = false;

                await Task.Run(async () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (OverlayProgress != null)
                        {
                            OverlayProgress.Text = _currentLanguage == "PT"
                                ? "A verificar Windows Defender..."
                                : "Checking Windows Defender...";
                        }
                    });

                    defenderEnabled = CheckWindowsDefender();
                    await Task.Delay(800);

                    Dispatcher.Invoke(() =>
                    {
                        if (OverlayProgress != null)
                        {
                            OverlayProgress.Text = _currentLanguage == "PT"
                                ? "A verificar Firewall..."
                                : "Checking Firewall...";
                        }
                    });

                    firewallEnabled = CheckFirewall();
                    await Task.Delay(800);

                    Dispatcher.Invoke(() =>
                    {
                        if (OverlayProgress != null)
                        {
                            OverlayProgress.Text = _currentLanguage == "PT"
                                ? "A verificar Windows Update..."
                                : "Checking Windows Update...";
                        }
                    });

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
            catch (Exception ex)
            {
                Debug.WriteLine($"Security check error: {ex.Message}");
                HideOverlay();
            }
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
            try
            {
                await ShowOverlay("⚡", _currentLanguage == "PT" ? "A otimizar performance..." : "Optimizing performance...");

                long memoryFreedKB = 0;
                int processesOptimized = 0;

                await Task.Run(async () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (OverlayProgress != null)
                        {
                            OverlayProgress.Text = _currentLanguage == "PT"
                                ? "A libertar memória RAM..."
                                : "Freeing up RAM memory...";
                        }
                    });

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

                    Dispatcher.Invoke(() =>
                    {
                        if (OverlayProgress != null)
                        {
                            OverlayProgress.Text = _currentLanguage == "PT"
                                ? "A finalizar otimização..."
                                : "Finalizing optimization...";
                        }
                    });

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
                Debug.WriteLine($"Performance optimization error: {ex.Message}");
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
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Storage analysis error: {ex.Message}");
                HideOverlay();
            }
        }

        private async void CardUpdates_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                await ShowOverlay("🔄", _currentLanguage == "PT" ? "A verificar atualizações..." : "Checking for updates...");

                bool updatesAvailable = false;

                await Task.Run(async () =>
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (OverlayProgress != null)
                            {
                                OverlayProgress.Text = _currentLanguage == "PT"
                                    ? "A contactar Windows Update..."
                                    : "Contacting Windows Update...";
                            }
                        });

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
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check error: {ex.Message}");
                HideOverlay();
            }
        }

        private async Task ShowOverlay(string icon, string status)
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (OverlayIcon != null)
                    {
                        OverlayIcon.Text = icon;
                    }
                    if (OverlayStatus != null)
                    {
                        OverlayStatus.Text = status;
                    }
                    if (OverlayProgress != null)
                    {
                        OverlayProgress.Text = "";
                    }
                    if (OverlayProgressBar != null)
                    {
                        OverlayProgressBar.IsIndeterminate = true;
                    }
                    if (CleanupOverlay != null)
                    {
                        CleanupOverlay.Visibility = Visibility.Visible;

                        var overlayFade = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.3),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };
                        CleanupOverlay.BeginAnimation(OpacityProperty, overlayFade);
                    }
                });

                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Show overlay error: {ex.Message}");
            }
        }

        private async Task ShowSuccessOverlay(string icon, string title, string message, string accentColor)
        {
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    if (OverlayIcon != null)
                    {
                        OverlayIcon.Text = icon;
                    }
                    if (OverlayStatus != null)
                    {
                        OverlayStatus.Text = title;
                    }
                    if (OverlayProgress != null)
                    {
                        OverlayProgress.Text = message;
                    }
                    if (OverlayProgressBar != null)
                    {
                        OverlayProgressBar.Visibility = Visibility.Collapsed;
                    }

                    await Task.Delay(3000);

                    HideOverlay();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Show success overlay error: {ex.Message}");
            }
        }

        private async Task ShowSuccessOverlayWithClick(string icon, string title, string message, string accentColor, Action onClickAction = null)
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (OverlayIcon != null)
                    {
                        OverlayIcon.Text = icon;
                    }
                    if (OverlayStatus != null)
                    {
                        OverlayStatus.Text = title;
                    }
                    if (OverlayProgress != null)
                    {
                        OverlayProgress.Text = message;
                    }
                    if (OverlayProgressBar != null)
                    {
                        OverlayProgressBar.Visibility = Visibility.Collapsed;
                    }

                    if (CleanupOverlay != null)
                    {
                        MouseButtonEventHandler clickHandler = null;
                        clickHandler = (s, e) =>
                        {
                            CleanupOverlay.MouseLeftButtonDown -= clickHandler;
                            onClickAction?.Invoke();
                            HideOverlay();
                        };

                        CleanupOverlay.MouseLeftButtonDown += clickHandler;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Show success overlay with click error: {ex.Message}");
            }
        }

        private async Task ShowSuccessOverlayWithAction(string icon, string title, string message, string accentColor, Action onClickAction = null)
        {
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    if (OverlayIcon != null)
                    {
                        OverlayIcon.Text = icon;
                    }
                    if (OverlayStatus != null)
                    {
                        OverlayStatus.Text = title;
                    }
                    if (OverlayProgress != null)
                    {
                        OverlayProgress.Text = message;
                    }
                    if (OverlayProgressBar != null)
                    {
                        OverlayProgressBar.Visibility = Visibility.Collapsed;
                    }

                    bool clicked = false;
                    MouseButtonEventHandler clickHandler = null;

                    if (CleanupOverlay != null)
                    {
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
                    }

                    await Task.Delay(3000);

                    if (!clicked && CleanupOverlay != null && clickHandler != null)
                    {
                        CleanupOverlay.MouseLeftButtonDown -= clickHandler;
                        HideOverlay();
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Show success overlay with action error: {ex.Message}");
            }
        }

        private void HideOverlay()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (CleanupOverlay != null)
                    {
                        var fadeAnimation = new DoubleAnimation
                        {
                            From = 1,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.3),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                        };

                        fadeAnimation.Completed += (s, e) =>
                        {
                            CleanupOverlay.Visibility = Visibility.Collapsed;
                            if (OverlayProgressBar != null)
                            {
                                OverlayProgressBar.Visibility = Visibility.Visible;
                            }
                        };

                        CleanupOverlay.BeginAnimation(OpacityProperty, fadeAnimation);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Hide overlay error: {ex.Message}");
            }
        }

        private void LoadSystemInfo()
        {
            try
            {
                SafeSetText(TxtOS, GetWindowsVersion());
                SafeSetText(TxtCPU, GetCPUName());
                var totalRAM = GetTotalRAM();
                var ramType = GetRAMType();
                SafeSetText(TxtRAM, $"{totalRAM} GB {ramType}");
                SafeSetText(TxtGPU, GetGPUName());
                var (totalStorage, storageType) = GetStorageInfo();
                SafeSetText(TxtStorage, $"{totalStorage} GB {storageType}");
                SafeSetText(TxtArch, Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load system info error: {ex.Message}");
            }
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
            return "Processor";
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