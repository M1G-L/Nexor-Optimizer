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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            AnimatePageLoad();
        }

        private async void AnimatePageLoad()
        {
            // Animate header with slide down and fade
            var headerStoryboard = new Storyboard();

            var headerOpacity = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.6),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var headerSlide = new DoubleAnimation
            {
                From = -50,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.6),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(headerOpacity, HeaderSection);
            Storyboard.SetTargetProperty(headerOpacity, new PropertyPath("Opacity"));
            Storyboard.SetTarget(headerSlide, HeaderSection);
            Storyboard.SetTargetProperty(headerSlide, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            headerStoryboard.Children.Add(headerOpacity);
            headerStoryboard.Children.Add(headerSlide);
            headerStoryboard.Begin();

            await Task.Delay(200);

            // Animate button with scale and fade
            var btnStoryboard = new Storyboard();

            var btnOpacity = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };

            var btnScaleX = new DoubleAnimation
            {
                From = 0.8,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };

            var btnScaleY = new DoubleAnimation
            {
                From = 0.8,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };

            Storyboard.SetTarget(btnOpacity, BtnRunAll);
            Storyboard.SetTargetProperty(btnOpacity, new PropertyPath("Opacity"));
            Storyboard.SetTarget(btnScaleX, BtnRunAll);
            Storyboard.SetTargetProperty(btnScaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTarget(btnScaleY, BtnRunAll);
            Storyboard.SetTargetProperty(btnScaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

            btnStoryboard.Children.Add(btnOpacity);
            btnStoryboard.Children.Add(btnScaleX);
            btnStoryboard.Children.Add(btnScaleY);
            btnStoryboard.Begin();

            await Task.Delay(150);

            // Animate Step 1 Card
            AnimateCardIn(Step1Card, 0);

            await Task.Delay(150);

            // Animate Step 2 Card
            AnimateCardIn(Step2Card, 0);

            await Task.Delay(150);

            // Animate Step 3 Card
            AnimateCardIn(Step3Card, 0);

            await Task.Delay(150);

            // Animate Info Card
            AnimateCardIn(InfoCard, 30);

            // Start subtle pulse on button
            StartButtonPulse();
        }

        private void AnimateCardIn(FrameworkElement card, double translateYFrom)
        {
            var cardStoryboard = new Storyboard();

            var cardOpacity = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var cardSlide = new DoubleAnimation
            {
                From = translateYFrom == 0 ? -100 : translateYFrom,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(cardOpacity, card);
            Storyboard.SetTargetProperty(cardOpacity, new PropertyPath("Opacity"));
            Storyboard.SetTarget(cardSlide, card);

            if (translateYFrom == 0)
            {
                Storyboard.SetTargetProperty(cardSlide, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            }
            else
            {
                Storyboard.SetTargetProperty(cardSlide, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            }

            cardStoryboard.Children.Add(cardOpacity);
            cardStoryboard.Children.Add(cardSlide);
            cardStoryboard.Begin();
        }

        private void StartButtonPulse()
        {
            var pulseStoryboard = new Storyboard
            {
                RepeatBehavior = RepeatBehavior.Forever
            };

            var pulseScaleX = new DoubleAnimation
            {
                From = 1,
                To = 1.02,
                Duration = TimeSpan.FromSeconds(2),
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            var pulseScaleY = new DoubleAnimation
            {
                From = 1,
                To = 1.02,
                Duration = TimeSpan.FromSeconds(2),
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(pulseScaleX, BtnRunAll);
            Storyboard.SetTargetProperty(pulseScaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTarget(pulseScaleY, BtnRunAll);
            Storyboard.SetTargetProperty(pulseScaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

            pulseStoryboard.Children.Add(pulseScaleX);
            pulseStoryboard.Children.Add(pulseScaleY);
            pulseStoryboard.Begin();
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
            var overlayFade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            DialogOverlay.BeginAnimation(OpacityProperty, overlayFade);

            // Scale and fade in dialog box
            var dialogStoryboard = new Storyboard();

            var scaleX = new DoubleAnimation
            {
                From = 0.7,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 }
            };

            var scaleY = new DoubleAnimation
            {
                From = 0.7,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 }
            };

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            Storyboard.SetTarget(scaleX, DialogBox);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTarget(scaleY, DialogBox);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            Storyboard.SetTarget(fadeIn, DialogBox);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));

            dialogStoryboard.Children.Add(scaleX);
            dialogStoryboard.Children.Add(scaleY);
            dialogStoryboard.Children.Add(fadeIn);
            dialogStoryboard.Begin();

            // Animate dialog icon with pulse
            AnimateDialogIcon();
        }

        private void AnimateDialogIcon()
        {
            var iconStoryboard = new Storyboard();

            var iconScaleX = new DoubleAnimation
            {
                From = 0.5,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2, Springiness = 5 }
            };

            var iconScaleY = new DoubleAnimation
            {
                From = 0.5,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2, Springiness = 5 }
            };

            Storyboard.SetTarget(iconScaleX, DialogIconBorder);
            Storyboard.SetTargetProperty(iconScaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTarget(iconScaleY, DialogIconBorder);
            Storyboard.SetTargetProperty(iconScaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

            iconStoryboard.Children.Add(iconScaleX);
            iconStoryboard.Children.Add(iconScaleY);
            iconStoryboard.BeginTime = TimeSpan.FromSeconds(0.2);
            iconStoryboard.Begin();
        }

        private void AnimateDialogOut(Action onComplete)
        {
            var dialogStoryboard = new Storyboard();

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

            Storyboard.SetTarget(scaleX, DialogBox);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTarget(scaleY, DialogBox);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            Storyboard.SetTarget(fadeOut, DialogOverlay);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));

            dialogStoryboard.Children.Add(scaleX);
            dialogStoryboard.Children.Add(scaleY);
            dialogStoryboard.Children.Add(fadeOut);

            dialogStoryboard.Completed += (s, e) =>
            {
                DialogOverlay.Visibility = Visibility.Collapsed;
                onComplete?.Invoke();
            };

            dialogStoryboard.Begin();
        }

        private void ShowNotification(string title, string message, string icon, SolidColorBrush accentColor)
        {
            NotificationTitle.Text = title;
            NotificationMessage.Text = message;
            NotificationIcon.Text = icon;
            NotificationBorder.BorderBrush = accentColor;

            // Update gradient background
            var gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(1, 1);
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(0x1A, accentColor.Color.R, accentColor.Color.G, accentColor.Color.B), 0));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(0x0D, accentColor.Color.R, accentColor.Color.G, accentColor.Color.B), 1));
            NotificationBorder.Background = gradient;

            NotificationPanel.Visibility = Visibility.Visible;

            // Slide in animation
            var slideStoryboard = new Storyboard();

            var slideX = new DoubleAnimation
            {
                From = 500,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }
            };

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.4)
            };

            Storyboard.SetTarget(slideX, NotificationPanel);
            Storyboard.SetTargetProperty(slideX, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            Storyboard.SetTarget(fadeIn, NotificationPanel);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));

            slideStoryboard.Children.Add(slideX);
            slideStoryboard.Children.Add(fadeIn);
            slideStoryboard.Begin();

            // Icon bounce
            var iconBounce = new Storyboard();
            var bounceScale = new DoubleAnimation
            {
                From = 0.5,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.6),
                EasingFunction = new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 2, Bounciness = 3 }
            };

            Storyboard.SetTarget(bounceScale, NotificationBorder);
            Storyboard.SetTargetProperty(bounceScale, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            iconBounce.Children.Add(bounceScale);

            var bounceScaleY = bounceScale.Clone();
            Storyboard.SetTarget(bounceScaleY, NotificationBorder);
            Storyboard.SetTargetProperty(bounceScaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            iconBounce.Children.Add(bounceScaleY);

            iconBounce.BeginTime = TimeSpan.FromSeconds(0.2);
            iconBounce.Begin();

            // Auto hide after 4 seconds
            var hideTimer = new System.Windows.Threading.DispatcherTimer();
            hideTimer.Interval = TimeSpan.FromSeconds(4);
            hideTimer.Tick += (s, e) =>
            {
                HideNotification();
                hideTimer.Stop();
            };
            hideTimer.Start();
        }

        private void HideNotification()
        {
            var slideOut = new Storyboard();

            var slideX = new DoubleAnimation
            {
                To = 500,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            Storyboard.SetTarget(slideX, NotificationPanel);
            Storyboard.SetTargetProperty(slideX, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            Storyboard.SetTarget(fadeOut, NotificationPanel);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));

            slideOut.Children.Add(slideX);
            slideOut.Children.Add(fadeOut);

            slideOut.Completed += (s, e) => NotificationPanel.Visibility = Visibility.Collapsed;
            slideOut.Begin();
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

                    try
                    {
                        CleanSoftwareDistribution();
                    }
                    catch { }

                    if (exitCode == 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ShowNotification(
                                _currentLanguage == "PT" ? "Sucesso!" : "Success!",
                                _currentLanguage == "PT" ? "Atualização concluída!" : "Update completed!",
                                "✅",
                                (SolidColorBrush)FindResource("AccentGreen")
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
                StopWindowsUpdateService();
                System.Threading.Thread.Sleep(5000);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c rd /s /q \"{path}\" && mkdir \"{path}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using (Process process = Process.Start(psi))
                {
                    process?.WaitForExit();
                }

                System.Threading.Thread.Sleep(2000);
                RestartComputer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
                try
                {
                    StartWindowsUpdateService();
                }
                catch { }
            }
        }

        private void RestartComputer()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = "/r /t 10 /c \"Restarting after Windows Update cleanup...\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }

        private void StopWindowsUpdateService()
        {
            ProcessStartInfo stopService = new ProcessStartInfo
            {
                FileName = "net.exe",
                Arguments = "stop wuauserv",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                Verb = "runas"
            };
            using (Process process = Process.Start(stopService))
            {
                process?.WaitForExit();
            }
        }

        private void StartWindowsUpdateService()
        {
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
                process?.WaitForExit();
            }
        }

        private void RemoveReadOnlyAttribute(DirectoryInfo dir)
        {
            foreach (FileInfo file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    file.Attributes = FileAttributes.Normal;
                }
                catch { }
            }

            foreach (DirectoryInfo subDir in dir.EnumerateDirectories())
            {
                try
                {
                    subDir.Attributes = FileAttributes.Normal;
                }
                catch { }
            }
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