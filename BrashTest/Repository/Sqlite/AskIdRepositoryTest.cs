using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Bogus;
using Serilog;
using Brash.Infrastructure;
using Brash.Infrastructure.Sqlite;
using BrashTest.Mock.Model;
using BrashTest.Mock.Repository;
using BrashTest.Mock.RepositorySql;

namespace BrashTest.Repository.Sqlite
{
    public class AskIdRepositoryTest
    {
        private static ILogger GetLogger(string filename)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File($"{filename}", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

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

            string logFile = $"{path}/MockDb.log";
            ILogger logger = GetLogger(logFile);


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

            IManageDatabase databaseManager = new DatabaseManager(databaseContext);
            Assert.NotNull(databaseManager);

            databaseManager.CreateDatabase();

            var personRepo = new PersonRepository(databaseManager, personRepoSql, logger);
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

            string logFile = $"{path}/{dbName}.log";
            ILogger logger = GetLogger(logFile);


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

            var personRepo = new PersonRepository(databaseManager, personRepoSql, logger);
            Assert.NotNull(personRepo);

            BrashActionResult<BrashTest.Mock.Model.Person> result = null;

            result = personRepo.Create(person);
            Assert.True(result.Status == BrashActionStatus.SUCCESS);
            Assert.True(result.Model.PersonId > 0);

            person.LastName = "Parker";
            result = personRepo.Update(person);
            Assert.True(result.Status == BrashActionStatus.SUCCESS);
            Assert.Equal("Parker", result.Model.LastName);

            result = personRepo.Delete(person);
            Assert.True(result.Status == BrashActionStatus.SUCCESS);
        }

        [Fact]
        public void RepoFetchModel()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            string dbName = $"{methodBase.ReflectedType.Name}_{methodBase.Name}"; 
            string path = "/shop/randomsilo/brash/BrashTest/sql/";
            string databaseFile = $"{path}/{dbName}.sqlite";
            System.IO.File.Delete(databaseFile);

            string logFile = $"{path}/{dbName}.log";
            ILogger logger = GetLogger(logFile);


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

            var personRepo = new PersonRepository(databaseManager, personRepoSql, logger);
            Assert.NotNull(personRepo);

            BrashActionResult<BrashTest.Mock.Model.Person> result = null;

            // setup bogus
            var random = new Random();
            int randomNumber = random.Next();
            Randomizer.Seed = new Random(randomNumber);
            
            var personFaker = new Faker<BrashTest.Mock.Model.Person>()
                .StrictMode(false)
                .Rules((f, m) =>
                {
                    m.PersonId = f.IndexFaker;
                    m.LastName = f.Name.LastName(0);   // 0 - Male, 1 - Female
                    m.FirstName = f.Name.FirstName(0); // 0 - Male, 1 - Female
                    m.MiddleName = f.Name.FirstName(0); // 0 - Male, 1 - Female
                })
                .FinishWith((f, m) => Console.WriteLine($"personFaker created. Id={m.PersonId}, FirstName={m.FirstName}, LastName={m.LastName}"));

            var people = personFaker.Generate(10);

            List<int?> personIds = new List<int?>();
            foreach (var p in people)
            {
                result = personRepo.Create(p);

                Assert.True(result.Status == BrashActionStatus.SUCCESS);
                Assert.True(result.Model.PersonId >= 0);
                personIds.Add(result.Model.PersonId);
            }

            foreach(var id in personIds)
            {
                var model = new BrashTest.Mock.Model.Person() {
                    PersonId = id
                };

                result = personRepo.Fetch(model);
                Assert.True(result.Status == BrashActionStatus.SUCCESS);
                Assert.True(result.Model.PersonId >= 0);
            }
        }

        [Fact]
        public void RepoFindModel()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            string dbName = $"{methodBase.ReflectedType.Name}_{methodBase.Name}"; 
            string path = "/shop/randomsilo/brash/BrashTest/sql/";
            string databaseFile = $"{path}/{dbName}.sqlite";
            System.IO.File.Delete(databaseFile);

            string logFile = $"{path}/{dbName}.log";
            ILogger logger = GetLogger(logFile);


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

            var personRepo = new PersonRepository(databaseManager, personRepoSql, logger);
            Assert.NotNull(personRepo);

            BrashActionResult<BrashTest.Mock.Model.Person> result = null;

            List<Mock.Model.Person> people = new List<Mock.Model.Person>();
            people.Add(new Mock.Model.Person()
            {
                PersonId = null
                , LastName = "Smith"
                , MiddleName = "Matt"
                , FirstName = "David"
            });

            people.Add(new Mock.Model.Person()
            {
                PersonId = null
                , LastName = "Quincy"
                , MiddleName = "Kyle"
                , FirstName = "Rick"
            });

            people.Add(new Mock.Model.Person()
            {
                PersonId = null
                , LastName = "Givens"
                , MiddleName = "Matt"
                , FirstName = "Robert"
            });

            List<int?> personIds = new List<int?>();
            foreach (var p in people)
            {
                result = personRepo.Create(p);

                Assert.True(result.Status == BrashActionStatus.SUCCESS);
                Assert.True(result.Model.PersonId >= 0);
                personIds.Add(result.Model.PersonId);
            }

            BrashQueryResult<Mock.Model.Person> findResult = null;

            findResult = personRepo.FindWhere("WHERE MiddleName = 'Matt'");
            Assert.True(findResult.Status == BrashQueryStatus.SUCCESS);
            Assert.True(findResult.Models.Count() == 2);

            findResult = personRepo.FindWhere("WHERE MiddleName = 'Jamie'");
            Assert.True(findResult.Status == BrashQueryStatus.NO_RECORDS);
            Assert.True(findResult.Models.Count() == 0);


        }

    }
}
