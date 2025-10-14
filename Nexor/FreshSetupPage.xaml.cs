using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Nexor
{
    public partial class FreshSetupPage : UserControl
    {
        private string _currentLanguage;
        private bool _isProcessing = false;
        private TaskCompletionSource<bool> _dialogResult;

        public FreshSetupPage(string language = "PT")
        {
            InitializeComponent();
            _currentLanguage = language;
            SetLanguage(language);
        }

        private void SetLanguage(string language)
        {
            if (language == "PT")
            {
                BtnRunAll.Content = "🚀 Iniciar Atualização do Windows";
                TxtTitle.Text = "Atualização Automática do Windows";
                TxtSubtitle.Text = "Atualize o Windows completamente";
            }
            else
            {
                BtnRunAll.Content = "🚀 Start Windows Update";
                TxtTitle.Text = "Automatic Windows Update";
                TxtSubtitle.Text = "Update Windows completely";
            }
        }

        private async void BtnRunAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
                return;

            bool result = await ShowConfirmationDialog();
            if (result)
            {
                StartWindowsUpdate();
            }
        }

        private async Task<bool> ShowConfirmationDialog()
        {
            string title = _currentLanguage == "PT" ? "Iniciar Atualização do Windows" : "Start Windows Update";
            string message = _currentLanguage == "PT"
                ? "O processo irá:\n\n1️⃣ Verificar e instalar TODAS as atualizações do Windows\n2️⃣ Reiniciar automaticamente quando necessário\n\n⚠️ Pode demorar 30-90 minutos\n⚠️ O PC irá REINICIAR automaticamente\n\n✅ Certifique-se de que o programa está a correr como Administrador\n\nDeseja continuar?"
                : "The process will:\n\n1️⃣ Check and install ALL Windows updates\n2️⃣ Restart automatically when needed\n\n⚠️ May take 30-90 minutes\n⚠️ PC will RESTART automatically\n\n✅ Make sure the program is running as Administrator\n\nDo you want to continue?";

            return await ShowModernDialog(title, message, "❓", DialogType.YesNo);
        }

        private async Task<bool> ShowModernDialog(string title, string message, string icon, DialogType dialogType)
        {
            _dialogResult = new TaskCompletionSource<bool>();

            // Set dialog content
            DialogTitle.Text = title;
            DialogMessage.Text = message;
            DialogIcon.Text = icon;

            // Configure buttons based on dialog type
            if (dialogType == DialogType.YesNo)
            {
                DialogBtnYes.Visibility = Visibility.Visible;
                DialogBtnNo.Visibility = Visibility.Visible;
                DialogBtnOk.Visibility = Visibility.Collapsed;
            }
            else // OK
            {
                DialogBtnYes.Visibility = Visibility.Collapsed;
                DialogBtnNo.Visibility = Visibility.Collapsed;
                DialogBtnOk.Visibility = Visibility.Visible;
            }

            // Show dialog with animation
            DialogOverlay.Visibility = Visibility.Visible;
            AnimateDialogIn();

            return await _dialogResult.Task;
        }

        private void AnimateDialogIn()
        {
            // Fade in overlay
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Scale and fade in dialog box
            var scaleX = new DoubleAnimation
            {
                From = 0.7,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };

            var scaleY = new DoubleAnimation
            {
                From = 0.7,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };

            var fadeInDialog = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.4)
            };

            DialogOverlay.BeginAnimation(OpacityProperty, fadeIn);

            var scaleTransform = new ScaleTransform(1, 1);
            DialogBox.RenderTransform = scaleTransform;
            DialogBox.RenderTransformOrigin = new Point(0.5, 0.5);

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            DialogBox.BeginAnimation(OpacityProperty, fadeInDialog);
        }

        private void AnimateDialogOut(Action onComplete)
        {
            // Scale and fade out dialog box
            var scaleX = new DoubleAnimation
            {
                To = 0.7,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var scaleY = new DoubleAnimation
            {
                To = 0.7,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.2)
            };

            fadeOut.Completed += (s, e) =>
            {
                DialogOverlay.Visibility = Visibility.Collapsed;
                onComplete?.Invoke();
            };

            var scaleTransform = DialogBox.RenderTransform as ScaleTransform;
            if (scaleTransform != null)
            {
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            }

            DialogOverlay.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void DialogBtnYes_Click(object sender, RoutedEventArgs e)
        {
            AnimateDialogOut(() => _dialogResult?.TrySetResult(true));
        }

        private void DialogBtnNo_Click(object sender, RoutedEventArgs e)
        {
            AnimateDialogOut(() => _dialogResult?.TrySetResult(false));
        }

        private void DialogBtnOk_Click(object sender, RoutedEventArgs e)
        {
            AnimateDialogOut(() => _dialogResult?.TrySetResult(true));
        }

        private void StartWindowsUpdate()
        {
            _isProcessing = true;
            BtnRunAll.IsEnabled = false;

            Task.Run(() =>
            {
                try
                {
                    string scriptPath = Path.Combine(Path.GetTempPath(), "NexorWinUpdate.ps1");

                    // Extract script from embedded resources or physical file
                    if (!ExtractScript(scriptPath))
                    {
                        Dispatcher.Invoke(async () =>
                        {
                            await ShowModernDialog(
                                _currentLanguage == "PT" ? "Erro" : "Error",
                                _currentLanguage == "PT"
                                    ? "Script 'NexorWinUpdate.ps1' não encontrado!"
                                    : "Script 'NexorWinUpdate.ps1' not found!",
                                "❌",
                                DialogType.OK
                            );
                        });
                        return;
                    }

                    // Run the PowerShell script
                    RunPowerShellScript(scriptPath);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(async () =>
                    {
                        await ShowModernDialog(
                            "Error",
                            $"Error: {ex.Message}",
                            "❌",
                            DialogType.OK
                        );
                    });
                }
                finally
                {
                    _isProcessing = false;
                    Dispatcher.Invoke(() => BtnRunAll.IsEnabled = true);
                }
            });
        }

        private bool ExtractScript(string outputPath)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "Nexor.NexorWinUpdate.ps1";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (FileStream fileStream = File.Create(outputPath))
                        {
                            stream.CopyTo(fileStream);
                        }
                        return true;
                    }
                }

                // Fallback: look for physical file
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string physicalScriptPath = Path.Combine(appDir, "NexorWinUpdate.ps1");

                if (!File.Exists(physicalScriptPath))
                {
                    string projectRoot = Path.GetFullPath(Path.Combine(appDir, @"..\..\"));
                    string altScriptPath = Path.Combine(projectRoot, "NexorWinUpdate.ps1");
                    if (File.Exists(altScriptPath))
                    {
                        physicalScriptPath = altScriptPath;
                    }
                }

                if (!File.Exists(physicalScriptPath))
                {
                    return false;
                }

                File.Copy(physicalScriptPath, outputPath, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void RunPowerShellScript(string scriptPath)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();

                    int exitCode = process.ExitCode;

                    // Clean up SoftwareDistribution folder
                    try
                    {
                        CleanSoftwareDistribution();
                    }
                    catch { }

                    if (exitCode == 0)
                    {
                        Dispatcher.Invoke(async () =>
                        {
                            await ShowModernDialog(
                                _currentLanguage == "PT" ? "Sucesso" : "Success",
                                _currentLanguage == "PT"
                                    ? "Atualização concluída com sucesso!"
                                    : "Update completed successfully!",
                                "✅",
                                DialogType.OK
                            );
                        });
                    }
                    else if (exitCode == 1)
                    {
                        Dispatcher.Invoke(async () =>
                        {
                            await ShowModernDialog(
                                _currentLanguage == "PT" ? "Erro" : "Error",
                                _currentLanguage == "PT"
                                    ? "O programa precisa de privilégios de Administrador!"
                                    : "The program needs Administrator privileges!",
                                "⚠️",
                                DialogType.OK
                            );
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(async () =>
                {
                    await ShowModernDialog(
                        "Error",
                        $"Error: {ex.Message}",
                        "❌",
                        DialogType.OK
                    );
                });
            }
        }

        private void CleanSoftwareDistribution()
        {
            string path = @"C:\Windows\SoftwareDistribution\Download";

            if (!Directory.Exists(path))
                return;

            try
            {
                // Stop Windows Update service
                ProcessStartInfo stopService = new ProcessStartInfo
                {
                    FileName = "net.exe",
                    Arguments = "stop wuauserv",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using (Process process = Process.Start(stopService))
                {
                    process.WaitForExit();
                }

                System.Threading.Thread.Sleep(2000);

                // Delete all files in the folder
                DirectoryInfo di = new DirectoryInfo(path);
                foreach (FileInfo file in di.EnumerateFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch { }
                }

                // Delete all subdirectories
                foreach (DirectoryInfo dir in di.EnumerateDirectories())
                {
                    try
                    {
                        dir.Delete(true);
                    }
                    catch { }
                }

                System.Threading.Thread.Sleep(1000);

                // Restart Windows Update service
                ProcessStartInfo startService = new ProcessStartInfo
                {
                    FileName = "net.exe",
                    Arguments = "start wuauserv",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using (Process process = Process.Start(startService))
                {
                    process.WaitForExit();
                }
            }
            catch { }
        }

        private void BtnStep1_Click(object sender, RoutedEventArgs e) { }
        private void BtnStep2_Click(object sender, RoutedEventArgs e) { }
        private void BtnStep3_Click(object sender, RoutedEventArgs e) { }

        private enum DialogType
        {
            YesNo,
            OK
        }
    }
}