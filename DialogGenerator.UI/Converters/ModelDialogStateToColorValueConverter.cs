using DialogGenerator.Model.Enum;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DialogGenerator.UI.Converters
{
    public class ModelDialogStateToColorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (ModelDialogState)value;

            return state == ModelDialogState.On ? Brushes.Green : Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
