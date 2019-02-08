using System;
using System.Globalization;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class SkipBtnContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool collectUserInput;

            try
            {
                collectUserInput = bool.Parse(value.ToString());
            }
            catch (Exception)
            {
                return "Skip";
            }

            return collectUserInput ? "Skip" : "Next";
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}