# brash

Brash is a tool for quickly building an API project.
It is evolving to include other boilerplate code including markup.
The code is increase productivity.

## How to Use

```bash

cd ./brashcli

# create project init script
dotnet run project-init -n MyProject -d /shop/randomsilo/MyProject

# create sample json file
dotnet run data-init -n MyProject -d /shop/randomsilo/MyProject

# make c# projects
# cd /shop/randomsilo/MyProject
# . ./init.sh

# modify structure.json to fit project needs

# generate sqlite files 
## - mysql and sql server are future features
## - run combine.sh in the sql directory to create a single file for execution
##   Add additional files (indexes, data seeding), if necessary
dotnet run sqlite-gen --file /shop/randomsilo/MyProject/structure.json

# generate domain classes (cs is the c# prefix)
dotnet run cs-domain --file /shop/randomsilo/MyProject/structure.json

# generate repository classes
dotnet run cs-repo --file /shop/randomsilo/MyProject/structure.json

# generate api classes
dotnet run cs-api --file /shop/randomsilo/MyProject/structure.json

# generate xunit classes
dotnet run cs-test --file /shop/randomsilo/MyProject/structure.json

# generate bootstrap v4 cards, forms, and drop down choices
## materialcss, foundation are future features
dotnet run bs-html --file /shop/randomsilo/MyProject/structure.json


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
* References - is an array of foriegn keys to other tables. Pro tip - add reference structures to the top of the list as you define your entities.  Tables are created in top down order, parent than children.
  * ColumnName - name of the column on the table being defined
  * TableName - name of the table being reference, column name is inferred from referenced table's IdPattern
* Extensions - is an array of single row child tables (1 to 1). _Think star schema pattern_
* Children - is an array of multiple row child tables (1 to Many)
* Choices - is an array of strings used to populate choice tables.
* AdditionalSqlStatements - is an array of strings containing sql statements to be appended to sql file output.  Useful for seeding choice data with custom fields.

Extensions and Children use the structure pattern.
They can each have extensions and children.


## Suggested Tools

```bash
sudo apt-get install sqlitemon
sudo apt-get install sqlitebrowser
```