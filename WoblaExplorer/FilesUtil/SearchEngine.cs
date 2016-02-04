using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Windows;
using WoblaExplorer.Util;

namespace WoblaExplorer.FilesUtil
{
    public class SearchEngine
    {
        public event ItemFound OnItemFound;
        public List<FileSystemInfo> SearchList { get; private set; }

        public SearchEngine()
        {
            SearchList = new List<FileSystemInfo>();
        }

        public void RecursiveSearch(string path, string find)
        {
            try
            {
                foreach (var info in new DirectoryInfo(path).GetFileSystemInfos())
                {
                    if (info.IsDirectory())
                    {
                        RecursiveSearch(info.FullName, find);
                    }
                    else
                    {
                        if (info.Name.ToLower(CultureInfo.CurrentCulture)
                                .Contains(find.ToLower(CultureInfo.CurrentCulture)))
                        {
                            SearchList.Add(info);
                            if (OnItemFound != null)
                            {
                                OnItemFound.Invoke(this, new SearchEventArgs(info, ""));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Recursive search
        /// </summary>
        /// <param name="path">path where search starts</param>
        /// <param name="find">item pattern</param>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="OperationCanceledException">When operation is canceled</exception>
        public void RecursiveSearch(string path, string find, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                new DirectoryInfo(path).GetFileSystemInfos().ToList().ForEach(info =>
                {
                    if (info.Attributes.HasFlag(FileAttributes.System))
                    {
                        return;
                    }
                    token.ThrowIfCancellationRequested();
                    if (info.Name.ToLower(CultureInfo.CurrentCulture).Contains(find.ToLower(CultureInfo.CurrentCulture)))
                    {
                        SearchList.Add(info);
                        try
                        {
                            OnItemFound?.Invoke(this, new SearchEventArgs(info, ""));
                        }
                        catch
                        {
                        }
                    }
                    if (info.IsDirectory())
                    {
                        RecursiveSearch(info.FullName, find, token);
                    }
                });
            }
            catch (SecurityException securityException)
            {
                //
            }
            catch (DirectoryNotFoundException directoryNotFoundException)
            {
                //
            }
            catch (ArgumentNullException argumentNullException)
            {
                //
            }
            catch (ArgumentException argumentException)
            {
                // 
            }
            catch (PathTooLongException pathTooLongException)
            {
                //
            }
            catch (AggregateException aggregateException)
            {
                // TODO: Handle the AggregateException 
            }
        }

        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public void SearchUsingEnumeration(string path, string find, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                new DirectoryInfo(path).EnumerateFileSystemInfos("*", SearchOption.AllDirectories).AsParallel().ForAll(info =>
                {
                    token.ThrowIfCancellationRequested();
                    if (info.Name.ToLower(CultureInfo.CurrentCulture).Contains(find.ToLower(CultureInfo.CurrentCulture)))
                    {
                        SearchList.Add(info);
                        if (OnItemFound != null)
                        {
                            OnItemFound.Invoke(this, new SearchEventArgs(info, ""));
                        }
                    }
                });
            }
            catch (DirectoryNotFoundException directoryNotFoundException)
            {
            }
            catch (SecurityException securityException)
            {
                // TODO: Handle the SecurityException 
            }
            catch (ArgumentNullException argumentNullException)
            {
                // TODO: Handle the ArgumentNullException 
            }
        }
    }



    public delegate void ItemFound(object sender, SearchEventArgs args);

    public class SearchEventArgs : EventArgs
    {
        public FileSystemInfo FoundedItem { get; private set; }
        public string Message { get; private set; }

        public SearchEventArgs(FileSystemInfo foundedItem, string message)
        {
            FoundedItem = foundedItem;
            Message = message;
        }
    }
}
