using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Chorus.WindowsReader.Common.Converters
{
    public class BoolToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null)
            {
                return Visibility.Collapsed;
            }
            var boolValue = System.Convert.ToBoolean(value);
            if ((string)parameter == "1")
            {
                if (boolValue)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
            if (boolValue)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is null)
            {
                return false;
            }
            var visibility = (Visibility)value;
            if ((string)parameter == "1")
            {
                return visibility == Visibility.Collapsed;
            }
            return visibility == Visibility.Visible;
        }
    }
}
