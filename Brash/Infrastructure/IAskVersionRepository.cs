using System;
using Brash.Model;

namespace Brash.Infrastructure
{
    public interface IAskVersionRepository<T> where T : IAskVersion
    {
        IManageDatabase DatabaseManager { get; }
        BrashActionResult<T> Create(T model);
        BrashActionResult<T> Fetch(T model);
        BrashActionResult<T> Update(T model);
        BrashActionResult<T> Delete(T model);
        BrashQueryResult<T> FindWhere(string where);
    }
}