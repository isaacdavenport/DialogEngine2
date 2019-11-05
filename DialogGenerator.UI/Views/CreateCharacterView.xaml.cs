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
        }

        private void ButtonPrev_Click(object sender, RoutedEventArgs e)
        {
            CreateCharacterViewModel model = this.DataContext as CreateCharacterViewModel;
            if (model != null)
            {
                CreateCharacterWizardStep _step = model.previousStep();
                this.ContentControl.Template = Resources[_step.StepControl] as ControlTemplate;
            }
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            CreateCharacterViewModel model = this.DataContext as CreateCharacterViewModel;
            if (model != null)
            {
                CreateCharacterWizardStep _step = model.nextStep();
                this.ContentControl.Template = Resources[_step.StepControl] as ControlTemplate;
            }
        }
    }
}
