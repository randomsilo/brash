using System;
using System.Reflection;
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

        public void SetId(int? id, T model)
        {
            PropertyInfo propertyInfo = model.GetType().GetProperty(model.GetAskIdPropertyName());
            if (propertyInfo != null)
            {
                Type t = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
                object safeValue = (id == null) ? null : Convert.ChangeType(id, t);
                propertyInfo.SetValue(model, safeValue, null);
            }
        }

        public ActionResult<T> Create(T model)
        {
            ActionResult<T> result = new ActionResult<T>()
            {
                Model = model
                , Message = "init"
                , Status = ActionStatus.INFORMATION
            };

            var id = InsertModel(model);
            if (id >= 0)
            {
                SetId(id, model);
                result.UpdateStatus(ActionStatus.SUCCESS, "Record created.");
            }
            else
            {
                result.UpdateStatus(ActionStatus.ERROR, "Record creation failed.");
            }

            return result;
        }
        public ActionResult<T> Fetch(int? id)
        {
            ActionResult<T> result = new ActionResult<T>();

            return result;
        }
        public ActionResult<T> Update(T model)
        {
            ActionResult<T> result = new ActionResult<T>()
            {
                Model = model
                , Message = "init"
                , Status = ActionStatus.INFORMATION
            };

            int rows = UpdateModel(model);
            if (rows == 1)
            {
                result.UpdateStatus(ActionStatus.SUCCESS, "Record updated.");
                // TODO fetch row
            }
            else if (rows > 1)
            {
                result.UpdateStatus(ActionStatus.ERROR, "More than 1 record updated.");
            }
            else
            {
                result.UpdateStatus(ActionStatus.ERROR, "Record update failed.");
            }

            return result;
        }
        public ActionResult<T> Delete(T model)
        {
            ActionResult<T> result = new ActionResult<T>()
            {
                Model = model
                , Message = "init"
                , Status = ActionStatus.INFORMATION
            };

            int rows = DeleteModel(model);
            if (rows == 1)
            {
                result.UpdateStatus(ActionStatus.SUCCESS, "Record removed.");
                // TODO fetch row
            }
            else if (rows > 1)
            {
                result.UpdateStatus(ActionStatus.ERROR, "More than 1 record removed.");
            }
            else
            {
                result.UpdateStatus(ActionStatus.ERROR, "Record remove failed.");
            }

            return result;
        }

        private int? InsertModel(T model)
        {
            return DatabaseManager.PerformInsert(DatabaseManager.RepositorySql.GetCreateStatement(), model);
        }

        private int UpdateModel(T model)
        {
            return DatabaseManager.PerformUpdate(DatabaseManager.RepositorySql.GetUpdateStatement(), model);
        }

        private int DeleteModel(T model)
        {
            return DatabaseManager.PerformDelete(DatabaseManager.RepositorySql.GetDeleteStatement(), model);
        }
    }
}