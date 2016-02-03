using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WoblaExplorer.Util
{
    [ValueConversion(typeof(string), typeof(string))]
    public class FileNameToIconConverter : IValueConverter
    {
        private string icon = "";
        private object fs;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            fs = value;
            if (Directory.Exists((string) value))
            {
                icon = "\uf07b";
                return "\uf07b";
            }
            icon = "\uf15b";
            return icon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return fs;
        }
    }
}
