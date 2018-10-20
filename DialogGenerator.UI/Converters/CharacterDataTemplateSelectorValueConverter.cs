using DialogGenerator.UI.Views;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class CharacterDataTemplateSelectorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int _currentDoll = int.Parse(value.ToString());
            CharactersDataTemplateSelector selector = new CharactersDataTemplateSelector();

            selector.CurrentDoll = _currentDoll;

            return selector;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
