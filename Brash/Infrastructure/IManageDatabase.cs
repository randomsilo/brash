using System;
using Brash.Model;

namespace Brash.Infrastructure
{
    public interface IManageDatabase
    {
        IDatabaseContext DatabaseContext { get; }
        AAskIdRepositorySql RepositorySql { get; }

        void CreateDatabase();
        int? PerformInsert( string sql, object model);
        int PerformUpdate( string sql, object model);
        int PerformDelete( string sql, object model);
    }
}