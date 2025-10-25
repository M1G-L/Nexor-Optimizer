using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Nexor
{
    public partial class PerformancePage : Page
    {
        private string _currentLanguage;

        public PerformancePage(string language)
        {
            InitializeComponent();
            _currentLanguage = language;
            UpdateLanguage();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            AnimatePageLoad();
            LoadCurrentSettings();
        }

        private async void AnimatePageLoad()
        {
            try
            {
                // Animate header
                AnimateElement(HeaderSection, 0, 1, -30, 0, 0.5);
                await Task.Delay(100);

                // Animate cards with stagger
                AnimateCard(QuickActionsCard, 150);
                await Task.Delay(150);

                AnimateCard(SystemTweaksCard, 150);
                await Task.Delay(150);

                AnimateCard(VisualEffectsCard, 150);
                await Task.Delay(150);

                AnimateCard(NetworkCard, 150);
                await Task.Delay(150);

                AnimateCard(GamingCard, 150);
                await Task.Delay(150);

                AnimateCard(MemoryCard, 150);
                await Task.Delay(150);

                AnimateCard(PrivacyCard, 150);
                await Task.Delay(150);

                AnimateCard(AdvancedCard, 150);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Animation error: {ex.Message}");
            }
        }

        private void AnimateElement(FrameworkElement element, double fromOpacity, double toOpacity,
            double fromTranslate, double toTranslate, double durationSeconds)
        {
            try
            {
                var storyboard = new Storyboard();

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
                    Storyboard.SetTargetProperty(translateAnimation,
                        new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.Y)"));
                    storyboard.Children.Add(translateAnimation);
                }

                storyboard.Begin();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Element animation error: {ex.Message}");
            }
        }

        private void AnimateCard(FrameworkElement card, int delay)
        {
            Task.Delay(delay).ContinueWith(_ =>
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        var storyboard = new Storyboard();

                        var opacityAnimation = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.6),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };

                        Storyboard.SetTarget(opacityAnimation, card);
                        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
                        storyboard.Children.Add(opacityAnimation);

                        var scaleXAnimation = new DoubleAnimation
                        {
                            From = 0.95,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.6),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };

                        Storyboard.SetTarget(scaleXAnimation, card);
                        Storyboard.SetTargetProperty(scaleXAnimation,
                            new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
                        storyboard.Children.Add(scaleXAnimation);

                        var scaleYAnimation = new DoubleAnimation
                        {
                            From = 0.95,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.6),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };

                        Storyboard.SetTarget(scaleYAnimation, card);
                        Storyboard.SetTargetProperty(scaleYAnimation,
                            new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
                        storyboard.Children.Add(scaleYAnimation);

                        storyboard.Begin();
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Card animation error: {ex.Message}");
                }
            });
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // Load current system settings and update toggles
                // This will be implemented when we add the functionality
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load settings error: {ex.Message}");
            }
        }

        private void UpdateLanguage()
        {
            try
            {
                if (_currentLanguage == "PT")
                {
                    TxtTitle.Text = "Ajustes de Performance";
                    TxtSubtitle.Text = "Otimize o seu sistema para máxima performance";
                    TxtQuickActions.Text = "⚡ Ações Rápidas";
                    TxtOptimizeAll.Text = "Otimizar Tudo";
                    TxtClearRAM.Text = "Limpar RAM";
                    TxtResetTweaks.Text = "Repor Tudo";

                    // System Tweaks
                    TxtSystemTweaks.Text = "🔧 Ajustes do Sistema";
                    TxtGameMode.Text = "Modo de Jogo";
                    TxtGameModeDesc.Text = "Otimizar sistema para performance de jogo";
                    TxtPowerPlan.Text = "Plano de Energia Alto Desempenho";
                    TxtPowerPlanDesc.Text = "Máxima performance do CPU, maior consumo de energia";
                    TxtUltimatePower.Text = "Modo de Performance Extrema";
                    TxtPro.Text = "PRO";
                    TxtUltimatePowerDesc.Text = "Plano de energia oculto da Microsoft para máxima performance";
                    TxtHibernation.Text = "Desativar Hibernação";
                    TxtHibernationDesc.Text = "Libertar GB de espaço em disco, arranques mais rápidos";
                    TxtFastStartup.Text = "Desativar Arranque Rápido";
                    TxtFastStartupDesc.Text = "Prevenir problemas do sistema, arranques mais limpos";

                    // Visual Effects
                    TxtVisualEffects.Text = "🎨 Efeitos Visuais";
                    TxtAnimations.Text = "Desativar Animações de Janelas";
                    TxtAnimationsDesc.Text = "Interface mais rápida, melhor performance";
                    TxtTransparency.Text = "Desativar Efeitos de Transparência";
                    TxtTransparencyDesc.Text = "Reduzir carga da GPU, poupar recursos";
                    TxtBestPerformance.Text = "Ajustar para Melhor Performance";
                    TxtBestPerformanceDesc.Text = "Desativar todos os efeitos visuais para máxima velocidade";

                    // Network
                    TxtNetwork.Text = "🌐 Otimização de Rede";
                    TxtDNS.Text = "Otimizar Definições DNS";
                    TxtDNSDesc.Text = "Usar Cloudflare DNS (1.1.1.1) para internet mais rápida";
                    TxtTCP.Text = "Otimização TCP/IP";
                    TxtTCPDesc.Text = "Otimizar stack de rede para melhor throughput";
                    TxtThrottle.Text = "Desativar Limitação de Rede";
                    TxtThrottleDesc.Text = "Remover limitações de largura de banda do Windows";

                    // Gaming
                    TxtGaming.Text = "🎮 Otimização para Jogos";
                    TxtGPUScheduling.Text = "Agendamento de GPU Acelerado por Hardware";
                    TxtGPUSchedulingDesc.Text = "Reduzir latência e melhorar FPS em jogos";
                    TxtFullscreen.Text = "Desativar Otimizações de Ecrã Inteiro";
                    TxtFullscreenDesc.Text = "Melhor performance e compatibilidade em jogos";
                    TxtMSI.Text = "Ativar Modo MSI para GPU";
                    TxtAdvanced.Text = "AVANÇADO";
                    TxtMSIDesc.Text = "Message Signaled Interrupts para menor latência";

                    // Memory
                    TxtMemory.Text = "💾 Memória e Armazenamento";
                    TxtSuperfetch.Text = "Desativar Superfetch/Prefetch";
                    TxtSuperfetchDesc.Text = "Reduzir uso do disco em SSDs";
                    TxtPageFile.Text = "Otimizar Memória Virtual";
                    TxtPageFileDesc.Text = "Definir tamanho ideal de ficheiro de página para a sua RAM";
                    TxtIndexing.Text = "Desativar Indexação de Pesquisa";
                    TxtIndexingDesc.Text = "Libertar recursos de CPU e disco";
                    TxtTRIM.Text = "Ativar TRIM para SSD";
                    TxtTRIMDesc.Text = "Manter performance e longevidade do SSD";

                    // Privacy
                    TxtPrivacy.Text = "🔒 Privacidade e Serviços em Segundo Plano";
                    TxtTelemetry.Text = "Desativar Telemetria e Recolha de Dados";
                    TxtTelemetryDesc.Text = "Impedir Microsoft de recolher dados de utilização";
                    TxtBackgroundApps.Text = "Desativar Apps em Segundo Plano";
                    TxtBackgroundAppsDesc.Text = "Prevenir apps de executar em segundo plano";
                    TxtCortana.Text = "Desativar Cortana";
                    TxtCortanaDesc.Text = "Libertar recursos do sistema";
                    TxtTips.Text = "Desativar Dicas e Sugestões do Windows";
                    TxtTipsDesc.Text = "Remover notificações e sugestões irritantes";

                    // Advanced
                    TxtAdvancedTweaks.Text = "⚠️ Ajustes Avançados";
                    TxtCaution.Text = "CUIDADO";
                    TxtCPUPriority.Text = "Otimizar Prioridade de CPU para Jogos";
                    TxtCPUPriorityDesc.Text = "Priorizar aplicações em primeiro plano";
                    TxtNagle.Text = "Desativar Algoritmo de Nagle";
                    TxtNagleDesc.Text = "Reduzir latência em jogos online";
                    TxtTimer.Text = "Timer de Alta Precisão";
                    TxtTimerDesc.Text = "Aumentar resolução do timer para gameplay mais suave";
                    TxtCoreParking.Text = "Desativar Core Parking do CPU";
                    TxtCoreParkingDesc.Text = "Manter todos os núcleos do CPU ativos para máxima performance";
                }
                else
                {
                    TxtTitle.Text = "Performance Tweaks";
                    TxtSubtitle.Text = "Optimize your system for maximum performance";
                    TxtQuickActions.Text = "⚡ Quick Actions";
                    TxtOptimizeAll.Text = "Optimize All";
                    TxtClearRAM.Text = "Clear RAM";
                    TxtResetTweaks.Text = "Reset All";

                    // System Tweaks
                    TxtSystemTweaks.Text = "🔧 System Tweaks";
                    TxtGameMode.Text = "Game Mode";
                    TxtGameModeDesc.Text = "Optimize system for gaming performance";
                    TxtPowerPlan.Text = "High Performance Power Plan";
                    TxtPowerPlanDesc.Text = "Maximum CPU performance, higher power consumption";
                    TxtUltimatePower.Text = "Ultimate Performance Mode";
                    TxtPro.Text = "PRO";
                    TxtUltimatePowerDesc.Text = "Microsoft's hidden power plan for maximum performance";
                    TxtHibernation.Text = "Disable Hibernation";
                    TxtHibernationDesc.Text = "Free up GB of disk space, faster boot times";
                    TxtFastStartup.Text = "Disable Fast Startup";
                    TxtFastStartupDesc.Text = "Prevent system issues, cleaner boots";

                    // Visual Effects
                    TxtVisualEffects.Text = "🎨 Visual Effects";
                    TxtAnimations.Text = "Disable Window Animations";
                    TxtAnimationsDesc.Text = "Snappier UI, better performance";
                    TxtTransparency.Text = "Disable Transparency Effects";
                    TxtTransparencyDesc.Text = "Reduce GPU load, save resources";
                    TxtBestPerformance.Text = "Adjust for Best Performance";
                    TxtBestPerformanceDesc.Text = "Disable all visual effects for maximum speed";

                    // Network
                    TxtNetwork.Text = "🌐 Network Optimization";
                    TxtDNS.Text = "Optimize DNS Settings";
                    TxtDNSDesc.Text = "Use Cloudflare DNS (1.1.1.1) for faster internet";
                    TxtTCP.Text = "TCP/IP Optimization";
                    TxtTCPDesc.Text = "Optimize network stack for better throughput";
                    TxtThrottle.Text = "Disable Network Throttling";
                    TxtThrottleDesc.Text = "Remove Windows bandwidth limitations";

                    // Gaming
                    TxtGaming.Text = "🎮 Gaming Optimization";
                    TxtGPUScheduling.Text = "Hardware Accelerated GPU Scheduling";
                    TxtGPUSchedulingDesc.Text = "Reduce latency and improve FPS in games";
                    TxtFullscreen.Text = "Disable Fullscreen Optimizations";
                    TxtFullscreenDesc.Text = "Better gaming performance and compatibility";
                    TxtMSI.Text = "Enable MSI Mode for GPU";
                    TxtAdvanced.Text = "ADVANCED";
                    TxtMSIDesc.Text = "Message Signaled Interrupts for lower latency";

                    // Memory
                    TxtMemory.Text = "💾 Memory & Storage";
                    TxtSuperfetch.Text = "Disable Superfetch/Prefetch";
                    TxtSuperfetchDesc.Text = "Reduce disk usage on SSDs";
                    TxtPageFile.Text = "Optimize Virtual Memory";
                    TxtPageFileDesc.Text = "Set optimal page file size for your RAM";
                    TxtIndexing.Text = "Disable Search Indexing";
                    TxtIndexingDesc.Text = "Free up CPU and disk resources";
                    TxtTRIM.Text = "Enable TRIM for SSD";
                    TxtTRIMDesc.Text = "Maintain SSD performance and longevity";

                    // Privacy
                    TxtPrivacy.Text = "🔒 Privacy & Background Services";
                    TxtTelemetry.Text = "Disable Telemetry & Data Collection";
                    TxtTelemetryDesc.Text = "Stop Microsoft from collecting usage data";
                    TxtBackgroundApps.Text = "Disable Background Apps";
                    TxtBackgroundAppsDesc.Text = "Prevent apps from running in background";
                    TxtCortana.Text = "Disable Cortana";
                    TxtCortanaDesc.Text = "Free up system resources";
                    TxtTips.Text = "Disable Windows Tips & Suggestions";
                    TxtTipsDesc.Text = "Remove annoying notifications and suggestions";

                    // Advanced
                    TxtAdvancedTweaks.Text = "⚠️ Advanced Tweaks";
                    TxtCaution.Text = "CAUTION";
                    TxtCPUPriority.Text = "Optimize CPU Priority for Games";
                    TxtCPUPriorityDesc.Text = "Prioritize foreground applications";
                    TxtNagle.Text = "Disable Nagle's Algorithm";
                    TxtNagleDesc.Text = "Reduce online gaming latency";
                    TxtTimer.Text = "High Precision Timer";
                    TxtTimerDesc.Text = "Increase timer resolution for smoother gameplay";
                    TxtCoreParking.Text = "Disable CPU Core Parking";
                    TxtCoreParkingDesc.Text = "Keep all CPU cores active for maximum performance";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Language update error: {ex.Message}");
            }
        }

        // Placeholder methods for button events - will be implemented next
        private void BtnOptimizeAll_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement optimize all functionality
        }

        private void BtnClearRAM_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement clear RAM functionality
        }

        private void BtnResetTweaks_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement reset all functionality
        }

        // Toggle event handlers
        private void ChkGameMode_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable Game Mode
        }

        private void ChkGameMode_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable Game Mode
        }

        private void ChkPowerPlan_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Set High Performance power plan
        }

        private void ChkPowerPlan_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Revert power plan
        }

        private void ChkUltimatePower_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable Ultimate Performance
        }

        private void ChkUltimatePower_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable Ultimate Performance
        }

        private void ChkHibernation_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable Hibernation
        }

        private void ChkHibernation_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable Hibernation
        }

        private void ChkFastStartup_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable Fast Startup
        }

        private void ChkFastStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable Fast Startup
        }

        private void ChkAnimations_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable animations
        }

        private void ChkAnimations_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable animations
        }

        private void ChkTransparency_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable transparency
        }

        private void ChkTransparency_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable transparency
        }

        private void ChkBestPerformance_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Set best performance
        }

        private void ChkBestPerformance_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Revert visual effects
        }

        private void ChkDNS_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Set Cloudflare DNS
        }

        private void ChkDNS_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Reset DNS
        }

        private void ChkTCP_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Optimize TCP/IP
        }

        private void ChkTCP_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Reset TCP/IP
        }

        private void ChkThrottle_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable throttling
        }

        private void ChkThrottle_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable throttling
        }

        private void ChkGPUScheduling_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable GPU scheduling
        }

        private void ChkGPUScheduling_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable GPU scheduling
        }

        private void ChkFullscreen_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable fullscreen optimizations
        }

        private void ChkFullscreen_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable fullscreen optimizations
        }

        private void ChkMSI_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable MSI mode
        }

        private void ChkMSI_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable MSI mode
        }

        private void ChkSuperfetch_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable Superfetch
        }

        private void ChkSuperfetch_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable Superfetch
        }

        private void ChkPageFile_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Optimize page file
        }

        private void ChkPageFile_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Reset page file
        }

        private void ChkIndexing_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable indexing
        }

        private void ChkIndexing_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable indexing
        }

        private void ChkTRIM_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable TRIM
        }

        private void ChkTRIM_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable TRIM
        }

        private void ChkTelemetry_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable telemetry
        }

        private void ChkTelemetry_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable telemetry
        }

        private void ChkBackgroundApps_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable background apps
        }

        private void ChkBackgroundApps_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable background apps
        }

        private void ChkCortana_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable Cortana
        }

        private void ChkCortana_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable Cortana
        }

        private void ChkTips_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable tips
        }

        private void ChkTips_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable tips
        }

        private void ChkCPUPriority_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Optimize CPU priority
        }

        private void ChkCPUPriority_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Reset CPU priority
        }

        private void ChkNagle_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable Nagle's algorithm
        }

        private void ChkNagle_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable Nagle's algorithm
        }

        private void ChkTimer_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable high precision timer
        }

        private void ChkTimer_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable high precision timer
        }

        private void ChkCoreParking_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Disable core parking
        }

        private void ChkCoreParking_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Enable core parking
        }
    }
}