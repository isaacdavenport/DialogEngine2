using DialogGenerator.Model.Enum;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DialogGenerator.UI.Converters
{
    public class CharacterStateToColorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            CharacterState state = (CharacterState)value;

            switch (state)
            {
                case CharacterState.Available:
                    return Brushes.Orange;
                case CharacterState.On:
                    return Brushes.Green;
                case CharacterState.Off:
                    return Brushes.Red;
                default:
                    return Brushes.Orange;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (CharacterState)parameter;
        }
    }
}
