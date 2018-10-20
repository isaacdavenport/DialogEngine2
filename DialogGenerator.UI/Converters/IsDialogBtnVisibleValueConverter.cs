using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class IsDialogBtnVisibleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool _isDialogStarted = (bool)value;
            string _parameter = parameter.ToString();

            if (_parameter.Equals("StartDialog"))
            {
                return _isDialogStarted ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return _isDialogStarted ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
