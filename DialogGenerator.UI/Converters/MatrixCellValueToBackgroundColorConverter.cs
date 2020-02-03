using DialogGenerator.Core;
using DialogGenerator.Model;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace DialogGenerator.UI.Converters
{
    public class MatrixCellValueToBackgroundColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var _gridCell = values[0] as DataGridCell;
            var _heatMap = values[1] as int[,];
            int row = DataGridRow.GetRowContainingElement(_gridCell).GetIndex();
            int column = _gridCell.Column.DisplayIndex;

            if (column == 0)
            {
                return Brushes.WhiteSmoke;
            }

            var characters = Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS);
            int ch1 = Session.Get<int>(Constants.NEXT_CH_1);
            int ch2 = Session.Get<int>(Constants.NEXT_CH_2);

            if (characters.Count == 0 || ch1 >= characters.Count || ch2 >= characters.Count
                || ch1 < 0 || ch2 < 0)
            {
                return null;
            }
            
            // first column is row header so we need to sub for 1
            int _heatMapColumn = column - 1;

            var nc1 = characters[ch1];
            var nc2 = characters[ch2];


            if (row == _heatMapColumn && _heatMap[row, _heatMapColumn] > 0)
            {
                if (row == nc1.RadioNum
                  || row == nc2.RadioNum)
                {
                    return Brushes.Red;
                }
            }

            return null;
        }

        public object[] ConvertBack(object values, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
