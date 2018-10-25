using DialogGenerator.Model.Enum;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class IsStatesEqualValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ModelDialogState _sourceState = (ModelDialogState)value;
            ModelDialogState _targetState = (ModelDialogState)parameter;

            return _sourceState == _targetState;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
