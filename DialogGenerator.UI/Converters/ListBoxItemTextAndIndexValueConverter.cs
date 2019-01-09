using DialogGenerator.Model;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class ListBoxItemTextAndIndexValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                DependencyObject item = (DependencyObject)values[0];
                ItemsControl ic = ItemsControl.ItemsControlFromItemContainer(item);
                int index = ic.ItemContainerGenerator.IndexFromContainer(item) + 1;
                TutorialStep step = (item as ListBoxItem).Content as TutorialStep;
                string _stepDescription = step.PhraseWeights.Keys.Count > 0 ? step.PhraseWeights.Keys.First() : "Not editable";
                return index + ". " + _stepDescription;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
