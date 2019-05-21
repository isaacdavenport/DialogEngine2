using DialogGenerator.Core;
using DialogGenerator.Model;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace DialogGenerator.UI.Converters
{
    public class ToyStatusColorValueConverter : IValueConverter
    {
        private ObservableCollection<Character> mCharacters;

        public ToyStatusColorValueConverter()
        {
            mCharacters = Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int _toyNumber = int.Parse(value.ToString());
            var _assignedCharacter = mCharacters.Where(ch => ch.RadioNum == _toyNumber).FirstOrDefault();

            return _assignedCharacter != null ? Brushes.Green : Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
