using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class StarWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ListView _listview = value as ListView;

            double width = ((_listview.Parent as TabItem).Parent as TabControl).ActualWidth;

            GridView _gridView = _listview.View as GridView;

            for (int i = 1; i < _gridView.Columns.Count; i++)
            {
                if (!Double.IsNaN(_gridView.Columns[i].Width))
                    width -= _gridView.Columns[i].Width;
            }

            return width - 20; // this is to take care of margin/padding        
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
