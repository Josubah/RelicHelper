using System;
using System.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using RelicHelper.Clients;

namespace RelicHelper
{
    public partial class GameClientOverlayWindow : Window
    {
        private readonly System.Windows.Media.Color _redBarColor = System.Windows.Media.Color.FromArgb(0xFF, 0xFF, 0x00, 0x00);
        private readonly System.Windows.Media.Color _greenBarColor = System.Windows.Media.Color.FromArgb(0xFF, 0x00, 0xC0, 0x00);
        private readonly System.Windows.Media.Color _yellowBarColor = System.Windows.Media.Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00); // Gold-ish Yellow

        private DispatcherTimer _playerStatsTimer;
        private DispatcherTimer _windowTimer;
        private DispatcherTimer _exhaustionMonitorTimer;
        private ExperienceCalculator? _experienceCalculator;
        private PingMeter _pingMeter;
        private PingMeter2 _pingMeter2;
        private CustomTimer _customTimer;
        private ExhaustionTimer _exhaustionTimer;
        private SpellTimer _spellTimer;
        private MouseHook _mouseHook;
        private KeyboardHook _keyboardHook;
        private DateTime _lastSpellHotkeyTime = DateTime.MinValue;
        private System.Collections.Generic.List<SpellBookEntry> _spellHotkeyEntries = new System.Collections.Generic.List<SpellBookEntry>();

        public GameClientOverlayWindow()
        {
            _pingMeter = new PingMeter();
            _pingMeter2 = new PingMeter2();
            _customTimer = new CustomTimer();
            _customTimer.Tick += CustomTimerTick;
            _exhaustionTimer = new ExhaustionTimer();
            _exhaustionTimer.Tick += (s, e) => {
                ExhaustionProgressBar.Value = _exhaustionTimer.Progress * 100;
            };
            _exhaustionTimer.Completed += (s, e) => {
                ExhaustionProgressBar.Value = 0;
            };
            _spellTimer = new SpellTimer();
            _spellTimer.Tick += (s, e) => {
            };
            _spellTimer.Completed += (s, e) => {
            };

            _mouseHook = new MouseHook();
            _mouseHook.LeftClick += (s, p) => {
                var gameClient = App.GameClient;
                if (gameClient == null || gameClient.Window == null || !gameClient.Window.IsActive) return;

                if (Properties.Settings.Default.ExhaustionTimerEnabled && _isMouseCrosshairActive)
                {
                    _exhaustionTimer.Start();
                    _isMouseCrosshairActive = false;
                }
            };
            _mouseHook.RightClick += (s, p) => {
                var gameClient = App.GameClient;
                if (gameClient == null || gameClient.Window == null || !gameClient.Window.IsActive) return;
                
                if (Properties.Settings.Default.ExhaustionTimerEnabled)
                {
                    // Check if the click was in the right-side panel (Inventory/Battle/Rune area)
                    var windowRect = gameClient.Window.GetRect();
                    int rightBarWidth = 180; // Estimated width of Tibia sidebars
                    bool isInRightPanel = p.X > (windowRect.Right - rightBarWidth - 25); // 25px margin for border

                    if (isInRightPanel)
                    {
                        _isMouseCrosshairActive = true;
                        _crosshairStartTime = DateTime.Now;
                    }
                }
            };

            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyDown += KeyboardHook_KeyDown;

            LoadSpellHotkeyEntries();

            InitializeComponent();
            CustomTimerTextBox.Text = Properties.Settings.Default.TimerInput;
            UpdateToggleExhaustionButtonText();
            UpdateToggleSpellTimerButtonText();

            VersionLabel.Content = $"v{App.Version}";
            ApplySectionVisibility();

            RefreshWindowState();

            _windowTimer = new DispatcherTimer();
            _windowTimer.Interval = TimeSpan.FromMilliseconds(250);
            _windowTimer.Tick += (sender, e) => RefreshWindowState();
            _windowTimer.Start();

            _playerStatsTimer = new DispatcherTimer();
            _playerStatsTimer.Interval = TimeSpan.FromSeconds(1);
            _playerStatsTimer.Tick += async (sender, e) => await RefreshPlayerStats();
            _playerStatsTimer.Start();

            _exhaustionMonitorTimer = new DispatcherTimer();
            _exhaustionMonitorTimer.Interval = TimeSpan.FromMilliseconds(50);
            _exhaustionMonitorTimer.Tick += async (sender, e) => await CheckExhaustion();
            if (Properties.Settings.Default.ExhaustionTimerEnabled)
            {
                _exhaustionMonitorTimer.Start();
                _mouseHook.Start();
            }

            if (Properties.Settings.Default.SpellTimerEnabled)
            {
                _exhaustionMonitorTimer.Start();
                _keyboardHook.Start();
            }
        }
        

        private bool _isActionPending = false;
        private bool _isMouseCrosshairActive = false;
        private DateTime _pendingActionStartTime = DateTime.MinValue;

        private DateTime _crosshairStartTime = DateTime.MinValue;

        private bool IsCursorChanged()
        {
            // Abandoning this check for now as it's too unreliable on user's system
            return false;
        }

        private async Task CheckExhaustion()
        {
            var gameClient = App.GameClient;
            if (gameClient == null) return;

            // Track crosshair/prime state timeout (5.0s max wait for the follow-up left click)
            if (_isMouseCrosshairActive)
            {
                if ((DateTime.Now - _crosshairStartTime).TotalSeconds > 5.0)
                {
                    _isMouseCrosshairActive = false;
                }
            }

            // Standalone Spell Cooldown Timer (18s one) - still uses OCR
            if (Properties.Settings.Default.SpellTimerEnabled && !_spellTimer.IsActive)
            {
                var gameWorldBitmap = gameClient.Window?.CaptureGameWorld();
                if (gameWorldBitmap != null)
                {
                    bool spellDetected = await Task.Run(() => new ImageProcessor().DetectSpellText(gameWorldBitmap));
                    if (spellDetected)
                    {
                        _spellTimer.Start();
                    }
                    gameWorldBitmap.Dispose();
                }
            }
        }

        private void ToggleExhaustionButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ExhaustionTimerEnabled = !Properties.Settings.Default.ExhaustionTimerEnabled;
            Properties.Settings.Default.Save();
            
            UpdateToggleExhaustionButtonText();

            if (Properties.Settings.Default.ExhaustionTimerEnabled)
            {
                _exhaustionMonitorTimer.Start();
                _mouseHook.Start();
            }
            else
            {
                _exhaustionMonitorTimer.Stop();
                _mouseHook.Stop();
                _exhaustionTimer.Stop();
                ExhaustionProgressBar.Value = 0;
            }
        }

        private void UpdateToggleExhaustionButtonText()
        {
            ToggleExhaustionButton.Content = Properties.Settings.Default.ExhaustionTimerEnabled 
                ? "Disable Exhaust Alert" 
                : "Enable Exhaust Alert";
        }

        private void UpdateToggleSpellTimerButtonText()
        {
            ToggleSpellTimerButton.Content = Properties.Settings.Default.SpellTimerEnabled 
                ? "Disable Timer Spell" 
                : "Enable Timer Spell";
        }

        private void ToggleSpellTimerButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SpellTimerEnabled = !Properties.Settings.Default.SpellTimerEnabled;
            Properties.Settings.Default.Save();
            
            UpdateToggleSpellTimerButtonText();

            if (Properties.Settings.Default.SpellTimerEnabled)
            {
                _exhaustionMonitorTimer.Start();
                _keyboardHook.Start();
            }
            else
            {
                _keyboardHook.Stop();
                _spellTimer.Stop();
                CooldownsContainer.Children.Clear();
            }
        }

        private void LoadSpellHotkeyEntries()
        {
            try
            {
                string json = Properties.Settings.Default.SpellBookData;
                if (!string.IsNullOrEmpty(json))
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<SpellBookEntry>>(json);
                    if (list != null) _spellHotkeyEntries = list.Where(s => s.IsEnabled && s.Hotkey != Key.None).ToList();
                }
            }
            catch { _spellHotkeyEntries = new System.Collections.Generic.List<SpellBookEntry>(); }
        }

        private void KeyboardHook_KeyDown(object? sender, Key key)
        {
            // Only trigger if Tibia is the active window
            var gameClient = App.GameClient;
            if (gameClient == null || gameClient.Window == null || !gameClient.Window.IsActive) return;

            var entry = _spellHotkeyEntries.FirstOrDefault(s => s.Hotkey == key);
            if (entry != null)
            {
                _lastSpellHotkeyTime = DateTime.Now;
                
                // For hotkeys, we check if they triggered a crosshair or immediate effect
                // Rune Hotkey: Prime the system for the next click (heuristically assuming 2s duration = rune)
                if (entry.DurationSeconds <= 2.1)
                {
                    _isMouseCrosshairActive = true;
                    _crosshairStartTime = DateTime.Now;
                }
                else
                {
                    // Instant Hotkey (Spell): Optional, user said "not for hotkey" but let's keep basic logic
                    // If we want it to work for spells too, we can trigger Start() here
                }

                Dispatcher.Invoke(() => StartDynamicCooldown(entry.Name, entry.DurationSeconds));
            }
        }

        private void StartDynamicCooldown(string name, double seconds)
        {
            // Look for existing bar for this spell to refresh it
            UIElement? existing = null;
            foreach (UIElement child in CooldownsContainer.Children)
            {
                if (child is StackPanel sp && sp.Tag?.ToString() == name)
                {
                    existing = child;
                    break;
                }
            }
            
            if (existing != null)
            {
                CooldownsContainer.Children.Remove(existing);
            }

            // Increased margin (0, 5, 0, 5) as requested
            var container = new StackPanel { Tag = name, Margin = new Thickness(0, 5, 0, 5) };
            var label = new System.Windows.Controls.Label { 
                Content = $"{name} ({seconds}s)", 
                Foreground = System.Windows.Media.Brushes.White, 
                FontSize = 10, 
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var bar = new System.Windows.Controls.ProgressBar { 
                Height = 4, 
                Width = 238, 
                Maximum = 100, 
                Value = 100,
                Foreground = new SolidColorBrush(_greenBarColor), // Defaults to green
                Background = System.Windows.Media.Brushes.Transparent,
                BorderBrush = System.Windows.Media.Brushes.Black
            };

            container.Children.Add(label);
            container.Children.Add(bar);
            CooldownsContainer.Children.Insert(0, container);

            // Timer to animate and remove
            var startTime = DateTime.Now;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            timer.Tick += (s, e) => {
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                if (elapsed >= seconds)
                {
                    timer.Stop();
                    CooldownsContainer.Children.Remove(container);
                }
                else
                {
                    double remainingPercent = (1 - (elapsed / seconds));
                    bar.Value = remainingPercent * 100;
                    
                    // Dynamic color transition
                    if (remainingPercent > 0.7) // More than 70% left
                    {
                        bar.Foreground = new SolidColorBrush(_greenBarColor);
                    }
                    else if (remainingPercent > 0.3) // Between 30% and 70% left
                    {
                        bar.Foreground = new SolidColorBrush(_yellowBarColor);
                    }
                    else // Less than 30% left
                    {
                        bar.Foreground = new SolidColorBrush(_redBarColor);
                    }
                }
            };
            timer.Start();
        }

        private void SpellBookButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new SpellBookWindow();
            win.Owner = this; // Bind to main overlay
            if (win.ShowDialog() == true)
            {
                LoadSpellHotkeyEntries();
            }
        }

        private bool IsAppActive()
        {
            var activatedHandle = WinApi.GetForegroundWindow();

            if (activatedHandle == IntPtr.Zero)
                return false;

            foreach (Window win in Application.Current.Windows)
            {
                try
                {
                    IntPtr handle = new WindowInteropHelper(win).Handle;
                    if (handle == activatedHandle) return true;
                }
                catch { }
            }

            return false;
        }

        private void RefreshWindowState()
        {
            try
            {
                GameClient? gameClient = App.GameClient;
                if (gameClient == null)
                {
                    WindowState = WindowState.Minimized;
                    return;
                }
                ClientWindow? window = gameClient.Window;

                var newWindowState = IsAppActive() || (window != null && window.IsActive) ? WindowState.Normal : WindowState.Minimized;
                var windowRect = window?.GetRect();

                if (windowRect != null && windowRect.Value != Rectangle.Empty)
                {
                    if (windowRect.Value.Width < 1000)
                        newWindowState = WindowState.Minimized;

                    Left = windowRect.Value.Left + 12;
                    Top = windowRect.Value.Top + 50;
                    Width = windowRect.Value.Width * 0.11;
                }

                if (WindowState != newWindowState)
                {
                    WindowState = newWindowState;
                    if (newWindowState == WindowState.Normal && Keyboard.FocusedElement != CharacterSearchTextBox)
                        window?.Activate();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RefreshWindowState: {ex.Message}");
            }
        }

        private async Task RefreshPlayerStats()
        {
            try
            {
                var gameClient = App.GameClient;
                if (gameClient == null)
                {
                    if (_experienceCalculator != null)
                        _experienceCalculator = null;

                    return;
                }

                int? experience = await gameClient.GetPlayerExperience();

                _experienceCalculator?.Tick(experience);

                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher == null) return;

                dispatcher.Invoke(() =>
                {
                    PingLabel.Content = string.Format("{0} ms", _pingMeter.CurrentPing > 1000 ? ">1000" : _pingMeter.CurrentPing);
                    PingLabel2.Content = string.Format("{0} ms", _pingMeter2.CurrentPing > 1000 ? ">1000" : _pingMeter2.CurrentPing);
                    LevelLabel.Content = _experienceCalculator?.ExperienceStats.Level?.ToString() ?? "-";
                    ExperienceLabel.Content = FormatExperience(_experienceCalculator?.ExperienceStats.Experience);
                    LevelProgressBar.Minimum = _experienceCalculator?.ExperienceStats.ExperienceForLevel ?? 0;
                    LevelProgressBar.Maximum = _experienceCalculator?.ExperienceStats.ExperienceForNextLevel ?? 1;
                    LevelProgressBar.Value = _experienceCalculator?.ExperienceStats.Experience ?? 0;
                    ExperienceRemainingLabel.Content = FormatExperience(_experienceCalculator?.ExperienceStats.RemainingExperience);
                    ExperiencePerHourLabel.Content = FormatExperience(_experienceCalculator?.ExperienceStats.ExperiencePerHour);

                    int hours = _experienceCalculator?.ExperienceStats.RemainingTotalMinutes >= 60 ? (int)Math.Floor((double)_experienceCalculator!.ExperienceStats.RemainingTotalMinutes / 60) : 0;
                    int minutes = (_experienceCalculator?.ExperienceStats.RemainingTotalMinutes ?? 0) % 60;
                    var remaingTime = string.Empty;

                    if (hours > 0)
                        remaingTime = hours + "h ";

                    remaingTime += minutes + "m";

                    if (hours + minutes == 0)
                        remaingTime = "< 1m";

                    LevelTimeRemainingLabel.Content = _experienceCalculator?.ExperienceStats.RemainingTotalMinutes != null ? remaingTime : "-";
                    LevelUpTimeLabel.Content = _experienceCalculator?.ExperienceStats.EstimatedAdvanceTime?.ToShortTimeString() ?? "-";
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RefreshPlayerStats: {ex.Message}");
            }
        }

        private string FormatExperience(int? experience) => experience?.ToString("N0") ?? "-";

        private void ToggleCounterButton_Click(object sender, RoutedEventArgs e)
        {
            bool newState = _experienceCalculator == null;

            Application.Current.Dispatcher.Invoke(async () =>
            {
                await SetExperienceCounterActive(newState);
            });
        }

        private async Task SetExperienceCounterActive(bool active)
        {
            var gameClient = App.GameClient;

            if (gameClient == null)
                active = false;

            int? experience = active ? await gameClient!.GetPlayerExperience() : null;

            _experienceCalculator = active ? new ExperienceCalculator(experience) : null;
            ToggleCounterButton.Content = active ? "Stop counter" : "Start counter";
            ResetCounterButton.IsEnabled = active;
        }

        private void ResetCounterButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                await SetExperienceCounterActive(false);
                await RefreshPlayerStats();
                await SetExperienceCounterActive(true);
            });
        }

        private void CharacterSearchButton_Click(object sender, RoutedEventArgs e)
        {
            _ = CharacterSearchSubmit();
        }

        private void InfoMapViewerButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://TibiaRelic.com");
        }

        private void NetMapViewerButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://opentibia.info/library/map/tibiarelic#32369,32241,7,7");
        }

        private void TibiaRelicInfoButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://TibiaRelic.wiki");
        }

        private void TibiaRelicXyzButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://opentibia.info");
        }

        private void OpenUrl(string url)
        {
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = url;
                    p.StartInfo.UseShellExecute = true;
                    p.Start();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening URL {url}: {ex.Message}");
            }
        }

        private void ApplySectionVisibility()
        {
            var profile = App.GameClient?.Profile;
            if (profile == null) return;

            SetVisibility(StatsContent, profile.StatsVisible);
            SetVisibility(TimerContent, profile.TimerVisible);
            SetVisibility(ExhaustContent, profile.ExhaustVisible);
            SetVisibility(SpellContent, profile.SpellVisible);
            SetVisibility(SearchContent, profile.SearchVisible);
            SetVisibility(LinksContent, profile.LinksVisible);

            // Update button texts based on visibility
            if (this.Content is Viewbox viewbox && viewbox.Child is StackPanel rootStack)
            {
                foreach (var sectionStack in rootStack.Children.OfType<StackPanel>())
                {
                    var headerGrid = sectionStack.Children.OfType<Grid>().FirstOrDefault();
                    if (headerGrid != null)
                    {
                        var toggleBtn = headerGrid.Children.OfType<Button>().FirstOrDefault(b => b.Tag != null);
                        if (toggleBtn != null && toggleBtn.Tag is string targetName)
                        {
                            var target = this.FindName(targetName) as FrameworkElement;
                            if (target != null)
                            {
                                toggleBtn.Content = target.Visibility == Visibility.Visible ? "[-]" : "[+]";
                            }
                        }
                    }
                }
            }
        }

        private void SetVisibility(FrameworkElement element, bool visible)
        {
            if (element != null)
                element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ToggleSection_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string targetName)
            {
                var target = this.FindName(targetName) as FrameworkElement;
                if (target != null)
                {
                    bool isBecomingHidden = target.Visibility == Visibility.Visible;
                    target.Visibility = isBecomingHidden ? Visibility.Collapsed : Visibility.Visible;
                    btn.Content = isBecomingHidden ? "[+]" : "[-]";

                    // Save to profile
                    var profile = App.GameClient?.Profile;
                    if (profile != null)
                    {
                        switch (targetName)
                        {
                            case "StatsContent": profile.StatsVisible = !isBecomingHidden; break;
                            case "TimerContent": profile.TimerVisible = !isBecomingHidden; break;
                            case "ExhaustContent": profile.ExhaustVisible = !isBecomingHidden; break;
                            case "SpellContent": profile.SpellVisible = !isBecomingHidden; break;
                            case "SearchContent": profile.SearchVisible = !isBecomingHidden; break;
                            case "LinksContent": profile.LinksVisible = !isBecomingHidden; break;
                        }
                        Profiles.ProfileManager.Instance.SaveProfiles();
                    }
                }
            }
        }

        private void OverlayWindow_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Keyboard.FocusedElement != CharacterSearchTextBox)
                {
                    App.GameClient?.Window?.Activate();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OverlayWindow_GotFocus: {ex.Message}");
            }
        }

        private async Task CharacterSearchSubmit()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(CharacterSearchTextBox.Text))
                {
                    string encodedName = HttpUtility.UrlEncode(CharacterSearchTextBox.Text.ToLower());
                    OpenUrl($"https://www.tibiarelic.com/characters/{encodedName}");
                    await Task.Delay(1000);
                    CharacterSearchTextBox.Clear();
                }

                App.GameClient?.Window?.Activate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CharacterSearchSubmit: {ex.Message}");
            }
        }

        private void CharacterSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _ = CharacterSearchSubmit();
            }

            if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9 ||
                e.Key >= Key.F1 && e.Key <= Key.F12 ||
                e.Key >= Key.Left && e.Key <= Key.Down ||
                e.Key == Key.Escape)
            {
                App.GameClient?.Window?.Activate();
            }
        }

        private void CustomTimerTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CustomTimerButton.IsEnabled = _customTimer?.State != CustomTimerState.Ready || CustomTimerTextBox.Text != "00:00:00";
        }

        private void CharacterSearchTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CharacterSearchTextBox.Clear();
        }

        private void CustomTimerButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleTimer();
        }

        private void ToggleTimer()
        {
            if (_customTimer.State == CustomTimerState.Ready && (string.IsNullOrEmpty(CustomTimerTextBox.Text) || CustomTimerTextBox.Text == "00:00:00"))
                return;

            if (_customTimer.State == CustomTimerState.Ready)
            {
                Properties.Settings.Default.TimerInput = CustomTimerTextBox.Text;
                Properties.Settings.Default.Save();
                _customTimer.Start(CustomTimerTextBox.Text);
                CustomTimerButton.Content = "Reset";
                CustomTimerBar.Foreground = new SolidColorBrush(_redBarColor);
                CustomTimerBar.Value = 0;
                CustomTimerBar.Maximum = _customTimer.TargetSeconds;
                return;
            }

            _customTimer.Reset();
            CustomTimerTextBox.Text = Properties.Settings.Default.TimerInput;
            CustomTimerButton.Content = "Start";
            CustomTimerBar.Value = 0;
            CustomTimerBar.Foreground = new SolidColorBrush(_redBarColor);
            CustomTimerBar.Maximum = 1;
        }

        private void CustomTimerTick(object? sender, EventArgs e)
        {
            CustomTimerBar.Value = _customTimer.TargetSeconds - _customTimer.CurrentInterval.TotalSeconds;
            CustomTimerTextBox.Text = _customTimer.TimeString;
            var barColor = _customTimer.State == CustomTimerState.End ? _greenBarColor : _redBarColor;
            CustomTimerBar.Foreground = new SolidColorBrush(barColor);
        }

        private void CustomTimerTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CustomTimerTextBox.Clear();
        }

        private void CustomTimerTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CustomTimerTextBox.Text = CustomTimerTextBox.Text.Replace('_', '0');
                ToggleTimer();
            }
        }

        private void CustomTimerTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CustomTimerTextBox.Text = CustomTimerTextBox.Text.Replace('_', '0');
        }

        private void CharacterSearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

    }
}
