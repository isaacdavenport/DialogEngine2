using System;
using System.Windows;
using System.Windows.Threading;

namespace DialogGenerator.UI.Core
{
    public class DispatcherWrapper : IDispatcher
    {
        private Dispatcher mDispatcher;

        public DispatcherWrapper()
        {
            mDispatcher = Application.Current.Dispatcher;
        }

        public void BeginInvoke(Action action)
        {
            mDispatcher.BeginInvoke(action);
        }

        public void Invoke(Action action)
        {
            mDispatcher.Invoke(action);
        }
    }
}
