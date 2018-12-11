using DialogGenerator.UI.Workflow.MP3RecorderStateMachine;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class MP3RecorderStateToBtnVisibilityValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            States _currentState = (States)value;
            States _expectedState = (States)parameter;

            return _currentState == _expectedState ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
