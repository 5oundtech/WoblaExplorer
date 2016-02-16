using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace WoblaExplorer.Util
{
    public class ChecksumCalculators
    {
        public event EventHandler HashProgressUpdate;
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive). </exception>
        /// <exception cref="UnauthorizedAccessException"><paramref name="path" /> specified a directory.-or- The caller does not have the required permission. </exception>
        /// <exception cref="FileNotFoundException">The file specified in <paramref name="path" /> was not found. </exception>
        public static string CalculateSha256(string file)
        {
            using (var bufferedStream = new BufferedStream(File.OpenRead(file)))
            {
                var sha = new SHA256Managed();
                var bytes = sha.ComputeHash(bufferedStream);
                return BitConverter.ToString(bytes).Replace("-", String.Empty);
            }
        }

        /// <exception cref="IOException">The underlying stream is null or closed. </exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive). </exception>
        /// <exception cref="UnauthorizedAccessException"><paramref name="path" /> specified a directory.-or- The caller does not have the required permission. </exception>
        /// <exception cref="FileNotFoundException">The file specified in <paramref name="path" /> was not found. </exception>
        /// <exception cref="CryptographicUnexpectedOperationException"><see cref="F:System.Security.Cryptography.HashAlgorithm.HashValue" /> is null. </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="oldValue" /> is null. </exception>
        /// <exception cref="ArgumentException"><paramref name="oldValue" /> is the empty string (""). </exception>
        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public string CalculateHash(string file, HashAlgorithm algorithm, CancellationToken token)
        {
            byte[] buffer;
            byte[] oldBuffer;
            int bytesRead;
            int oldBytesRead;
            long size;
            long totalBytesRead = 0;
            using (var bufferedStream = new BufferedStream(File.OpenRead(file)))
            {
                using (algorithm)
                {
                    size = bufferedStream.Length;
                    buffer = new byte[4096];
                    bytesRead = bufferedStream.Read(buffer, 0, buffer.Length);
                    totalBytesRead += bytesRead;

                    do
                    {
                        token.ThrowIfCancellationRequested();
                        oldBytesRead = bytesRead;
                        oldBuffer = buffer;

                        buffer = new byte[4096];
                        bytesRead = bufferedStream.Read(buffer, 0, buffer.Length);
                        totalBytesRead += bytesRead;

                        if (bytesRead == 0)
                        {
                            algorithm.TransformFinalBlock(oldBuffer, 0, oldBytesRead);
                        }
                        else
                        {
                            algorithm.TransformBlock(oldBuffer, 0, oldBytesRead, oldBuffer, 0);
                        }
                        HashProgressUpdate?.Invoke(this, new ProgressEventArgs((double) totalBytesRead*100/size));
                    } while (bytesRead != 0);
                    return BitConverter.ToString(algorithm.Hash).Replace("-", string.Empty).ToUpper();
                }
            }
        }

        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive). </exception>
        /// <exception cref="UnauthorizedAccessException"><paramref name="path" /> specified a directory.-or- The caller does not have the required permission. </exception>
        /// <exception cref="FileNotFoundException">The file specified in <paramref name="path" /> was not found. </exception>
        public static string CalculateSha1(string file)
        {
            using (var bufferedStream = new BufferedStream(File.OpenRead(file)))
            {
                var sha = new SHA1Managed();
                var bytes = sha.ComputeHash(bufferedStream);
                return BitConverter.ToString(bytes).Replace("-", String.Empty);
            }
        }

        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive). </exception>
        /// <exception cref="UnauthorizedAccessException"><paramref name="path" /> specified a directory.-or- The caller does not have the required permission. </exception>
        /// <exception cref="FileNotFoundException">The file specified in <paramref name="path" /> was not found. </exception>
        public static string CalculateMd5(string file)
        {
            using (var md5 = MD5.Create())
            {
                using (var bufferedStream = new BufferedStream(File.OpenRead(file)))
                {
                    var bytes = md5.ComputeHash(bufferedStream);
                    return BitConverter.ToString(bytes).Replace("-", String.Empty);
                }
            }
        }
    }
}
