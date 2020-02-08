using System;
using System.Collections.Generic;

namespace Brash.Infrastructure
{
    public interface IManualSqlRepository<T> 
    {
        IManageDatabase DatabaseManager { get; }
        IEnumerable<T> Query(string sql, object param);
        int Execute(string sql, object param);
    }
}