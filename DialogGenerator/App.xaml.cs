using System;
using System.Windows;
using DialogGenerator.UI.ViewModels;
using DialogGenerator.UI.Views;
using Microsoft.Practices.Unity;


namespace DialogGenerator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            SplashScreenManager.CreateSplashScreen();
            base.OnStartup(e);
            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();

            bootstrapper.Container.Resolve<CharacterDetailViewModel>();
            bootstrapper.Container.Resolve<DialogModelDetailViewModel>();

            var _appInit = bootstrapper.Container.Resolve<AppInitializer>();
            _appInit.Error += _appInit_Error;
            _appInit.Completed += _appInit_Completed;
            _appInit.Initialize();
        }

        private void _appInit_Completed(object sender, EventArgs e)
        {
            SplashScreenManager.Close();
            Current.MainWindow.Show();
        }

        private void _appInit_Error(object sender, string e)
        {
        }
    }
}
