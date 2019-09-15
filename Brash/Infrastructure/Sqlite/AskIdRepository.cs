using System;
using Brash.Model;

namespace Brash.Infrastructure.Sqlite
{
    public class AskIdRepository<T> : IAskIdRepository<T> where T : IAskId
    {
        public IManageDatabase DatabaseManager { get; private set; }
        public AskIdRepository(IManageDatabase databaseManager)
        {
            DatabaseManager = databaseManager;
        }

        public ActionResult<T> Create(T model)
        {
            ActionResult<T> result = new ActionResult<T>();

            return result;
        }
        public ActionResult<T> Fetch(int? id)
        {
            ActionResult<T> result = new ActionResult<T>();

            return result;
        }
        public ActionResult<T> Update(T model)
        {
            ActionResult<T> result = new ActionResult<T>();

            return result;
        }
        public ActionResult<T> Delete(T model)
        {
            ActionResult<T> result = new ActionResult<T>();

            return result;
        }
    }
}