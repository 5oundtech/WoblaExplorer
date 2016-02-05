using System.Deployment.Application;
using System.Windows;

namespace WoblaExplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for UpdateDialog.xaml
    /// </summary>
    public partial class UpdateDialog : Window
    {

        public ApplicationDeployment AppDeploy { get; set; }

        public bool CanceledCheck;
        public bool CanceledUpdate;
        public UpdateDialog()
        {
            InitializeComponent();
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            if (AppDeploy != null)
            {
                if (TbUpdateStage.Text.Equals(Properties.Resources.UdUpdateStageCheck))
                {
                    AppDeploy.CheckForUpdateAsyncCancel();
                    CanceledCheck = true;
                }
                else
                {
                    AppDeploy.UpdateAsyncCancel();
                    CanceledUpdate = true;
                }
            }
        }
    }
}
