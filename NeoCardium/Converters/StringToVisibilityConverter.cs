using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace NeoCardium.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool hasText = value is string str && !string.IsNullOrWhiteSpace(str);
            return hasText ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
