using System;
using System.Windows.Threading;

namespace RelicHelper
{
    public class SpellTimer
    {
        private DispatcherTimer _timer;
        private DateTime _startTime;
        private double _durationSeconds = 18.0;

        public event EventHandler? Tick;
        public event EventHandler? Completed;

        public bool IsActive => _timer.IsEnabled;
        public double Progress => IsActive 
            ? Math.Min(1.0, (DateTime.Now - _startTime).TotalSeconds / _durationSeconds) 
            : 0;

        public double RemainingSeconds => IsActive 
            ? Math.Max(0, _durationSeconds - (DateTime.Now - _startTime).TotalSeconds) 
            : 0;

        public SpellTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += (s, e) => {
                if ((DateTime.Now - _startTime).TotalSeconds >= _durationSeconds)
                {
                    Stop();
                    Completed?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Tick?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        public void Start()
        {
            _startTime = DateTime.Now;
            if (!_timer.IsEnabled)
                _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Reset()
        {
            Stop();
            Tick?.Invoke(this, EventArgs.Empty);
        }
    }
}
