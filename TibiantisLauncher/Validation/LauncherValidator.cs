using System.Diagnostics;

namespace RelicHelper.Validation
{
    internal class LauncherValidator
    {
        public static void ValidateLauncherNotRunning()
        {
            Process[] processList = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);

            if (processList.Length >= 2)
                throw new ValidationException("Another Relic Helper instance is already running.");
        }
    }
}
