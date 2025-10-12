using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Nexor
{
    public partial class FreshSetupPage : UserControl
    {
        private readonly string _currentLanguage;
        private bool _isProcessing = false;
        private DispatcherTimer _scriptMonitorTimer;
        private Process _updateProcess;
        private string _currentLogFile;

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
                TxtTitle.Text = "Atualização Automática do Windows";
                TxtSubtitle.Text = "Atualize o Windows completamente";
                TxtStatusTitle.Text = "Estado do Processo";
                TxtStatusDesc.Text = "Pronto para iniciar";
                BtnRunAll.Content = "🚀 Iniciar Atualização do Windows";

                TxtStep1Title.Text = "Atualizar Windows";
                TxtStep1Desc.Text = "Instalar todas as atualizações disponíveis (múltiplas verificações)";
                TxtStep1Status.Text = "⏳ Pendente";

                TxtInfoTitle.Text = "ℹ️ Informação Importante";
                TxtInfo1.Text = "• Este processo pode demorar 30-90 minutos";
                TxtInfo2.Text = "• DEVE ser executado como Administrador";
                TxtInfo3.Text = "• Mantenha o computador ligado e conectado à internet";
                TxtInfo4.Text = "• O sistema pode reiniciar automaticamente várias vezes";
            }
            else
            {
                TxtTitle.Text = "Automatic Windows Update";
                TxtSubtitle.Text = "Update Windows completely";
                TxtStatusTitle.Text = "Process Status";
                TxtStatusDesc.Text = "Ready to start";
                BtnRunAll.Content = "🚀 Start Windows Update";

                TxtStep1Title.Text = "Update Windows";
                TxtStep1Desc.Text = "Install all available updates (multiple checks)";
                TxtStep1Status.Text = "⏳ Pending";

                TxtInfoTitle.Text = "ℹ️ Important Information";
                TxtInfo1.Text = "• This process may take 30-90 minutes";
                TxtInfo2.Text = "• MUST be run as Administrator";
                TxtInfo3.Text = "• Keep computer powered on and connected to internet";
                TxtInfo4.Text = "• System may restart automatically multiple times";
            }
        }

        private async void BtnRunAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
                return;

            ShowCustomDialog(
                _currentLanguage == "PT" ? "Iniciar Atualização do Windows" : "Start Windows Update",
                _currentLanguage == "PT"
                    ? "O processo irá:\n\n1️⃣ Verificar e instalar TODAS as atualizações do Windows\n2️⃣ Reiniciar automaticamente quando necessário\n\n⚠️ Pode demorar 30-90 minutos\n⚠️ O PC irá REINICIAR automaticamente\n\n✅ Certifique-se de que o programa está a correr como Administrador\n\nDeseja continuar?"
                    : "The process will:\n\n1️⃣ Check and install ALL Windows updates\n2️⃣ Restart automatically when needed\n\n⚠️ May take 30-90 minutes\n⚠️ PC will RESTART automatically\n\n✅ Make sure the program is running as Administrator\n\nDo you want to continue?",
                DialogType.Question,
                async () =>
                {
                    await StartWindowsUpdate();
                },
                null
            );
        }

        private async Task StartWindowsUpdate()
        {
            _isProcessing = true;
            BtnRunAll.IsEnabled = false;
            LogCard.Visibility = Visibility.Visible;

            AddLog("═══════════════════════════════════════");
            AddLog("🚀 " + (_currentLanguage == "PT"
                ? "Iniciando Atualização do Windows..."
                : "Starting Windows Update..."));
            AddLog("═══════════════════════════════════════\n");

            UpdateStepStatus("🔄 " + (_currentLanguage == "PT" ? "Em progresso..." : "In progress..."), Brushes.Orange);
            AnimateStep(Step1Card, Step1Badge);
            ProgressStep1.Visibility = Visibility.Visible;

            try
            {
                UpdateProgressBar(5);

                AddLog("📦 " + (_currentLanguage == "PT"
                    ? "Extraindo script do recurso incorporado..."
                    : "Extracting script from embedded resource..."));

                // Extract embedded script to temp folder
                string scriptPath = Path.Combine(Path.GetTempPath(), "nexor_winupdateall.ps1");

                try
                {
                    // Try to get the script from embedded resources
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    var resourceName = "Nexor.winupdateall.ps1"; // Namespace.FileName

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                        {
                            // Fallback: look for physical file
                            AddLog("⚠️ " + (_currentLanguage == "PT"
                                ? "Recurso não encontrado, procurando arquivo físico..."
                                : "Resource not found, looking for physical file..."));

                            string appDir = AppDomain.CurrentDomain.BaseDirectory;
                            string physicalScriptPath = Path.Combine(appDir, "winupdateall.ps1");

                            // If not found in bin directory, try project root (for development)
                            if (!File.Exists(physicalScriptPath))
                            {
                                string projectRoot = Path.GetFullPath(Path.Combine(appDir, @"..\..\"));
                                string altScriptPath = Path.Combine(projectRoot, "winupdateall.ps1");
                                if (File.Exists(altScriptPath))
                                {
                                    physicalScriptPath = altScriptPath;
                                }
                            }

                            if (!File.Exists(physicalScriptPath))
                            {
                                throw new FileNotFoundException($"Script not found at: {physicalScriptPath}");
                            }

                            // Copy physical file to temp
                            File.Copy(physicalScriptPath, scriptPath, true);
                        }
                        else
                        {
                            // Extract from embedded resource
                            using (FileStream fileStream = File.Create(scriptPath))
                            {
                                stream.CopyTo(fileStream);
                            }
                        }
                    }

                    AddLog("✅ " + (_currentLanguage == "PT"
                        ? $"Script extraído para: {scriptPath}"
                        : $"Script extracted to: {scriptPath}"));
                }
                catch (Exception ex)
                {
                    AddLog($"❌ " + (_currentLanguage == "PT"
                        ? $"Erro ao extrair script: {ex.Message}"
                        : $"Error extracting script: {ex.Message}"));
                    throw;
                }

                if (!File.Exists(scriptPath))
                {
                    AddLog("❌ " + (_currentLanguage == "PT"
                        ? $"Script não encontrado!\nCaminho esperado: {scriptPath}"
                        : $"Script not found!\nExpected path: {scriptPath}"));

                    ShowCustomDialog(
                        _currentLanguage == "PT" ? "Erro" : "Error",
                        _currentLanguage == "PT"
                            ? $"O script 'winupdateall.ps1' não foi encontrado!\n\nCaminho esperado:\n{scriptPath}\n\nCertifique-se de que o script está na mesma pasta que o executável."
                            : $"Script 'winupdateall.ps1' not found!\n\nExpected path:\n{scriptPath}\n\nMake sure the script is in the same folder as the executable.",
                        DialogType.Error,
                        null,
                        null
                    );

                    UpdateStepStatus("❌ " + (_currentLanguage == "PT" ? "Erro" : "Error"), Brushes.Red);
                    _isProcessing = false;
                    BtnRunAll.IsEnabled = true;
                    return;
                }

                AddLog("✅ " + (_currentLanguage == "PT"
                    ? "Script encontrado!"
                    : "Script found!"));

                UpdateProgressBar(10);

                AddLog("🚀 " + (_currentLanguage == "PT"
                    ? "Executando script de atualização..."
                    : "Running update script..."));

                AddLog("⚠️ " + (_currentLanguage == "PT"
                    ? "Uma janela PowerShell irá abrir. NÃO A FECHE!"
                    : "A PowerShell window will open. DO NOT CLOSE IT!"));

                // Run the PowerShell script
                await RunWindowsUpdateScript(scriptPath);

            }
            catch (Exception ex)
            {
                AddLog($"❌ Exception: {ex.Message}");
                AddLog($"❌ Stack Trace: {ex.StackTrace}");
                UpdateStepStatus("❌ " + (_currentLanguage == "PT" ? "Erro" : "Error"), Brushes.Red);

                ShowCustomDialog(
                    _currentLanguage == "PT" ? "Erro" : "Error",
                    _currentLanguage == "PT"
                        ? $"Ocorreu um erro:\n\n{ex.Message}\n\nVerifique o log para mais detalhes."
                        : $"An error occurred:\n\n{ex.Message}\n\nCheck the log for more details.",
                    DialogType.Error,
                    null,
                    null
                );
            }
            finally
            {
                _isProcessing = false;
                BtnRunAll.IsEnabled = true;
            }
        }

        private async Task RunWindowsUpdateScript(string scriptPath)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Create a log file to capture output
                    _currentLogFile = Path.Combine(Path.GetTempPath(), $"nexor_winupdate_{DateTime.Now:yyyyMMdd_HHmmss}.log");

                    Dispatcher.Invoke(() =>
                    {
                        AddLog("📝 " + (_currentLanguage == "PT"
                            ? $"Log será guardado em:\n{_currentLogFile}"
                            : $"Log will be saved to:\n{_currentLogFile}"));
                        UpdateProgressBar(15);
                    });

                    // Check if running as admin
                    bool isAdmin = new System.Security.Principal.WindowsPrincipal(
                        System.Security.Principal.WindowsIdentity.GetCurrent())
                        .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

                    Dispatcher.Invoke(() =>
                    {
                        if (!isAdmin)
                        {
                            AddLog("⚠️ " + (_currentLanguage == "PT"
                                ? "AVISO: O programa NÃO está a correr como Administrador!"
                                : "WARNING: Program is NOT running as Administrator!"));
                        }
                        else
                        {
                            AddLog("✅ " + (_currentLanguage == "PT"
                                ? "Programa a correr como Administrador"
                                : "Program running as Administrator"));
                        }
                    });

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = false,
                        WindowStyle = ProcessWindowStyle.Normal
                    };

                    Dispatcher.Invoke(() =>
                    {
                        AddLog("▶️ " + (_currentLanguage == "PT"
                            ? "Iniciando PowerShell..."
                            : "Starting PowerShell..."));
                    });

                    _updateProcess = Process.Start(psi);

                    if (_updateProcess == null)
                    {
                        throw new Exception("Failed to start PowerShell process");
                    }

                    Dispatcher.Invoke(() =>
                    {
                        AddLog("✅ " + (_currentLanguage == "PT"
                            ? "PowerShell iniciado com sucesso"
                            : "PowerShell started successfully"));
                        AddLog("📖 " + (_currentLanguage == "PT"
                            ? "A ler saída do script..."
                            : "Reading script output..."));
                        UpdateProgressBar(20);
                    });

                    // Read output in real-time
                    _updateProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                AddLog($"📄 {e.Data}");

                                // Update progress based on script output
                                string line = e.Data.ToLower();
                                if (line.Contains("installing pswindowsupdate"))
                                {
                                    UpdateProgressBar(25);
                                }
                                else if (line.Contains("checking for windows updates"))
                                {
                                    UpdateProgressBar(35);
                                }
                                else if (line.Contains("found") && line.Contains("update"))
                                {
                                    UpdateProgressBar(45);
                                }
                                else if (line.Contains("starting update installation"))
                                {
                                    UpdateProgressBar(55);
                                }
                                else if (line.Contains("installing") || line.Contains("downloading"))
                                {
                                    UpdateProgressBar(70);
                                }
                                else if (line.Contains("installed successfully"))
                                {
                                    UpdateProgressBar(85);
                                }
                                else if (line.Contains("restart") || line.Contains("reboot"))
                                {
                                    UpdateProgressBar(95);
                                }
                            });

                            // Also write to log file
                            try
                            {
                                File.AppendAllText(_currentLogFile, e.Data + Environment.NewLine);
                            }
                            catch { }
                        }
                    };

                    _updateProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                AddLog($"⚠️ {e.Data}");
                            });

                            try
                            {
                                File.AppendAllText(_currentLogFile, "[ERROR] " + e.Data + Environment.NewLine);
                            }
                            catch { }
                        }
                    };

                    _updateProcess.BeginOutputReadLine();
                    _updateProcess.BeginErrorReadLine();

                    // Wait for the process to exit
                    _updateProcess.WaitForExit();

                    int exitCode = _updateProcess.ExitCode;

                    Dispatcher.Invoke(() =>
                    {
                        UpdateProgressBar(100);

                        AddLog($"\n📊 " + (_currentLanguage == "PT"
                            ? $"Script terminou com código: {exitCode}"
                            : $"Script ended with exit code: {exitCode}"));

                        if (exitCode == 0)
                        {
                            AddLog("\n✅ " + (_currentLanguage == "PT"
                                ? "Atualização concluída com sucesso!"
                                : "Update completed successfully!"));
                            UpdateStepStatus("✅ " + (_currentLanguage == "PT" ? "Concluído" : "Completed"), Brushes.LightGreen);
                            AnimateCompletion();

                            ShowCustomNotification(
                                _currentLanguage == "PT" ? "Sucesso!" : "Success!",
                                _currentLanguage == "PT"
                                    ? "Todas as atualizações do Windows foram instaladas!\n\nO sistema irá reiniciar em breve se necessário."
                                    : "All Windows updates have been installed!\n\nThe system will restart soon if needed.",
                                NotificationType.Success
                            );
                        }
                        else if (exitCode == 1)
                        {
                            AddLog("\n❌ " + (_currentLanguage == "PT"
                                ? "Erro: O script precisa de privilégios de Administrador!"
                                : "Error: Script needs Administrator privileges!"));
                            UpdateStepStatus("❌ " + (_currentLanguage == "PT" ? "Erro Admin" : "Admin Error"), Brushes.Red);

                            ShowCustomDialog(
                                _currentLanguage == "PT" ? "Erro de Permissão" : "Permission Error",
                                _currentLanguage == "PT"
                                    ? "O programa precisa de privilégios de Administrador!\n\nPor favor:\n1. Feche o programa\n2. Clique direito no executável\n3. Selecione 'Executar como Administrador'"
                                    : "The program needs Administrator privileges!\n\nPlease:\n1. Close the program\n2. Right-click the executable\n3. Select 'Run as Administrator'",
                                DialogType.Error,
                                null,
                                null
                            );
                        }
                        else
                        {
                            AddLog($"\n⚠️ " + (_currentLanguage == "PT"
                                ? $"Script terminou com aviso (código: {exitCode})"
                                : $"Script ended with warning (code: {exitCode})"));
                            UpdateStepStatus("⚠️ " + (_currentLanguage == "PT" ? "Aviso" : "Warning"), Brushes.Orange);
                        }

                        AddLog("\n📁 " + (_currentLanguage == "PT"
                            ? $"Log completo guardado em:\n{_currentLogFile}"
                            : $"Full log saved to:\n{_currentLogFile}"));
                    });
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddLog("❌ " + (_currentLanguage == "PT"
                            ? "Erro Win32: " + ex.Message
                            : "Win32 Error: " + ex.Message));
                        AddLog("❌ " + (_currentLanguage == "PT"
                            ? "A operação pode ter sido cancelada ou faltam permissões"
                            : "Operation may have been cancelled or missing permissions"));
                        UpdateStepStatus("❌ " + (_currentLanguage == "PT" ? "Cancelado" : "Cancelled"), Brushes.Red);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddLog($"❌ Error: {ex.GetType().Name}");
                        AddLog($"❌ Message: {ex.Message}");
                        AddLog($"❌ Stack: {ex.StackTrace}");
                        UpdateStepStatus("❌ " + (_currentLanguage == "PT" ? "Erro" : "Error"), Brushes.Red);
                    });
                }
            });
        }

        private void UpdateStepStatus(string text, Brush color)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStep1Status.Text = text;
                TxtStep1Status.Foreground = color;
            });
        }

        private void UpdateProgressBar(double value)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressStep1.Value = Math.Min(value, 100);
                ProgressBarMain.Value = Math.Min(value, 100);
                TxtProgress.Text = $"{(int)Math.Min(value, 100)}%";
            });
        }

        private void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtLog.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";

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
            });
        }

        private enum DialogType { Question, Warning, Error, Info }
        private enum NotificationType { Success, Error, Warning, Info }

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
                        DialogBtnYes.Visibility = Visibility.Visible;
                        DialogBtnNo.Visibility = Visibility.Visible;
                        DialogBtnOk.Visibility = Visibility.Collapsed;
                        break;
                    case DialogType.Error:
                        DialogIcon.Text = "❌";
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
                else
                {
                    DialogBtnNo.Click += (s, e) => AnimateDialogClose();
                }

                DialogBtnOk.Click += (s, e) => AnimateDialogClose();

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
                }

                AnimateNotificationIn();

                Task.Run(async () =>
                {
                    await Task.Delay(8000);
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

        private void BtnStep1_Click(object sender, RoutedEventArgs e)
        {
            // Not used
        }

        private void BtnStep2_Click(object sender, RoutedEventArgs e)
        {
            // Not used
        }

        private void BtnStep3_Click(object sender, RoutedEventArgs e)
        {
            // Not used
        }
    }
}