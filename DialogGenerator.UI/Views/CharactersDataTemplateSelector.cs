using DialogGenerator.Model;
using System.Windows;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views
{
    public class CharactersDataTemplateSelector : DataTemplateSelector
    {
        private int mCurrentToy;

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement; 

            if(element != null && item != null && item is Character)
            {
                Character character = item as Character;

                return element.FindResource("UnassignedCharacterTemplate") as DataTemplate;
            }

            return null;
        }

        public int CurrentToy
        {
            get { return mCurrentToy; }
            set { mCurrentToy = value; }
        }
    }
}
