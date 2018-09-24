using DialogGenerator.UI.Workflow.WizardWorkflow;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class IsEnabledSaveAndNextBtnConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool _isLineRecorded = (bool)values[0];
                int _currentStepIndex = int.Parse(values[1].ToString());
                int _stepsCount = int.Parse(values[2].ToString());
                States state = (States)values[3];

                return (_isLineRecorded || (_currentStepIndex == _stepsCount - 1)) && state == States.ReadyForUserAction;
            }
            catch (Exception) { }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
