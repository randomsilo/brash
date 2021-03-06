using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brash.Model;
using Serilog;
using Brash.Infrastructure;
using Brash.Infrastructure.Sqlite;
using BrashTest.Mock.Model;
using BrashTest.Mock.RepositorySql;

namespace BrashTest.Mock.Repository
{
    public class PersonRepository : AskIdRepository<Person>
    {
        public PersonRepository(IManageDatabase databaseManager, AAskIdRepositorySql repositorySql, ILogger logger) : base(databaseManager, repositorySql, logger)
        {

        }
    }
}