using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using RelicHelper.Clients;
using RelicHelper.Profiles;
using RelicHelper.UI;
using RelicHelper.Validation;

namespace RelicHelper
{
    public partial class ProfileListWindow : Window
    {
        private readonly ProfileManager _profileManager;
        public ObservableCollection<Profile> Profiles => _profileManager.Profiles;

        public ProfileListWindow()
        {
            InitializeComponent();
            _profileManager = ProfileManager.Instance;

            VersionLabel.Content = $"v{App.Version}";
            WindowDragHandle.MouseLeftButtonDown += (sender, e) => DragMove();

            _profileManager.LoadProfiles();

            DataContext = this;
        }

        #region Callbacks
        private void StartClientButton_Click(object sender, RoutedEventArgs e)
        {
            StartGameClient();
        }

        private void StartCamPlayerButton_Click(object sender, RoutedEventArgs e)
        {
            StartCamPlayer();
        }

        private void MinimalizeIconButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAddProfileWindow();
        }

        private void AddProfileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowAddProfileWindow();
        }

        private void RenameProfileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowRenameProfileWindow();
        }

        private void RemoveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ShowRemoveProfileWindow();
        }

        private void RemoveProfileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowRemoveProfileWindow();
        }

        private void ProfileListBoxItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            StartGameClient();
        }

        private void ProfileListBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (ProfileListBox.SelectedIndex != -1)
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.F2:
                        ShowRenameProfileWindow();
                        break;
                    case System.Windows.Input.Key.Delete:
                        ShowRemoveProfileWindow();
                        break;
                    case System.Windows.Input.Key.Enter:
                        StartGameClient();
                        break;
                }
            }
        }
        #endregion

        private void StartGameClient()
        {
            try
            {
                GameClientValidator.ValidateClientExistence();
                GameClientValidator.ValidateClientVersion();
                GameClientValidator.ValidateClientNotRunning();
            }
            catch (ValidationException ex)
            {
                MessageBox.Show (ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            App.GameClient = new GameClient((Profile)ProfileListBox.SelectedItem);
            App.GameClient.Exit += (sender, e) => Dispatcher.Invoke(Application.Current.Shutdown);
            App.GameClient.Start();
            App.Current.MainWindow = new GameClientOverlayWindow();
            App.Current.MainWindow.Show();
            Thread.Sleep(1000);
            App.GameClient?.Window?.Activate();
            Close();
        }

        private void StartCamPlayer()
        {
            try
            {
                GameClientValidator.ValidateCamPlayerExistence();
                GameClientValidator.ValidateClientVersion();
                GameClientValidator.ValidateClientNotRunning();
            }
            catch (ValidationException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            App.CamPlayer = new CamPlayer();
            App.CamPlayer.Start();
            Thread.Sleep(1000);
            App.Current.MainWindow = new GameClientOverlayWindow();
            App.Current.MainWindow.Show();
            Application.Current.Shutdown();
        }

        private void ShowAddProfileWindow()
        {
            var window = new ProfileWindow() { Owner = this };
            var result = window.ShowDialog();

            if (result == true)
                _profileManager.AddProfile(window.ProfileName!);
        }

        private void ShowRenameProfileWindow()
        {
            var selectedProfile = (Profile)ProfileListBox.SelectedItem;
            if (selectedProfile is null)
                return;

            var window = new ProfileWindow() { ProfileName = selectedProfile.Name, Owner = this, Title = $"Rename Profile {selectedProfile.Name}" };
            var result = window.ShowDialog();

            if (result == true)
                _profileManager.RenameProfile(selectedProfile, window.ProfileName);
        }

        private void ShowRemoveProfileWindow()
        {
            var selectedProfile = (Profile)ProfileListBox.SelectedItem;
            if (selectedProfile is null)
                return;

            var window = new YesNoWindow() { Message = $"Are you sure you want to delete {selectedProfile.Name} profile?", Owner = this };
            var result = window.ShowDialog();

            if (result == true)
                _profileManager.RemoveProfile((Profile)ProfileListBox.SelectedItem);
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            ProfileListBox.Focus();
        }
    }
}
