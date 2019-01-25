using DialogGenerator.Model.Enum;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class CharaccterStateToNameValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            CharacterState state = (CharacterState)value;

            switch (state)
            {
                case CharacterState.Available:
                    {
                        return "Maybe talk";
                    }
                case CharacterState.On:
                    {
                        return "Do talk";
                    }
                case CharacterState.Off:
                    {
                        return "Don't talk";
                    }
                default:
                    {
                        return "";
                    }
            }            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
