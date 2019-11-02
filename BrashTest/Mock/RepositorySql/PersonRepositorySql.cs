using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brash.Model;
using Brash.Infrastructure;
using Brash.Infrastructure.Sqlite;

namespace BrashTest.Mock.RepositorySql
{
    public class PersonRepositorySql : AAskIdRepositorySql
    {
        public PersonRepositorySql() : base()
        {
            _sql[AskIdRepositorySqlTypes.CREATE] = @"
            INSERT INTO Person (
                LastName
                , FirstName
                , MiddleName
            ) VALUES (
                @LastName
                , @FirstName
                , @MiddleName);
            SELECT last_insert_rowid();
            ";
            
            _sql[AskIdRepositorySqlTypes.FETCH] = @"
            SELECT 
                PersonId
                , LastName
                , FirstName
                , MiddleName 
            FROM 
                Person 
            WHERE 
                PersonId = @PersonId;
            ";
            
            _sql[AskIdRepositorySqlTypes.UPDATE] = @"
            UPDATE Person
            SET
                LastName = @LastName
                , FirstName = @FirstName
                , MiddleName = @MiddleName
            WHERE
                PersonId = @PersonId;
            ";
            
            _sql[AskIdRepositorySqlTypes.DELETE] = @"
            DELETE FROM Person
            WHERE
                PersonId = @PersonId;
            ";

            _sql[AskIdRepositorySqlTypes.FIND] = @"
            SELECT 
                PersonId
                , LastName
                , FirstName
                , MiddleName 
            FROM 
                Person 
            ";
        }
    }
}