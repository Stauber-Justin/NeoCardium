using System;
using Microsoft.UI.Xaml.Data;

namespace NeoCardium.Converters
{
    public class BooleanToAnswerTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Sicherstellen, dass der Zieltyp String ist (WinUI Best Practice)
            if (targetType != typeof(string))
            {
                throw new InvalidOperationException("BooleanToAnswerTextConverter kann nur für String-Bindings verwendet werden.");
            }

            // Wert prüfen und sicher casten
            if (value is bool isRevealed)
            {
                return isRevealed ? "Nächste Frage" : "Antwort anzeigen";
            }

            return "Antwort anzeigen"; // Sicherer Fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException("ConvertBack wird in BooleanToAnswerTextConverter nicht unterstützt.");
        }
    }
}
