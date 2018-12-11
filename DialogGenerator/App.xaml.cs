using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using DialogGenerator.UI.ViewModels;
using DialogGenerator.Utilities;
using DialogGenerator.ViewModels;
using Microsoft.Practices.Unity;


namespace DialogGenerator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex msMutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            string _appName = Assembly.GetExecutingAssembly().GetName().Name;
            bool _createdNew;

            msMutex = new Mutex(true, _appName, out _createdNew);

            if (!_createdNew)
            {
                Current.Shutdown();
                return;
            }

            SplashScreenManager.CreateSplashScreen();
            base.OnStartup(e);
            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();

            bootstrapper.Container.Resolve<ShellViewModel>()
                .MessageDialogService = bootstrapper.Container.Resolve<IMessageDialogService>();
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

            AutomationElement element = AutomationElement.FromHandle(Process.GetCurrentProcess().MainWindowHandle);
            if (element != null)
            {
                element.SetFocus();
            }
        }

        private void _appInit_Error(object sender, string e)
        {
        }
    }
}
