using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using WoblaExplorer.Util;

namespace WoblaExplorer.Windows
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow
    {
        public static DependencyProperty AssemblyVersionDependencyProperty =
            DependencyProperty.Register("AssemblyVersion", typeof (string), typeof (AboutWindow));

        
        public string AssemblyVersion
        {
            get { return (string) GetValue(AssemblyVersionDependencyProperty); }
            set { SetValue(AssemblyVersionDependencyProperty, value);}
        }


        public AboutWindow()
        {
            InitializeComponent();
            try
            {
                SetValue(AssemblyVersionDependencyProperty, Assembly.GetExecutingAssembly().GetName().Version.ToString());

                AppImage.Source = Properties.Resources.ExplorerImage.ToImageSource();
            }
            catch
            {
                
            }
        }

        private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            var link = e.OriginalSource as Hyperlink;
            if (link != null)
            {
                Process.Start(link.NavigateUri.ToString());
            }
        }
    }
}
