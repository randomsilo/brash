using System;
using System.Collections.Generic;

namespace Brash.Infrastructure
{
    public enum AskGuidRepositorySqlTypes
    {
        UNKNOWN
        , CREATE
        , FETCH
        , UPDATE
        , DELETE
        , FIND
    }

    public abstract class AAskGuidRepositorySql
    {
        protected Dictionary<AskGuidRepositorySqlTypes,string> _sql;

        public AAskGuidRepositorySql()
        {
            _sql = new Dictionary<AskGuidRepositorySqlTypes,string>();
        }

        public string GetCreateStatement()
        {
            return _sql[AskGuidRepositorySqlTypes.CREATE];
        }

        public string GetFetchStatement()
        {
            return _sql[AskGuidRepositorySqlTypes.FETCH];
        }

        public string GetUpdateStatement()
        {
            return _sql[AskGuidRepositorySqlTypes.UPDATE];
        }

        public string GetDeleteStatement()
        {
            return _sql[AskGuidRepositorySqlTypes.DELETE];
        }

        public string GetFindStatement()
        {
            return _sql[AskGuidRepositorySqlTypes.FIND];
        }
        
    }
}