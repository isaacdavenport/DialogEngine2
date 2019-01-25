using DialogGenerator.UI.ViewModels;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for CreateView.xaml
    /// </summary>
    public partial class CreateView : UserControl
    {
        public CreateView()
        {
            InitializeComponent();

            Loaded += _createView_Loaded;
        }

        private void _createView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            (this.DataContext as CreateViewModel).Load();
        }
    }
}
