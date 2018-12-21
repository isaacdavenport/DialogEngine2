using DialogGenerator.Model;
using DialogGenerator.UI.Data;
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
        private ICharacterDataProvider mCharacterDataProvider;

        public AssignCharactersToDollsView(ICharacterDataProvider _characterDataProvider)
        {
            mCharacterDataProvider = _characterDataProvider;

            InitializeComponent();

            Loaded += _assignCharactersToDollsView_Loaded;
        }

        private void _assignCharactersToDollsView_Loaded(object sender, RoutedEventArgs e)
        {
            this.ItemsControl.Items.Refresh();
        }

        private void _popup_Opened(object sender, System.Windows.RoutedEventArgs e)
        {
          var _comboBox =  (((sender as PopupBox).PopupContent as GroupBox).Content as Grid).GetVisualChild<ComboBox>();
            _comboBox.SelectedValue = mCharacterDataProvider.GetByAssignedRadio((int)_comboBox.DataContext);
        }
    }
}
