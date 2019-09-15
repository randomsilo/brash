using System;
using System.Collections.Generic;

namespace Brash.Infrastructure
{
    public enum AskIdRepositorySqlTypes
    {
        UNKNOWN
        , CREATE
        , FETCH
        , UPDATE
        , DELETE
    }

    public abstract class AAskIdRepositorySql
    {
        protected Dictionary<AskIdRepositorySqlTypes,string> _sql;

        public AAskIdRepositorySql()
        {
            _sql = new Dictionary<AskIdRepositorySqlTypes,string>();
        }

        public string GetCreateStatement()
        {
            return _sql[AskIdRepositorySqlTypes.CREATE];
        }

        public string GetFetchStatement()
        {
            return _sql[AskIdRepositorySqlTypes.FETCH];
        }

        public string GetUpdateStatement()
        {
            return _sql[AskIdRepositorySqlTypes.UPDATE];
        }

        public string GetDeleteStatement()
        {
            return _sql[AskIdRepositorySqlTypes.DELETE];
        }
        
    }
}