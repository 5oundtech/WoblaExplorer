using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Elysium;
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
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace WoblaExplorer.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly FileDiver _fileDiver;
        private SearchEngine _searchEngine;
        private CancellationTokenSource _tokenSource;
        private Task _searchTask;
        private SearchWindow _searchWindow;

        public SolidColorBrush GetCurrentAccentBrush
        {
            get { return this.GetAccentBrush(); }
        }

        public SolidColorBrush GetCurrentContrastBrush
        {
            get
            {
                if (Elysium.Manager.GetTheme(Application.Current) == Theme.Dark)
                {
                    return Elysium.Manager.DefaultContrastBrush;
                }
                return new SolidColorBrush(Colors.Black);
            }
        }

        public Theme GetCurrenTheme
        {
            get { return Application.Current.GetTheme(); }
        }

        public bool IsRunningWithAdminRights
        {
            get
            {
                try
                {
                    var identity = WindowsIdentity.GetCurrent();
                    if (identity == null) return false;
                    var principal = new WindowsPrincipal(identity);

                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
                catch (SecurityException securityException)
                {
                }
                return false;
            }
        }

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

            if (!File.Exists(DbHelper.DbPath))
            {
                DbHelper.CreateDb();
                DbHelper.CreateReadedFilesTable();
            }

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
                var messageDialog = new MessageDialog(ioException.Message)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };
                messageDialog.ShowDialog();
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                var messageDialog = new MessageDialog(unauthorizedAccessException.Message)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };
                messageDialog.ShowDialog();
            }
        }

        private void ChangeLanguageClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var menuItem = sender as MenuItem;
            CultureInfo language = menuItem?.Tag as CultureInfo;
            if (language != null)
            {
                App.Language = language;
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
                Settings.Default.DefaultAccentColor = GetCurrentAccentBrush;
                Settings.Default.DefaultTheme = GetCurrenTheme == Theme.Light ? 0 : 1;
                Settings.Default.Save();
            };
            var settings = Settings.Default;
            string path;
            var theme = settings.DefaultTheme == 0 ? Theme.Light : Theme.Dark;
            Application.Current.Apply(theme, settings.DefaultAccentColor, Elysium.Manager.DefaultContrastBrush);
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

            UpdateWindowTitle();
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

                UpdateWindowTitle();

                DbHelper.InsertReadedFile(newPath);

                await PbVisualization.TogglePbVisibilityAsync();
            }
            else
            {
                try
                {
                    Process.Start(newPath);

                    DbHelper.InsertReadedFile(newPath);
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

            UpdateWindowTitle();

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
                UpdateWindowTitle();
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

        private async void UpdateWindowTitle()
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

                    UpdateWindowTitle();

                    DbHelper.InsertReadedFile(fsEntry.FullName);

                    await PbVisualization.TogglePbVisibilityAsync();
                }
                else
                {
                    Process.Start(fsEntry.FullName);
                    DbHelper.InsertReadedFile(fsEntry.FullName);
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

                        UpdateWindowTitle();

                        DbHelper.InsertReadedFile(selectedItem.FullName);

                        await PbVisualization.TogglePbVisibilityAsync();
                    }
                    else
                    {
                        Process.Start(selectedItem.FullName);
                        DbHelper.InsertReadedFile(selectedItem.FullName);
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

        private async void MoveFilesExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ListViewExplorer.SelectedItems.Count < 1)
                return;

            await PbVisualization.TogglePbVisibilityAsync();


            bool doMove = false;
            string destinationPath = string.Empty;
            await Dispatcher.InvokeAsync(() =>
            {
                var dialog = new PickDialog(PickDialogType.Directory, _fileDiver.CurrentPath)
                {
                    Title = Properties.Resources.PdTitleDir,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                if (dialog.ShowDialog() == true)
                {
                    doMove = true;
                    destinationPath = dialog.SelectedPath;
                }
            });

            if (doMove)
            {
                
                var fsEntries = ListViewExplorer.SelectedItems;
                if (fsEntries != null)
                {
                    var moveTask = Task.Factory.StartNew(() =>
                    {
                        foreach (FileSystemInfo entry in fsEntries)
                        {
                            try
                            {
                                string watch = Path.Combine(destinationPath, entry.Name);
                                Directory.Move(entry.FullName, watch);
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
                    await moveTask;
                }
            }

            ListViewExplorer_Refresh();
            await PbVisualization.TogglePbVisibilityAsync();
        }

        private async void CopyToExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ListViewExplorer.SelectedItems.Count <= 0) return;
            await PbVisualization.TogglePbVisibilityAsync();
            var dialog = new PickDialog(PickDialogType.Directory, _fileDiver.CurrentPath)
            {
                Title = Properties.Resources.PdTitleDir,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.ShowDialog();
            if (dialog.PickDialogResult == PickDialogResult.Ok)
            {
                var destinationPath = dialog.SelectedPath;
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
                                    Dispatcher.InvokeAsync(() =>
                                    {
                                        copyDialog.Close();
                                    });
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
            if (ListViewExplorer.SelectedItems.Count <= 0) return;
            await PbVisualization.TogglePbVisibilityAsync();
            var fsEntries = ListViewExplorer.SelectedItems;
            if (fsEntries != null)
            {
                var messageDialog = new MessageDialog(Properties.Resources.MwRemoveDialogText,
                    Properties.Resources.MwRemoveDialogTitle,
                    MessageDialogButtons.YesNo)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };
                messageDialog.ShowDialog();
                if (messageDialog.MessageDialogResult != MessageDialogResult.Yes)
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
            UpdateWindowTitle();

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
            else if (entries.Count == 1)
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
        private async void CreateFileExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter == null) return;

            await PbVisualization.TogglePbVisibilityAsync();
            var dialog = new CreateFileDialog
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var curDirPath = _fileDiver.CurrentPath;
            switch (e.Parameter.ToString())
            {
                case "0":
                    if (dialog.ShowDialog() == true)
                    {
                        var file = Path.Combine(curDirPath, dialog.FileName);
                        File.Create(file);
                        if (!File.Exists(file))
                        {
                            ErrorPopup.IsOpen = true;
                            await PbVisualization.TogglePbVisibilityAsync();
                            return;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(dialog.ErrorString))
                        {
                            CustomErrorPopupTextBlock.Text = dialog.ErrorString;
                            CustomErrorPopup.IsOpen = true;
                            await PbVisualization.TogglePbVisibilityAsync();
                            return;
                        }
                    }
                    break;
                case "1":
                    var selectedItem = ListViewExplorer.SelectedItem as FileSystemInfo;
                    if (selectedItem == null)
                        return;
                    if (!selectedItem.IsDirectory())
                    {
                        CustomErrorPopupTextBlock.Text = Properties.Resources.MwCreateFileInFileError;
                        CustomErrorPopup.IsOpen = true;
                        await PbVisualization.TogglePbVisibilityAsync();
                        return;
                    }
                    if (dialog.ShowDialog() == true)
                    {

                        var file = Path.Combine(selectedItem.FullName, dialog.FileName);
                        File.Create(file);
                        if (!File.Exists(file))
                        {
                            ErrorPopup.IsOpen = true;
                            await PbVisualization.TogglePbVisibilityAsync();
                            return;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(dialog.ErrorString))
                        {
                            CustomErrorPopupTextBlock.Text = dialog.ErrorString;
                            CustomErrorPopup.IsOpen = true;
                            await PbVisualization.TogglePbVisibilityAsync();
                            return;
                        }
                    }
                    break;
            }

            ListViewExplorer_Refresh();
            await PbVisualization.TogglePbVisibilityAsync();
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
                var messageDialog = new MessageDialog(Properties.Resources.MwDeploymentDownloadException + dde.Message)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageDialog.ShowDialog();
            }
            catch (InvalidDeploymentException ide)
            {
                var messageDialog = new MessageDialog(Properties.Resources.MwInvalidDeploymentException + ide.Message)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageDialog.ShowDialog();
            }
            catch (InvalidOperationException ioe)
            {
                var messageDialog = new MessageDialog(Properties.Resources.MwInvalidOperationException + ioe.Message)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageDialog.ShowDialog();
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
                var messageDialog = new MessageDialog(Properties.Resources.MwDeploymentDownloadException + args.Error.Message)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageDialog.ShowDialog();
                return;
            }
            if (args.Cancelled)
            {
                _updateDialog.Close();
                return;
            }

            var mDialog = new MessageDialog(Properties.Resources.MwAppUpdatedText,
                Properties.Resources.MwAppUpdatedHeader, MessageDialogButtons.Ok)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            mDialog.ShowDialog();
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
                    var messageDialog = new MessageDialog(Properties.Resources.MwDeploymentDownloadException + dde.Message)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    messageDialog.ShowDialog();
                }
                catch (InvalidDeploymentException ide)
                {
                    var messageDialog = new MessageDialog(Properties.Resources.MwInvalidDeploymentException + ide.Message)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    messageDialog.ShowDialog();
                }
                catch (InvalidOperationException ioe)
                {
                    var messageDialog = new MessageDialog(Properties.Resources.MwInvalidOperationException + ioe.Message)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    messageDialog.ShowDialog();
                }
            }
            else
            {
                var messageDialog = new MessageDialog(Properties.Resources.MwWrongAppUpdate)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageDialog.ShowDialog();
            }
        }

        private void AppDeployOnCheckForUpdateCompleted(object sender, CheckForUpdateCompletedEventArgs args)
        {
            if (args.Error != null)
            {
                var messageDialog = new MessageDialog(Properties.Resources.MwDeploymentDownloadException + args.Error.Message)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageDialog.ShowDialog();
                return;
            }
            if (args.Cancelled)
            {
                _updateDialog.Close();
                return;
            }

            if (args.UpdateAvailable)
            {
                var messageDialog = new MessageDialog(Properties.Resources.MwUpdateDialogContentBeforeVersion +
                                                   args.AvailableVersion +
                                                   Properties.Resources.MwUpdateDialogContentAfterVersion,
                    Properties.Resources.MwUpdateDialogHeader, MessageDialogButtons.YesNo)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageDialog.ShowDialog();

                if (messageDialog.MessageDialogResult == MessageDialogResult.Yes)
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
                var messageDialog = new MessageDialog(Properties.Resources.MwNoUpdatesText,
                    Properties.Resources.MwNoUpdatesHeader, MessageDialogButtons.Ok)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageDialog.ShowDialog();
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
                // ignored
            }
        }

        private async void BrowseForwardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            await PbVisualization.TogglePbVisibilityAsync();

            var diveTask = Task.Factory.StartNew(async () =>
            {
                string path = _fileDiver.PathHistory.Count > 0 ? _fileDiver.PathHistory.Pop() : string.Empty;
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        var source = _fileDiver.DiveInto(path);
                        await Dispatcher.InvokeAsync(() =>
                        {
                            ListViewExplorer.ItemsSource = source;
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
                }
            });
            await diveTask;
            UpdateWindowTitle();

            await PbVisualization.TogglePbVisibilityAsync();
        }

        private async void GetCheckSumExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter == null)
            {
                return;
            }
            if (ListViewExplorer.SelectedItems.Count != 1)
            {
                return;
            }
            var file = ListViewExplorer.SelectedItem as FileSystemInfo;
            if (file == null)
            {
                return;
            }
            if (file.IsDirectory())
            {
                return;
            }

            var checksumsCalculator = new ChecksumCalculators();
            HashAlgorithm hashAlgorithm = null;
            var hashDialog = new ChecksumDialog
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            switch (e.Parameter.ToString())
            {
                case "md5":
                    hashAlgorithm = new MD5CryptoServiceProvider();
                    hashDialog.TbAlgorithmName.Text = Properties.Resources.HashMd5;
                    break;
                case "sha1":
                    hashAlgorithm = new SHA1Managed();
                    hashDialog.TbAlgorithmName.Text = Properties.Resources.HashSha1;
                    break;
                case "sha256":
                    hashAlgorithm = new SHA256Managed();
                    hashDialog.TbAlgorithmName.Text = Properties.Resources.HashSha256;
                    break;
                case "sha512":
                    hashAlgorithm = new SHA512Managed();
                    hashDialog.TbAlgorithmName.Text = Properties.Resources.HashSha512;
                    break;
            }
            checksumsCalculator.HashProgressUpdate += async (o, args) =>
            {
                var progress = args as ProgressEventArgs;
                if (progress == null)
                    return;
                await Dispatcher.InvokeAsync(() =>
                {
                    if (hashDialog.Visibility == Visibility.Visible)
                    {
                        hashDialog.PbCalculationProgress.Value = progress.Percent;
                        hashDialog.TbCheckSum.Text = progress.Message;
                    }
                });
            };
            var tokenSource = new CancellationTokenSource();
            hashDialog.CancellationToken = tokenSource;
            var task = Task.Factory.StartNew(() =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    hashDialog.ShowDialog();
                });
                try
                {
                    return checksumsCalculator.CalculateHash(file.FullName, hashAlgorithm,
                        hashDialog.CancellationToken.Token);
                }
                catch (Exception)
                {
                    return null;
                }
            }, tokenSource.Token);
            var result = await task;
            if (hashDialog.Visibility == Visibility.Visible)
            {
                hashDialog.TbCheckSum.Text = result;
            }
        }

        private async void ClearDbExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            await PbVisualization.TogglePbVisibilityAsync();

            var result = await DbHelper.ClearDb();
            if (!result)
            {
                CustomErrorPopupTextBlock.Text = Properties.Resources.MwCantClearDb;
                CustomErrorPopup.IsOpen = true;
            }

            await PbVisualization.TogglePbVisibilityAsync();
        }

        private void ChangeAccentColorExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter == null)
                return;

            var param = e.Parameter.ToString();
            SolidColorBrush color;
            switch (param)
            {
                case "Brown":
                    color = AccentBrushes.Brown;
                    break;
                case "Green":
                    color = AccentBrushes.Green;
                    break;
                case "Lime":
                    color = AccentBrushes.Lime;
                    break;
                case "Magenta":
                    color = AccentBrushes.Magenta;
                    break;
                case "Mango":
                    color = AccentBrushes.Mango;
                    break;
                case "Orange":
                    color = AccentBrushes.Orange;
                    break;
                case "Pink":
                    color = AccentBrushes.Pink;
                    break;
                case "Purple":
                    color = AccentBrushes.Purple;
                    break;
                case "Red":
                    color = AccentBrushes.Red;
                    break;
                case "Rose":
                    color = AccentBrushes.Rose;
                    break;
                case "Violet":
                    color = AccentBrushes.Violet;
                    break;
                case "Viridian":
                    color = AccentBrushes.Viridian;
                    break;
                case "Sky":
                    color = AccentBrushes.Sky;
                    break;
                case "Blue":
                default:
                    color = AccentBrushes.Blue;
                    break;
            }
            Settings.Default.DefaultAccentColor = color;
            Elysium.Manager.Apply(Application.Current, color, Elysium.Manager.DefaultContrastBrush);
        }

        private void ColorsMenu_OnSubmenuOpened(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem item in ColorsMenu.Items)
            {
                var left = item.Background as SolidColorBrush;
                item.IsChecked = Equals(left.Color, Settings.Default.DefaultAccentColor.Color);
            }
        }

        private void MenuItem_OnSubmenuOpened(object sender, RoutedEventArgs e)
        {
            var theme = GetCurrenTheme;
            foreach (MenuItem item in ThemesMenu.Items)
            {
                if (item.Tag.ToString() == "Dark")
                {
                    item.IsChecked = theme == Theme.Dark;
                }
                if (item.Tag.ToString() == "Light")
                {
                    item.IsChecked = theme == Theme.Light;
                }
            }
        }

        private void ChangeThemeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter == null)
                return;

            Elysium.Theme theme = Theme.Light;

            switch (e.Parameter.ToString())
            {
                case "dark":
                    theme = Theme.Dark;
                    break;
                case "light":
                default:
                    theme = Theme.Light;
                    break;
            }

            Settings.Default.DefaultTheme = theme == Theme.Light ? 0 : 1;
            Elysium.Manager.Apply(Application.Current, theme);

            var nameBinding = NameColumn.GetBindingExpression(GridViewColumnHeader.ForegroundProperty);
            nameBinding?.UpdateSource();
            nameBinding?.UpdateTarget();
            var dateBinding = DateColumn.GetBindingExpression(GridViewColumnHeader.ForegroundProperty);
            dateBinding?.UpdateSource();
            dateBinding?.UpdateTarget();
            var sizeBinding = SizeColumn.GetBindingExpression(GridViewColumnHeader.ForegroundProperty);
            sizeBinding?.UpdateSource();
            sizeBinding?.UpdateTarget();
        }
    }
}
