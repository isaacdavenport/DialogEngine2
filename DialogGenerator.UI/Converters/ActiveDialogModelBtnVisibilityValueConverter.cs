using DialogGenerator.Model;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class ActiveDialogModelBtnVisibilityValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var _currentDialogModel = values[0] as ModelDialog;
                var _activeDialogModel = values[1];

                if (_activeDialogModel == null)
                    return Visibility.Visible;

                return ((ModelDialog)_activeDialogModel).Name.Equals(_currentDialogModel.Name) ? Visibility.Hidden : Visibility.Visible;
            }
            catch (Exception){}
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
