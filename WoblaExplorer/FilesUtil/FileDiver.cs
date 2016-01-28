using System;
using System.IO;
using System.Security;
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

        /// <exception cref="IOException">The directory specified by <paramref name="path" /> is a file.-or-The network name is not known.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission. </exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive). </exception>
        /// <exception cref="SecurityException">The caller does not have the required permission. </exception>
        /// <exception cref="ArgumentException"><paramref name="path" /> does not specify a valid file path or contains invalid DirectoryInfo characters. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path" /> is null. </exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters. The specified path, file name, or both are too long.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path" /> contains a colon character (:) that is not part of a drive label ("C:\").</exception>
        public void CopyAllInDir(DirectoryInfo source, DirectoryInfo destination)
        {
            Directory.CreateDirectory(destination.FullName);

            foreach (var directory in source.GetDirectories())
            {
                DirectoryInfo nextDestSubDir = destination.CreateSubdirectory(directory.Name);
                CopyAllInDir(directory, nextDestSubDir);
            }

            foreach (var file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(destination.FullName, file.Name), true);
            }
        }
    }
}
