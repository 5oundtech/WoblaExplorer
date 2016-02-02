using System;
using System.Collections.Generic;
using System.Deployment.Application;
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
    /// Interaction logic for UpdateDialog.xaml
    /// </summary>
    public partial class UpdateDialog : Window
    {

        public ApplicationDeployment AppDeploy;
        public UpdateDialog()
        {
            InitializeComponent();
        }

        public UpdateDialog(ref ApplicationDeployment appDeploy)
        {
            AppDeploy = appDeploy;
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            if (AppDeploy != null)
            {
                if (TbUpdateStage.Text.Equals(Properties.Resources.UdUpdateStageCheck))
                {
                    AppDeploy.CheckForUpdateAsyncCancel();
                }
                else
                {
                    AppDeploy.UpdateAsyncCancel();
                }
            }
        }
    }
}
