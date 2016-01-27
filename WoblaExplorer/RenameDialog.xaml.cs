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

namespace WoblaExplorer
{
    /// <summary>
    /// Interaction logic for RenameDialog.xaml
    /// </summary>
    public partial class RenameDialog : Window
    {
        public string Filename { get; set; }

        public RenameDialog(string fileName)
        {
            InitializeComponent();

            Filename = fileName.Clone().ToString();
            TbFileName.Text = fileName;
        }


        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            Filename = TbFileName.Text.Clone().ToString();
            this.DialogResult = true;
        }

        private void RenameDialog_OnContentRendered(object sender, EventArgs e)
        {
            TbFileName.SelectAll();
            TbFileName.Focus();
        }

        private void TbFileName_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (TbFileName.Text.Length > 0)
                BtnOk_OnClick(sender, e);
        }
    }
}
