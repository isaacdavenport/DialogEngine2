using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using DialogGenerator.Core;
using DialogGenerator.Handlers;
using DialogGenerator.UI.ViewModels;
using DialogGenerator.Utilities;
using DialogGenerator.Views;
using Microsoft.Practices.Unity;


namespace DialogGenerator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex msMutex = null;
        private FileChangesHandler mFileChangesHandler;
        private UpdatesHandler mUpdatesHandler;

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

            bootstrapper.Container.Resolve<CharacterDetailViewModel>();
            mFileChangesHandler = bootstrapper.Container.Resolve<FileChangesHandler>();
            mUpdatesHandler = bootstrapper.Container.Resolve<UpdatesHandler>();
            bootstrapper.Container.Resolve<Shell>().MessageDialogService = bootstrapper.Container.Resolve<IMessageDialogService>();

            var _appInit = bootstrapper.Container.Resolve<AppInitializer>();
            _appInit.Completed += _appInit_Completed;
            _appInit.Initialize();
        }

        private void _appInit_Completed(object sender, EventArgs e)
        {

            SplashScreenManager.Close();
            Current.MainWindow.Show();
            FocusHelper.RequestFocus();

            mFileChangesHandler.StartWatching(ApplicationData.Instance.EditorTempDirectory,"*.json");
            mUpdatesHandler.CheckForUpdates();
        }
    }
}
