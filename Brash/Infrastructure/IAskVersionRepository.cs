using System;
using Brash.Model;

namespace Brash.Infrastructure
{
    public interface IAskVersionRepository<T> where T : IAskVersion
    {
        IManageDatabase DatabaseManager { get; }
        ActionResult<T> Create(T model);
        ActionResult<T> Fetch(T model);
        ActionResult<T> Update(T model);
        ActionResult<T> Delete(T model);
        QueryResult<T> FindWhere(string where);
    }
}