using System;
using System.Windows.Threading;
using RelicHelper.Properties;

namespace RelicHelper
{
    internal enum CustomTimerState
    {
        Ready,
        Countdown,
        End
    }

    internal class CustomTimer
    {
        private DispatcherTimer? _timer;
        public CustomTimerState State { get; private set; } = CustomTimerState.Ready;
        public int TargetSeconds { get; private set; } = 0;
        public TimeSpan CurrentInterval { get; private set; } = TimeSpan.Zero;
        public string TimeString => string.Format("{0:00}:{1:00}:{2:00}", (int)CurrentInterval.TotalHours, CurrentInterval.Minutes, CurrentInterval.Seconds);

        public EventHandler? Tick { get; set; }

        public void Start(string input)
        {
            string[] parts = input.Split(':');
            int hours = int.Parse(parts[0]);
            int minutes = int.Parse(parts[1]);
            int seconds = int.Parse(parts[2]);

            TargetSeconds = hours * 3600 + minutes * 60 + seconds;
            CurrentInterval = TimeSpan.FromSeconds(TargetSeconds);
            State = CustomTimerState.Countdown;
            _timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                CurrentInterval = CurrentInterval.Subtract(TimeSpan.FromSeconds(1));

                if (CurrentInterval == TimeSpan.Zero)
                {
                    State = CustomTimerState.End;
                    _timer?.Stop();
                }

                Tick?.Invoke(this, new EventArgs());
            }, App.Current.Dispatcher);
        }

        public void Reset()
        {
            _timer?.Stop();
            TargetSeconds = 0;
            CurrentInterval = TimeSpan.Zero;
            State = CustomTimerState.Ready;
        }
    }
}
