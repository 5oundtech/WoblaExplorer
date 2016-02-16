using System.Threading;
using System.Windows;

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
