using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaction logic for CreateDirDialog.xaml
    /// </summary>
    public partial class CreateDirDialog : Window
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
            this.DialogResult = true;
        }
    }
}
