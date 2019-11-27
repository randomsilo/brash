using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using System.Data.SQLite;
using Dapper;
using Brash.Model;

namespace Brash.Infrastructure.Sqlite
{
    public class AskGuidRepository<T> : IAskGuidRepository<T> where T : IAskGuid
    {
        public IManageDatabase DatabaseManager { get; private set; }
        public AAskGuidRepositorySql RepositorySql { get; private set; }
        public ILogger Logger { get; private set; }
        public AskGuidRepository(IManageDatabase databaseManager, AAskGuidRepositorySql askIdRepositorySql, ILogger logger)
        {
            DatabaseManager = databaseManager;
            RepositorySql = askIdRepositorySql;
            Logger = logger;
        }

        public SQLiteConnection GetDatabaseConnection()
        {
            return new SQLiteConnection(
                DatabaseManager.DatabaseContext.GetProperty(DatabaseProperty.CONNECTION_STRING)
            );
        }

        public void SetGuid(string guid, T model)
        {
            // value object
            object propertyVal = (object)guid;

            //find out the type
            Type type = model.GetType();
        
            //get the property information based on the type
            System.Reflection.PropertyInfo propertyInfo = type.GetProperty(model.GetAskGuidPropertyName());
        
            //find the property type
            Type propertyType = propertyInfo.PropertyType;
            
            //Convert.ChangeType does not handle conversion to nullable types
            //if the property type is nullable, we need to get the underlying type of the property
            var targetType = IsNullableType(propertyInfo.PropertyType) ? Nullable.GetUnderlyingType(propertyInfo.PropertyType) : propertyInfo.PropertyType;
            
            //Returns an System.Object with the specified System.Type and whose value is equivalent to the specified object.
            propertyVal = Convert.ChangeType(propertyVal, targetType);
        
            //Set the value of the property
            propertyInfo.SetValue(model, propertyVal, null);
        }

        private bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }

        public string GetGuid(T model)
        {
            string guid = null;

            //find out the type
            Type type = model.GetType();
        
            //get the property information based on the type
            System.Reflection.PropertyInfo propertyInfo = type.GetProperty(model.GetAskGuidPropertyName());
        
            //find the property type
            Type propertyType = propertyInfo.PropertyType;
        
            //Set the value of the property
            guid = (string)propertyInfo.GetValue(model);

            return guid;
        }

        public ActionResult<T> Create(T model)
        {
            ActionResult<T> result = new ActionResult<T>()
            {
                Model = model
                , Message = "init"
                , Status = ActionStatus.INFORMATION
            };

            string modelGuid = Guid.NewGuid().ToString();
            SetGuid(modelGuid, model);

            var recordGuid = PerformInsert(model);
            if (recordGuid.Equals(modelGuid))
            {
                var fetchResult = Fetch(model);
                if (fetchResult.Status == ActionStatus.SUCCESS)
                {
                    result.Model = fetchResult.Model;
                    result.UpdateStatus(ActionStatus.SUCCESS, $"Record created. ({GetGuid(result.Model)})");
                }
                else
                {
                    result.UpdateStatus(ActionStatus.ERROR, "Record creation failed.");
                }
            }
            else
            {
                result.UpdateStatus(ActionStatus.ERROR, "Record creation failed. (fetch failure)");
            }

            return result;
        }
        public ActionResult<T> Fetch(T model)
        {
            ActionResult<T> result = new ActionResult<T>()
            {
                Model = model
                , Message = "init"
                , Status = ActionStatus.INFORMATION
            };

            IEnumerable<T> models = PerformFetch(model);
            if (models.Count() == 1)
            {
                result.UpdateStatus(ActionStatus.SUCCESS, "Record updated.");
                result.Model = models.FirstOrDefault();
            }
            else if (models.Count() > 1)
            {
                result.UpdateStatus(ActionStatus.ERROR, "More than 1 record found.");
                result.Model = models.FirstOrDefault();
            }
            else if (models.Count() == 0)
            {
                result.UpdateStatus(ActionStatus.ERROR, "Record not found.");
            }

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

            int rows = PerformUpdate(model);
            if (rows == 1)
            {
                result.Model = PerformFetch(model).FirstOrDefault();
                result.UpdateStatus(ActionStatus.SUCCESS, "Record updated.");
            }
            else if (rows > 1)
            {
                result.Model = PerformFetch(model).FirstOrDefault();
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

            int rows = PerformDelete(model);
            if (rows == 1)
            {
                result.UpdateStatus(ActionStatus.SUCCESS, "Record removed.");
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

        public QueryResult<T> FindWhere(string where)
        {
            QueryResult<T> result = new QueryResult<T>()
            {
                Models = new List<T>()
                , Message = "init"
                , Status = QueryStatus.INFORMATION
            };

            IEnumerable<T> models = PerformFind(where);
            if (models.Count() > 1)
            {
                result.UpdateStatus(QueryStatus.SUCCESS, $"{models.Count()} records found");
                result.Models = models.ToList();
            }
            else if (models.Count() == 0)
            {
                result.UpdateStatus(QueryStatus.NO_RECORDS, $"{models.Count()} records found");
            }
            else
            {
                result.UpdateStatus(QueryStatus.ERROR, $"Query count issue: this should never happen");
            }

            return result;
        }

        private string PerformInsert(T model)
        {
            string guid;

            using (var connection = GetDatabaseConnection())
            {
                connection.Open();
                guid = connection.Query<string>(RepositorySql.GetCreateStatement(), model).First();
            }

            return guid;
        }

        private int PerformUpdate(T model)
        {
            int rows = 0;

            using (var connection = GetDatabaseConnection())
            {
                connection.Open();
                rows = connection.Execute(RepositorySql.GetUpdateStatement(), model);
            }

            return rows;
        }

        private int PerformDelete(T model)
        {
            int rows = 0;

            using (var connection = GetDatabaseConnection())
            {
                connection.Open();
                rows = connection.Execute(RepositorySql.GetDeleteStatement(), model);
            }

            return rows;
        }

        private IEnumerable<T> PerformFetch(T model)
        {
            IEnumerable<T> models;

            using (var connection = GetDatabaseConnection())
            {
                connection.Open();
                models = connection.Query<T>(RepositorySql.GetFetchStatement(), model);
            }

            return models;
        }

        private IEnumerable<T> PerformFind(string whereClause)
        {
            IEnumerable<T> models;

            using (var connection = GetDatabaseConnection())
            {
                connection.Open();
                models = connection.Query<T>($"{RepositorySql.GetFindStatement()} {whereClause}");
            }

            return models;
        }

        
    }
}