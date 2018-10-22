using DialogGenerator.UI.Workflow.WizardWorkflow;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class IsMediaPlayerPlayBtnEnabledValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            States state = (States)values[0];
            string[] states = parameter.ToString().Split('|');

            string _filePath = values[1]?.ToString();

            return states.Contains(state.ToString()) && !string.IsNullOrEmpty(_filePath) && File.Exists(_filePath);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
