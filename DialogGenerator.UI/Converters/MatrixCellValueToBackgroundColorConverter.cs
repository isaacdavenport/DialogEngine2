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
            if(characters.Count == 0)
            {
                return null;
            }
            
            // first column is row header so we need to sub for 1
            int _heatMapColumn = column - 1;

            var nc1 = characters[Session.Get<int>(Constants.NEXT_CH_1)];
            var nc2 = characters[Session.Get<int>(Constants.NEXT_CH_2)];


            if (row == _heatMapColumn && _heatMap[row, _heatMapColumn] > 0)
            {
                if (row == characters[Session.Get<int>(Constants.NEXT_CH_1)].RadioNum
                  || row == characters[Session.Get<int>(Constants.NEXT_CH_2)].RadioNum)
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
