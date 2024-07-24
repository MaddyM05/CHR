using System;
using Windows.UI.Xaml.Data;

namespace Chorus.WindowsReader.Common.Converters
{
    public class ObjectToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var data = value as (Payload, string)?;
            if (data != null)
            {
                if (data.Value.Item1 != null)
                {
                    return data.Value.Item2;
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
    