using System;
using System.IO;
using System.Security;
using System.Windows;
using WoblaExplorer.Util;

namespace WoblaExplorer.FilesUtil
{
    public class FileDiver
    {
        public string CurrentPath { get; set; }

        public FileDiver()
        {}

        public FileDiver(string currentPath)
        {
            CurrentPath = currentPath;
            if (!CurrentPath.EndsWith("\\"))
            {
                CurrentPath += "\\";
            }
        }

        /// <exception cref="AggregateException">Occurs when app have no right to access dir or smth else.</exception>
        public FileSystemInfo[] DiveBack()
        {
            DirectoryInfo newPath = new DirectoryInfo(CurrentPath);
            if (newPath.Parent != null)
            {
                return DiveInto(newPath.Parent.FullName);
            }

            return DiveInto(CurrentPath);
        }

        /// <exception cref="SecurityException">Occurs when app have propblems with access.</exception>
        /// <exception cref="DirectoryNotFoundException"><c>:)</c></exception>
        public FileSystemInfo[] DiveInto(string path)
        {
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }
            try
            {
                var fsInfo = new DirectoryInfo(path).GetFileSystemInfos();

                CurrentPath = path.Clone().ToString();
                Array.Sort(fsInfo, (left, right) =>
                {
                    if (left.IsDirectory() && !right.IsDirectory())
                    {
                        return -1;
                    }
                    if (!left.IsDirectory() && right.IsDirectory())
                    {
                        return 1;
                    }
                    return string.Compare(left.FullName, right.FullName, StringComparison.CurrentCulture);
                });

                return fsInfo;
            }
            catch (IOException ioException)
            {
                throw;
            }
            catch (ArgumentNullException argumentNullException)
            {
                throw;
            }
            catch (ArgumentException argumentException)
            {
                throw;
            }
        }
    }
}
