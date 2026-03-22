using System.IO;

namespace RelicHelper.Clients
{
    internal class CamPlayer : Client
    {
        public const string FileName = "CamPlayer.exe";
        protected override string _clientFullPath => ClientFullPath;
        public static string ClientFullPath => Path.Combine(ClientDirectoryFullPath, FileName);
    }
}
