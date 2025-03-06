using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using NeoCardium.Helpers;

namespace NeoCardium.Converters
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Enum enumValue && parameter is string enumString)
            {
                return enumValue.ToString() == enumString ? Visibility.Visible : Visibility.Collapsed;
            }

            ExceptionHelper.LogError($"EnumToVisibilityConverter: Ungültige Werte – Value: {value}, Parameter: {parameter}");
            return Visibility.Collapsed; // Sicherer Fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            ExceptionHelper.LogError("EnumToVisibilityConverter: ConvertBack wurde aufgerufen, aber ist nicht implementiert.");
            throw new NotImplementedException();
        }
    }
}