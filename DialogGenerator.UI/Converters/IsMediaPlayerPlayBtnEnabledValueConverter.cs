using DialogGenerator.UI.Workflow.WizardWorkflow;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class IsMediaPlayerPlayBtnEnabledValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            States state = (States)value;
            string[] states = parameter.ToString().Split('|');

            return states.Contains(state.ToString());
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
