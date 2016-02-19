using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using WoblaExplorer.FilesUtil;

namespace WoblaExplorer.Converters
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
            string fileExt = Path.GetExtension(value.ToString());
            if (fileExt.Length.Equals(0))
            {
                icon = "\uf15b";
                return icon;
            }
            var item = FilesExtensions.FilesIcons.ContainsKey(fileExt) ? FilesExtensions.FilesIcons[fileExt] : "";
            if (item.Length == 0)
            {
                icon = "\uf15b";
                return icon;
            }
            return item;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return fs;
        }
    }
}
