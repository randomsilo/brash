using System;
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
    }
}
