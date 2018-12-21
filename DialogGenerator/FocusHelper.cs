using System.Diagnostics;
using System.Windows.Automation;

namespace DialogGenerator
{
    public static class FocusHelper
    {
        public static void RequestFocus()
        {
            try
            {
                AutomationElement element = AutomationElement.FromHandle(Process.GetCurrentProcess().MainWindowHandle);
                if (element != null)
                {
                    element.SetFocus();
                }
            }
            catch { }
        }
    }
}
