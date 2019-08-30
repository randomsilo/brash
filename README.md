# hasty

Hasty is a tool for quickly building sqlite backed APIs


## How to Use

```bash

cd ./brashcli

# create project init script
dotnet run project-init -n PollBook -d /shop/randomsilo/PollBook

# create sample json file
dotnet run data-init -n PollBook -d /shop/randomsilo/PollBook

# make c# projects
# cd /shop/randomsilo/PollBook
# . ./init.sh

# modify structure.json to fit project needs
# generate sql files
dotnet run sql-gen -f /shop/randomsilo/PollBook/structure.json

# generate domain classes
dotnet run domain-gen -f /shop/randomsilo/PollBook/structure.json

# generate repository classes
dotnet run repo-gen -f /shop/randomsilo/PollBook/structure.json

# generate api classes
dotnet run api-gen -f /shop/randomsilo/PollBook/structure.json

# generate xunit classes
dotnet run test-gen -f /shop/randomsilo/PollBook/structure.json



```

## Deploy NuGet Package

```bash
cd /shop/randomsilo/brash/Brash/bin/Debug/
dotnet nuget push Brash.1.0.0.nupkg -k oy2f6zfjelxyfzypku7qjwze4d3ev2quhm6zvresyvywka -s https://api.nuget.org/v3/index.json

```

## Data File

The generated data file is used to build the database structure, domain classes, repository classes, and api routes.

### Structure

The structure section of the data file contains a hierarchy of objects.
Each object represents a table.

* Name - is the table name
* IdPattern - is the id pattern used to make/manage a row
  * AskId - (Default) Anonymous Surrogate Key Id pattern uses an autoincrementing column called _table_Id as the primary key
  * AskGuid - Anonymous Surrogate Key Guid pattern uses a string column called _table_Guid as the primary key
  * AskVersion - Anonymous Surrogate Key Version pattern uses an id, guid, version, and current indicator column
* TrackingPattern - is a set columns to be added to the table for tracking purposes
  * None - (Default) No additional fields
  * Audit - fields CreatedBy, CreatedOn, UpdatedBy, UpdatedOn
  * AuditPreserve - CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted
  * AuditVersion - fields RecordState (Created, Updated, Deleted, Restored), PerformedBy, PeformedOn, PerformedReason
  * Session - TBD, SessionGuidRef, join to Session table with user, ip address, and location information
  * Device - TBD, DeviceGuidRef, join to Device table with mobile device information like serial number, ip address, os, etc..
* AdditionalPatterns - an array of Pattern names to add additional columns and behavior 
  * Choice - fields ChoiceName, OrderNo, IsDisabled
* Fields - an array of field definitions to make addional columns on the table
  * Name - name of the column
  * Type - single character code for data type mapping
    * S = String
    * G = Guid
    * D = DateTime
    * N = Decimal
    * I = Integer
    * B = Blob
    * C = Clob
* References - is an array of foriegn keys to other tables
  * ColumnName - name of the column on the table being defined
  * TableName - name of the table being reference, column name is inferred from referenced table's IdPattern
* Extensions - is an array of single row child tables (1 to 1). _Think star schema pattern_
* Children - is an array of multiple row child tables (1 to Many)
* Choices - is an array of strings used to populate choice tables.
* AdditionalSqlStatements - is an array of strings containing sql statements to be appended to sql file output.  Useful for seeding choice data with custom fields.

Extensions and Children use the structure pattern.
They can each have extensions and children.
