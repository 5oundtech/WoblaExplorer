using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WoblaExplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for CreateFileDialog.xaml
    /// </summary>
    public partial class CreateFileDialog
    {
        public string FileName { get; set; }
        public string ErrorString { get; set; }

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
