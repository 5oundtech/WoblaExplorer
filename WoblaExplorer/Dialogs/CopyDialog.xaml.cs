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
    /// Interaction logic for CopyDialog.xaml
    /// </summary>
    public partial class CopyDialog : Window
    {
        public bool Canceled = false;

        public CopyDialog()
        {
            InitializeComponent();
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            PbCopyProgress.IsIndeterminate = true;
            TbCopyObject.Text = "";
            Canceled = true;
        }
    }
}
