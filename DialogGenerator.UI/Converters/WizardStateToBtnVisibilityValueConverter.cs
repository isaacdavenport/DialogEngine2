using DialogGenerator.UI.Workflow.WizardWorkflow;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class WizardStateToBtnVisibilityValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                WizardStates _currentState = (WizardStates)value;
                WizardStates _expectedState = (WizardStates)parameter;

                return _currentState == _expectedState ?Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception){}

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
