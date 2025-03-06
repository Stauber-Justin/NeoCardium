using System;
using Microsoft.UI.Xaml.Data;
using NeoCardium.Helpers;

namespace NeoCardium.Converters
{
    public class AnswerIndexToGridColumnConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                if (value is int index)
                {
                    return index % 2; // Weist 0 oder 1 zu (erste oder zweite Spalte)
                }

                ExceptionHelper.LogError("Ungültiger Wert im AnswerIndexToGridColumnConverter: " + value);
                return 0; // Standardwert
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler in AnswerIndexToGridColumnConverter.", ex);
                return 0; // Sicherer Fallback
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            ExceptionHelper.LogError("ConvertBack wurde in AnswerIndexToGridColumnConverter aufgerufen, aber ist nicht implementiert.");
            throw new NotImplementedException();
        }
    }
}