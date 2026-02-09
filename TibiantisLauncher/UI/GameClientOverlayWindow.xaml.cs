using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using RelicHelper.Clients;

namespace RelicHelper
{
    public partial class GameClientOverlayWindow : Window
    {
        private readonly Color _redBarColor = Color.FromArgb(0xFF, 0xFF, 0x00, 0x00);
        private readonly Color _greenBarColor = Color.FromArgb(0xFF, 0x00, 0xC0, 0x00);

        private DispatcherTimer _playerStatsTimer;
        private DispatcherTimer _windowTimer;
        private ExperienceCalculator? _experienceCalculator;
        private PingMeter _pingMeter;
        private PingMeter2 _pingMeter2;
        private CustomTimer _customTimer;

        public GameClientOverlayWindow()
        {
            _pingMeter = new PingMeter();
            _pingMeter2 = new PingMeter2();
            _customTimer = new CustomTimer();
            _customTimer.Tick += CustomTimerTick;

            InitializeComponent();
            CustomTimerTextBox.Text = Properties.Settings.Default.TimerInput;

            VersionLabel.Content = $"v{App.Version}";

            RefreshWindowState();

            _windowTimer = new DispatcherTimer();
            _windowTimer.Interval = TimeSpan.FromMilliseconds(250);
            _windowTimer.Tick += (sender, e) => RefreshWindowState();
            _windowTimer.Start();

            _playerStatsTimer = new DispatcherTimer();
            _playerStatsTimer.Interval = TimeSpan.FromSeconds(1);
            _playerStatsTimer.Tick += async (sender, e) => await RefreshPlayerStats();
            _playerStatsTimer.Start();
        }

        private bool IsWindowActive()
        {
            var activatedHandle = WinApi.GetForegroundWindow();

            if (activatedHandle == IntPtr.Zero)
                return false;

            IntPtr windowHandle = new WindowInteropHelper(this).Handle;

            return windowHandle == activatedHandle;
        }

        private void RefreshWindowState()
        {
            GameClient gameClient = App.GameClient!;
            if (gameClient == null)
            {
                WindowState = WindowState.Minimized;
                return;
            }
            ClientWindow? window = gameClient.Window;

            var newWindowState = IsWindowActive() || window != null && window.IsActive ? WindowState.Normal : WindowState.Minimized;
            var windowRect = window?.GetRect();

            if (windowRect != null)
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

        private async Task RefreshPlayerStats()
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

            Application.Current.Dispatcher.Invoke(() =>
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
            CharacterSearchSubmit();
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
            using (Process p = new Process())
            {
                p.StartInfo.FileName = url;
                p.StartInfo.UseShellExecute = true;
                p.Start();
            }
        }

        private void OverlayWindow_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Keyboard.FocusedElement != CharacterSearchTextBox)
            {
                App.GameClient?.Window?.Activate();
            }
        }

        private void CharacterSearchSubmit()
        {
            if (!string.IsNullOrWhiteSpace(CharacterSearchTextBox.Text))
            {
                string encodedName = HttpUtility.UrlEncode(CharacterSearchTextBox.Text.ToLower());
                OpenUrl($"https://www.tibiarelic.com/characters/{encodedName}");
                Task.Delay(1000).Wait();
                CharacterSearchTextBox.Clear();
            }

            App.GameClient?.Window?.Activate();
        }

        private void CharacterSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CharacterSearchSubmit();
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
