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

            InitializeComponent();
        }

    }
}
