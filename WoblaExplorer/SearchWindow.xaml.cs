using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WoblaExplorer
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        private CancellationTokenSource _cancellationTokenSource;
        public SearchWindow()
        {
            InitializeComponent();

        }

        public SearchWindow(CancellationTokenSource tokenSource)
        {
            InitializeComponent();
            _cancellationTokenSource = tokenSource;
            SWindow.Closing += (sender, args) =>
            {
                _cancellationTokenSource.Cancel();
            };
        }

        private void LbSearchResults_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void LbSearchResults_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem) sender;
            var menuHeader = menuItem.Header.ToString();
            switch (menuHeader)
            {
                case "Open":
                    var fs = (FileSystemInfo) LbSearchResults.SelectedItem;
                    if (fs != null)
                    {
                        Process.Start(fs.FullName);
                    }
                    break;
                case "Show in Windows Explorer":
                    var file = (FileSystemInfo) LbSearchResults.SelectedItem;
                    if (file != null)
                    {
                        Process.Start("explorer.exe", @"/select, " + file.FullName);
                    }
                    break;
            }
        }
    }
}
