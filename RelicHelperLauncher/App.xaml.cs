using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using RelicHelper.Clients;
using RelicHelper.Profiles;
using RelicHelper.Validation;




namespace RelicHelper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static GameClient? GameClient { get; set; }
        internal static CamPlayer? CamPlayer { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var libsDir = Path.Combine(baseDir, "libs");

            if (Directory.Exists(libsDir))
            {
                var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                Environment.SetEnvironmentVariable("PATH", path + ";" + libsDir);
            }

            base.OnStartup(e);
        }

        public App()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        public static string? Version { get => Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString(3); }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string logFilePath = "RelicHelper.log";
            Trace.Listeners.Add(new TextWriterTraceListener(logFilePath));
            File.CreateText(logFilePath);

            ShutdownMode = ShutdownMode.OnLastWindowClose;

            try
            {
                LauncherValidator.ValidateLauncherNotRunning();
                GameClientValidator.ValidateClientExistence();
                GameClientValidator.ValidateClientVersion();
                GameClientValidator.ValidateClientNotRunning();
                ProfileManager.Instance.CreateProfilesDirectory();
            }
            catch (ValidationException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(0x1);
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                Trace.TraceError($"Application terminated unexpectedly: {ex}");
                MessageBox.Show("Access denied while attempting to create profiles folder in client directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(0x5);
                return;
            }

            try
            {
                Current.MainWindow = new ProfileListWindow();
                if (GameClient != null)
                    GameClient.Exit += (sender, e) => Dispatcher.Invoke(Shutdown);
                Current.MainWindow.Show();
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Application terminated unexpectedly: {ex}");
                MessageBox.Show("Relic Helper terminated unexpectedly. Please report this issue to k.standarski@gmail.com.\r\nPlease remember to attach TibiaRelic-launcher.log file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(0x1);
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            GameClient?.UnsetConfig();
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Trace.TraceError($"Application terminated unexpectedly: {e.Exception}");
            MessageBox.Show("Relic Helper terminated unexpectedly. Please report this issue to k.standarski@gmail.com.\r\nPlease remember to attach TibiaRelic-launcher.log file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(0x1);
        }
    }
}
