using System.Linq;
using System.Windows;
using RelicHelper.Profiles;
using RelicHelper.Validation;

namespace RelicHelper.UI
{
    /// <summary>
    /// Interaction logic for ProfileWindow.xaml
    /// </summary>
    public partial class ProfileWindow : Window
    {
        public string? ProfileName { get; set; }
        private readonly string? startProfileName;
        public int ProfileNameMaxLength => Profile.ProfileNameMaxLength;

        public ProfileWindow()
        {
            startProfileName = ProfileName;
            InitializeComponent();

            DataContext = this;

            Loaded += (sender, e) => ProfileNameTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (startProfileName == ProfileName)
            {
                DialogResult = false;
                Close();
                return;
            }

            try
            {
                ProfileValidator.ValidateProfileName(ProfileName, ProfileManager.Instance.Profiles.Select(p => p.Name));
            }
            catch (ValidationException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
