using DialogGenerator.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for CreateCharacterView.xaml
    /// </summary>
    public partial class CreateCharacterView : UserControl
    {
        public CreateCharacterView()
        {
            InitializeComponent();
            (this.DataContext as CreateCharacterViewModel).PropertyChanged += CreateCharacterView_PropertyChanged;
        }

        private void CreateCharacterView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            CreateCharacterViewModel _viewModel = sender as CreateCharacterViewModel;
            if(sender != null)
            {
                if(e.PropertyName.Equals("CurrentStep"))
                {
                    this.ContentControl.Template = Resources[_viewModel.CurrentStep.StepControl] as ControlTemplate;
                }
            }
        }

        private void ButtonPrev_Click(object sender, RoutedEventArgs e)
        {
            CreateCharacterViewModel model = this.DataContext as CreateCharacterViewModel;
            model?.previousStep();
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            CreateCharacterViewModel model = this.DataContext as CreateCharacterViewModel;
            model?.nextStep();
        }

        private void viewLoaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
