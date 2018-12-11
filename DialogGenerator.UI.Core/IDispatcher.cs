using System;

namespace DialogGenerator.UI.Core
{
    public interface IDispatcher
    {
        void Invoke(Action action);
        void BeginInvoke(Action action);
    }
}
