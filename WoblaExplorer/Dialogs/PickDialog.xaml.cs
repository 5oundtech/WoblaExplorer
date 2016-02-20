using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Elysium;
using WoblaExplorer.Converters;
using WoblaExplorer.Util;

namespace WoblaExplorer.Dialogs
{
    public enum PickDialogType
    {
        Directory,
        File
    }

    public enum PickDialogResult
    {
        Aborted,
        Ok
    }

    /// <summary>
    /// Interaction logic for PickDialog.xaml
    /// </summary>
    public partial class PickDialog
    {
        public PickDialogResult PickDialogResult { get; set; } = PickDialogResult.Aborted;
        public PickDialogType DialogType { get; set; } = PickDialogType.Directory;
        public string SelectedPath { get; set; }

        private readonly FileNameToIconConverter _converter;

        public PickDialog()
        {
            InitializeComponent();
            _converter = new FileNameToIconConverter();
            try
            {
                FileSystemTree.Focus();
                foreach (var drive in Directory.GetLogicalDrives())
                {
                    var dirInfo = new DirectoryInfo(drive);
                    TreeViewItem item = new TreeViewItem();
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition() {Width = GridLength.Auto});
                    grid.ColumnDefinitions.Add(new ColumnDefinition() {Width = new GridLength(5, GridUnitType.Star)});
                    var textBlock = new TextBlock
                    {
                        Text =
                            (string)
                                _converter.Convert(dirInfo.FullName, typeof (string), null, CultureInfo.CurrentCulture)
                    };
                    textBlock.SetValue(Grid.ColumnProperty, 0);
                    textBlock.Foreground = new SolidColorBrush(Colors.SlateGray);
                    textBlock.FontSize = 15;
                    grid.Children.Add(textBlock);
                    var driveTb = new TextBlock {Text = dirInfo.ToString()};
                    driveTb.SetValue(Grid.ColumnProperty, 1);
                    driveTb.SetValue(Grid.MarginProperty, new Thickness(2, 0, 0, 0));
                    driveTb.FontSize = 15;
                    grid.Children.Add(driveTb);
                    item.Tag = dirInfo;
                    item.Header = grid;
                    item.Items.Add("*");
                    FileSystemTree.Items.Add(item);
                }
                GoToSelectedPath();
            }
            catch (IOException)
            {
                Close();
            }
            catch (UnauthorizedAccessException)
            {            
                Close();
            }
        }

        public PickDialog(PickDialogType dialogType, string selectedPath)
        {
            InitializeComponent();
            _converter = new FileNameToIconConverter();
            try
            {
                DialogType = PickDialogType.File;
                FileSystemTree.Focus();
                foreach (var drive in Directory.GetLogicalDrives())
                {
                    var dirInfo = new DirectoryInfo(drive);
                    TreeViewItem item = new TreeViewItem();
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
                    var textBlock = GetTreeViewItemTextBox((string)
                                                        _converter.Convert(dirInfo.FullName, typeof(string), null,
                                                            CultureInfo.CurrentCulture));
                    grid.Children.Add(textBlock);
                    var driveTb = new TextBlock { Text = dirInfo.ToString() };
                    driveTb.SetValue(Grid.ColumnProperty, 1);
                    driveTb.SetValue(Grid.MarginProperty, new Thickness(2, 0, 0, 0));
                    driveTb.FontSize = 15;
                    grid.Children.Add(driveTb);
                    item.Tag = dirInfo;
                    item.Header = grid;
                    item.Items.Add("*");
                    FileSystemTree.Items.Add(item);
                }
                GoToSelectedPath();
            }
            catch (IOException)
            {
                Close();
            }
            catch (UnauthorizedAccessException)
            {
                Close();
            }
            DialogType = dialogType;
            SelectedPath = selectedPath;
        }

        public void GoToSelectedPath()
        {
            if (string.IsNullOrEmpty(SelectedPath))
            {
                return;
            }

            foreach (TreeViewItem item in FileSystemTree.Items)
            {
                var fsInfo = item.Tag as FileSystemInfo;
                if (fsInfo != null && fsInfo.FullName == Path.GetPathRoot(SelectedPath))
                {
                    item.IsExpanded = true;
                    return;
                }
            }

/*            var partsOfWay = new List<string>();
            var inputInfo = new DirectoryInfo(SelectedPath);
            var currentDir = SelectedPath.Clone().ToString();
            while (inputInfo.Parent != null)
            {
                partsOfWay.Add(currentDir);
                currentDir = inputInfo.Parent.FullName;
                inputInfo = new DirectoryInfo(currentDir);
                if (inputInfo.Parent == null)
                {
                    partsOfWay.Add(inputInfo.FullName);
                }
            }

            Action<TreeViewItem> action = null;
            action = item =>
            {
                FileSystemTree.UpdateLayout();

                foreach (object treeItem in item.Items)
                {
                    var treeViewItem = treeItem as TreeViewItem;
                    var fsInfo = treeViewItem?.Tag as FileSystemInfo;
                    if (fsInfo != null && partsOfWay.EqualToOneFromList(fsInfo.FullName))
                    {
                        treeViewItem.IsSelected = true;
                        treeViewItem.ExpandSubtree();
                        treeViewItem.Focus();
                        try
                        {
                            action(treeViewItem);
                        }
                        catch (Exception exception)
                        {
                        }
                    }
                }
            };

            foreach (TreeViewItem item in FileSystemTree.Items)
            {
                var fsInfo = item.Tag as FileSystemInfo;
                if (fsInfo != null && partsOfWay.EqualToOneFromList(fsInfo.FullName))
                {
                    item.IsExpanded = true;
                    item.Focus();
                    action(item);
                    break;
                }
            }*/
        }

        private async void FileSystemTree_OnExpanded(object sender, RoutedEventArgs e)
        {
            await ProgressBar.ToggleControlVisibilityAsync();
            TreeViewItem item = (TreeViewItem) e.OriginalSource;
            if (item == null)
            {
                await ProgressBar.ToggleControlVisibilityAsync();
                return;
            }
            item.Items.Remove("*");

            DirectoryInfo directory;
            var tag = item.Tag as DriveInfo;
            if (tag != null)
            {
                DriveInfo drive = tag;
                directory = drive.RootDirectory;
            }
            else directory = item.Tag as DirectoryInfo;
            if (directory == null)
            {
                await ProgressBar.ToggleControlVisibilityAsync();
                return;
            }

            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    if (DialogType == PickDialogType.Directory)
                    {
                        foreach (DirectoryInfo subDirectory in
                            directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                        {

                            Dispatcher.Invoke(() =>
                            {
                                TreeViewItem treeViewItem = new TreeViewItem();
                                var grid = new Grid();
                                grid.ColumnDefinitions.Add(new ColumnDefinition() {Width = GridLength.Auto});
                                grid.ColumnDefinitions.Add(new ColumnDefinition()
                                {
                                    Width = new GridLength(5, GridUnitType.Star)
                                });
                                var textBlock = GetTreeViewItemTextBox((string)
                                    _converter.Convert(subDirectory.FullName, typeof (string), null,
                                        CultureInfo.CurrentCulture));
                                grid.Children.Add(textBlock);
                                var fsEntry = new TextBlock {Text = subDirectory.Name};
                                fsEntry.SetValue(Grid.ColumnProperty, 1);
                                fsEntry.SetValue(Grid.MarginProperty, new Thickness(2, 0, 0, 0));
                                fsEntry.FontSize = 15;
                                grid.Children.Add(fsEntry);


                                treeViewItem.Tag = subDirectory;
                                treeViewItem.Header = grid;
                                treeViewItem.Items.Add("*");
                                item.Items.Add(treeViewItem);
                            });
                        }

                    }
                    else if (DialogType == PickDialogType.File)
                    {
                        foreach (
                            FileSystemInfo info in
                                directory.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly)
                                    .OrderBy(info => !info.IsDirectory()))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                TreeViewItem treeViewItem = new TreeViewItem();
                                var grid = new Grid();
                                grid.ColumnDefinitions.Add(new ColumnDefinition() {Width = GridLength.Auto});
                                grid.ColumnDefinitions.Add(new ColumnDefinition()
                                {
                                    Width = new GridLength(5, GridUnitType.Star)
                                });
                                var textBlock = GetTreeViewItemTextBox((string)
                                    _converter.Convert(info.FullName, typeof(string), null,
                                        CultureInfo.CurrentCulture));
                                grid.Children.Add(textBlock);
                                var fsEntry = new TextBlock {Text = info.Name};
                                fsEntry.SetValue(Grid.ColumnProperty, 1);
                                fsEntry.SetValue(Grid.MarginProperty, new Thickness(2, 0, 0, 0));
                                fsEntry.FontSize = 15;
                                grid.Children.Add(fsEntry);


                                treeViewItem.Tag = info;
                                treeViewItem.Header = grid;
                                treeViewItem.Items.Add("*");
                                item.Items.Add(treeViewItem);
                            });
                        }
                    }
                }
                catch (Exception)
                {
                    //
                    await ProgressBar.ToggleControlVisibilityAsync();
                }
            });

            await ProgressBar.ToggleControlVisibilityAsync();
        }

        private TextBlock GetTreeViewItemTextBox(string text)
        {
            DirectoryInfo subDirectory;
            var textBlock = new TextBlock
            {
                Text = text
            };
            textBlock.SetValue(Grid.ColumnProperty, 0);
            textBlock.FontSize = 15;
            textBlock.Foreground = new SolidColorBrush(Colors.SlateGray);
            return textBlock;
        }

        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            PickDialogResult = PickDialogResult.Ok;
            var selectedItem = FileSystemTree.SelectedItem as TreeViewItem;
            if (selectedItem != null)
            {
                var fsInfo = selectedItem.Tag as FileSystemInfo;
                if (fsInfo != null)
                {
                    SelectedPath = fsInfo.FullName;
                }
            }
            DialogResult = true;
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            PickDialogResult = PickDialogResult.Aborted;
            DialogResult = false;
        }
    }
}
