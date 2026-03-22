using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace RelicHelper.Profiles
{
    [Serializable]
    public class Profile : INotifyPropertyChanged
    {
        public const int ProfileNameMaxLength = 40;

        [XmlIgnore]
        private string nameValue = string.Empty;

        [XmlAttribute]
        public string Name
        {
            get => nameValue;
            set
            {
                if (value != nameValue)
                {
                    nameValue = value;
                    NotifyPropertyChanged(nameof(Name));
                }
            }
        }

        [XmlAttribute]
        public byte CfgId { get; set; }
        [XmlAttribute]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [XmlAttribute]
        public DateTime LastUpdatedAt { get; set; } = DateTime.Now;

        [XmlIgnore]
        public string CfgFile { get => $"{CfgId}.cfg"; }
        [XmlIgnore]
        public string CfgPath { get => $@"{ProfileManager.ProfilesDirectory}/{CfgFile}"; }

        [XmlAttribute]
        public bool StatsVisible { get; set; } = true;
        [XmlAttribute]
        public bool TimerVisible { get; set; } = true;
        [XmlAttribute]
        public bool ExhaustVisible { get; set; } = true;
        [XmlAttribute]
        public bool SpellVisible { get; set; } = true;
        [XmlAttribute]
        public bool SearchVisible { get; set; } = true;
        [XmlAttribute]
        public bool LinksVisible { get; set; } = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
