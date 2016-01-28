using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using WoblaExplorer.FilesUtil;
using WoblaExplorer.Util;
using Application = System.Windows.Application;
using ComboBox = System.Windows.Controls.ComboBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListView = System.Windows.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
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

            App.LanguageChanged += LanguageChanged;

            CultureInfo currentLanguage = App.Language;
            foreach (var language in App.Languages)
            {
                var menuItem = new MenuItem();
                menuItem.Header = language.DisplayName;
                menuItem.Tag = language;
                menuItem.IsChecked = Equals(currentLanguage);
                menuItem.Click += ChangeLanguageClick;
                LanguageMenu.Items.Add(menuItem);
            }
            try
            {
                App.Language = Properties.Settings.Default.DefaultLanguage;

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

        private void ChangeLanguageClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                CultureInfo language = menuItem.Tag as CultureInfo;
                if (language != null)
                {
                    App.Language = language;
                }
            }
        }

        private void LanguageChanged(object sender, EventArgs eventArgs)
        {
            CultureInfo currentLang = App.Language;

            foreach (MenuItem item in LanguageMenu.Items)
            {
                CultureInfo info = item.Tag as CultureInfo;
                item.IsChecked = info != null && info.Equals(currentLang);
            }
        }

        private async void InitExplorer()
        {
            Closing += (sender, args) =>
            {
                Properties.Settings.Default.LastDirectory = _fileDiver.CurrentPath;
                Properties.Settings.Default.WindowSize = new Size((int)MainWindowX.Width, (int)MainWindowX.Height);
                Properties.Settings.Default.WindowLocation = new Point((int)MainWindowX.Left, (int)MainWindowX.Top);
                Properties.Settings.Default.Save();
            };

            var settings = Properties.Settings.Default;
            string path;
            if (string.IsNullOrWhiteSpace(settings.LastDirectory))
            {
                path = CbDrives.SelectedValue.ToString();
            }
            else
            {
                path = settings.LastDirectory;
                CbDrives.SelectedItem = path.Substring(0, 3);
            }
            MainWindowX.Left = settings.WindowLocation.X;
            MainWindowX.Top = settings.WindowLocation.Y;
            MainWindowX.Height = settings.WindowSize.Height;
            MainWindowX.Width = settings.WindowSize.Width;

            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    return _fileDiver.DiveInto(path);
                }
                catch (Exception)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        ErrorPopup.IsOpen = true;
                        SystemSounds.Exclamation.Play();
                    });
                    return null;
                }
            });
            ListViewExplorer.ItemsSource = await task;
            ListViewExplorer.Focus();

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
                        _searchWindow.Title += $" || {Properties.Resources.MwSearchCompleted} {stopwatch.Elapsed}";
                    }
                }
                catch
                {
                    if (stopwatch.IsRunning)
                        stopwatch.Stop();
                    if (_searchWindow != null)
                    {
                        _searchWindow.Title += $" || {Properties.Resources.MwSearchCompleted} {stopwatch.Elapsed}";
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

                var task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        return _fileDiver.DiveInto(newPath);
                    }
                    catch (Exception)
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            ErrorPopup.IsOpen = true;
                            SystemSounds.Exclamation.Play();
                        });
                        return null;
                    }
                });
                ListViewExplorer.ItemsSource = await task;

                ChangeWindowTitle();

                await PbVisualization.TogglePbVisibilityAsync();
            }
        }

        private async void ListViewExplorer_Refresh()
        {
            await PbVisualization.TogglePbVisibilityAsync();

            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    return _fileDiver.DiveInto(_fileDiver.CurrentPath);
                }
                catch (Exception)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        ErrorPopup.IsOpen = true;
                        SystemSounds.Exclamation.Play();
                    });
                    return null;
                }
            });
            ListViewExplorer.ItemsSource = await task;

            ChangeWindowTitle();

            await PbVisualization.TogglePbVisibilityAsync();
        }

        private async void CbDrives_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await PbVisualization.TogglePbVisibilityAsync();

            try
            {
                if (!((ComboBox) sender).IsFocused)
                {
                    return;
                }
                string path = ((ComboBox) sender).SelectedValue.ToString();
                var task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        return _fileDiver.DiveInto(path);
                    }
                    catch
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            ErrorPopup.IsOpen = true;
                            SystemSounds.Exclamation.Play();
                        });
                        return null;
                    }
                });
                ListViewExplorer.ItemsSource = await task;
                ChangeWindowTitle();
            }
            catch (Exception)
            {
                ErrorPopup.IsOpen = true;
                SystemSounds.Exclamation.Play();
            }
            finally
            {
                await PbVisualization.TogglePbVisibilityAsync();
            }
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

        private async void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var listViewItem = e.OriginalSource as ListViewItem;
            var fsEntry = listViewItem?.DataContext as FileSystemInfo;
            if (fsEntry != null)
            {
                if (fsEntry.IsDirectory())
                {
                    await PbVisualization.TogglePbVisibilityAsync();

                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            return _fileDiver.DiveInto(fsEntry.FullName);
                        }
                        catch (Exception)
                        {
                            Dispatcher.InvokeAsync(() =>
                            {
                                ErrorPopup.IsOpen = true;
                                SystemSounds.Exclamation.Play();
                            });
                            return null;
                        }
                    });
                    ListViewExplorer.ItemsSource = await task;

                    ChangeWindowTitle();

                    await PbVisualization.TogglePbVisibilityAsync();
                }
                else
                {
                    Process.Start(fsEntry.FullName);
                }
            }
            else
            {
                var selectedItem = ListViewExplorer.SelectedItem as FileSystemInfo;
                if (selectedItem != null)
                {
                    if (selectedItem.IsDirectory())
                    {
                        await PbVisualization.TogglePbVisibilityAsync();

                        var task = Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                return _fileDiver.DiveInto(selectedItem.FullName);
                            }
                            catch (Exception)
                            {
                                Dispatcher.InvokeAsync(() =>
                                {
                                    ErrorPopup.IsOpen = true;
                                    SystemSounds.Exclamation.Play();
                                });
                                return null;
                            }
                        });
                        ListViewExplorer.ItemsSource = await task;

                        ChangeWindowTitle();

                        await PbVisualization.TogglePbVisibilityAsync();
                    }
                    else
                    {
                        Process.Start(selectedItem.FullName);
                    }
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
            if (ListViewExplorer.SelectedItems.Count > 1)
            {
                CustomErrorPopupTextBlock.Text = Properties.Resources.MwRenameError;
                CustomErrorPopup.IsOpen = true;
                SystemSounds.Exclamation.Play();
                return;
            }
            var listViewItem = e.OriginalSource as ListViewItem;
            var fsEntry = listViewItem?.DataContext as FileSystemInfo;
            if (fsEntry != null)
            {
                var renameDialog = new RenameDialog(fsEntry.Name)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };
                if (renameDialog.ShowDialog() != true) return;
                try
                {
                    var dialogRes = renameDialog.Filename.Clone().ToString();
                    var dirName = fsEntry.FullName.Substring(0, fsEntry.FullName.Length - fsEntry.Name.Length);
                    Directory.Move(fsEntry.FullName, dirName + dialogRes);
                    ListViewExplorer_Refresh();
                }
                catch (Exception)
                {
                    ErrorPopup.IsOpen = true;
                    SystemSounds.Exclamation.Play();
                }
            }
            else
            {
                var selectedItem = ListViewExplorer.SelectedItem as FileSystemInfo;
                if (selectedItem != null)
                {
                    var renameDialog = new RenameDialog(selectedItem.Name)
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = this
                    };
                    if (renameDialog.ShowDialog() != true) return;
                    try
                    {
                        var dialogRes = renameDialog.Filename.Clone().ToString();
                        var dirName = selectedItem.FullName.Substring(0, selectedItem.FullName.Length - selectedItem.Name.Length);
                        Directory.Move(selectedItem.FullName, dirName + dialogRes);
                        ListViewExplorer_Refresh();
                    }
                    catch (Exception)
                    {
                        ErrorPopup.IsOpen = true;
                        SystemSounds.Exclamation.Play();
                    }
                }
            }
        }

        private async void CopyToExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            await PbVisualization.TogglePbVisibilityAsync();
            var folder = new FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                Description = Properties.Resources.MwCopyToDescription
            };
            if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var destinationPath = folder.SelectedPath;
                var fsEntries = ListViewExplorer.SelectedItems;
                if (fsEntries != null)
                {
                    try
                    {
                        foreach (FileSystemInfo entry in fsEntries)
                        {
                            if (entry.IsDirectory())
                            {
                                var task = Task.Factory.StartNew(() =>
                                {
                                    try
                                    {
                                        _fileDiver.CopyAllInDir((DirectoryInfo) entry,
                                            new DirectoryInfo(destinationPath));
                                    }
                                    catch (Exception)
                                    {
                                        Dispatcher.InvokeAsync(() =>
                                        {
                                            ErrorPopup.IsOpen = true;
                                            SystemSounds.Exclamation.Play();
                                        });
                                    }
                                });
                                await task;
                            }
                            else
                            {
                                var task = Task.Factory.StartNew(() =>
                                {
                                    try
                                    {
                                        File.Copy(entry.FullName,
                                            Path.Combine(destinationPath, entry.Name), true);
                                    }
                                    catch (Exception)
                                    {
                                        Dispatcher.InvokeAsync(() =>
                                        {
                                            ErrorPopup.IsOpen = true;
                                            SystemSounds.Exclamation.Play();
                                        });
                                    }
                                });
                                await task;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        ErrorPopup.IsOpen = true;
                        SystemSounds.Exclamation.Play();
                    }
                    finally
                    {
                        ListViewExplorer_Refresh();
                    }
                }
            }
            await PbVisualization.TogglePbVisibilityAsync();
        }

        private async void RemoveExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            await PbVisualization.TogglePbVisibilityAsync();
            var fsEntries = ListViewExplorer.SelectedItems;
            if (fsEntries != null)
            {
                try
                {
                    foreach (FileSystemInfo entry in fsEntries)
                    {
                        if (entry.IsDirectory())
                        {
                            var task = Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    Directory.Delete(entry.FullName, true);
                                }
                                catch (Exception)
                                {
                                    Dispatcher.InvokeAsync(() =>
                                    {
                                        ErrorPopup.IsOpen = true;
                                        SystemSounds.Exclamation.Play();
                                    });
                                }
                            });
                            await task;
                        }
                        else
                        {
                            entry.Delete();
                        }
                    }
                }
                catch (Exception)
                {
                    ErrorPopup.IsOpen = true;
                    SystemSounds.Exclamation.Play();
                }
            }
            ListViewExplorer_Refresh();
            await PbVisualization.TogglePbVisibilityAsync();
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

        private void Popup_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ((Popup) sender).IsOpen = true;
            e.Handled = true;
        }

        private async void BackspaceExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            await PbVisualization.TogglePbVisibilityAsync();

            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    return _fileDiver.DiveBack();
                }
                catch (Exception)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        ErrorPopup.IsOpen = true;
                        SystemSounds.Exclamation.Play();
                    });
                    return null;
                }
            });

            ListViewExplorer.ItemsSource = await task;
            ChangeWindowTitle();

            await PbVisualization.TogglePbVisibilityAsync();
        }

        private void PopupLeftMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            var popup = sender as Popup;
            if (popup != null)
            {
                popup.IsOpen = false;
            }
        }
    }
}
