using System;
using System.Reflection;
using Xunit;
using Brash.Infrastructure;
using Brash.Infrastructure.Sqlite;
using BrashTest.Mock.Model;
using BrashTest.Mock.Repository;
using BrashTest.Mock.RepositorySql;

namespace BrashTest.Repository.Sqlite
{
    public class AskIdRepositoryTest
    {
        [Fact]
        public void ModelInit()
        {
            var person = new Person();
            Assert.NotNull(person); 
        }

        [Fact]
        public void RepoInit()
        {
            string path = "/shop/randomsilo/brash/BrashTest/sql/";
            string databaseFile = $"{path}/MockDb.sqlite";
            System.IO.File.Delete(databaseFile);


            var person = new Person();
            Assert.NotNull(person);

            IDatabaseContext databaseContext = new DatabaseContext(
                $"Data Source={databaseFile}" 
                , "MockDb"
                , "MockSchema"
                , $"{path}/Person.sql"
            );
            Assert.NotNull(databaseContext);

            var personRepoSql = new PersonRepositorySql();
            Assert.NotNull(personRepoSql);

            IManageDatabase databaseManager = new DatabaseManager(databaseContext, personRepoSql);
            Assert.NotNull(databaseManager);

            databaseManager.CreateDatabase();

            var personRepo = new PersonRepository(databaseManager);
            Assert.NotNull(personRepo); 
        }

        [Fact]
        public void RepoCreateModel()
        {
            MethodBase m = MethodBase.GetCurrentMethod();
            string dbName = $"{m.ReflectedType.Name}_{m.Name}"; 
            string path = "/shop/randomsilo/brash/BrashTest/sql/";
            string databaseFile = $"{path}/{dbName}.sqlite";
            System.IO.File.Delete(databaseFile);


            var person = new Person()
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

            IManageDatabase databaseManager = new DatabaseManager(databaseContext, personRepoSql);
            Assert.NotNull(databaseManager);

            databaseManager.CreateDatabase();

            var personRepo = new PersonRepository(databaseManager);
            Assert.NotNull(personRepo);

            ActionResult<Person> result = null;

            result = personRepo.Create(person);
            Assert.True(result.Status == ActionStatus.SUCCESS);
            Assert.True(result.Model.PersonId > 0);

            person.LastName = "Parker";
            result = personRepo.Update(person);
            Assert.True(result.Status == ActionStatus.SUCCESS);
            Assert.Equal("Parker", result.Model.LastName);

            result = personRepo.Delete(person);
            Assert.True(result.Status == ActionStatus.SUCCESS);
        }
    }
}
