using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Brash.Infrastructure;
using BrashTest.Mock.Model;

namespace BrashTest.Mock.Service
{
	public class PersonService : AAskIdService<Person>
	{
        public PersonService(IAskIdRepository<Person> repository, ILogger logger) : base(repository, logger)
        {
            
        }

        public override ActionResult<Person> CreatePreWork(Person model)
        {
            Logger.Verbose("PersonService.CreatePreWork");
            var result = new ActionResult<Person>() 
            {
                Model = model,
                Status = ActionStatus.SUCCESS,
                Message = ""
            };

            if (model.LastName.Equals("EXPLODE"))
            {
                result = new ActionResult<Person>() 
                {
                    Model = model,
                    Status = ActionStatus.ERROR,
                    Message = "LastName has exploded!"
                };
            }

            return result;
        }

        public override ActionResult<Person> CreatePostWork(Person model)
        {
            Logger.Verbose("PersonService.CreatePostWork");
            var result = new ActionResult<Person>() 
            {
                Model = model,
                Status = ActionStatus.INFORMATION,
                Message = "Before simulated data transmission."
            };

            // send to another system via topic, queue, or url
            // - for low availability systems, the data, might need to be stored in a table for mutliple retries
            Logger.Verbose("Before thread sleep");
            System.Threading.Thread.Sleep(1000);
            Logger.Verbose("After thread sleep");

            result = new ActionResult<Person>() 
            {
                Model = model,
                Status = ActionStatus.SUCCESS,
                Message = "Simulate 1 second delay"
            };

            return result;
        } 
    }
}