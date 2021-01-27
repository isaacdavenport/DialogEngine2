using DialogGenerator.Utilities.Model;
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace DialogGenerator.Utilities
{
    public class UserLogger : IUserLogger
    {
        public void Error(string message = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (Application.Current.Dispatcher == null)
                return;
            
            if (Application.Current.Dispatcher.CheckAccess())
            {
                if (ErrorsCollection.Count > 150)
                    ErrorsCollection.RemoveAt(ErrorsCollection.Count - 1);

                ErrorsCollection.Insert(0, new UserLoggerModel(message, file, line));
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (ErrorsCollection.Count > 150)
                        ErrorsCollection.RemoveAt(ErrorsCollection.Count - 1);

                    ErrorsCollection.Insert(0, new UserLoggerModel(message, file, line));
                }));
            }
        }

        public void Info(string message = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (Application.Current.Dispatcher == null)
                return;
            
            if (Application.Current.Dispatcher.CheckAccess())
            {
                if (InformationsCollection.Count > 150)
                    InformationsCollection.RemoveAt(InformationsCollection.Count - 1);

                InformationsCollection.Insert(0, new UserLoggerModel(message, file, line));
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (InformationsCollection.Count > 150)
                        InformationsCollection.RemoveAt(InformationsCollection.Count - 1);

                    InformationsCollection.Insert(0, new UserLoggerModel(message, file, line));
                }));
            }
        }

        public void Warning(string message = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (Application.Current.Dispatcher == null)
                return;
            
            if (Application.Current.Dispatcher.CheckAccess())
            {
                if (WarningsCollection.Count > 150)
                    WarningsCollection.RemoveAt(WarningsCollection.Count - 1);

                WarningsCollection.Insert(0, new UserLoggerModel(message, file, line));
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (WarningsCollection.Count > 150)
                        WarningsCollection.RemoveAt(WarningsCollection.Count - 1);

                    WarningsCollection.Insert(0, new UserLoggerModel(message, file, line));
                }));
            }
        }

        public ObservableCollection<UserLoggerModel> ErrorsCollection { get; set; } = new ObservableCollection<UserLoggerModel>();

        public ObservableCollection<UserLoggerModel> WarningsCollection { get; set; } = new ObservableCollection<UserLoggerModel>();

        public ObservableCollection<UserLoggerModel> InformationsCollection { get; set; } = new ObservableCollection<UserLoggerModel>();
    }
}
