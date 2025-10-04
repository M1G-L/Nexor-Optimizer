using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Nexor
{
    public partial class ProcessesPage : Page
    {
        private ObservableCollection<ProcessInfo> _processes = new ObservableCollection<ProcessInfo>();
        private List<ProcessInfo> _allProcesses = new List<ProcessInfo>();
        private DispatcherTimer? _refreshTimer;
        private string _currentLanguage;
        private Dictionary<string, DateTime> _lastCpuTime = new Dictionary<string, DateTime>();
        private Dictionary<string, TimeSpan> _lastTotalProcessorTime = new Dictionary<string, TimeSpan>();
        private int _currentSortMode = 0; // 0 = Name, 1 = CPU, 2 = Memory

        private readonly Dictionary<string, (string icon, string description)> _appInfo = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            // Browsers
            { "chrome", ("🌐", "Google Chrome") },
            { "firefox", ("🦊", "Mozilla Firefox") },
            { "msedge", ("🌐", "Microsoft Edge") },
            { "opera", ("🎭", "Opera Browser") },
            { "brave", ("🦁", "Brave Browser") },
            { "safari", ("🧭", "Safari") },
            
            // Communication
            { "discord", ("💬", "Discord") },
            { "spotify", ("🎵", "Spotify") },
            { "teams", ("👥", "Microsoft Teams") },
            { "zoom", ("📹", "Zoom") },
            { "skype", ("📞", "Skype") },
            { "slack", ("💼", "Slack") },
            { "whatsapp", ("💚", "WhatsApp") },
            { "telegram", ("✈️", "Telegram") },
            
            // Gaming
            { "steam", ("🎮", "Steam") },
            { "epicgameslauncher", ("🎮", "Epic Games") },
            { "riotclientservices", ("🎮", "Riot Client") },
            { "battlenet", ("⚔️", "Battle.net") },
            { "leagueclient", ("🎮", "League of Legends") },
            { "valorant", ("🎮", "Valorant") },
            
            // Development
            { "code", ("💻", "VS Code") },
            { "devenv", ("🔧", "Visual Studio") },
            { "notepad++", ("📝", "Notepad++") },
            { "sublime_text", ("📝", "Sublime Text") },
            { "atom", ("⚛️", "Atom") },
            { "rider", ("🔧", "JetBrains Rider") },
            { "pycharm", ("🐍", "PyCharm") },
            { "webstorm", ("🌐", "WebStorm") },
            
            // Media
            { "vlc", ("🎬", "VLC Media Player") },
            { "wmplayer", ("🎵", "Windows Media Player") },
            { "foobar2000", ("🎵", "Foobar2000") },
            { "itunes", ("🎵", "iTunes") },
            
            // Office
            { "winword", ("📄", "Microsoft Word") },
            { "excel", ("📊", "Microsoft Excel") },
            { "powerpnt", ("📽️", "PowerPoint") },
            { "onenote", ("📒", "OneNote") },
            { "outlook", ("📧", "Outlook") },
            
            // Design
            { "photoshop", ("🎨", "Photoshop") },
            { "illustrator", ("🖌️", "Illustrator") },
            { "blender", ("🎨", "Blender") },
            { "gimp", ("🎨", "GIMP") },
            { "figma", ("🎨", "Figma") },
            
            // System
            { "explorer", ("📁", "Windows Explorer") },
            { "notepad", ("📝", "Notepad") },
            { "calc", ("🔢", "Calculator") },
            { "cmd", ("⚫", "Command Prompt") },
            { "powershell", ("🔵", "PowerShell") },
            { "taskmgr", ("⚙️", "Task Manager") },
            { "mmc", ("⚙️", "Management Console") },
            { "regedit", ("📝", "Registry Editor") },
            
            // Other
            { "dropbox", ("📦", "Dropbox") },
            { "onedrive", ("☁️", "OneDrive") },
            { "googledrive", ("☁️", "Google Drive") },
            { "7zfm", ("📦", "7-Zip") },
            { "winrar", ("📦", "WinRAR") }
        };

        public ProcessesPage(string language = "PT")
        {
            InitializeComponent();
            _currentLanguage = language;
            SetLanguage(language);

            // Setup search box placeholder
            TxtSearch.Text = _currentLanguage == "PT" ? "🔍 Pesquisar aplicações..." : "🔍 Search applications...";
            TxtSearch.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)); // TextSecondary color

            TxtSearch.GotFocus += (s, e) =>
            {
                if (TxtSearch.Text.Contains("🔍"))
                {
                    TxtSearch.Text = "";
                    TxtSearch.Foreground = new SolidColorBrush(Colors.White);
                }
            };

            TxtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(TxtSearch.Text))
                {
                    TxtSearch.Text = _currentLanguage == "PT" ? "🔍 Pesquisar aplicações..." : "🔍 Search applications...";
                    TxtSearch.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                }
            };

            LoadProcesses();
            SetupAutoRefresh();
            UpdateSortButtons();
        }

        private void SetLanguage(string language)
        {
            if (language == "PT")
            {
                TxtTitle.Text = "Gestor de Processos";
                TxtSubtitle.Text = "Monitorize e controle as aplicações em execução";
                BtnRefresh.Content = "🔄 Atualizar";
                TxtProcessLabel.Text = "Aplicações";
                TxtCPULabel.Text = "Uso de CPU";
                TxtMemoryLabel.Text = "Memória RAM";
                TxtHeaderName.Text = "APLICAÇÃO";
                TxtHeaderCPU.Text = "CPU";
                TxtHeaderMemory.Text = "MEMÓRIA";
                TxtHeaderAction.Text = "AÇÃO";
                BtnSortName.Content = "Nome";
                BtnSortCPU.Content = "CPU";
                BtnSortMemory.Content = "Memória";
            }
            else
            {
                TxtTitle.Text = "Process Manager";
                TxtSubtitle.Text = "Monitor and control running applications";
                BtnRefresh.Content = "🔄 Refresh";
                TxtProcessLabel.Text = "Applications";
                TxtCPULabel.Text = "CPU Usage";
                TxtMemoryLabel.Text = "RAM Memory";
                TxtHeaderName.Text = "APPLICATION";
                TxtHeaderCPU.Text = "CPU";
                TxtHeaderMemory.Text = "MEMORY";
                TxtHeaderAction.Text = "ACTION";
                BtnSortName.Content = "Name";
                BtnSortCPU.Content = "CPU";
                BtnSortMemory.Content = "Memory";
            }
        }

        private void SetupAutoRefresh()
        {
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _refreshTimer.Tick += (s, e) => LoadProcesses();
            _refreshTimer.Start();
        }

        private void LoadProcesses()
        {
            try
            {
                var processes = Process.GetProcesses()
                    .Where(p => !string.IsNullOrEmpty(p.ProcessName) &&
                                _appInfo.ContainsKey(p.ProcessName))
                    .ToList();

                _allProcesses.Clear();
                double totalCpu = 0;
                long totalMemory = 0;

                foreach (var process in processes)
                {
                    try
                    {
                        double cpuUsage = GetProcessCpuUsage(process);
                        long memoryMB = process.WorkingSet64 / (1024 * 1024);

                        totalCpu += cpuUsage;
                        totalMemory += memoryMB;

                        var appData = _appInfo.TryGetValue(process.ProcessName, out var info)
                            ? info
                            : ("📦", process.ProcessName);

                        _allProcesses.Add(new ProcessInfo
                        {
                            ProcessId = process.Id,
                            ProcessName = appData.Item2,
                            ProcessDescription = $"PID: {process.Id}",
                            Icon = appData.Item1,
                            CpuUsage = $"{cpuUsage:F1}%",
                            MemoryUsage = $"{memoryMB} MB",
                            CpuValue = cpuUsage,
                            MemoryValue = memoryMB
                        });
                    }
                    catch { }
                }

                // Update stats
                TxtProcessCount.Text = _allProcesses.Count.ToString();
                TxtCPUUsage.Text = $"{totalCpu:F1}%";

                double totalMemoryGB = totalMemory / 1024.0;
                TxtMemoryUsage.Text = totalMemoryGB >= 1
                    ? $"{totalMemoryGB:F2} GB"
                    : $"{totalMemory} MB";

                ApplyFiltersAndSort();
            }
            catch { }
        }

        private double GetProcessCpuUsage(Process process)
        {
            try
            {
                string processKey = $"{process.Id}_{process.ProcessName}";

                if (_lastCpuTime.ContainsKey(processKey) && _lastTotalProcessorTime.ContainsKey(processKey))
                {
                    DateTime currentTime = DateTime.Now;
                    TimeSpan currentTotalProcessorTime = process.TotalProcessorTime;

                    double cpuUsedMs = (currentTotalProcessorTime - _lastTotalProcessorTime[processKey]).TotalMilliseconds;
                    double totalMsPassed = (currentTime - _lastCpuTime[processKey]).TotalMilliseconds;

                    if (totalMsPassed > 0)
                    {
                        double cpuUsageTotal = (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) * 100;

                        _lastCpuTime[processKey] = currentTime;
                        _lastTotalProcessorTime[processKey] = currentTotalProcessorTime;

                        return Math.Min(Math.Max(cpuUsageTotal, 0), 100);
                    }
                }

                _lastCpuTime[processKey] = DateTime.Now;
                _lastTotalProcessorTime[processKey] = process.TotalProcessorTime;

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private void ApplyFiltersAndSort()
        {
            var filtered = _allProcesses.AsEnumerable();

            // Apply search filter
            string searchText = TxtSearch.Text;
            if (!string.IsNullOrWhiteSpace(searchText) && !searchText.Contains("🔍"))
            {
                filtered = filtered.Where(p =>
                    p.ProcessName.ToLower().Contains(searchText.ToLower()) ||
                    p.ProcessDescription.ToLower().Contains(searchText.ToLower()));
            }

            // Apply sorting
            var sortedList = _currentSortMode switch
            {
                1 => filtered.OrderByDescending(p => p.CpuValue).ToList(),
                2 => filtered.OrderByDescending(p => p.MemoryValue).ToList(),
                _ => filtered.OrderBy(p => p.ProcessName).ToList()
            };

            // Update UI
            _processes.Clear();
            foreach (var proc in sortedList)
            {
                _processes.Add(proc);
            }

            ProcessList.ItemsSource = null;
            ProcessList.ItemsSource = _processes;
        }

        private void UpdateSortButtons()
        {
            var defaultBg = (SolidColorBrush)FindResource("CardBg");
            var activeBg = (SolidColorBrush)FindResource("AccentBlue");

            BtnSortName.Background = _currentSortMode == 0 ? activeBg : defaultBg;
            BtnSortCPU.Background = _currentSortMode == 1 ? activeBg : defaultBg;
            BtnSortMemory.Background = _currentSortMode == 2 ? activeBg : defaultBg;
        }

        private void BtnSortName_Click(object sender, RoutedEventArgs e)
        {
            _currentSortMode = 0;
            UpdateSortButtons();
            ApplyFiltersAndSort();
        }

        private void BtnSortCPU_Click(object sender, RoutedEventArgs e)
        {
            _currentSortMode = 1;
            UpdateSortButtons();
            ApplyFiltersAndSort();
        }

        private void BtnSortMemory_Click(object sender, RoutedEventArgs e)
        {
            _currentSortMode = 2;
            UpdateSortButtons();
            ApplyFiltersAndSort();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadProcesses();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void BtnKillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int processId)
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    var processInfo = _allProcesses.FirstOrDefault(p => p.ProcessId == processId);
                    string processName = processInfo?.ProcessName ?? process.ProcessName;

                    var result = MessageBox.Show(
                        _currentLanguage == "PT"
                            ? $"Tem a certeza que deseja terminar '{processName}'?\n\nIsto pode causar perda de dados não guardados."
                            : $"Are you sure you want to terminate '{processName}'?\n\nThis may cause loss of unsaved data.",
                        "Nexor - " + (_currentLanguage == "PT" ? "Confirmar Ação" : "Confirm Action"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        process.Kill();
                        process.WaitForExit(2000);

                        // Clean up tracking dictionaries
                        string processKey = $"{processId}_{process.ProcessName}";
                        _lastCpuTime.Remove(processKey);
                        _lastTotalProcessorTime.Remove(processKey);

                        LoadProcesses();

                        MessageBox.Show(
                            _currentLanguage == "PT"
                                ? $"'{processName}' foi terminado com sucesso."
                                : $"'{processName}' was terminated successfully.",
                            "Nexor",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        _currentLanguage == "PT"
                            ? $"Erro ao terminar o processo:\n{ex.Message}"
                            : $"Error terminating process:\n{ex.Message}",
                        "Nexor - " + (_currentLanguage == "PT" ? "Erro" : "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void ProcessItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(37, 37, 37));
            }
        }

        private void ProcessItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            }
        }

        // Destructor to clean up timer
        ~ProcessesPage()
        {
            _refreshTimer?.Stop();
            _lastCpuTime.Clear();
            _lastTotalProcessorTime.Clear();
        }
    }

    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "";
        public string ProcessDescription { get; set; } = "";
        public string Icon { get; set; } = "📦";
        public string CpuUsage { get; set; } = "";
        public string MemoryUsage { get; set; } = "";
        public double CpuValue { get; set; }
        public long MemoryValue { get; set; }
    }
}