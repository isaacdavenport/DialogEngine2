using DialogGenerator.UI.Workflow.MP3RecorderStateMachine;
using DialogGenerator.UI.Workflow.WizardWorkflow;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    class WizardAndMP3PlayerStatesToBtnVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                States _currentState = (States)values[0];
                WizardStates _wizardState = (WizardStates)values[1];
                States _expectedState = (States)parameter;

                return _currentState == _expectedState && _wizardState != WizardStates.PlayingInContext
                       ? Visibility.Visible
                       : Visibility.Collapsed;
            }
            catch (Exception) { }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
