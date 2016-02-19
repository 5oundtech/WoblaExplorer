using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Elysium;

namespace WoblaExplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for CreateFileDialog.xaml
    /// </summary>
    public partial class CreateFileDialog
    {
        public string FileName { get; set; }
        public string ErrorString { get; set; }

        public SolidColorBrush GetCurrentContrastBrush
        {
            get
            {
                if (Elysium.Manager.GetTheme(Application.Current) == Theme.Dark)
                {
                    return Elysium.Manager.DefaultContrastBrush;
                }
                return new SolidColorBrush(Colors.Black);
            }
        }

        public CreateFileDialog()
        {
            InitializeComponent();

            var mainFileTypes = new[]
            {
                ".txt",
                ".doc",
                ".docx",
                ".xls",
                ".bmp",
                "..."
            };
            CbFileType.ItemsSource = mainFileTypes;
            CbFileType.SelectedIndex = 0;
        }

        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            var fileName = TbFileName.Text;
            if (fileName.Length < 3)
            {
                ErrorString = Properties.Resources.FileValidationFileNameLength;
                DialogResult = false;
                return;
            }
            var fileNameRegex = new Regex("[^\\/|*<>\" ?:]{1,}");
            if (fileNameRegex.IsMatch(fileName))
            {
                var fNameRegex = new Regex(@".{1,}\.[0-9\w]{1,}");
                if (fNameRegex.IsMatch(fileName))
                {
                    FileName = fileName;
                    DialogResult = true;
                }
                else
                {
                    ErrorString = Properties.Resources.FileValidationBadFileName;
                    DialogResult = false;
                }
            }
            else
            {
                ErrorString = Properties.Resources.FileValidationBadFileName;
                DialogResult = false;
            }
        }

        private void CbFileType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!((ComboBox) sender).IsFocused && !((ComboBox) sender).IsKeyboardFocusWithin)
            {
                return;
            }
            var tbText = TbFileName.Text;
            var extension = new Regex(@"\.[0-9\w]{1,}");
            var newExtension = e.AddedItems[0].ToString();
            if (!extension.IsMatch(tbText))
            {
                TbFileName.Text += newExtension;
                return;
            }
            if (newExtension.Equals("..."))
            {
                return;
            }
            TbFileName.Text = extension.Replace(tbText, newExtension);
        }
    }
}
