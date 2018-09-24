using System;
using System.Globalization;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class SaveAndNextBtnContentConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int _currentStep;
            int _stepsCount;

            try
            {
                _currentStep = Int32.Parse(values[0].ToString());
                _stepsCount = Int32.Parse(values[1].ToString());
            }
            catch (Exception)
            {
                return "Save and next";
            }

            return (_currentStep < _stepsCount - 1) ? "Save and next" : "Finish";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
