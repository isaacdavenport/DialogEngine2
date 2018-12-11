using DialogGenerator.Core;
using DialogGenerator.Model;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class AssignedCharacter2NameValueConverter : IValueConverter
    {
        private ObservableCollection<Character> mCharacters;

        public AssignedCharacter2NameValueConverter()
        {
            mCharacters = Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS);
        }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int _dollNumber = int.Parse(value.ToString());

            var _assignedCharacter = mCharacters.Where(ch => ch.RadioNum == _dollNumber).FirstOrDefault();

            if(_assignedCharacter != null)
            {
                return _assignedCharacter.ToString();
            }

            return "Not assigned";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
