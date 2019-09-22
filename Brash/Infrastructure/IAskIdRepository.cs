using System;
using Brash.Model;

namespace Brash.Infrastructure
{
    public interface IAskIdRepository<T> where T : IAskId
    {
        IManageDatabase DatabaseManager { get; }
        ActionResult<T> Create(T model);
        ActionResult<T> Fetch(T model);
        ActionResult<T> Update(T model);
        ActionResult<T> Delete(T model);
    }
}