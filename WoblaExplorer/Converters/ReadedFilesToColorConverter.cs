using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using WoblaExplorer.Util;

namespace WoblaExplorer.Converters
{
    class ReadedFilesToColorConverter : IValueConverter
    {
        private string fileName = string.Empty;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            fileName = (string) value;
            if (DbHelper.IsReadedFile(fileName))
            {
                return new SolidColorBrush(Colors.Black);
            }
            return new SolidColorBrush(Colors.DarkSlateBlue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return fileName;
        }
    }
}
