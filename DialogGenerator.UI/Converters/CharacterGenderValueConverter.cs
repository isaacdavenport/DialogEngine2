using System;
using System.Globalization;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class CharacterGenderValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //M-Male F-Female
            var _genderShortName = (string)value;

            return _genderShortName.Equals("M") ? "Male" : "Female";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var _genderName = (string)value;

            return _genderName.Equals("Male") ? "M" : "F";
        }
    }
}
