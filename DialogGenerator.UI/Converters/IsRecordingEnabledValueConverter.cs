using DialogGenerator.UI.Workflow.WizardWorkflow;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class IsRecordingEnabledValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            States state = (States)values[0];
            string[] states = parameter.ToString().Split('|');
            bool _isRecordingAllowed = false;
            try
            {
                _isRecordingAllowed = (bool)values[1];
            }
            catch { }

            return _isRecordingAllowed && states.Contains(state.ToString());
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
