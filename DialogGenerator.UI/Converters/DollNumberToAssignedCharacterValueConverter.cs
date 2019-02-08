using DialogGenerator.Core;
using DialogGenerator.Model;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace DialogGenerator.UI.Converters
{
    public class DollNumberToAssignedCharacterValueConverter : IValueConverter
    {
        private ObservableCollection<Character> mCharacters;

        public DollNumberToAssignedCharacterValueConverter()
        {
            mCharacters = Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "Unassigned";

            int _dollNumber = int.Parse(value.ToString());

            var _assignedCharacter = mCharacters.Where(ch => ch.RadioNum == _dollNumber).FirstOrDefault();

            return _assignedCharacter == null ?  "Unassigned" : _assignedCharacter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
