using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.UI.Views;
using DialogGenerator.Utilities;
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

        private async void _window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (ProcessHandler.HasActiveProcess)
            {
                MessageDialogResult result = await MessageDialogService
                    .ShowOKCancelDialogAsync("JSON editor didn't closed. If you made changes, please save and close editor.","Warning","Close message","Close editor");

                if(result == MessageDialogResult.Cancel)
                {
                    ProcessHandler.ClearAll();
                    e.Cancel = false;
                    Application.Current.Shutdown();
                }
            }
            else
            {
                e.Cancel = false;
                Application.Current.Shutdown();
            }
            
        }

        private void _onGoToPage(object sender, ExecutedRoutedEventArgs e)
        {
            var parameters = (object[])e.Parameter;

            if (parameters != null)
            {
                //var activeView = mRegionManager.Regions[Constants.ContentRegion].ActiveViews.FirstOrDefault();

                //if(activeView != null  && activeView.GetType().FullName.Equals(typeof(WizardView).FullName ))
                //{
                //    return;
                //}

                if(parameters.Length == 3)
                {
                    mRegionManager.Regions[Constants.ContentRegion].Context = parameters[2] as Character;
                }

                mRegionManager.RequestNavigate(parameters[0].ToString(), parameters[1].ToString());
            }
        }

        #endregion

        public IMessageDialogService MessageDialogService { get; set; }
    }
}
