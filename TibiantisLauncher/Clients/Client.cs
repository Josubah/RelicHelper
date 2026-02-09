using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RelicHelper.Clients
{
    internal abstract class Client : IDisposable
    {
        public delegate void ClientExitEventHandler(object sender, EventArgs e);

        public static string ClientDirectoryFullPath => Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty;
        protected abstract string _clientFullPath { get; }
        //protected ProcessMemory? _memory { get; set; }
        protected ClientWindow? _window;
        protected Process _process { get; init; }
        public ClientWindow? Window => _window;
        public event ClientExitEventHandler? Exit;

        protected Client()
        {
            _process = new Process();

            var clientDir = ClientDirectoryFullPath;

            if (!clientDir.EndsWith(@"\"))
                clientDir += @"\";

            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.WorkingDirectory = @$"{clientDir}";
            _process.StartInfo.FileName = _clientFullPath;
            _process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            _process.StartInfo.CreateNoWindow = true;
            _process.EnableRaisingEvents = true;
            _process.Exited += OnExit;
        }

        protected void OnExit(object? sender, EventArgs e)
        {
            Exit?.Invoke(this, e);
        }

        public virtual void Start()
        {
            _process.Start();
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
