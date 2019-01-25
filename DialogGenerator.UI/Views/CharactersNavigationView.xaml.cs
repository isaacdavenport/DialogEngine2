using DialogGenerator.UI.ViewModels;
using System.Linq;
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
            (this.DataContext as CreateViewModel).Load();
        }

        private void _listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems != null && e.AddedItems.Count > 0)
            {
                (sender as ListView).SelectedIndex = -1;
            }
        }
    }
}
