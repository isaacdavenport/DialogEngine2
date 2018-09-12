using Autofac;
using DialogGenerator.UI.Startup;
using DialogGenerator.UI.ViewModel;
using GalaSoft.MvvmLight.Threading;
using System.Windows;

namespace DialogGenerator.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private SplashScreenViewModel mSplashScreen;
        private MainWindow mMainWindow;

        protected  override void OnStartup(StartupEventArgs e)
        {
            mSplashScreen = SplashScreenManager.CreateSplashScreen();

            var bootstrapper = new Bootstrapper();
            var container = bootstrapper.Bootstrap();

            var _appInit = container.Resolve<AppInitializer>();
            _appInit.Completed += _appInit_Completed;
            _appInit.Error += _appInit_Error;
            _appInit.Initialize();

            DispatcherHelper.Initialize();
            mMainWindow = container.Resolve<MainWindow>();
        }

        private void _appInit_Error(object sender, string e)
        {
            throw new System.NotImplementedException();
        }

        private void _appInit_Completed(object sender, System.EventArgs e)
        {
            SplashScreenManager.Close();
            mMainWindow?.Show();
        }

        private void _application_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
        }
    }
}
