using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using System.Data.SQLite;
using Dapper;
using Brash.Model;

namespace Brash.Infrastructure.Sqlite
{
    public class ManualSqlRepository<T>
    {
        public IManageDatabase DatabaseManager { get; private set; }
        public ILogger Logger { get; private set; }
        public ManualSqlRepository(IManageDatabase databaseManager, ILogger logger)
        {
            DatabaseManager = databaseManager;
            Logger = logger;
        }

        public SQLiteConnection GetDatabaseConnection()
        {
            return new SQLiteConnection(
                DatabaseManager.DatabaseContext.GetProperty(DatabaseProperty.CONNECTION_STRING)
            );
        }

        public IEnumerable<T> Query(string sql, object param)
        {
            IEnumerable<T> models;

            using (var connection = GetDatabaseConnection())
            {
                connection.Open();
                Logger.Verbose(sql);
                models = connection.Query<T>(sql, param);
            }

            return models;
        }

        private int Execute(string sql, object param)
        {
            int rows = 0;

            using (var connection = GetDatabaseConnection())
            {
                connection.Open();
                Logger.Verbose(sql);
                rows = connection.Execute(sql, param);
            }

            return rows;
        }
    }
}