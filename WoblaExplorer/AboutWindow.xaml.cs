using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using WoblaExplorer.Annotations;
using WoblaExplorer.Util;

namespace WoblaExplorer
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
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
