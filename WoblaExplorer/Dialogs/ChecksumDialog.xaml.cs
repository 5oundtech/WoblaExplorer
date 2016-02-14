using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for ChecksumDialog.xaml
    /// </summary>
    public partial class ChecksumDialog
    {
        public CancellationTokenSource CancellationToken { get; set; }
        public ChecksumDialog()
        {
            InitializeComponent();
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            CancellationToken.Cancel(true);
            DialogResult = false;
        }
    }
}
