using DialogGenerator.UI.ViewModels;
using Prism.Regions;
using System.Windows;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for WizardView.xaml
    /// </summary>
    public partial class WizardView : UserControl
    {
        private IRegionManager mRegionManager;

        public WizardView(IRegionManager _regionManager)
        {
            mRegionManager = _regionManager;
            Loaded += _wizardView_Loaded;
            Unloaded += _wizardView_Unloaded;
            SizeChanged += _wizardView_SizeChanged;

            InitializeComponent();
        }

        private void _wizardView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MediaGrid.Width = (LeftGrid.ActualHeight - dialogStrTb.ActualHeight - 40 - 45) * 4 / 3;

            LeftGrid.Margin = new Thickness((WizardMainGrid.ColumnDefinitions[2].ActualWidth - MediaGrid.Width) / 2, 10.0, 0.0, 0.0);
        }

        private void _wizardView_Unloaded(object sender, RoutedEventArgs e)
        {
            (this.DataContext as WizardViewModel).Reset();
        }

        private void _wizardView_Loaded(object sender, RoutedEventArgs e)
        {
            MediaGrid.Width = (LeftGrid.ActualHeight - dialogStrTb.ActualHeight - 40 - 45) * 4 / 3;

            LeftGrid.Margin = new Thickness((WizardMainGrid.ColumnDefinitions[2].ActualWidth - MediaGrid.Width) / 2, 10.0, 0.0, 0.0);
        }
    }
}
