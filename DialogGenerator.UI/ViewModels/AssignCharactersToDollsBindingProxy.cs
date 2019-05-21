using System.Windows;

namespace DialogGenerator.UI.ViewModels
{
    public class AssignCharactersToToysBindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new AssignCharactersToToysBindingProxy();
        }

        public object ViewModel
        {
            get { return (object)GetValue(CharactersProperty); }
            set { SetValue(CharactersProperty, value); }
        }

        public static readonly DependencyProperty CharactersProperty = 
            DependencyProperty.Register("ViewModel", typeof(object), typeof(AssignCharactersToToysBindingProxy), new UIPropertyMetadata(null));
    }
}
