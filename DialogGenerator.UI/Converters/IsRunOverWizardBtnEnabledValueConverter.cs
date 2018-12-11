using System;
using System.Globalization;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class IsRunOverWizardBtnEnabledValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool _isDeleteBtnEnabled = bool.Parse(values[0].ToString());
                bool _isDialogStarted = bool.Parse(values[1].ToString());

                return _isDeleteBtnEnabled && !_isDialogStarted;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw  new NotImplementedException();
        }
    }
}
