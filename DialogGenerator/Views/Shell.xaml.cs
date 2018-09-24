using DialogGenerator.Infrastructure;
using DialogGenerator.Model;
using DialogGenerator.UI.Views;
using Prism.Regions;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DialogGenerator.Views
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class Shell : Window
    {
        #region - fields -

        private IRegionManager mRegionManager;

        #endregion

        #region - constructor -

        public Shell(IRegionManager _regionManager)
        {
            InitializeComponent();

            mRegionManager = _regionManager;

            this.CommandBindings.Add(new CommandBinding(NavigationCommands.GoToPage, _onGoToPage, (sender, e) => { e.CanExecute = e.Parameter != null; }));
        }

        #endregion

        #region - event handlers -

        private void _onGoToPage(object sender, ExecutedRoutedEventArgs e)
        {
            var parameters = (object[])e.Parameter;

            if (parameters != null)
            {
                var activeView = mRegionManager.Regions[RegionNames.ContentRegion].ActiveViews.FirstOrDefault();

                if(activeView != null && activeView.GetType().FullName.Equals(typeof(WizardView).FullName))
                {
                    return;
                }

                if (parameters[1].ToString().Contains(typeof(WizardView).Name))
                {
                    mRegionManager.Regions[RegionNames.NavigationRegion].RemoveAll();
                    mRegionManager.Regions[RegionNames.ContentRegion].Context = parameters[2] as Character;
                }

                mRegionManager.RequestNavigate(parameters[0].ToString(), parameters[1].ToString());
            }
        }

        #endregion
    }
}
