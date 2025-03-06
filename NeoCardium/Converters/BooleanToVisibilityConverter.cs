using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using NeoCardium.Helpers;

namespace NeoCardium.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                // Standardverhalten ohne Parameter (wie vorher)
                bool invert = parameter is string str && bool.TryParse(str, out bool paramValue) && paramValue;
                return boolValue ^ invert ? Visibility.Visible : Visibility.Collapsed;
            }

            ExceptionHelper.LogError($"BooleanToVisibilityConverter: Ungültiger Wert: {value}");
            return Visibility.Collapsed; // Sicherer Fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            try
            {
                if (value is Visibility visibility)
                    return visibility == Visibility.Visible;

                ExceptionHelper.LogError($"BooleanToVisibilityConverter: ConvertBack erhielt ungültigen Wert: {value}");
                return false; // Sicherer Fallback
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler in BooleanToVisibilityConverter.ConvertBack.", ex);
                return false;
            }
        }
    }
}