using System;
using System.Data.SQLite;
using Dapper;
using Brash.Model;

namespace Brash.Infrastructure.Sqlite
{
    public class DatabaseManager : IManageDatabase
    {
        public AAskIdRepositorySql RepositorySql { get; private set; }
        public IDatabaseContext DatabaseContext { get; private set; }
        public DatabaseManager(IDatabaseContext databaseContext, AAskIdRepositorySql repositorySql)
        {
            DatabaseContext = databaseContext;
            RepositorySql = repositorySql;
        }

        public void CreateDatabase()
        {
            ExecuteScript(
                DatabaseContext.GetProperty(DatabaseProperty.DATABASE_INITIALIZE_SCRIPT_FILEPATH)
            );
        }

        private void ExecuteScript(string filePath)
        {
            try
            {
                using (var connection = GetDatabaseConnection())
                {
                    connection.Open();  
                    string script = System.IO.File.ReadAllText(filePath);
                    connection.Execute(script);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                throw e;
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public SQLiteConnection GetDatabaseConnection()
        {
            return new SQLiteConnection(
                DatabaseContext.GetProperty(DatabaseProperty.CONNECTION_STRING)
            );
        }
    }
}