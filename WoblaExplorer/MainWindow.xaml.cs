using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml;
using WoblaExplorer.CustomCommands;
using WoblaExplorer.FilesUtil;
using WoblaExplorer.Util;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace WoblaExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly FileDiver _fileDiver;
        private SearchEngine _searchEngine;
        private CancellationTokenSource _tokenSource;
        private Task _searchTask;
        private SearchWindow _searchWindow;
        public MainWindow()
        {
            InitializeComponent();

            _fileDiver = new FileDiver();

            try
            {
                MainWindowX.WindowStartupLocation = WindowStartupLocation.Manual;

                _tokenSource = new CancellationTokenSource();
                _searchEngine = new SearchEngine();
                CbDrives.ItemsSource = Directory.GetLogicalDrives();

                CbDrives.SelectedIndex = 0;

                InitExplorer();
            }
            catch (IOException ioException)
            {
                MessageBox.Show(ioException.Message);
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                MessageBox.Show(unauthorizedAccessException.Message);
            }
        }

        private async void InitExplorer()
        {
            this.Closing += (sender, args) =>
            {
                Properties.Settings.Default.LastDirectory = _fileDiver.CurrentPath;
                Properties.Settings.Default.WindowSize = new Size((int)MainWindowX.Width, (int)MainWindowX.Height);
                Properties.Settings.Default.WindowLocation = new Point((int)MainWindowX.Left, (int)MainWindowX.Top);
                Properties.Settings.Default.Save();
            };

            var settings = Properties.Settings.Default;
            string path = string.Empty;
            if (string.IsNullOrWhiteSpace(settings.LastDirectory))
            {
                path = CbDrives.SelectedValue.ToString();
            }
            else
            {
                path = settings.LastDirectory;
            }
            MainWindowX.Left = settings.WindowLocation.X;
            MainWindowX.Top = settings.WindowLocation.Y;
            MainWindowX.Height = settings.WindowSize.Height;
            MainWindowX.Width = settings.WindowSize.Width;

            var task = Task.Factory.StartNew(() => _fileDiver.DiveInto(path));
            var fs = await task;
            await Task.Delay(300);
            ListViewExplorer.ItemsSource = fs;
            ChangeWindowTitle();
        }

        private async void BtnSearch_OnClick(object sender, RoutedEventArgs e)
        {
            if (_searchTask == null)
            {
                await PbVisualization.TogglePbVisibilityAsync();

                var stopwatch = Stopwatch.StartNew();

                _tokenSource = new CancellationTokenSource();
                _searchEngine = new SearchEngine();
                string pattern = TbSearchInput.Text;

                await Dispatcher.InvokeAsync(() =>
                {
                    _searchWindow?.LbSearchResults.Items.Clear();
                });

                _searchEngine.OnItemFound += async (o, args) =>
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (_searchWindow == null)
                        {
                            _searchWindow = new SearchWindow(_tokenSource);
                            _searchWindow.LbSearchResults.Items.Add(args.FoundedItem);
                            _searchWindow.Closing += (sender1, eventArgs) =>
                            {
                                _searchWindow = null;
                            };
                            _searchWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            _searchWindow.Owner = this;
                            _searchWindow.Show();
                        }
                        else
                        {
                            _searchWindow.LbSearchResults.Items.Add(args.FoundedItem);
                        }
                    });
                };
                
                _searchTask = new Task(()=>
                {
                    try
                    {
                        _searchEngine.RecursiveSearch(_fileDiver.CurrentPath, pattern, _tokenSource.Token);
                    }
                    catch(Exception)
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            SearchPopup.IsOpen = true;
                            SystemSounds.Exclamation.Play();
                        });
                    }
                }, _tokenSource.Token);
                _searchTask.Start();

                try
                {
                    await _searchTask;
                    stopwatch.Stop();
                    if (_searchWindow != null)
                    {
                        _searchWindow.Title += $" || Completed in {stopwatch.Elapsed}";
                    }
                }
                catch
                {
                    if (stopwatch.IsRunning)
                        stopwatch.Stop();
                    if (_searchWindow != null)
                    {
                        _searchWindow.Title += $" || Completed in {stopwatch.Elapsed}";
                    }
                    SearchPopup.IsOpen = true;
                    SystemSounds.Exclamation.Play();
                }


                _searchTask = null;

                await PbVisualization.TogglePbVisibilityAsync();
            }
            else
            {
                _tokenSource.Cancel(true);
                await PbVisualization.TogglePbVisibilityAsync();
            }
        }

        private async void ListViewExplorer_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = ((ListView) sender).SelectedItem.ToString();
            string newPath = _fileDiver.CurrentPath + selectedItem;
            if (Directory.Exists(newPath))
            {
                await PbVisualization.TogglePbVisibilityAsync();

                var task = Task.Factory.StartNew(() => _fileDiver.DiveInto(newPath));
                ListViewExplorer.ItemsSource = await task;

                ChangeWindowTitle();

                await PbVisualization.TogglePbVisibilityAsync();
            }
        }

        private async void CbDrives_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await PbVisualization.TogglePbVisibilityAsync();

            try
            {
                string path = ((ComboBox) sender).SelectedValue.ToString();
                var task = Task.Factory.StartNew(() => _fileDiver.DiveInto(path));
                var fs = await task;
                await ListViewExplorer.Dispatcher.InvokeAsync(() =>
                {
                    ListViewExplorer.ItemsSource = fs;
                });
                ChangeWindowTitle();
            }
            catch (Exception ex)
            {
                ErrorPopup.IsOpen = true;
                SystemSounds.Exclamation.Play();
            }

            await PbVisualization.TogglePbVisibilityAsync();
        }

        private async void BtnDiveBack_OnClick(object sender, RoutedEventArgs e)
        {
            await PbVisualization.TogglePbVisibilityAsync();

            var task = Task.Factory.StartNew(() => _fileDiver.DiveBack());
            var fs = await task;

            await ListViewExplorer.Dispatcher.InvokeAsync(() =>
            {
                ListViewExplorer.ItemsSource = fs;
            });
            ChangeWindowTitle();

            await PbVisualization.TogglePbVisibilityAsync();
        }

        private async void ChangeWindowTitle()
        {
            string fileDiverPath = _fileDiver.CurrentPath;
            await MainWindowX.Dispatcher.InvokeAsync(() =>
            {
                MainWindowX.Title = "WoblaExplorer || " + fileDiverPath;
            });
        }

        private void TbSearchInput_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && TbSearchInput.Text.Length > 0)
                BtnSearch_OnClick(sender, e);
        }

        private void DefaultCommands_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var listViewItem = e.OriginalSource as ListViewItem;
            var fsEntry = listViewItem?.DataContext as FileSystemInfo;
            if (fsEntry != null)
            {
                Process.Start(fsEntry.FullName);
            }
            else
            {
                var selectedItem = ListViewExplorer.SelectedItem as FileSystemInfo;
                if (selectedItem != null)
                {
                    Process.Start(selectedItem.FullName);
                }
            }
        }

        private void ShowInExplorerExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var listViewItem = e.OriginalSource as ListViewItem;
            var fsEntry = listViewItem?.DataContext as FileSystemInfo;
            if (fsEntry != null)
            {
                Process.Start("explorer.exe", @"/select, " + fsEntry.FullName);
            }
            else
            {
                var selectedItem = ListViewExplorer.SelectedItem as FileSystemInfo;
                if (selectedItem != null)
                {
                    Process.Start("explorer.exe", @"/select, " + selectedItem.FullName);
                }
            }
        }

        private void RenameExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            //TODO make rename func
        }

        private void CopyToExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            //TODO Copy func
        }

        private void RemoveExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            //TODO remove func
        }

        private void ExitExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _searchWindow?.Close();
            Application.Current.Shutdown();
        }

        private void AboutDialogExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            aboutWindow.ShowDialog();
        }

        private void SearchPopup_OnMouseEnter(object sender, MouseEventArgs e)
        {
            SearchPopup.IsOpen = true;
            e.Handled = true;
        }

        private void ErrorPopup_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ErrorPopup.IsOpen = true;
            e.Handled = true;
        }
    }
}
