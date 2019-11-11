using System;
using Brash.Model;

namespace Brash.Infrastructure
{
    public interface IAskIdService<T> where T : IAskId
    {
        ServiceResult<T> Create(T model);
        ServiceResult<T> Fetch(T model);
        ServiceResult<T> Update(T model);
        ServiceResult<T> Delete(T model);

        ActionResult<T> CreatePreWork(T model);
        ActionResult<T> FetchPreWork(T model);
        ActionResult<T> UpdatePreWork(T model);
        ActionResult<T> DeletePreWork(T model);

        ActionResult<T> CreatePostWork(T model);
        ActionResult<T> FetchPostWork(T model);
        ActionResult<T> UpdatePostWork(T model);
        ActionResult<T> DeletePostWork(T model);
        QueryResult<T> FindWhere(string where);
    }
}