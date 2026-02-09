using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RelicHelper
{
    internal class PingMeter2
    {
        private readonly string _host = "www.google.com";
        private int _currentPing = int.MaxValue;
        public int CurrentPing => _currentPing;

        public PingMeter2()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += async (sender, e) => await CheckPing();
            timer.Start();
        }

        public async Task CheckPing()
        {
            Ping ping = new Ping();
            PingReply pingReply = await ping.SendPingAsync(_host, 1200);

            if (pingReply.Status == IPStatus.Success)
            {
                _currentPing = (int)pingReply.RoundtripTime;
                return;
            }

            _currentPing = int.MaxValue;
        }
    }
}
