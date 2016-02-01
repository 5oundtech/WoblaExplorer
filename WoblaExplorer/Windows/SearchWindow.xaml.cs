using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WoblaExplorer.Windows
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

        private void DefaultCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var listBoxItem = e.OriginalSource as ListBoxItem;
            var fsEntry = listBoxItem?.DataContext as FileSystemInfo;
            if (fsEntry != null)
            {
                Process.Start(fsEntry.FullName);
            }
            else
            {
                var selectedItem = LbSearchResults.SelectedItem as FileSystemInfo;
                if (selectedItem != null)
                {
                    Process.Start(selectedItem.FullName);
                }
            }
        }

        private void ShowInExplorerExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var listBoxItem = e.OriginalSource as ListBoxItem;
            var fsEntry = listBoxItem?.DataContext as FileSystemInfo;
            if (fsEntry != null)
            {
                Process.Start("explorer.exe", @"/select, " + fsEntry.FullName);
            }
            else
            {
                var selectedItem = LbSearchResults.SelectedItem as FileSystemInfo;
                if (selectedItem != null)
                {
                    Process.Start("explorer.exe", @"/select, " + selectedItem.FullName);
                }
            }
        }
    }
}
