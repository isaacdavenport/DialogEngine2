using DialogGenerator.Model;
using DialogGenerator.UI.Helper;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for AssignCharactersToDollsView.xaml
    /// </summary>
    public partial class AssignCharactersToDollsView : UserControl
    {
        public AssignCharactersToDollsView()
        {
            InitializeComponent();
        }

        private void _popup_Opened(object sender, System.Windows.RoutedEventArgs e)
        {
          var _comboBox =  (((sender as PopupBox).PopupContent as GroupBox).Content as Grid).GetVisualChild<ComboBox>();
            _comboBox.SelectedValue = ((KeyValuePair<int, Character>)_comboBox.DataContext).Value;
        }
    }
}
