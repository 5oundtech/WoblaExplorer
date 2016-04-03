using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace WoblaExplorer.Util
{
    public static class DbHelper
    {
        public static string DbFolder
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                       "\\Woblavobla\\";
            }
        }
        public static readonly string DbFileName = "woblaexplorer.db";

        public static string DbPath => DbFolder + DbFileName;

        public static string ConnectionString => $"Data Source={DbPath};";

        public static void CreateDb()
        {
            if (!Directory.Exists(DbFolder))
            {
                try
                {
                    Directory.CreateDirectory(DbFolder);
                }
                catch (IOException ioException)
                {
                }
                catch (UnauthorizedAccessException unauthorizedAccessException)
                {
                    // TODO: Handle the UnauthorizedAccessException 
                }
            }
            SQLiteConnection.CreateFile(DbPath);
        }

        public static bool CreateReadedFilesTable()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string sqlCommand = "DROP TABLE IF EXISTS readed_files;" +
                                    "CREATE TABLE readed_files(" +
                                    "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                                    "path TEXT);";
                try
                {
                    var command = connection.CreateCommand();
                    command.CommandText = sqlCommand;
                    command.ExecuteNonQuery();

                    return true;
                }
                catch (SQLiteException)
                {
                    return false;
                }
            }
        }

        public static async Task<bool> InsertReadedFile(string path)
        {
            return await Task.Factory.StartNew(async () =>
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    try
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = $"INSERT INTO readed_files(path) VALUES ('{path}');";
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                        return true;
                    }
                    catch (SQLiteException)
                    {
                        return false;
                    }
                }
            }).Result;
        }

        /// <exception cref="ArgumentNullException">The exception that is thrown when the <paramref name="function" /> argument is null.</exception>
        public static async Task<bool> RemoveReadedFile(string path)
        {
            return await Task.Factory.StartNew(async () =>
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    try
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = $"DELETE FROM readed_files WHERE path = '{path}';";
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                        return true;
                    }
                    catch (SQLiteException)
                    {
                        return false;
                    }
                }
            }).Result;
        }

        public static bool IsReadedFile(string path)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                try
                {
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT 1 from readed_files rf where rf.path = '{path}';";
                    var result = command.ExecuteScalar();
                    if (result == null)
                        return false;
                    return true;
                }
                catch (SQLiteException)
                {
                    return false;
                }
            }
        }

        public static async Task<bool> ClearDb()
        {
            return await Task.Factory.StartNew(async () =>
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    try
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = "DELETE FROM readed_files WHERE id >= 0;";
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (SQLiteException)
                    {
                        return false;
                    }
                }
            }).Result;
        }
    }
}
