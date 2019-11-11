using System;
using Serilog;
using Brash.Model;

namespace Brash.Infrastructure
{
    public abstract class AAskIdService<T> : IAskIdService<T> where T : IAskId
    {
        protected IAskIdRepository<T> Repository { get; private set; }
        protected ILogger Logger { get; set; }
        
        public AAskIdService(IAskIdRepository<T> repository, ILogger logger)
        {
            Repository = repository;
            Logger = logger;
        }

        public ServiceResult<T> Create(T model)
        {
            ServiceResult<T> serviceResult = new ServiceResult<T>();

            serviceResult.PreWorkResult = CreatePreWork(model);

            if (serviceResult.PreWorkResult.Status != ActionStatus.ERROR)
            {
                serviceResult.WorkResult = Repository.Create(model);

                if (serviceResult.WorkResult.Status != ActionStatus.ERROR)
                {
                    serviceResult.PostWorkResult = CreatePostWork(serviceResult.WorkResult.Model);
                }
                else
                {
                    Logger.Error(serviceResult.WorkResult.Message);
                }
            }
            else
            {
                Logger.Error(serviceResult.PreWorkResult.Message);
            }

            return serviceResult;
        }

        public ServiceResult<T> Fetch(T model)
        {
            ServiceResult<T> serviceResult = new ServiceResult<T>();

            serviceResult.PreWorkResult = FetchPreWork(model);

            if (serviceResult.PreWorkResult.Status != ActionStatus.ERROR)
            {
                serviceResult.WorkResult = Repository.Fetch(model);

                if (serviceResult.WorkResult.Status != ActionStatus.ERROR)
                {
                    serviceResult.PostWorkResult = FetchPostWork(serviceResult.WorkResult.Model);
                }
                else
                {
                    Logger.Error(serviceResult.WorkResult.Message);
                }
            }
            else
            {
                Logger.Error(serviceResult.PreWorkResult.Message);
            }

            return serviceResult;
        }

        public ServiceResult<T> Update(T model)
        {
            ServiceResult<T> serviceResult = new ServiceResult<T>();

            serviceResult.PreWorkResult = UpdatePreWork(model);

            if (serviceResult.PreWorkResult.Status != ActionStatus.ERROR)
            {
                serviceResult.WorkResult = Repository.Update(model);

                if (serviceResult.WorkResult.Status != ActionStatus.ERROR)
                {
                    serviceResult.PostWorkResult = UpdatePostWork(serviceResult.WorkResult.Model);
                }
                else
                {
                    Logger.Error(serviceResult.WorkResult.Message);
                }
            }
            else
            {
                Logger.Error(serviceResult.PreWorkResult.Message);
            }

            return serviceResult;
        }

        public ServiceResult<T> Delete(T model)
        {
            ServiceResult<T> serviceResult = new ServiceResult<T>();

            serviceResult.PreWorkResult = DeletePreWork(model);

            if (serviceResult.PreWorkResult.Status != ActionStatus.ERROR)
            {
                serviceResult.WorkResult = Repository.Delete(model);

                if (serviceResult.WorkResult.Status != ActionStatus.ERROR)
                {
                    serviceResult.PostWorkResult = DeletePostWork(serviceResult.WorkResult.Model);
                }
                else
                {
                    Logger.Error(serviceResult.WorkResult.Message);
                }
            }
            else
            {
                Logger.Error(serviceResult.PreWorkResult.Message);
            }

            return serviceResult;
        }

        public QueryResult<T> FindWhere(string where)
        {
            return Repository.FindWhere(where);
        }

        public virtual ActionResult<T> CreatePreWork(T model)
        {
            return new ActionResult<T>() {
                Model = model,
                Status = ActionStatus.SUCCESS,
                Message = ""
            };
        }

        public virtual ActionResult<T> FetchPreWork(T model)
        {
            return new ActionResult<T>() {
                Model = model,
                Status = ActionStatus.SUCCESS,
                Message = ""
            };
        }

        public virtual ActionResult<T> UpdatePreWork(T model)
        {
            return new ActionResult<T>() {
                Model = model,
                Status = ActionStatus.SUCCESS,
                Message = ""
            };
        }

        public virtual ActionResult<T> DeletePreWork(T model)
        {
            return new ActionResult<T>() {
                Model = model,
                Status = ActionStatus.SUCCESS,
                Message = ""
            };
        }

        public virtual ActionResult<T> CreatePostWork(T model)
        {
            return new ActionResult<T>() {
                Model = model,
                Status = ActionStatus.SUCCESS,
                Message = ""
            };
        }

        public virtual ActionResult<T> FetchPostWork(T model)
        {
            return new ActionResult<T>() {
                Model = model,
                Status = ActionStatus.SUCCESS,
                Message = ""
            };
        }

        public virtual ActionResult<T> UpdatePostWork(T model)
        {
            return new ActionResult<T>() {
                Model = model,
                Status = ActionStatus.SUCCESS,
                Message = ""
            };
        }

        public virtual ActionResult<T> DeletePostWork(T model)
        {
            return new ActionResult<T>() {
                Model = model,
                Status = ActionStatus.SUCCESS,
                Message = ""
            };
        }
    }
}