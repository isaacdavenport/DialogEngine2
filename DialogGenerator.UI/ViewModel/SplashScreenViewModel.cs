﻿using GalaSoft.MvvmLight;
using System;
using System.Windows.Threading;

namespace DialogGenerator.UI.ViewModel
{
    public class SplashScreenViewModel:ViewModelBase, IDisposable
    {
        private string mMessage;
        public SplashScreenViewModel()
        {
                
        }

        ~SplashScreenViewModel()
        {
            this.Dispose(false);
        }

        public string Message
        {
            get { return mMessage; }
            set
            {
                mMessage = value;
                RaisePropertyChanged();
            }
        }

        public Dispatcher Dispatcher { get; set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if(Dispatcher != null)
                {
                    Dispatcher.InvokeShutdown();
                    Dispatcher = null;
                }
            }
        }
    }
}
