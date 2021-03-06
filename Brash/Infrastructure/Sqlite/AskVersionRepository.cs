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
    public class AskVersionRepository<T> : IAskVersionRepository<T> where T : IAskVersion
    {
        public IManageDatabase DatabaseManager { get; private set; }
        public AAskVersionRepositorySql RepositorySql { get; private set; }
        public ILogger Logger { get; private set; }
        public AskVersionRepository(IManageDatabase databaseManager, AAskVersionRepositorySql askIdRepositorySql, ILogger logger)
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

        public void SetId(int? id, T model)
        {
            // value object
            object propertyVal = (object)id;

            //find out the type
            Type type = model.GetType();
        
            //get the property information based on the type
            System.Reflection.PropertyInfo propertyInfo = type.GetProperty(model.GetIdPropertyName());
        
            //find the property type
            Type propertyType = propertyInfo.PropertyType;
            
            //Convert.ChangeType does not handle conversion to nullable types
            //if the property type is nullable, we need to get the underlying type of the property
            var targetType = IsNullableType(propertyInfo.PropertyType) ? Nullable.GetUnderlyingType(propertyInfo.PropertyType) : propertyInfo.PropertyType;
            
            //Returns an System.Object with the specified System.Type and whose value is equivalent to the specified object.
            if (propertyVal != null)
                propertyVal = Convert.ChangeType(propertyVal, targetType);
        
            //Set the value of the property
            propertyInfo.SetValue(model, propertyVal, null);
        }

        private bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }


        public int? GetId(T model)
        {
            int? id = null;

            //find out the type
            Type type = model.GetType();
        
            //get the property information based on the type
            System.Reflection.PropertyInfo propertyInfo = type.GetProperty(model.GetIdPropertyName());
        
            //find the property type
            Type propertyType = propertyInfo.PropertyType;
        
            //Set the value of the property
            id = (int?)propertyInfo.GetValue(model);

            return id;
        }

        public string GetGuid(T model)
        {
            string guid = null;

            //find out the type
            Type type = model.GetType();
        
            //get the property information based on the type
            System.Reflection.PropertyInfo propertyInfo = type.GetProperty(model.GetGuidPropertyName());
        
            //find the property type
            Type propertyType = propertyInfo.PropertyType;
        
            //Set the value of the property
            guid = (string)propertyInfo.GetValue(model);

            return guid;
        }

        public decimal? GetVersion(T model)
        {
            decimal? version = null;

            //find out the type
            Type type = model.GetType();
        
            //get the property information based on the type
            System.Reflection.PropertyInfo propertyInfo = type.GetProperty(model.GetVersionPropertyName());
        
            //find the property type
            Type propertyType = propertyInfo.PropertyType;
        
            //Set the value of the property
            version = (decimal?)propertyInfo.GetValue(model);

            return version;
        }

        public bool? GetIsCurrent(T model)
        {
            bool? isCurrent = null;

            //find out the type
            Type type = model.GetType();
        
            //get the property information based on the type
            System.Reflection.PropertyInfo propertyInfo = type.GetProperty("IsCurrent");
        
            //find the property type
            Type propertyType = propertyInfo.PropertyType;
        
            //Set the value of the property
            isCurrent = (bool?)propertyInfo.GetValue(model);

            return isCurrent;
        }

        public void SetGuid(string guid, T model)
        {
            // value object
            object propertyVal = (object)guid;

            //find out the type
            Type type = model.GetType();
        
            //get the property information based on the type
            System.Reflection.PropertyInfo propertyInfo = type.GetProperty(model.GetGuidPropertyName());
        
            //find the property type
            Type propertyType = propertyInfo.PropertyType;
            
            //Convert.ChangeType does not handle conversion to nullable types
            //if the property type is nullable, we need to get the underlying type of the property
            var targetType = IsNullableType(propertyInfo.PropertyType) ? Nullable.GetUnderlyingType(propertyInfo.PropertyType) : propertyInfo.PropertyType;
            
            //Returns an System.Object with the specified System.Type and whose value is equivalent to the specified object.
            if (propertyVal != null)
                propertyVal = Convert.ChangeType(propertyVal, targetType);
        
            //Set the value of the property
            propertyInfo.SetValue(model, propertyVal, null);
        }

        public void SetVersion(decimal? version, T model)
        {
            // value object
            object propertyVal = (object)version;

            //find out the type
            Type type = model.GetType();
        
            //get the property information based on the type
            System.Reflection.PropertyInfo propertyInfo = type.GetProperty(model.GetVersionPropertyName());
        
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

        public BrashActionResult<T> Create(T model)
        {
            SetId(null, model);
            SetGuid(Guid.NewGuid().ToString(), model);
            SetVersion(1.0M, model);

            BrashActionResult<T> result = new BrashActionResult<T>()
            {
                Model = model
                , Message = "init"
                , Status = BrashActionStatus.INFORMATION
            };

            var id = PerformInsert(model);
            if (id > 0)
            {
                SetId(id, model);
                var fetchResult = Fetch(model);
                if (fetchResult.Status == BrashActionStatus.SUCCESS)
                {
                    result.Model = fetchResult.Model;
                    result.UpdateStatus(BrashActionStatus.SUCCESS, $"Record created. ({GetId(result.Model)})");
                    Logger.Information(result.Message);
                }
                else
                {
                    result.UpdateStatus(BrashActionStatus.ERROR, "Record creation failed.");
                    Logger.Error(result.Message);
                }
            }
            else
            {
                result.UpdateStatus(BrashActionStatus.ERROR, "Record creation failed. (fetch failure)");
                Logger.Error(result.Message);
            }

            return result;
        }
        public BrashActionResult<T> Fetch(T model)
        {
            BrashActionResult<T> result = new BrashActionResult<T>()
            {
                Model = model
                , Message = "init"
                , Status = BrashActionStatus.INFORMATION
            };

            IEnumerable<T> models = PerformFetch(model);
            if (models.Count() == 1)
            {
                result.UpdateStatus(BrashActionStatus.SUCCESS, "Record updated.");
                Logger.Information(result.Message);
                result.Model = models.FirstOrDefault();
            }
            else if (models.Count() > 1)
            {
                result.UpdateStatus(BrashActionStatus.ERROR, "More than 1 record found.");
                Logger.Error(result.Message);
                result.Model = models.FirstOrDefault();
            }
            else if (models.Count() == 0)
            {
                result.UpdateStatus(BrashActionStatus.ERROR, "Record not found.");
                Logger.Error(result.Message);
            }

            return result;
        }
        public BrashActionResult<T> Update(T model)
        {
            BrashActionResult<T> result = new BrashActionResult<T>()
            {
                Model = model
                , Message = "init"
                , Status = BrashActionStatus.INFORMATION
            };

            var record = PerformFetch(model).FirstOrDefault();
            Logger.Verbose($"Post Fetch to determine if we can insert new recrd from this version");
            Logger.Verbose($"ID: {GetId(record)}, GUID: {GetGuid(record)}, Version: {GetVersion(record)}, IsCurrent: {GetIsCurrent(record)}");
            if(record == null)
            {
                result.UpdateStatus(BrashActionStatus.ERROR, "Record not found.  Unable to make new version.");
                return result;
            }

            if(record != null && GetIsCurrent(record) == false )
            {
                result.UpdateStatus(BrashActionStatus.ERROR, "Record version is not current.");
                return result;
            }

            decimal? currentVersion = GetVersion(model);
            int rows = PerformUpdate(model);
            if (rows >= 1) // versioned records, multiple rows may be modified when setting IsCurrent to 0.
            {
                // reset other id fields to force retreival of new record
                SetId(0, model);
                SetVersion(GetVersion(model)+1, model);

                result.Model = PerformFetch(model).FirstOrDefault();
                if (result.Model != null)
                {
                    result.UpdateStatus(BrashActionStatus.SUCCESS, "Record updated.");
                }
                else
                {
                    result.UpdateStatus(BrashActionStatus.ERROR, "Record update failed.  Unable to find new record version.");
                }
            }
            else
            {
                result.UpdateStatus(BrashActionStatus.ERROR, "Record update failed.");
            }

            return result;
        }
        public BrashActionResult<T> Delete(T model)
        {
            BrashActionResult<T> result = new BrashActionResult<T>()
            {
                Model = model
                , Message = "init"
                , Status = BrashActionStatus.INFORMATION
            };

            int rows = PerformDelete(model);
            if (rows == 1)
            {
                result.UpdateStatus(BrashActionStatus.SUCCESS, "Record removed.");
            }
            else if (rows > 1)
            {
                result.UpdateStatus(BrashActionStatus.ERROR, "More than 1 record removed.");
            }
            else
            {
                result.UpdateStatus(BrashActionStatus.ERROR, "Record remove failed.");
            }

            return result;
        }

        public BrashQueryResult<T> FindWhere(string where)
        {
            BrashQueryResult<T> result = new BrashQueryResult<T>()
            {
                Models = new List<T>()
                , Message = "init"
                , Status = BrashQueryStatus.INFORMATION
            };

            IEnumerable<T> models = PerformFind(where);
            if (models.Count() > 0)
            {
                result.UpdateStatus(BrashQueryStatus.SUCCESS, $"{models.Count()} records found");
                result.Models = models.ToList();
            }
            else if (models.Count() == 0)
            {
                result.UpdateStatus(BrashQueryStatus.NO_RECORDS, $"{models.Count()} records found");
            }

            return result;
        }

        private int? PerformInsert(T model)
        {
            int? id;

            using (var connection = GetDatabaseConnection())
            {
                connection.Open();
                Logger.Verbose(RepositorySql.GetCreateStatement());
                Logger.Verbose($"ID: {GetId(model)}, GUID: {GetGuid(model)}, Version: {GetVersion(model)}");
                id = connection.Query<int>(RepositorySql.GetCreateStatement(), model).First();
            }

            return id;
        }

        private int PerformUpdate(T model)
        {
            int rows = 0;

            using (var connection = GetDatabaseConnection())
            {
                connection.Open();
                Logger.Verbose(RepositorySql.GetUpdateStatement());
                Logger.Verbose($"ID: {GetId(model)}, GUID: {GetGuid(model)}, Version: {GetVersion(model)}");
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
                Logger.Verbose(RepositorySql.GetDeleteStatement());
                Logger.Verbose($"ID: {GetId(model)}, GUID: {GetGuid(model)}, Version: {GetVersion(model)}");
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
                Logger.Verbose(RepositorySql.GetFetchStatement());
                Logger.Verbose($"ID: {GetId(model)}, GUID: {GetGuid(model)}, Version: {GetVersion(model)}");
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
                Logger.Verbose($"{RepositorySql.GetFindStatement()} {whereClause}");
                models = connection.Query<T>($"{RepositorySql.GetFindStatement()} {whereClause}");
            }

            return models;
        }
    }
}