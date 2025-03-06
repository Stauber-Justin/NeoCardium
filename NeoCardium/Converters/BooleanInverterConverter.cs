using System;
using Microsoft.UI.Xaml.Data;
using NeoCardium.Helpers;

namespace NeoCardium.Converters
{
    public class BooleanInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
                return !boolValue;

            ExceptionHelper.LogError($"BooleanInverterConverter erhielt ungültigen Wert: {value}");
            return true; // Sicherer Fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
                return !boolValue;

            ExceptionHelper.LogError("BooleanInverterConverter: ConvertBack erhielt ungültigen Wert.");
            return false;
        }
    }
}