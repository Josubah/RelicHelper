using System;
using System.Globalization;
using System.Windows.Data;
using RelicHelper.Profiles;

namespace RelicHelper.UI
{
    internal class ProfileCounterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return $"{intValue} / {ProfileManager.MaxProfileCount}";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
