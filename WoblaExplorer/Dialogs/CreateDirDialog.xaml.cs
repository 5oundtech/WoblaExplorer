using System;
using System.Windows;

namespace WoblaExplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for CreateDirDialog.xaml
    /// </summary>
    public partial class CreateDirDialog
    {
        public string DirName { get; set; }
        public CreateDirDialog()
        {
            InitializeComponent();
        }

        private void CreateDirDialog_OnContentRendered(object sender, EventArgs e)
        {
            TbDirectoryName.SelectAll();
            TbDirectoryName.Focus();
        }

        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            DirName = TbDirectoryName.Text;
            DialogResult = true;
        }
    }
}
