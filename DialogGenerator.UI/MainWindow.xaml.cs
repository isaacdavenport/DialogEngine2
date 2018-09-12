using DialogGenerator.UI.ViewModel;
using System.Windows;

namespace DialogGenerator.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel mViewModel;

        public MainWindow(MainViewModel _viewModel)
        {
            mViewModel = _viewModel;
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            DataContext = mViewModel;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            mViewModel.Load();
        }
    }
}
