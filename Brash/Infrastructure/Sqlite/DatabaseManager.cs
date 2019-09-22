using System;
using System.Linq;
using System.Data.SQLite;
using Dapper;

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

        public int? PerformInsert( string sql, object model)
        {
            int? id;

            using (var connection = GetDatabaseConnection())
            {
                connection.Open();
                id = connection.Query<int>(sql, model).First();
            }

            return id;
        }

        public int PerformUpdate( string sql, object model)
        {
            int rows = 0;

            using (var connection = GetDatabaseConnection())
            {
                connection.Open();
                rows = connection.Execute(sql, model);
            }

            return rows;
        }

        public int PerformDelete( string sql, object model)
        {
            int rows = 0;

            using (var connection = GetDatabaseConnection())
            {
                connection.Open();
                rows = connection.Execute(sql, model);
            }

            return rows;
        }
    }
}