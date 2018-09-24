using DialogGenerator.UI.Workflow.WizardWorkflow;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class IsEnabledCancelBtnConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                States state = (States)value;
                return state == States.ReadyForUserAction;
            }
            catch (Exception) { }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
