using DialogGenerator.Core;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class ImageToFullPathValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Path.Combine(ApplicationData.Instance.ImagesDirectory,value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
