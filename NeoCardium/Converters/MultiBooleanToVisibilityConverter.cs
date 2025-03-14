﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace NeoCardium.Converters
{
    public class MultiBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool booleanValue)
            {
                // Falls das Parameter "Invert" ist, kehrt es die Sichtbarkeit um
                bool isInverted = parameter != null && parameter.ToString() == "Invert";
                return booleanValue ^ isInverted ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}