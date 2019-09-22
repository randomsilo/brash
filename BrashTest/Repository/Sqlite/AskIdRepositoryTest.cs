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

namespace BrashTest.Repository.Sqlite
{
    public class AskIdRepositoryTest
    {
        [Fact]
        public void ModelInit()
        {
            var person = new BrashTest.Mock.Model.Person();
            Assert.NotNull(person); 
        }

        [Fact]
        public void RepoInit()
        {
            string path = "/shop/randomsilo/brash/BrashTest/sql/";
            string databaseFile = $"{path}/MockDb.sqlite";
            System.IO.File.Delete(databaseFile);


            var person = new BrashTest.Mock.Model.Person();
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

            IManageDatabase databaseManager = new DatabaseManager(databaseContext, personRepoSql);
            Assert.NotNull(databaseManager);

            databaseManager.CreateDatabase();

            var personRepo = new PersonRepository(databaseManager);
            Assert.NotNull(personRepo);

            ActionResult<BrashTest.Mock.Model.Person> result = null;

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

        [Fact]
        public void RepoFetchModel()
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

            IManageDatabase databaseManager = new DatabaseManager(databaseContext, personRepoSql);
            Assert.NotNull(databaseManager);

            databaseManager.CreateDatabase();

            var personRepo = new PersonRepository(databaseManager);
            Assert.NotNull(personRepo);

            ActionResult<BrashTest.Mock.Model.Person> result = null;

            // setup bogus
            var random = new Random();
            int randomNumber = random.Next();
            Randomizer.Seed = new Random(randomNumber);
            
            var faker = new Faker<BrashTest.Mock.Model.Person>()
                .RuleFor(m => m.FirstName, f => f.Name.FirstName(0))
                .RuleFor(m => m.LastName, f => f.Name.LastName(0))
                .RuleFor(m => m.MiddleName, f => f.Name.FirstName(0))
                .FinishWith((f, m) => Console.WriteLine($"Person modeled. FirstName={m.FirstName} LastName={m.LastName}"));

            var people = faker.Generate(10);

            List<int?> personIds = new List<int?>();
            foreach (var p in people)
            {
                result = personRepo.Create(p);

                Assert.True(result.Status == ActionStatus.SUCCESS);
                Assert.True(result.Model.PersonId >= 0);
                personIds.Add(result.Model.PersonId);
            }

            foreach(var id in personIds)
            {
                var model = new BrashTest.Mock.Model.Person() {
                    PersonId = id
                };

                result = personRepo.Fetch(model);
                Assert.True(result.Status == ActionStatus.SUCCESS);
                Assert.True(result.Model.PersonId >= 0);
            }
        }
    }
}
