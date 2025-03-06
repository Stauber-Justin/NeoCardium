using System;
using Microsoft.UI.Xaml.Data;
using NeoCardium.Helpers;

namespace NeoCardium.Converters
{
    public class BooleanToAnswerTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType != typeof(string))
            {
                ExceptionHelper.LogError("BooleanToAnswerTextConverter: Zieltyp ist nicht String.");
                return "Antwort anzeigen"; // Fallback
            }

            if (value is bool isRevealed)
                return isRevealed ? "Nächste Frage" : "Antwort anzeigen";

            ExceptionHelper.LogError($"BooleanToAnswerTextConverter: Ungültiger Wert: {value}");
            return "Antwort anzeigen"; // Sicherer Fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            ExceptionHelper.LogError("BooleanToAnswerTextConverter: ConvertBack wird nicht unterstützt.");
            throw new NotSupportedException();
        }
    }
}