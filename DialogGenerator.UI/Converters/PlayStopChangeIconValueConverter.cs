using MaterialDesignThemes.Wpf;
using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class PlayStopChangeIconValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool _isRecording = (bool)values[0];
            Button button = values[1] as Button;

            string _parameterString = parameter as string;
            string[] keys = _parameterString.Split('|');
            string _resourceKey = _isRecording ? keys[1] : keys[0];

            return button.FindResource(_resourceKey) as PackIcon;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
