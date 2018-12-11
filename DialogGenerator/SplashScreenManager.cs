using DialogGenerator.ViewModels;
using DialogGenerator.Views;
using System;
using System.Threading;
using System.Windows.Threading;

namespace DialogGenerator
{
    internal class SplashScreenManager
    {
        private static object msLocker = new object();
        private static SplashScreenViewModel msVieModel;

        public static SplashScreenViewModel  CreateSplashScreen()
        {
            lock (msLocker)
            {
                msVieModel = new SplashScreenViewModel();

                AutoResetEvent ev = new AutoResetEvent(false);

                Thread _uiThread = new Thread(() =>
                {
                    msVieModel.Dispatcher = Dispatcher.CurrentDispatcher;
                    ev.Set();

                    Dispatcher.CurrentDispatcher.BeginInvoke((Action)delegate () {
                        SplashScreenView _splashScreenWindow = new SplashScreenView
                        {
                            DataContext = msVieModel
                        };
                        _splashScreenWindow.Show();
                    });

                    Dispatcher.Run();
                });

                _uiThread.SetApartmentState(ApartmentState.STA);
                _uiThread.IsBackground = true;
                _uiThread.Start();
                ev.WaitOne();

                return msVieModel;
            }
        }

        public static void ShowMessage(string message)
        {
            if (msVieModel != null)
            {
                msVieModel.Message = message;
            }
        }

        public static void Close()
        {
            if(msVieModel != null)
            {
                msVieModel.Dispose(true);
                msVieModel = null;
            }
        }
    }
}
