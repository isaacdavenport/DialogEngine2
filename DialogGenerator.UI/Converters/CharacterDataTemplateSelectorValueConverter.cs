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
            CharactersDataTemplateSelector selector = new CharactersDataTemplateSelector();

            try
            {
                int _currentDoll = int.Parse(value.ToString());

                selector.CurrentDoll = _currentDoll;

                return selector;
            }
            catch (Exception)
            {
            }

            selector.CurrentDoll = -1;
            return selector;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
