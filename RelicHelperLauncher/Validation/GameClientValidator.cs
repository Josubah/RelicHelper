using System.Diagnostics;
using System.IO;
using RelicHelper.Clients;

namespace RelicHelper.Validation
{
    internal static class GameClientValidator
    {
        public static void ValidateClientExistence()
        {
            if (!File.Exists(GameClient.ClientFullPath))
                throw new ValidationException($"{GameClient.FileName} not found. Extract Relic Helper files directly into TibiaRelic client folder and try again.");
        }

        public static void ValidateCamPlayerExistence()
        {
            if (!File.Exists(CamPlayer.ClientFullPath))
                throw new ValidationException($"{CamPlayer.FileName} not found. Extract Relic Helper files directly into TibiaRelic client folder and try again.");
        }

        public static void ValidateClientNotRunning()
        {
            if (GameClient.FindClientProcess() != null)
                throw new ValidationException($"{GameClient.FileName} is already running. Please close TibiaRelic client and try again.");
        }

        public static void ValidateClientVersion()
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(GameClient.ClientFullPath);
            if (!string.IsNullOrEmpty(versionInfo.ProductVersion) && !versionInfo.ProductVersion.Equals(GameClient.ClientVersion, System.StringComparison.InvariantCultureIgnoreCase))
                throw new ValidationException($"Unsupported Client version detected. This launcher supports TibiaRelic Client v{GameClient.ClientVersion}.");
        }

        public static void ValidateCfgPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ValidationException($"Specified cfg path is empty.");
        }
    }
}
