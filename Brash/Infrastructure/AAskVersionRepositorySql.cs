using System;
using System.Collections.Generic;

namespace Brash.Infrastructure
{
    public enum AskVersionRepositorySqlTypes
    {
        UNKNOWN
        , CREATE
        , FETCH
        , UPDATE
        , DELETE
        , FIND
    }

    public abstract class AAskVersionRepositorySql
    {
        protected Dictionary<AskVersionRepositorySqlTypes,string> _sql;

        public AAskVersionRepositorySql()
        {
            _sql = new Dictionary<AskVersionRepositorySqlTypes,string>();
        }

        public string GetCreateStatement()
        {
            return _sql[AskVersionRepositorySqlTypes.CREATE];
        }

        public string GetFetchStatement()
        {
            return _sql[AskVersionRepositorySqlTypes.FETCH];
        }

        public string GetUpdateStatement()
        {
            return _sql[AskVersionRepositorySqlTypes.UPDATE];
        }

        public string GetDeleteStatement()
        {
            return _sql[AskVersionRepositorySqlTypes.DELETE];
        }

        public string GetFindStatement()
        {
            return _sql[AskVersionRepositorySqlTypes.FIND];
        }
        
    }
}