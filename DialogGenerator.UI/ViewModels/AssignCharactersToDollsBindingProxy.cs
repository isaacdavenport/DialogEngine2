using System.Windows;

namespace DialogGenerator.UI.ViewModels
{
    public class AssignCharactersToDollsBindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new AssignCharactersToDollsBindingProxy();
        }

        public object ViewModel
        {
            get { return (object)GetValue(CharactersProperty); }
            set { SetValue(CharactersProperty, value); }
        }

        public static readonly DependencyProperty CharactersProperty = 
            DependencyProperty.Register("ViewModel", typeof(object), typeof(AssignCharactersToDollsBindingProxy), new UIPropertyMetadata(null));
    }
}
