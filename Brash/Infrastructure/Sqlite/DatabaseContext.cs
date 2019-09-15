using System;
using System.Collections.Generic;
using Brash.Model;

namespace Brash.Infrastructure.Sqlite
{
    public class DatabaseContext : IDatabaseContext
    {
        private Dictionary<DatabaseProperty, string> _properties = new Dictionary<DatabaseProperty, string>();
        public DatabaseContext(
            string connectionString
            , string databaseName
            , string databaseSchema
            , string databaseInitializationScript = null)
        {
            _properties[DatabaseProperty.DATABASE_NAME] = databaseName;
            _properties[DatabaseProperty.DATABASE_SCHEMA] = databaseSchema;
            _properties[DatabaseProperty.DATABASE_INITIALIZE_SCRIPT_FILEPATH] = databaseInitializationScript;
            _properties[DatabaseProperty.CONNECTION_STRING] = connectionString;
        }
        public string GetProperty(DatabaseProperty property)
        {
            return _properties[property];
        }
    }
}