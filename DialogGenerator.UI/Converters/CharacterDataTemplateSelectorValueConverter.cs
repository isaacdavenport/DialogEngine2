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
                int _currentToy = int.Parse(value.ToString());

                selector.CurrentToy = _currentToy;

                return selector;
            }
            catch (Exception)
            {
            }

            selector.CurrentToy = -1;
            return selector;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
