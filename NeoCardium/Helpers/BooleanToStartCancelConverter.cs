using Microsoft.UI.Xaml.Data;
using System;

namespace NeoCardium.Helpers {
    public class BooleanToStartCancelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? "Abbrechen" : "Starten";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}