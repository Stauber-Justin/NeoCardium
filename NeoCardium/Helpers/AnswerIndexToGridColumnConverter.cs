using System;
using Microsoft.UI.Xaml.Data;

namespace NeoCardium.Helpers
{
    public class AnswerIndexToGridColumnConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int index)
            {
                return index % 2; // Weist 0 oder 1 zu (erste oder zweite Spalte)
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
