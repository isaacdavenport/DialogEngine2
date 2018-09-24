using DialogGenerator.UI.ViewModels;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for DialogModelsNavigationView.xaml
    /// </summary>
    public partial class DialogModelsNavigationView : UserControl
    {
        public DialogModelsNavigationView()
        {
            InitializeComponent();

            Loaded += _dialogModelsNavigationView_Loaded;
        }

        private void _dialogModelsNavigationView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            (this.DataContext as DialogModelsNavigationViewModel).Load();
        }
    }
}
