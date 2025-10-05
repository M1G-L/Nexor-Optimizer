using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Nexor
{
    public partial class CleanupPage : Page
    {
        private string _currentLanguage;
        private Dictionary<string, CleanupCategory> _categories = new Dictionary<string, CleanupCategory>();
        private bool _isScanning = false;

        public CleanupPage(string language)
        {
            InitializeComponent();
            _currentLanguage = language;
            InitializeCategories();
            UpdateLanguage();
        }

        private void InitializeCategories()
        {
            _categories = new Dictionary<string, CleanupCategory>
            {
                ["temp"] = new CleanupCategory { Name = "Temporary Files", DisplayName = "Temporary Files", IsEnabled = true, Size = 0, FileCount = 0 },
                ["windowsTemp"] = new CleanupCategory { Name = "Windows Temp", DisplayName = "Windows Temp", IsEnabled = true, Size = 0, FileCount = 0 },
                ["prefetch"] = new CleanupCategory { Name = "Prefetch", DisplayName = "Prefetch", IsEnabled = true, Size = 0, FileCount = 0 },
                ["recycleBin"] = new CleanupCategory { Name = "Recycle Bin", DisplayName = "Recycle Bin", IsEnabled = true, Size = 0, FileCount = 0 },
                ["downloads"] = new CleanupCategory { Name = "Downloads Cache", DisplayName = "Downloads Cache", IsEnabled = false, Size = 0, FileCount = 0 },
                ["browserCache"] = new CleanupCategory { Name = "Browser Cache", DisplayName = "Browser Cache", IsEnabled = true, Size = 0, FileCount = 0 },
                ["thumbnails"] = new CleanupCategory { Name = "Thumbnails", DisplayName = "Thumbnails", IsEnabled = true, Size = 0, FileCount = 0 },
                ["logs"] = new CleanupCategory { Name = "System Logs", DisplayName = "System Logs", IsEnabled = false, Size = 0, FileCount = 0 }
            };
        }

        private void UpdateLanguage()
        {
            if (_currentLanguage == "PT")
            {
                TxtPageTitle.Text = "Limpeza do Sistema";
                TxtPageSubtitle.Text = "Liberte espaço e melhore a performance";
                BtnScan.Content = "🔍 Analisar Sistema";
                BtnCleanSelected.Content = "🧹 Limpar Selecionados";
                TxtTotalSpace.Text = "Espaço Total a Libertar";
                TxtSelectedItems.Text = "Itens Selecionados";

                _categories["temp"].DisplayName = "Ficheiros Temporários";
                _categories["windowsTemp"].DisplayName = "Temp do Windows";
                _categories["prefetch"].DisplayName = "Prefetch";
                _categories["recycleBin"].DisplayName = "Reciclagem";
                _categories["downloads"].DisplayName = "Cache de Downloads";
                _categories["browserCache"].DisplayName = "Cache do Browser";
                _categories["thumbnails"].DisplayName = "Miniaturas";
                _categories["logs"].DisplayName = "Logs do Sistema";
            }
            else
            {
                TxtPageTitle.Text = "System Cleanup";
                TxtPageSubtitle.Text = "Free up space and improve performance";
                BtnScan.Content = "🔍 Analyze System";
                BtnCleanSelected.Content = "🧹 Clean Selected";
                TxtTotalSpace.Text = "Total Space to Free";
                TxtSelectedItems.Text = "Selected Items";

                _categories["temp"].DisplayName = "Temporary Files";
                _categories["windowsTemp"].DisplayName = "Windows Temp";
                _categories["prefetch"].DisplayName = "Prefetch";
                _categories["recycleBin"].DisplayName = "Recycle Bin";
                _categories["downloads"].DisplayName = "Downloads Cache";
                _categories["browserCache"].DisplayName = "Browser Cache";
                _categories["thumbnails"].DisplayName = "Thumbnails";
                _categories["logs"].DisplayName = "System Logs";
            }
        }

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanning) return;

            _isScanning = true;
            BtnScan.IsEnabled = false;
            BtnCleanSelected.IsEnabled = false;
            CategoriesList.Items.Clear();

            ScanProgress.Visibility = Visibility.Visible;
            TxtScanStatus.Text = _currentLanguage == "PT" ? "A analisar..." : "Scanning...";

            await Task.Run(async () =>
            {
                await ScanCategory("temp", Path.GetTempPath());
                await ScanCategory("windowsTemp", @"C:\Windows\Temp");
                await ScanCategory("prefetch", @"C:\Windows\Prefetch");
                await ScanRecycleBin();
                await ScanBrowserCache();
                await ScanThumbnails();
                await ScanLogs();
            });

            Dispatcher.Invoke(() =>
            {
                PopulateCategoriesList();
                UpdateTotals();
                ScanProgress.Visibility = Visibility.Collapsed;
                BtnScan.IsEnabled = true;
                BtnCleanSelected.IsEnabled = true;
                _isScanning = false;
            });
        }

        private async Task ScanCategory(string categoryKey, string path)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(path)) return;

                    var (size, count) = GetDirectorySizeAndCount(path);
                    _categories[categoryKey].Size = size;
                    _categories[categoryKey].FileCount = count;

                    Dispatcher.Invoke(() =>
                    {
                        TxtScanStatus.Text = $"{(_currentLanguage == "PT" ? "A analisar" : "Scanning")}: {_categories[categoryKey].DisplayName}";
                    });
                }
                catch { }
            });

            await Task.Delay(300);
        }

        private async Task ScanRecycleBin()
        {
            await Task.Run(() =>
            {
                try
                {
                    string recycleBinPath = @"C:\$Recycle.Bin";
                    if (Directory.Exists(recycleBinPath))
                    {
                        var (size, count) = GetDirectorySizeAndCount(recycleBinPath);
                        _categories["recycleBin"].Size = size;
                        _categories["recycleBin"].FileCount = count;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        TxtScanStatus.Text = $"{(_currentLanguage == "PT" ? "A analisar" : "Scanning")}: {_categories["recycleBin"].DisplayName}";
                    });
                }
                catch { }
            });

            await Task.Delay(300);
        }

        private async Task ScanBrowserCache()
        {
            await Task.Run(() =>
            {
                try
                {
                    long totalSize = 0;
                    int totalCount = 0;

                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                    string[] browserPaths = new[]
                    {
                        Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache"),
                        Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache"),
                        Path.Combine(localAppData, @"Mozilla\Firefox\Profiles")
                    };

                    foreach (var browserPath in browserPaths)
                    {
                        if (Directory.Exists(browserPath))
                        {
                            var (size, count) = GetDirectorySizeAndCount(browserPath);
                            totalSize += size;
                            totalCount += count;
                        }
                    }

                    _categories["browserCache"].Size = totalSize;
                    _categories["browserCache"].FileCount = totalCount;

                    Dispatcher.Invoke(() =>
                    {
                        TxtScanStatus.Text = $"{(_currentLanguage == "PT" ? "A analisar" : "Scanning")}: {_categories["browserCache"].DisplayName}";
                    });
                }
                catch { }
            });

            await Task.Delay(300);
        }

        private async Task ScanThumbnails()
        {
            await Task.Run(() =>
            {
                try
                {
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string thumbPath = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");

                    if (Directory.Exists(thumbPath))
                    {
                        var files = Directory.GetFiles(thumbPath, "thumbcache_*.db");
                        long size = files.Sum(f => new FileInfo(f).Length);
                        _categories["thumbnails"].Size = size;
                        _categories["thumbnails"].FileCount = files.Length;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        TxtScanStatus.Text = $"{(_currentLanguage == "PT" ? "A analisar" : "Scanning")}: {_categories["thumbnails"].DisplayName}";
                    });
                }
                catch { }
            });

            await Task.Delay(300);
        }

        private async Task ScanLogs()
        {
            await Task.Run(() =>
            {
                try
                {
                    string logsPath = @"C:\Windows\Logs";
                    if (Directory.Exists(logsPath))
                    {
                        var (size, count) = GetDirectorySizeAndCount(logsPath);
                        _categories["logs"].Size = size;
                        _categories["logs"].FileCount = count;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        TxtScanStatus.Text = $"{(_currentLanguage == "PT" ? "A analisar" : "Scanning")}: {_categories["logs"].DisplayName}";
                    });
                }
                catch { }
            });

            await Task.Delay(300);
        }

        private (long, int) GetDirectorySizeAndCount(string path)
        {
            long size = 0;
            int count = 0;

            try
            {
                DirectoryInfo di = new DirectoryInfo(path);

                foreach (FileInfo file in di.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += file.Length;
                        count++;
                    }
                    catch { }
                }
            }
            catch { }

            return (size, count);
        }

        private void PopulateCategoriesList()
        {
            CategoriesList.Items.Clear();

            foreach (var category in _categories.Values.OrderByDescending(c => c.Size))
            {
                var item = CreateCategoryItem(category);
                CategoriesList.Items.Add(item);
            }
        }

        private Border CreateCategoryItem(CleanupCategory category)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1C1C")),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 12),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Checkbox
            var checkBox = new CheckBox
            {
                IsChecked = category.IsEnabled,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 15, 0),
                Tag = category
            };
            checkBox.Checked += (s, e) => { category.IsEnabled = true; UpdateTotals(); };
            checkBox.Unchecked += (s, e) => { category.IsEnabled = false; UpdateTotals(); };
            Grid.SetColumn(checkBox, 0);

            // Content Stack
            var contentStack = new StackPanel();

            // Icon Border
            var iconBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                BorderThickness = new Thickness(1),
                Width = 44,
                Height = 44,
                CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var icon = new TextBlock
            {
                Text = GetCategoryIcon(category.Name),
                FontSize = 22,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = icon;
            contentStack.Children.Add(iconBorder);

            // Category Name
            var nameText = new TextBlock
            {
                Text = category.DisplayName ?? category.Name,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 12, 0, 4)
            };
            contentStack.Children.Add(nameText);

            // Details Text
            var detailsText = new TextBlock
            {
                Text = $"{category.FileCount} {(_currentLanguage == "PT" ? "ficheiros" : "files")}",
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                TextWrapping = TextWrapping.Wrap
            };
            contentStack.Children.Add(detailsText);

            Grid.SetColumn(contentStack, 1);

            // Size Display
            var sizeStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            var sizeText = new TextBlock
            {
                Text = FormatSize(category.Size),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                TextAlignment = TextAlignment.Right
            };
            sizeStack.Children.Add(sizeText);

            Grid.SetColumn(sizeStack, 2);

            mainGrid.Children.Add(checkBox);
            mainGrid.Children.Add(contentStack);
            mainGrid.Children.Add(sizeStack);
            border.Child = mainGrid;

            // Hover effect
            border.MouseEnter += (s, e) =>
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#242424"));
            };
            border.MouseLeave += (s, e) =>
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1C1C"));
            };

            return border;
        }

        private string GetCategoryIcon(string categoryName)
        {
            return categoryName switch
            {
                "Temporary Files" => "🗑️",
                "Windows Temp" => "🪟",
                "Prefetch" => "⚡",
                "Recycle Bin" => "♻️",
                "Downloads Cache" => "📥",
                "Browser Cache" => "🌐",
                "Thumbnails" => "🖼️",
                "System Logs" => "📋",
                _ => "📁"
            };
        }

        private void UpdateTotals()
        {
            long totalSize = _categories.Values.Where(c => c.IsEnabled).Sum(c => c.Size);
            int totalCount = _categories.Values.Where(c => c.IsEnabled).Sum(c => c.FileCount);

            TxtTotalSize.Text = FormatSize(totalSize);
            TxtTotalFiles.Text = $"{totalCount} {(_currentLanguage == "PT" ? "ficheiros" : "files")}";
        }

        private async void BtnCleanSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedCategories = _categories.Values.Where(c => c.IsEnabled && c.Size > 0).ToList();

            if (selectedCategories.Count == 0)
            {
                MessageBox.Show(
                    _currentLanguage == "PT" ? "Nenhuma categoria selecionada!" : "No categories selected!",
                    "Nexor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            var result = MessageBox.Show(
                _currentLanguage == "PT"
                    ? $"Tem a certeza que deseja limpar {FormatSize(selectedCategories.Sum(c => c.Size))}?"
                    : $"Are you sure you want to clean {FormatSize(selectedCategories.Sum(c => c.Size))}?",
                "Nexor",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes) return;

            BtnScan.IsEnabled = false;
            BtnCleanSelected.IsEnabled = false;
            CleanupProgress.Visibility = Visibility.Visible;

            long totalFreed = 0;
            int filesDeleted = 0;

            await Task.Run(async () =>
            {
                foreach (var category in selectedCategories)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtCleanupStatus.Text = $"{(_currentLanguage == "PT" ? "A limpar" : "Cleaning")}: {category.DisplayName}";
                    });

                    var (freed, deleted) = await CleanCategory(category);
                    totalFreed += freed;
                    filesDeleted += deleted;

                    await Task.Delay(500);
                }
            });

            CleanupProgress.Visibility = Visibility.Collapsed;

            MessageBox.Show(
                _currentLanguage == "PT"
                    ? $"Limpeza concluída!\n\n{FormatSize(totalFreed)} libertados\n{filesDeleted} ficheiros removidos"
                    : $"Cleanup complete!\n\n{FormatSize(totalFreed)} freed\n{filesDeleted} files removed",
                "Nexor",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            BtnScan_Click(sender, e);
        }

        private async Task<(long, int)> CleanCategory(CleanupCategory category)
        {
            return await Task.Run(() =>
            {
                long freed = 0;
                int deleted = 0;

                try
                {
                    switch (category.Name)
                    {
                        case "Temporary Files":
                            (freed, deleted) = CleanDirectory(Path.GetTempPath());
                            break;
                        case "Windows Temp":
                            (freed, deleted) = CleanDirectory(@"C:\Windows\Temp");
                            break;
                        case "Prefetch":
                            (freed, deleted) = CleanDirectory(@"C:\Windows\Prefetch");
                            break;
                        case "Recycle Bin":
                            (freed, deleted) = EmptyRecycleBin();
                            break;
                        case "Browser Cache":
                            (freed, deleted) = CleanBrowserCache();
                            break;
                        case "Thumbnails":
                            (freed, deleted) = CleanThumbnails();
                            break;
                    }
                }
                catch { }

                return (freed, deleted);
            });
        }

        private (long, int) CleanDirectory(string path)
        {
            long bytesFreed = 0;
            int filesCount = 0;

            try
            {
                if (!Directory.Exists(path)) return (0, 0);

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
                        var (size, count) = GetDirectorySizeAndCount(dir.FullName);
                        dir.Delete(true);
                        bytesFreed += size;
                        filesCount += count;
                    }
                    catch { }
                }
            }
            catch { }

            return (bytesFreed, filesCount);
        }

        private (long, int) EmptyRecycleBin()
        {
            long freed = _categories["recycleBin"].Size;
            int count = _categories["recycleBin"].FileCount;

            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c rd /s /q C:\\$Recycle.Bin",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                if (process != null)
                {
                    process.WaitForExit();
                }
            }
            catch { }

            return (freed, count);
        }

        private (long, int) CleanBrowserCache()
        {
            long totalFreed = 0;
            int totalDeleted = 0;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] browserPaths = new[]
            {
                Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache"),
                Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache")
            };

            foreach (var path in browserPaths)
            {
                if (Directory.Exists(path))
                {
                    var (freed, deleted) = CleanDirectory(path);
                    totalFreed += freed;
                    totalDeleted += deleted;
                }
            }

            return (totalFreed, totalDeleted);
        }

        private (long, int) CleanThumbnails()
        {
            long freed = 0;
            int deleted = 0;

            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string thumbPath = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");

                if (Directory.Exists(thumbPath))
                {
                    var files = Directory.GetFiles(thumbPath, "thumbcache_*.db");
                    foreach (var file in files)
                    {
                        try
                        {
                            long size = new FileInfo(file).Length;
                            File.Delete(file);
                            freed += size;
                            deleted++;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return (freed, deleted);
        }

        private string FormatSize(long bytes)
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
    }

    public class CleanupCategory
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public long Size { get; set; }
        public int FileCount { get; set; }
    }
}