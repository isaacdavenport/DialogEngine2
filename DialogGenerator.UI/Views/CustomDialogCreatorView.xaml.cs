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
    /// Interaction logic for CustomDialogGeneratorView.xaml
    /// </summary>
    public partial class CustomDialogCreatorView : UserControl
    {
        public CustomDialogCreatorView()
        {            
            InitializeComponent();            
        }

        private void CustomDialogCreatorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CustomDialogCreatorViewModel _model = this.DataContext as CustomDialogCreatorViewModel;
            if(_model != null)
            {
                this.LeftCharacterSlot.DataContext = _model.LeftCharacterModel;
                this.RightSlot.DataContext = _model.RightCharacterModel;
                this.DialogSlot.DataContext = _model.DialogModel;
            }
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CustomDialogCreatorViewModel _model = this.DataContext as CustomDialogCreatorViewModel;
            if (_model != null)
            {
                this.LeftCharacterSlot.DataContext = _model.LeftCharacterModel;
                this.RightSlot.DataContext = _model.RightCharacterModel;
                this.DialogSlot.DataContext = _model.DialogModel;                
            }

            this.DataContextChanged += CustomDialogCreatorView_DataContextChanged;
        }
    }
}
