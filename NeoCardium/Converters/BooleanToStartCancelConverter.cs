using System;
using Microsoft.UI.Xaml.Data;
using NeoCardium.Helpers;

namespace NeoCardium.Converters
{
    public class BooleanToStartCancelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isActive)
                return isActive ? "Abbrechen" : "Starten";

            ExceptionHelper.LogError($"BooleanToStartCancelConverter: Ungültiger Wert: {value}");
            return "Starten"; // Sicherer Fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            ExceptionHelper.LogError("BooleanToStartCancelConverter: ConvertBack wird nicht unterstützt.");
            throw new NotImplementedException();
        }
    }
}