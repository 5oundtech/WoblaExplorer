using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WoblaExplorer.Util
{
    public static class Extensions
    {
#region files
        public static bool IsDirectory(this FileSystemInfo self)
        {
            return Directory.Exists(self.FullName);
        }

        public static double BytesToKb(this long bytes)
        {
            return bytes/1024.0;
        }

        public static double BytesToMb(this long bytes)
        {
            return (bytes/1024.0)/1024.0;
        }

        public static double BytesToGb(this long bytes)
        {
            return ((bytes/1024.0)/1024.0)/1024.0;
        }

        public static string BytesToBestSize(this long bytes)
        {
            string[] sizes = 
            {
                Properties.Resources.Bytes, Properties.Resources.KiloBytes, Properties.Resources.MegaBytes,
                Properties.Resources.GigaBytes
            };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len /= 1024;
            }


            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }
#endregion
        #region UI
        public static void TogglePbVisibility(this ProgressBar self)
        {
            self.Visibility = self.Visibility == Visibility.Hidden || self.Visibility == Visibility.Collapsed
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        public static async Task TogglePbVisibilityAsync(this ProgressBar self)
        {
            await self.Dispatcher.InvokeAsync(() =>
            {
                self.Visibility = self.Visibility == Visibility.Hidden || self.Visibility == Visibility.Collapsed
                    ? Visibility.Visible
                    : Visibility.Hidden;
            });
        }

        public static ImageSource ToImageSource(this Icon icon)
        {
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));

            return imageSource;
        }

        public static ImageSource ToImageSource(this Bitmap bitmap)
        {
            var imageSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));

            return imageSource;
        }
        #endregion
    }
}
