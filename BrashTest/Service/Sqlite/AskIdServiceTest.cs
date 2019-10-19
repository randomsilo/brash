using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Bogus;
using Brash.Infrastructure;
using Brash.Infrastructure.Sqlite;
using BrashTest.Mock.Model;
using BrashTest.Mock.Repository;
using BrashTest.Mock.RepositorySql;
using BrashTest.Mock.Service;

namespace BrashTest.Repository.Sqlite
{
    public class AskIdServiceTest
    {
        
        [Fact]
        public void ServiceCreateModel()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            string dbName = $"{methodBase.ReflectedType.Name}_{methodBase.Name}"; 
            string path = "/shop/randomsilo/brash/BrashTest/sql/";
            string databaseFile = $"{path}/{dbName}.sqlite";
            System.IO.File.Delete(databaseFile);


            var person = new BrashTest.Mock.Model.Person()
            {
                LastName = "Smith"
                , FirstName = "Jane"
                , MiddleName = "Francis"
            };
            Assert.NotNull(person);

            IDatabaseContext databaseContext = new DatabaseContext(
                $"Data Source={databaseFile}" 
                , $"{dbName}"
                , "MockSchema"
                , $"{path}/Person.sql"
            );
            Assert.NotNull(databaseContext);

            var personRepoSql = new PersonRepositorySql();
            Assert.NotNull(personRepoSql);

            IManageDatabase databaseManager = new DatabaseManager(databaseContext);
            Assert.NotNull(databaseManager);

            databaseManager.CreateDatabase();

            var personRepo = new PersonRepository(databaseManager, personRepoSql);
            Assert.NotNull(personRepo);

            var personService = new PersonService(personRepo);
            Assert.NotNull(personService);

            // valid lastname
            person = new BrashTest.Mock.Model.Person()
            {
                LastName = "Smith"
                , FirstName = "Dave"
                , MiddleName = "Milton"
            };

            var serviceResult = personService.Create(person);
            Assert.False(serviceResult.HasError());
            Assert.True(serviceResult.WorkResult.Model.PersonId > 0);

            // invalid lastname
            person = new BrashTest.Mock.Model.Person()
            {
                LastName = "EXPLODE"
                , FirstName = "Dave"
                , MiddleName = "Milton"
            };

            serviceResult = personService.Create(person);
            Assert.True(serviceResult.HasError());
        }

    }
}
