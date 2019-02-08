using DialogGenerator.Model;
using System;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace DialogGenerator.UI.Converters
{
    public class MatrixToDataViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var data = value as HeatMapData;
            if (data == null)
                return null; ;

            var array = data.HeatMap;
            var rows = array.GetLength(0);
            if (rows == 0) return null;

            var columns = array.GetLength(1);
            if (columns == 0) return null;

            var t = new DataTable();
            // Add columns with name "0", "1", "2", ...
            t.Columns.Add(new DataColumn("--"));

            for (var c = 0; c < columns; c++)
            {
                t.Columns.Add(new DataColumn(c.ToString()));
            }

            t.Columns.Add(new DataColumn("M"));

            t.Columns.Add(new DataColumn("Update time"));

            // Add data to DataTable
            for (var r = 0; r < rows; r++)
            {
                var newRow = t.NewRow();
                newRow[0] = r.ToString();

                for (var c = 1; c <= columns; c++)
                {
                    newRow[c] = array[r, c - 1];
                }

                newRow[columns + 1] = data.MotionVector[r];
                newRow[columns + 2] = data.LastHeatMapUpdateTime[r].ToString("mm.ss.fff");
                t.Rows.Add(newRow);
            }

            return t.DefaultView;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
