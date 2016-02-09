using System.Windows;

namespace WoblaExplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for CopyDialog.xaml
    /// </summary>
    public partial class CopyDialog
    {
        public bool Canceled;

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
