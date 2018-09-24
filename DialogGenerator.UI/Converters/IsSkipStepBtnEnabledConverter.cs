using DialogGenerator.UI.Workflow.WizardWorkflow;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class IsSkipStepBtnEnabledConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                int _currentStep = int.Parse(values[0].ToString());
                int _stepsCount = int.Parse(values[1].ToString());
                States state = (States)values[2];

                return (_currentStep < _stepsCount - 1) && state == States.ReadyForUserAction;
            }
            catch { }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
