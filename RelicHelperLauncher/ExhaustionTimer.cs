using System;
using System.Windows.Threading;

namespace RelicHelper
{
    internal class ExhaustionTimer
    {
        private DispatcherTimer? _timer;
        private DateTime _startTime;
        private const double DurationSeconds = 2.0;

        public bool IsActive { get; private set; } = false;
        public double RemainingSeconds { get; private set; } = 0;
        public double Progress => Math.Max(0, Math.Min(1, RemainingSeconds / DurationSeconds));

        public EventHandler? Tick { get; set; }
        public EventHandler? Completed { get; set; }

        public void Start()
        {
            Stop();

            IsActive = true;
            _startTime = DateTime.Now;
            RemainingSeconds = DurationSeconds;

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Normal, (s, e) =>
            {
                var elapsed = (DateTime.Now - _startTime).TotalSeconds;
                RemainingSeconds = Math.Max(0, DurationSeconds - elapsed);

                Tick?.Invoke(this, EventArgs.Empty);

                if (RemainingSeconds <= 0)
                {
                    Stop();
                    Completed?.Invoke(this, EventArgs.Empty);
                }
            }, App.Current.Dispatcher);
            
            _timer.Start();
        }

        public void Stop()
        {
            IsActive = false;
            RemainingSeconds = 0;
            _timer?.Stop();
            _timer = null;
        }
    }
}
