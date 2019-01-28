using DialogGenerator.Core;
using MaterialDesignThemes.Wpf;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DialogGenerator.UI.Converters
{
    public class ImageContentValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string _imagePath = value.ToString();
            string _imageFullPath = System.IO.Path.Combine(ApplicationData.Instance.ImagesDirectory, _imagePath);

            if (_imagePath.Equals(ApplicationData.Instance.DefaultImage) 
                || !File.Exists(_imageFullPath))
            {
                PackIcon icon = new PackIcon();
                icon.Foreground = Brushes.DarkGray;
                icon.Height = 128;
                icon.Width = 128;
                icon.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                icon.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                icon.Kind = PackIconKind.AccountCircle;

                return icon;
            }
            else
            {
                BitmapImage _imageSource = new BitmapImage();
                _imageSource.BeginInit();
                _imageSource.CacheOption = BitmapCacheOption.OnLoad;
                _imageSource.UriSource = new Uri(_imageFullPath);
                _imageSource.EndInit();
                ImageBrush image = new ImageBrush();
                image.Stretch = Stretch.UniformToFill;
                image.ImageSource = _imageSource;
                

                Ellipse ellipse = new Ellipse();
                ellipse.Height = 110;
                ellipse.Width = 110;
                ellipse.Margin = new System.Windows.Thickness(0);
                ellipse.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                ellipse.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                ellipse.Fill = image;

                return ellipse;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
