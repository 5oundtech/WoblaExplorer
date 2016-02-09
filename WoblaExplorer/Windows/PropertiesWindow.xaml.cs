using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WoblaExplorer.Util;

namespace WoblaExplorer.Windows
{
    /// <summary>
    /// Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow
    {
        public PropertiesWindow(FileInfo info)
        {
            InitializeComponent();

            try
            {
                Title += info.Name;
                TbFileName.Text = info.Name;
                TbFilePath.Text = info.FullName;
                TbFileExtension.Text = info.Extension;
                TbFileSizeBytes.Text = $"{info.Length:N}";
                TbFileSizeMb.Text = $"{info.Length.BytesToMb():F3}";
                TbFileSizeGb.Text = $"{info.Length.BytesToGb():F3}";
            }
            catch (IOException ioException)
            {

            }
            catch (SecurityException securityException)
            {
                
            }
        }

        public PropertiesWindow()
        {
            InitializeComponent();
        }

        private void DefaultCommandsCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CopyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var textBlock = e.OriginalSource as TextBlock;
            if (textBlock != null)
            {
                Clipboard.SetText(textBlock.Text);
            }
        }
    }
}
