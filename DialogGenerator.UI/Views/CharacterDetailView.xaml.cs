using Prism.Regions;
using System.Windows;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for CharacterDetailView.xaml
    /// </summary>
    public partial class CharacterDetailView : UserControl
    {
        public CharacterDetailView()
        {
            InitializeComponent();

            Loaded += _characterDetailView_Loaded;
            SizeChanged += _characterDetailView_SizeChanged;
        }

        private void _characterDetailView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //if(TabControl.SelectedIndex == 1) 
            //{
            //    PhrasesItemsControl.Items.Refresh();
            //}
        }

        private void _characterDetailView_Loaded(object sender,RoutedEventArgs e)
        {
            TabControl.SelectedIndex = 0;
        }
    }
}
