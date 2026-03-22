using System;
using System.Windows.Input;

namespace RelicHelper
{
    public class SpellBookEntry
    {
        public string Name { get; set; } = "New Spell";
        public Key Hotkey { get; set; } = Key.None;
        public double DurationSeconds { get; set; } = 2.0;
        public bool IsEnabled { get; set; } = true;

        public SpellBookEntry() { }

        public SpellBookEntry(string name, Key hotkey, double duration)
        {
            Name = name;
            Hotkey = hotkey;
            DurationSeconds = duration;
        }
    }
}
