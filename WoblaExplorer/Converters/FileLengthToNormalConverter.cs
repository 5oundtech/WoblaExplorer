using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using WoblaExplorer.Util;

namespace WoblaExplorer.Converters
{
    class FileLengthToNormalConverter : IValueConverter
    {
        private long inputValue = 0;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            inputValue = (long) value;
            return inputValue.BytesToBestSize();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return inputValue;
        }
    }
}
