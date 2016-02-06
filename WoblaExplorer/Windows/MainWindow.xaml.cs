using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using WoblaExplorer.Annotations;
using WoblaExplorer.Dialogs;
using WoblaExplorer.FilesUtil;
using WoblaExplorer.Properties;
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

namespace WoblaExplorer.Windows
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
                var menuItem = new MenuItem
                {
                    Header = language.DisplayName,
                    Tag = language,
                    IsChecked = Equals(currentLanguage)
                };
                menuItem.Click += ChangeLanguageClick;
                LanguageMenu.Items.Add(menuItem);
            }
            try
            {
                App.Language = Settings.Default.DefaultLanguage;
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
                Settings.Default.LastDirectory = _fileDiver.CurrentPath;
                Settings.Default.WindowSize = new Size((int)MainWindowX.Width, (int)MainWindowX.Height);
                Settings.Default.WindowLocation = new Point((int)MainWindowX.Left, (int)MainWindowX.Top);
                Settings.Default.Save();
            };
            var settings = Settings.Default;
            string path;
            if (String.IsNullOrWhiteSpace(settings.LastDirectory))
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
                        _searchEngine.SearchUsingEnumeration(_fileDiver.CurrentPath, pattern, _tokenSource.Token);
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
            var selectedItem = ((ListView) sender).SelectedItem;
            if (selectedItem == null) return;
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
            else
            {
                try
                {
                    Process.Start(newPath);
                }
                catch (Exception)
                {
                    ErrorPopup.IsOpen = true;
                    SystemSounds.Exclamation.Play();
                }
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
                        var copyDialog = new CopyDialog
                        {
                            Owner = this,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        };
                        var copyTask = Task.Factory.StartNew(() =>
                        {
                            Dispatcher.InvokeAsync(() =>
                            {
                                copyDialog.PbCopyProgress.Maximum = fsEntries.Count;
                                copyDialog.ShowDialog();
                            });
                            foreach (FileSystemInfo entry in fsEntries)
                            {
                                if (copyDialog.Canceled)
                                {
                                    copyDialog.Close();
                                    return;
                                }
                                Dispatcher.InvokeAsync(() =>
                                {
                                    copyDialog.TbCopyObject.Text = entry.Name;
                                });
                                if (entry.IsDirectory())
                                {
                                    try
                                    {
                                        Directory.CreateDirectory(Path.Combine(destinationPath, entry.Name));
                                        _fileDiver.CopyAllInDir((DirectoryInfo) entry,
                                            new DirectoryInfo(Path.Combine(destinationPath, entry.Name)));
                                    }
                                    catch (Exception)
                                    {
                                        Dispatcher.InvokeAsync(() =>
                                        {
                                            ErrorPopup.IsOpen = true;
                                            SystemSounds.Exclamation.Play();
                                        });
                                    }
                                }
                                else
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
                                }
                                Dispatcher.InvokeAsync(() => { copyDialog.PbCopyProgress.Value++; });
                            }
                        });
                        await copyTask;
                        if (copyDialog.Visibility == Visibility.Visible)
                        {
                            copyDialog.Close();
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
                var dialogResult = MessageBox.Show(Properties.Resources.MwRemoveDialogText, Properties.Resources.MwRemoveDialogTitle,
MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (dialogResult != MessageBoxResult.Yes)
                {
                    await PbVisualization.TogglePbVisibilityAsync();
                    return;
                }
                try
                {
                    var delTask = Task.Factory.StartNew(() =>
                    {
                        foreach (FileSystemInfo entry in fsEntries)
                        {
                            try
                            {
                                if (entry.IsDirectory())
                                {
                                    Directory.Delete(entry.FullName, true);
                                }
                                else
                                {
                                    entry.Delete();
                                }
                            }
                            catch (Exception)
                            {
                                Dispatcher.InvokeAsync(() =>
                                {
                                    ErrorPopup.IsOpen = true;
                                    SystemSounds.Exclamation.Play();
                                });
                            }
                        }
                    });
                    await delTask;
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

        private async void PropertiesDialogExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var entries = ListViewExplorer.SelectedItems;
            if (entries.Count > 1)
            {
                var properties = new PropertiesWindow
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                properties.Show();
                properties.Title += $" {((FileSystemInfo) entries[0]).Name}, ...";
                properties.TbFileName.Text = $"{((FileSystemInfo) entries[0]).Name}, ...";
                properties.TbFilePath.Text = $"{_fileDiver.CurrentPath}";
                properties.MultipleFilesPanel.Visibility = Visibility.Visible;
                await properties.MainProgressBar.TogglePbVisibilityAsync();
                var task = new Task(async () =>
                {
                    try
                    {
                        long[] mainArray = {0, 0, 0};
                        foreach (FileSystemInfo entry in entries)
                        {
                            if (entry.IsDirectory())
                            {
                                var dir = new DirectoryInfo(entry.FullName);
                                dir.EnumerateDirectories("*", SearchOption.AllDirectories)
                                    .AsParallel()
                                    .ForAll(item => mainArray[0]++);
                                dir.EnumerateFiles("*", SearchOption.AllDirectories).AsParallel().ForAll(
                                    item =>
                                    {
                                        mainArray[2] += item.Length;
                                        mainArray[1]++;
                                    });

                                mainArray[0]++;
                            }
                            else
                            {
                                var file = new FileInfo(entry.FullName);
                                mainArray[2] += file.Length;
                                mainArray[1]++;
                            }

                            await Dispatcher.InvokeAsync(() =>
                            {
                                if (properties.Visibility != Visibility.Visible) return;
                                properties.TbDirsCount.Text = mainArray[0].ToString();
                                properties.TbFilesCount.Text = mainArray[1].ToString();
                                properties.TbFileSizeBytes.Text = $"{mainArray[2]}";
                                properties.TbFileSizeMb.Text = $"{mainArray[2].BytesToMb():F3}";
                                properties.TbFileSizeGb.Text = $"{mainArray[2].BytesToGb():F3}";
                            });
                        }
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
                task.Start();
                await task;
                await properties.MainProgressBar.TogglePbVisibilityAsync();
            }
            else
            {
                var entry = entries[0] as FileSystemInfo;
                if (entry.IsDirectory())
                {
                    var dir = entry as DirectoryInfo;
                    if (dir != null)
                    {
                        var properties = new PropertiesWindow
                        {
                            Owner = this,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        };
                        properties.Show();
                        properties.Title += $" {dir.Name}";
                        properties.TbFileName.Text = $"{dir.Name}";
                        properties.TbFilePath.Text = dir.FullName;
                        await properties.MainProgressBar.TogglePbVisibilityAsync();
                        var calcTask = new Task(async () =>
                        {
                            try
                            {
                                long dirsCount = 0;
                                dir.EnumerateDirectories("*", SearchOption.AllDirectories).AsParallel().ForAll(
                                    item => dirsCount++);
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    if (properties.Visibility != Visibility.Visible) return;
                                    properties.TbDirsCount.Text = dirsCount.ToString();
                                    properties.MultipleFilesPanel.Visibility = Visibility.Visible;
                                });

                                long filesSize = 0;
                                long filesCount = 0;
                                dir.EnumerateFiles("*", SearchOption.AllDirectories)
                                    .AsParallel()
                                    .ForAll(item =>
                                    {
                                        filesCount++;
                                        filesSize += item.Length;
                                    });

                                await Dispatcher.InvokeAsync(() =>
                                {
                                    if (properties.Visibility != Visibility.Visible) return;
                                    properties.TbFilesCount.Text = filesCount.ToString();
                                    properties.TbFileSizeBytes.Text = $"{filesSize}";
                                    properties.TbFileSizeMb.Text = $"{filesSize.BytesToMb():F3}";
                                    properties.TbFileSizeGb.Text = $"{filesSize.BytesToGb():F3}";
                                });
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
                        calcTask.Start();
                        await calcTask;
                        await properties.MainProgressBar.TogglePbVisibilityAsync();
                    }
                }
                else
                {
                    var properties = new PropertiesWindow((FileInfo) entry)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    properties.Show();
                }
            }
        }

        private void ReRunAsAdministratorExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var processInfo = new ProcessStartInfo(Assembly.GetExecutingAssembly().CodeBase)
            {
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(processInfo);

                Application.Current.Shutdown();
            }
            catch (Exception)
            {
                ErrorPopup.IsOpen = true;
                SystemSounds.Exclamation.Play();
            }
        }

        private void CreateDirectoryExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new CreateDirDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };
            if (e.Parameter != null)
            {
                if (e.Parameter.ToString() == "0")
                {
                    var curDirPath = _fileDiver.CurrentPath;
                    if (dialog.ShowDialog() == true)
                    {
                        var newDir = curDirPath + dialog.DirName;
                        try
                        {
                            Directory.CreateDirectory(newDir);
                        }
                        catch (Exception)
                        {
                            ErrorPopup.IsOpen = true;
                            SystemSounds.Exclamation.Play();
                        }
                    }
                }
                if (e.Parameter.ToString() == "1")
                {
                    var selectedItem = ListViewExplorer.SelectedItem as FileSystemInfo;
                    if (selectedItem == null)
                    {
                        return;
                    }
                    if (selectedItem.IsDirectory())
                    {
                        if (dialog.ShowDialog() == true)
                        {
                            var newDir = selectedItem.FullName + "\\" + dialog.DirName;
                            try
                            {
                                Directory.CreateDirectory(newDir);
                            }
                            catch (Exception)
                            {
                                ErrorPopup.IsOpen = true;
                                SystemSounds.Exclamation.Play();
                            }
                        }
                    }
                    else
                    {
                        CustomErrorPopupTextBlock.Text = Properties.Resources.MwCreateDirInFileError;
                        CustomErrorPopup.IsOpen = true;
                        SystemSounds.Exclamation.Play();
                    }
                }
            }
            else
            {
                var curDirPath = _fileDiver.CurrentPath;
                if (dialog.ShowDialog() == true)
                {
                    var newDir = curDirPath + dialog.DirName;
                    try
                    {
                        Directory.CreateDirectory(newDir);
                    }
                    catch (Exception)
                    {
                        ErrorPopup.IsOpen = true;
                        SystemSounds.Exclamation.Play();
                    }
                }
            }
            ListViewExplorer_Refresh();
        }

        private void CheckForUpdatesExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CheckForUpdates();
        }

        private UpdateDialog _updateDialog;
        private ApplicationDeployment _appDeploy;

        private void BeginUpdate()
        {
            _appDeploy = ApplicationDeployment.CurrentDeployment;
            _updateDialog.TbUpdateStage.Text = Properties.Resources.UdUpdateStageUpdating;
            _updateDialog.Title = Properties.Resources.UdUpdateStageUpdating;
            _appDeploy.UpdateCompleted -= AppDeployOnUpdateCompleted;
            _appDeploy.UpdateCompleted += AppDeployOnUpdateCompleted;
            _appDeploy.UpdateProgressChanged -= AppDeployOnUpdateProgressChanged;
            _appDeploy.UpdateProgressChanged += AppDeployOnUpdateProgressChanged;
            try
            {
                _appDeploy.UpdateAsync();
            }
            catch (DeploymentDownloadException dde)
            {
                MessageBox.Show(Properties.Resources.MwDeploymentDownloadException + dde.Message);
            }
            catch (InvalidDeploymentException ide)
            {
                MessageBox.Show(Properties.Resources.MwInvalidDeploymentException + ide.Message);
            }
            catch (InvalidOperationException ioe)
            {
                MessageBox.Show(Properties.Resources.MwInvalidOperationException + ioe.Message);
            }
        }

        private void AppDeployOnUpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs args)
        {
            if (_updateDialog.CanceledUpdate)
                _appDeploy.UpdateAsyncCancel();
            switch (args.State)
            {
                case DeploymentProgressState.DownloadingApplicationFiles:
                    _updateDialog.TbUpdateState.Text =
                        Properties.Resources.DeploymentProgressStateDownloadingApplicationFiles;
                    break;
                case DeploymentProgressState.DownloadingApplicationInformation:
                    _updateDialog.TbUpdateState.Text =
                        Properties.Resources.DeploymentProgressStateDownloadingApplicationInformation;
                    break;
                default:
                    _updateDialog.TbUpdateState.Text =
                        Properties.Resources.DeploymentProgressStateDownloadingDeploymentInformation;
                    break;
            }
            _updateDialog.PbDownloadProgress.Value = args.ProgressPercentage;
        }

        private void AppDeployOnUpdateCompleted(object sender, AsyncCompletedEventArgs args)
        {
            if (args.Error != null)
            {
                MessageBox.Show(Properties.Resources.MwDeploymentDownloadException + args.Error.Message);
                return;
            }
            if (args.Cancelled)
            {
                _updateDialog.Close();
                return;
            }

            MessageBox.Show(Properties.Resources.MwAppUpdatedText, Properties.Resources.MwAppUpdatedHeader,
                MessageBoxButton.OK, MessageBoxImage.Information);
            _updateDialog.Close();
        }

        private void CheckForUpdates()
        {
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                _appDeploy = ApplicationDeployment.CurrentDeployment;
                _updateDialog = new UpdateDialog
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    TbUpdateStage = {Text = Properties.Resources.UdUpdateStageCheck},
                    Title = Properties.Resources.UdUpdateStageUpdating,
                    AppDeploy = _appDeploy
                };
                _appDeploy.CheckForUpdateProgressChanged -= AppDeployOnCheckForUpdateProgressChanged;
                _appDeploy.CheckForUpdateProgressChanged += AppDeployOnCheckForUpdateProgressChanged;
                _appDeploy.CheckForUpdateCompleted -= AppDeployOnCheckForUpdateCompleted;
                _appDeploy.CheckForUpdateCompleted += AppDeployOnCheckForUpdateCompleted;

                try
                {
                    _appDeploy.CheckForUpdateAsync();
                    _updateDialog.ShowDialog();
                }
                catch (DeploymentDownloadException dde)
                {
                    MessageBox.Show(Properties.Resources.MwDeploymentDownloadException + dde.Message);
                }
                catch (InvalidDeploymentException ide)
                {
                    MessageBox.Show(Properties.Resources.MwInvalidDeploymentException + ide.Message);
                }
                catch (InvalidOperationException ioe)
                {
                    MessageBox.Show(Properties.Resources.MwInvalidOperationException + ioe.Message);
                }
            }
            else
            {
                MessageBox.Show(Properties.Resources.MwWrongAppUpdate);
            }
        }

        private void AppDeployOnCheckForUpdateCompleted(object sender, CheckForUpdateCompletedEventArgs args)
        {
            if (args.Error != null)
            {
                MessageBox.Show(Properties.Resources.MwDeploymentDownloadException + args.Error.Message);
                return;
            }
            if (args.Cancelled)
            {
                _updateDialog.Close();
                return;
            }

            if (args.UpdateAvailable)
            {
                var dialogResult = MessageBox.Show(Properties.Resources.MwUpdateDialogContentBeforeVersion +
                                                   args.AvailableVersion +
                                                   Properties.Resources.MwUpdateDialogContentAfterVersion,
                    Properties.Resources.MwUpdateDialogHeader, MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    _updateDialog.PbDownloadProgress.Value = 0;
                    BeginUpdate();
                }
                else
                {
                    _updateDialog.Close();
                }
            }
            else
            {
                MessageBox.Show(Properties.Resources.MwNoUpdatesText, Properties.Resources.MwNoUpdatesHeader,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                _updateDialog.Close();
            }
        }

        private void AppDeployOnCheckForUpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs args)
        {
            _updateDialog.PbDownloadProgress.Value = args.ProgressPercentage;
            if (_updateDialog.CanceledCheck)
                _appDeploy.CheckForUpdateAsyncCancel();
            switch (args.State)
            {
                case DeploymentProgressState.DownloadingApplicationFiles:
                    _updateDialog.TbUpdateState.Text =
                        Properties.Resources.DeploymentProgressStateDownloadingApplicationFiles;
                    break;
                case DeploymentProgressState.DownloadingApplicationInformation:
                    _updateDialog.TbUpdateState.Text =
                        Properties.Resources.DeploymentProgressStateDownloadingApplicationInformation;
                    break;
                default:
                    _updateDialog.TbUpdateState.Text =
                        Properties.Resources.DeploymentProgressStateDownloadingDeploymentInformation;
                    break;
            }
        }

        private void ModifySelectionExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter == null) return;
            if (e.Parameter.ToString().ToLowerInvariant().Equals("selectall"))
            {
                ListViewExplorer.SelectedItems.Clear();
                ListViewExplorer.Items.Cast<FileSystemInfo>()
                    .AsParallel()
                    .ForAll(info => { Dispatcher.InvokeAsync(()=> { ListViewExplorer.SelectedItems.Add(info); }); });
            }
            else
            {
                var fs = new FileSystemInfo[ListViewExplorer.SelectedItems.Count];
                ListViewExplorer.SelectedItems.CopyTo(fs, 0);
                if (fs.Length > 0)
                {
                    ListViewExplorer.SelectedItems.Clear();
                    ListViewExplorer.Items.Cast<FileSystemInfo>()
                        .AsParallel()
                        .ForAll(info =>
                        {
                            if (!fs.Contains(info))
                            {
                                Dispatcher.InvokeAsync(() => { ListViewExplorer.SelectedItems.Add(info); });
                            }
                        });
                }
            }
        }

        private void CbDrives_OnDropDownOpened(object sender, EventArgs e)
        {
            var curDrive = CbDrives.SelectedValue;
            try
            {
                var context = Directory.GetLogicalDrives();
                if (context.Length == 0) return;
                CbDrives.ItemsSource = context;
                CbDrives.SelectedItem = curDrive;
            }
            catch (Exception)
            {
                
            }
        }
    }
}
