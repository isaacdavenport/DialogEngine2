using DialogGenerator.Model;
using System.Windows;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views
{
    public class CharacterDataTemplateSelector:DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is Character)
            {
                Character character = item as Character;

                if (string.IsNullOrEmpty(character.CharacterName))
                    return element.FindResource("CreateNewCharacterTemplate") as DataTemplate;
                else
                    return element.FindResource("RegularCharacterTemplate") as DataTemplate;
            }

            return null;
        }
    }
}
