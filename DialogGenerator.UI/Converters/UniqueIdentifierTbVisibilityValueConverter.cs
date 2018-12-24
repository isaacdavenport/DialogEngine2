using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class UniqueIdentifierTbVisibilityValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string identifier = value.ToString();

                return string.IsNullOrEmpty(identifier) ? Visibility.Collapsed : Visibility.Visible;
            }
            catch { }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
