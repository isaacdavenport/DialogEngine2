using DialogGenerator.UI.ViewModels;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for CharactersNavigationView.xaml
    /// </summary>
    public partial class CharactersNavigationView : UserControl
    {
        public CharactersNavigationView()
        {
            InitializeComponent();

            Loaded += _charactersNavigationView_Loaded;
        }

        private void _charactersNavigationView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            (this.DataContext as CharactersNavigationViewModel).Load();
        }
    }
}
