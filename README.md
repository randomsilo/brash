# brash

Brash is a tool for quickly building an API project.

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
dotnet run sqlite-gen --file /shop/randomsilo/MyProject/structure.json

## - run combine.sh in the sql directory to create a single file for execution
cd ./sql/sqlite
. ./combine.sh
cd ..
##   Add additional files (indexes, data seeding), if necessary

# generate domain classes (cs is the c# prefix)
dotnet run cs-domain --file /shop/randomsilo/MyProject/structure.json

# generate sqlite repository and service classes
dotnet run cs-repo-sqlite --file /shop/randomsilo/MyProject/structure.json

# generate xunit classes
dotnet run cs-xtest-sqlite --file /shop/randomsilo/MyProject/structure.json

# generate api classes
dotnet run cs-api-sqlite --file /shop/randomsilo/MyProject/structure.json

```

## Data File

The generated data file is used to build the database structure, domain classes, repository classes, and api routes.

### Structure

The structure section of the data file contains a hierarchy of objects.
Each object represents a table.

* [x] Name - is the table name
* [x] IdPattern - is the id pattern used to make/manage a row
  * [x] AskId - (Default) Anonymous Surrogate Key Id pattern uses an autoincrementing column called _table_Id as the primary key
    * [x] Database
    * [x] Domain
    * [x] Repository Sql
    * [x] Repository
    * [x] Service
    * [x] Tests
    * [ ] Api
    * [ ] Api Client
  * [ ] AskGuid - Anonymous Surrogate Key Guid pattern uses a string column called _table_Guid as the primary key
    * [x] Database
      * [ ] Indexes
    * [x] Domain
    * [x] Repository Sql
    * [x] Repository
    * [x] Service
    * [x] Tests
    * [ ] Api
    * [ ] Api Client
  * [ ] AskVersion - Anonymous Surrogate Key Version pattern uses an id, guid, version, and current indicator column
    * [x] Database
      * [ ] Indexes
    * [x] Domain
    * [x] Repository Sql
    * [x] Repository
    * [x] Service
    * [x] Tests
    * [ ] Api
    * [ ] Api Client
* [ ] TrackingPattern - is a set columns to be added to the table for tracking purposes
  * [x] None - (Default) No additional fields
  * [ ] Audit - fields CreatedBy, CreatedOn, UpdatedBy, UpdatedOn
    * [ ] Database
    * [ ] Domain
    * [ ] Repository Sql
    * [ ] Repository
    * [ ] Service
    * [ ] Tests
    * [ ] Api
    * [ ] Api Client
  * [ ] AuditPreserve - CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted
    * [ ] Database
    * [ ] Domain
    * [ ] Repository Sql
    * [ ] Repository
    * [ ] Service
    * [ ] Tests
    * [ ] Api
    * [ ] Api Client
  * [ ] AuditVersion - fields RecordState (Created, Updated, Deleted, Restored), PerformedBy, PeformedOn, PerformedReason
    * [ ] Database
    * [ ] Repository Sql
    * [ ] Repository
    * [ ] Service
    * [ ] Tests
    * [ ] Api
    * [ ] Api Client
* [x] AdditionalPatterns - an array of Pattern names to add additional columns and behavior
  * [x] Choice - fields ChoiceName, OrderNo, IsDisabled
* [x] Fields - an array of field definitions to make addional columns on the table
  * [x] Name - name of the column
  * [x] Type - single character code for data type mapping
    * [x] S = String
    * [x] G = Guid
    * [x] D = DateTime
    * [x] N = Decimal
    * [x] I = Integer
    * [ ] B = Blob
    * [ ] C = Clob
  * [x] Faker - a code string used when building the xunit tests for repo testing.
    * [x] Faker="YOUR_CONTENT" is the will result in m.columnName = YOUR_CONTENT;
    * [x] f is for Faker
    * [x] m is for Model
    * [x] Brash will write the rules based on the data type if you don't override it.
    * [x] Bogus can grow and expand without changes to Brash by using this convention.
  
    ```c#
    // https://github.com/bchavez/Bogus#bogus-api-support

    var myModelFaker = new Faker<MyModel>()
        .StrictMode(false)
        .Rules((f, m) =>
            {
              m.Id = f.IndexFaker;
              m.Guid = Guid.NewGuid();
              m.Lastname = f.Name.LastName(0);   // 0 - Male, 1 - Female
              m.Firstname = f.Name.FirstName(0); // 0 - Male, 1 - Female
              m.UserName = f.Internet.UserName(m.FirstName, m.LastName);
              m.Content = f.Lorem.Paragraphs();
              m.Created = f.Date.Past();
              m.IsActive = f.PickRandomParam(new bool[] { true, true, false });
              m.MetaDescription= f.Lorem.Sentences(3);
              m.Keywords = string.Join(", ", f.Lorem.Words());
              m.Title = f.Lorem.Sentence(10);
            })
        .FinishWith((f, m) => Console.WriteLine($"MyModel created. Id={m.Id}"));

    var myModel = myModelFaker.Generate();
    ```
  
* [x] References - is an array of foriegn keys to other tables. Pro tip - add reference structures to the top of the list as you define your entities.  Tables are created in top down order, parent than children.
  * [x] ColumnName - name of the column on the table being defined
  * [x] TableName - name of the table being reference, column name is inferred from referenced table's IdPattern
* [x] Extensions - is an array of single row child tables (1 to 1). _Think star schema pattern_
* [x] Children - is an array of multiple row child tables (1 to Many)
* [x] Choices - is an array of strings used to populate choice tables.
* [x] AdditionalSqlStatements - is an array of strings containing sql statements to be appended to sql file output.  Useful for seeding choice data with custom fields.

Extensions and Children use the structure pattern.
They can each have extensions and children.

* [ ] Api
  * [ ] AskId
    * [x] List
    * [x] Get
    * [x] Create
    * [x] Update
    * [x] Delete
    * [ ] Referential
      * [ ] Find By AskId Parent
      * [ ] Find By AskGuid Parent
      * [ ] Find By AskVersion Parent
  * [ ] AskGuid
    * [ ] List
    * [ ] Get
    * [ ] Create
    * [ ] Update
    * [ ] Delete
    * [ ] Referential
      * [ ] Find By AskId Parent
      * [ ] Find By AskGuid Parent
      * [ ] Find By AskVersion Parent
  * [ ] AskVersion
    * [ ] List
    * [ ] Get
    * [ ] Create
    * [ ] Update
    * [ ] Delete
    * [ ] Referential
      * [ ] Find By AskId Parent
      * [ ] Find By AskGuid Parent
      * [ ] Find By AskVersion Parent

## Suggested Tools

```bash
sudo apt-get install sqlitemon
sudo apt-get install sqlitebrowser
```

## Install dotnet 3.0

```bash
wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo add-apt-repository universe
sudo apt-get update
sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install dotnet-sdk-3.0

```

### Add Local Brash Reference

```bash
# Open the each csproj and remove brash nuget package then
dotnet add Strawman.Domain/Strawman.Domain.csproj reference ../brash/Brash/Brash.csproj
dotnet add Strawman.Infrastructure/Strawman.Infrastructure.csproj reference ../brash/Brash/Brash.csproj
dotnet add Strawman.Infrastructure.Test/Strawman.Infrastructure.Test.csproj reference ../brash/Brash/Brash.csproj
dotnet add Strawman.Api/Strawman.Api.csproj reference ../brash/Brash/Brash.csproj

```
