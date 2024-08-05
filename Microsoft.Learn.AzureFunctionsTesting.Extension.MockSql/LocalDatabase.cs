using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using System;
using System.IO;
using System.Text.RegularExpressions;
using static System.Text.RegularExpressions.RegexOptions;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockSql
{
    // Borrows liberally from https://github.com/joshclark/TemporaryDb
    // But often the initial DB creation can timeout and that library does not allow setting the connection timeout
    public class LocalDatabase : IDisposable
    {
        private readonly string dbName;
        private readonly string fileName;
        private bool isDisposed;

        public LocalDatabase(string name)
        {
            this.dbName = name;
            this.fileName = Path.Combine(Directory.GetCurrentDirectory(), $"{dbName}.mdf");
            CreateDatabase();
        }

        public virtual string ConnectionString => BuildConnectionString(dbName);

        protected virtual string MasterConnectionString => BuildConnectionString("master");

        public void CreateDatabase()
        {
            DropDatabase();

            string createDatabase = $"CREATE DATABASE [{dbName}] ON PRIMARY (NAME=[{dbName}], FILENAME = '{fileName}')";
            using var connection = new SqlConnection(MasterConnectionString);
            using var command = new SqlCommand(createDatabase, connection);

            connection.Open();
            command.ExecuteNonQuery();
        }

        public void DeployDacPac(string filePath)
        {
            var dacServices = new DacServices(ConnectionString);

            using var dacpac = DacPackage.Load(filePath);
            dacServices.Deploy(dacpac, dbName, true);
        }

        public bool RunSqlScriptFile(string filePath)
        {
            string script = File.ReadAllText(filePath);

            // split script on GO command
            var commandStrings = Regex.Split(script, @"^\s*GO\s*$", Multiline | IgnoreCase);
            using var connection = new SqlConnection(ConnectionString);

            connection.Open();
            foreach (var commandString in commandStrings)
            {
                ExecuteCommand(connection, commandString);
            }
            connection.Close();
            return true;
        }

        public void ExecuteCommand(string command)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            ExecuteCommand(connection, command);
            connection.Close();
        }

        static void ExecuteCommand(SqlConnection connection, string commandString)
        {
            if (commandString.Trim() == "") return;

            using var command = new SqlCommand(commandString, connection);
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                string spError = commandString.Length > 100 ? string.Concat(commandString.AsSpan(0, 100), " ...\n...") : commandString;
                throw new Exception($"Please check the SqlServer script.\nLine: {ex.LineNumber} \nError: {ex.Message} \nSQL Command: \n{spError}", ex);
            }
        }

        private void DropDatabase()
        {
            var deleteIfExists = $@"
IF EXISTS(SELECT * FROM sys.databases where name = '{dbName}')
BEGIN
    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{dbName}]
END";
            using var connection = new SqlConnection(MasterConnectionString);
            using var command = new SqlCommand(deleteIfExists, connection);

            connection.Open();

            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception) when (!File.Exists(fileName))
            {
                // If the file does not exist, we might get an exception when
                // dropping the database.  LocalDB still removes to database
                // from sys.databases, so let's just eat this error and move on...
            }

            if (File.Exists(fileName)) File.Delete(fileName);
        }

        private static string BuildConnectionString(string name)
        {
            var builder = new SqlConnectionStringBuilder()
            {
                DataSource = "(localdb)\\MSSQLLocalDB",
                InitialCatalog = name,
                IntegratedSecurity = true,
                ConnectTimeout = 60
            };
            return builder.ConnectionString;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    DropDatabase();
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
