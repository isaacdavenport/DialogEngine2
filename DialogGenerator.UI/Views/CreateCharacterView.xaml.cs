using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for CreateCharacterView.xaml
    /// </summary>
    public partial class CreateCharacterView : UserControl
    {
        public CreateCharacterView()
        {
            //this.DataContext = new CreateCharacterViewModel();
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
            //if (model != null)
            //{
            //    model.previousStep();                
            //}
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            CreateCharacterViewModel model = this.DataContext as CreateCharacterViewModel;
            model?.nextStep();
            //if (model != null)
            //{
            //    model.nextStep();                
            //}
        }
    }
}
