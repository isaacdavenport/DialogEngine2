using System;
using System.Threading.Tasks;

namespace DialogGenerator.Utilities
{
    public static class TaskExtensions
    {
        public static async void FireAndForgetSafeAsync(this Task task, Action<Exception> onException = null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try
                {
                    onException?.Invoke(ex);
                }
                catch { }
            }
        }
    }
}
