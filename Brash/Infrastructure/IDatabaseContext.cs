using System;
using Brash.Model;

namespace Brash.Infrastructure
{
    public enum DatabaseProperty
    {
        UNKNOWN
        , DATABASE_NAME
        , DATABASE_SCHEMA
        , DATABASE_INITIALIZE_SCRIPT_FILEPATH
        , CONNECTION_STRING
    }
    public interface IDatabaseContext
    {
        string GetProperty(DatabaseProperty property);
    }
}