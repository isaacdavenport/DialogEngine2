using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class IsActiveModelDialogValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int _selectedIndex = int.Parse(value.ToString());

            return _selectedIndex < 0 ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
