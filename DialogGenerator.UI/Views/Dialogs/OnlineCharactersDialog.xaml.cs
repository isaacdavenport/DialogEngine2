using DialogGenerator.UI.ViewModels;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for OnlineCharactersDialog.xaml
    /// </summary>
    public partial class OnlineCharactersDialog : UserControl
    {
        public OnlineCharactersDialog(OnlineCharactersDialogViewModel _viewModel)
        {
            this.DataContext = _viewModel;

            InitializeComponent();
        }
    }
}
