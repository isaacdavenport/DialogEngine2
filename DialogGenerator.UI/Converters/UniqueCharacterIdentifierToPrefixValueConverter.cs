using System;
using System.Globalization;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class UniqueCharacterIdentifierToPrefixValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string _characterIdentifier = value.ToString();
                if (string.IsNullOrEmpty(_characterIdentifier))
                    return "";

                string[] _prefixAndGuid = _characterIdentifier.Split('_');
                if (_prefixAndGuid.Length > 0)
                    return _prefixAndGuid[0];
            }
            catch { }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }
    }
}
