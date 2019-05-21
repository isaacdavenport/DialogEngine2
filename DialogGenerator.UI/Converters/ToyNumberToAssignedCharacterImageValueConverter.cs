using DialogGenerator.Core;
using DialogGenerator.Model;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DialogGenerator.UI.Converters
{
    public class ToyNumberToAssignedCharacterImageValueConverter : IValueConverter
    {
        private ObservableCollection<Character> mCharacters;

        public ToyNumberToAssignedCharacterImageValueConverter()
        {
            mCharacters = Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PackIcon icon = new PackIcon();
            icon.Foreground = Brushes.DarkGray;
            icon.Height = 128;
            icon.Width = 128;
            icon.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            icon.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            icon.Kind = PackIconKind.AccountCircle;

            if (value == null)
            {
                return icon;
            }

            int _toyNumber = int.Parse(value.ToString());

            var _assignedCharacter = mCharacters.Where(ch => ch.RadioNum == _toyNumber).FirstOrDefault();
            if (_assignedCharacter == null)
            {
                return icon;
            }

            string _imagePath = _assignedCharacter.CharacterImage;
            string _imageFullPath = System.IO.Path.Combine(ApplicationData.Instance.ImagesDirectory, _imagePath);

            if (_imagePath.Equals(ApplicationData.Instance.DefaultImage)
                || !File.Exists(_imageFullPath))
            {
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

