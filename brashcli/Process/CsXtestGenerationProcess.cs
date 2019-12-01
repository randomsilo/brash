using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Serilog;
using Brash.Extension;
using brashcli.Option;
using brashcli.Model;


namespace brashcli.Process
{
    public class CsXtestGenerationProcess
    {
        private ILogger _logger;
        private CsXtestGeneration _options;
		private string _pathProject;
		private string _pathFakerDirectory;
		private string _pathRepositoryTestDirectory;
		private string _pathServiceTestDirectory;
		private DomainStructure _domainStructure;
		private Dictionary<string,string> _tablePrimaryKeyDataType = new Dictionary<string, string>();
		private List<string> _ruleStatements = new List<string>();
		private List<string> _fakerStatements = new List<string>();
		private List<string> _repoStatements = new List<string>();
		private bool _addCounter = false;
		
        public CsXtestGenerationProcess(ILogger logger, CsXtestGeneration options)
        {
            _logger = logger;
            _options = options;
			_pathProject = System.IO.Path.GetDirectoryName(_options.FilePath);
        }

        public int Execute()
        {
            int returnCode = 0;

            _logger.Debug("CsXtestGenerationProcess: start");
            do
            {
                try 
                {
					ReadDataJsonFile();
                    MakeDirectories();
					CreateXunitFiles();
                }
                catch(Exception exception)
                {
                    _logger.Error(exception, "CsXtestGenerationProcess, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while(false);
            _logger.Debug("CsXtestGenerationProcess: end");

            return returnCode;
        }

        private void MakeDirectories()
        {
			var directory = _domainStructure.Domain + "." + "Infrastructure.Test";
			_pathFakerDirectory = System.IO.Path.Combine(_pathProject, directory, "Sqlite/Faker");
			System.IO.Directory.CreateDirectory(_pathFakerDirectory);

			_pathRepositoryTestDirectory = System.IO.Path.Combine(_pathProject, directory, "Sqlite/Repository");
			System.IO.Directory.CreateDirectory(_pathRepositoryTestDirectory);

			_pathServiceTestDirectory = System.IO.Path.Combine(_pathProject, directory, "Sqlite/Service");
			System.IO.Directory.CreateDirectory(_pathServiceTestDirectory);	
        }

        private void ReadDataJsonFile()
        {
			string json = System.IO.File.ReadAllText(_options.FilePath);
			_domainStructure = JsonConvert.DeserializeObject<DomainStructure>(json, new JsonSerializerSettings()
			{
				MissingMemberHandling = MissingMemberHandling.Ignore
			});
			_logger.Information($"Domain: {_domainStructure.Domain}, Structures: {_domainStructure.Structure.Count}");
        }

		private void CreateXunitFiles()
		{
			_logger.Debug("CreateXunitFiles");
			
			foreach( var entry in _domainStructure.Structure)
			{
				MakeFiles(null, entry);
			}
		}

		private void MakeFiles( Structure parent, Structure entity)
		{
			_logger.Debug($"{entity.Name}");
			if (parent != null)
				_logger.Debug($"\t Parent: {parent.Name}");
			
			SaveTableIdDataType(entity);
			AnalyzeStructure(parent, entity);

			MakeFakerFileCs(parent, entity);
			MakeRepoTestFileCs(parent, entity);
			MakeServiceTestFileCs(parent, entity);
			
			if (entity.Children != null && entity.Children.Count > 0)
			{
				foreach( var child in entity.Children)
				{
					MakeFiles(entity, child);
				}
			}
			
			if (entity.Extensions != null && entity.Extensions.Count > 0)
			{
				foreach( var extension in entity.Extensions)
				{
					MakeFiles(entity, extension);
				}
			}
		}

		public string MakeRepoTestFilePath(Structure entry)
		{
			return System.IO.Path.Combine(_pathRepositoryTestDirectory, entry.Name + "RepositoryTest.cs");
		}

		private void MakeRepoTestFileCs(Structure parent, Structure entry)
		{
			string fileNamePath = MakeRepoTestFilePath(entry);
			StringBuilder lines = new StringBuilder();

			lines.Append( TplCsRepoXtest(
				_domainStructure.Domain
				, entry.Name
				, entry.IdPattern ?? "AskId"
				, _pathProject
			));

			System.IO.File.WriteAllText( fileNamePath, lines.ToString());
		}

		public string TplCsRepoXtest(
			string domain
			, string entityName
			, string idPattern
			, string basePath
			)
        {
            string template = null;

			switch(idPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					template = TplCsRepoXtestAskGuid(domain, entityName, idPattern, basePath);
					break;
				case Global.IDPATTERN_ASKVERSION:
					template = TplCsRepoXtestAskVersion(domain, entityName, idPattern, basePath);
					break;
				case Global.IDPATTERN_ASKID:
				default:
					template = TplCsRepoXtestAskId(domain, entityName, idPattern, basePath);
					break;
			}

            return template;
        }

		public string TplCsRepoXtestAskId(
			string domain
			, string entityName
			, string idPattern
			, string projectPath
			)
        {
            StringBuilder lines = new StringBuilder();

			lines.Append($"\nusing System.Collections.Generic;");
			lines.Append($"\nusing System.Reflection;");
			lines.Append($"\nusing Xunit;");
			lines.Append($"\nusing Serilog;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing Brash.Infrastructure.Sqlite;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Repository;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.RepositorySql;");
			lines.Append($"\nusing {domain}.Infrastructure.Test.Sqlite.Faker;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Infrastructure.Test.Sqlite.Repository");
			lines.Append( "\n{");
			lines.Append($"\n\tpublic class {entityName}RepositoryTest");
			lines.Append( "\n\t{");
			lines.Append($"\n\t\tpublic string GetDatabase(string path, MethodBase methodBase)");
			lines.Append( "\n\t\t{");
			lines.Append( "\n\t\t\tstring dbName = $\"{methodBase.ReflectedType.Name}_{methodBase.Name}\";"); 
			lines.Append( "\n\t\t\tstring databaseFile = $\"{path}/{dbName}.sqlite\";");
			lines.Append($"\n\t\t\tSystem.IO.File.Delete(databaseFile);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\treturn databaseFile;");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\tpublic static ILogger GetLogger(string filename)");
			lines.Append( "\n\t\t{");
			lines.Append( "\n\t\t\treturn new LoggerConfiguration()");
            lines.Append( "\n\t\t\t\t.MinimumLevel.Verbose()");
            lines.Append( "\n\t\t\t\t.WriteTo.File($\"{filename}\", rollingInterval: RollingInterval.Day)");
            lines.Append( "\n\t\t\t\t.CreateLogger();");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\t[Fact]");
			lines.Append($"\n\t\tpublic void CreateUpdateDeleteFetch()");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\t// file system");
			lines.Append($"\n\t\t\tvar path = \"{projectPath}\";");
			lines.Append($"\n\t\t\tvar project = \"{domain}\";");
			lines.Append( "\n\t\t\tvar outputPath = $\"{path}/{project}.Infrastructure.Test/TestOutput/\";");
			lines.Append( "\n\t\t\tvar databaseFile = GetDatabase(outputPath, MethodBase.GetCurrentMethod());");
			lines.Append($"\n\t\t\t");

			lines.Append($"\n\t\t\t// logger");
			lines.Append( "\n\t\t\tILogger logger = GetLogger($\"{outputPath}/{MethodBase.GetCurrentMethod().ReflectedType.Name}_{MethodBase.GetCurrentMethod().Name}.log\");");
			lines.Append($"\n\t\t\t");

			lines.Append($"\n\t\t\t// database setup");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - context");
			lines.Append($"\n\t\t\tIDatabaseContext databaseContext = new DatabaseContext(");
			lines.Append( "\n\t\t\t\t$\"Data Source={databaseFile}\" ");
			lines.Append($"\n\t\t\t\t, \"TestDb\"");
			lines.Append($"\n\t\t\t\t, \"TestSchema\"");
			lines.Append( "\n\t\t\t\t, $\"{path}/sql/sqlite/ALL.sql\"");
			lines.Append($"\n\t\t\t);");
			lines.Append($"\n\t\t\tAssert.NotNull(databaseContext);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - manager");
			lines.Append($"\n\t\t\tIManageDatabase databaseManager = new DatabaseManager(databaseContext);");
			lines.Append($"\n\t\t\tAssert.NotNull(databaseManager);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - create tables");
			lines.Append($"\n\t\t\tdatabaseManager.CreateDatabase();");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - repository");
			lines.Append($"\n\t\t\tvar {entityName.ToLowerFirstChar()}Repository = new {entityName}Repository(databaseManager, new {entityName}RepositorySql(), logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entityName.ToLowerFirstChar()}Repository);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// faker");
			lines.Append($"\n\t\t\tBrashActionResult<{entityName}> result = null;");
			lines.Append($"\n\t\t\tvar {entityName.ToLowerFirstChar()}Faker = new {entityName}Faker(databaseManager, logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entityName.ToLowerFirstChar()}Faker);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// create");
			lines.Append($"\n\t\t\tvar {entityName.ToLowerFirstChar()}CreateModel = {entityName.ToLowerFirstChar()}Faker.GetOne();");
			lines.Append($"\n\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Create({entityName.ToLowerFirstChar()}CreateModel);");
			lines.Append($"\n\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n\t\t\tAssert.True(result.Model.{entityName}Id > 0);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// use model with id");
			lines.Append($"\n\t\t\t{entityName.ToLowerFirstChar()}CreateModel = result.Model;");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// update");
			lines.Append($"\n\t\t\tvar {entityName.ToLowerFirstChar()}UpdateModel = {entityName.ToLowerFirstChar()}Faker.GetOne();");
			lines.Append($"\n\t\t\t{entityName.ToLowerFirstChar()}UpdateModel.{entityName}Id = {entityName.ToLowerFirstChar()}CreateModel.{entityName}Id;");
			lines.Append($"\n\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Update({entityName.ToLowerFirstChar()}UpdateModel);");
			lines.Append($"\n\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// delete");
			lines.Append($"\n\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Delete({entityName.ToLowerFirstChar()}CreateModel);");
			lines.Append($"\n\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// fetch"); 
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - make fakes");
			lines.Append($"\n\t\t\tvar fakes = {entityName.ToLowerFirstChar()}Faker.GetMany(10);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - add fakes to database");
			lines.Append($"\n\t\t\tList<int?> ids = new List<int?>();");
			lines.Append($"\n\t\t\tforeach (var f in fakes)");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Create(f);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n\t\t\t\tAssert.True(result.Model.{entityName}Id >= 0);");
			lines.Append($"\n\t\t\t\tids.Add(result.Model.{entityName}Id);");
			lines.Append( "\n\t\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - get fakes from database");
			lines.Append($"\n\t\t\tforeach(var id in ids)");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\tvar model = new {entityName}()"); 
			lines.Append( "\n\t\t\t\t{");
			lines.Append($"\n\t\t\t\t\t{entityName}Id = id");
			lines.Append( "\n\t\t\t\t};");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Fetch(model);");
			lines.Append($"\n\t\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n\t\t\t\tAssert.True(result.Model.{entityName}Id >= 0);");
			lines.Append( "\n\t\t\t}");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append( "\n\t}");
			lines.Append( "\n}");

			return lines.ToString();
		}

		public string TplCsRepoXtestAskGuid(
			string domain
			, string entityName
			, string idPattern
			, string projectPath
			)
        {
            StringBuilder lines = new StringBuilder();

			lines.Append($"using System;");
			lines.Append($"\nusing System.Collections.Generic;");
			lines.Append($"\nusing System.Reflection;");
			lines.Append($"\nusing Xunit;");
			lines.Append($"\nusing Serilog;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing Brash.Infrastructure.Sqlite;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Repository;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.RepositorySql;");
			lines.Append($"\nusing {domain}.Infrastructure.Test.Sqlite.Faker;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Infrastructure.Test.Sqlite.Repository");
			lines.Append( "\n{");
			lines.Append($"\n\tpublic class {entityName}RepositoryTest");
			lines.Append( "\n\t{");
			lines.Append($"\n\t\tpublic string GetDatabase(string path, MethodBase methodBase)");
			lines.Append( "\n\t\t{");
			lines.Append( "\n\t\t\tstring dbName = $\"{methodBase.ReflectedType.Name}_{methodBase.Name}\";"); 
			lines.Append( "\n\t\t\tstring databaseFile = $\"{path}/{dbName}.sqlite\";");
			lines.Append($"\n\t\t\tSystem.IO.File.Delete(databaseFile);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\treturn databaseFile;");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\tpublic static ILogger GetLogger(string filename)");
			lines.Append( "\n\t\t{");
			lines.Append( "\n\t\t\treturn new LoggerConfiguration()");
            lines.Append( "\n\t\t\t\t.MinimumLevel.Verbose()");
            lines.Append( "\n\t\t\t\t.WriteTo.File($\"{filename}\", rollingInterval: RollingInterval.Day)");
            lines.Append( "\n\t\t\t\t.CreateLogger();");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\t[Fact]");
			lines.Append($"\n\t\tpublic void CreateUpdateDeleteFetch()");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\t// file system");
			lines.Append($"\n\t\t\tvar path = \"{projectPath}\";");
			lines.Append($"\n\t\t\tvar project = \"{domain}\";");
			lines.Append( "\n\t\t\tvar outputPath = $\"{path}/{project}.Infrastructure.Test/TestOutput/\";");
			lines.Append( "\n\t\t\tvar databaseFile = GetDatabase(outputPath, MethodBase.GetCurrentMethod());");
			lines.Append($"\n\t\t\t");

			lines.Append($"\n\t\t\t// logger");
			lines.Append( "\n\t\t\tILogger logger = GetLogger($\"{outputPath}/{MethodBase.GetCurrentMethod().ReflectedType.Name}_{MethodBase.GetCurrentMethod().Name}.log\");");
			lines.Append($"\n\t\t\t");

			lines.Append($"\n\t\t\t// database setup");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - context");
			lines.Append($"\n\t\t\tIDatabaseContext databaseContext = new DatabaseContext(");
			lines.Append( "\n\t\t\t\t$\"Data Source={databaseFile}\" ");
			lines.Append($"\n\t\t\t\t, \"TestDb\"");
			lines.Append($"\n\t\t\t\t, \"TestSchema\"");
			lines.Append( "\n\t\t\t\t, $\"{path}/sql/sqlite/ALL.sql\"");
			lines.Append($"\n\t\t\t);");
			lines.Append($"\n\t\t\tAssert.NotNull(databaseContext);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - manager");
			lines.Append($"\n\t\t\tIManageDatabase databaseManager = new DatabaseManager(databaseContext);");
			lines.Append($"\n\t\t\tAssert.NotNull(databaseManager);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - create tables");
			lines.Append($"\n\t\t\tdatabaseManager.CreateDatabase();");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - repository");
			lines.Append($"\n\t\t\tvar {entityName.ToLowerFirstChar()}Repository = new {entityName}Repository(databaseManager, new {entityName}RepositorySql(), logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entityName.ToLowerFirstChar()}Repository);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// faker");
			lines.Append($"\n\t\t\tBrashActionResult<{entityName}> result = null;");
			lines.Append($"\n\t\t\tvar {entityName.ToLowerFirstChar()}Faker = new {entityName}Faker(databaseManager, logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entityName.ToLowerFirstChar()}Faker);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// create");
			lines.Append($"\n\t\t\tvar {entityName.ToLowerFirstChar()}CreateModel = {entityName.ToLowerFirstChar()}Faker.GetOne();");
			lines.Append($"\n\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Create({entityName.ToLowerFirstChar()}CreateModel);");
			lines.Append($"\n\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n\t\t\tAssert.False(string.IsNullOrWhiteSpace(result.Model.{entityName}Guid));");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// use model with guid");
			lines.Append($"\n\t\t\t{entityName.ToLowerFirstChar()}CreateModel = result.Model;");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// update");
			lines.Append($"\n\t\t\tvar {entityName.ToLowerFirstChar()}UpdateModel = {entityName.ToLowerFirstChar()}Faker.GetOne();");
			lines.Append($"\n\t\t\t{entityName.ToLowerFirstChar()}UpdateModel.{entityName}Guid = {entityName.ToLowerFirstChar()}CreateModel.{entityName}Guid;");
			lines.Append($"\n\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Update({entityName.ToLowerFirstChar()}UpdateModel);");
			lines.Append($"\n\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// delete");
			lines.Append($"\n\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Delete({entityName.ToLowerFirstChar()}CreateModel);");
			lines.Append($"\n\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// fetch"); 
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - make fakes");
			lines.Append($"\n\t\t\tvar fakes = {entityName.ToLowerFirstChar()}Faker.GetMany(10);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - add fakes to database");
			lines.Append($"\n\t\t\tList<string> guids = new List<string>();");
			lines.Append($"\n\t\t\tforeach (var f in fakes)");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Create(f);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n\t\t\t\tAssert.False(string.IsNullOrWhiteSpace(result.Model.{entityName}Guid));");
			lines.Append($"\n\t\t\t\tguids.Add(result.Model.{entityName}Guid);");
			lines.Append( "\n\t\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - get fakes from database");
			lines.Append($"\n\t\t\tforeach(var guid in guids)");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\tvar model = new {entityName}()"); 
			lines.Append( "\n\t\t\t\t{");
			lines.Append($"\n\t\t\t\t\t{entityName}Guid = guid");
			lines.Append( "\n\t\t\t\t};");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Fetch(model);");
			lines.Append($"\n\t\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n\t\t\t\tAssert.False(string.IsNullOrWhiteSpace(result.Model.{entityName}Guid));");
			lines.Append( "\n\t\t\t}");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append( "\n\t}");
			lines.Append( "\n}");

			return lines.ToString();
		}

		public string TplCsRepoXtestAskVersion(
			string domain
			, string entityName
			, string idPattern
			, string projectPath
			)
        {
            StringBuilder lines = new StringBuilder();

			lines.Append($"using System;");
			lines.Append($"\nusing System.Collections.Generic;");
			lines.Append($"\nusing System.Reflection;");
			lines.Append($"\nusing Xunit;");
			lines.Append($"\nusing Serilog;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing Brash.Infrastructure.Sqlite;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Repository;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.RepositorySql;");
			lines.Append($"\nusing {domain}.Infrastructure.Test.Sqlite.Faker;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Infrastructure.Test.Sqlite.Repository");
			lines.Append( "\n{");
			lines.Append($"\n\tpublic class {entityName}RepositoryTest");
			lines.Append( "\n\t{");
			lines.Append($"\n\t\tpublic string GetDatabase(string path, MethodBase methodBase)");
			lines.Append( "\n\t\t{");
			lines.Append( "\n\t\t\tstring dbName = $\"{methodBase.ReflectedType.Name}_{methodBase.Name}\";"); 
			lines.Append( "\n\t\t\tstring databaseFile = $\"{path}/{dbName}.sqlite\";");
			lines.Append($"\n\t\t\tSystem.IO.File.Delete(databaseFile);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\treturn databaseFile;");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\tpublic static ILogger GetLogger(string filename)");
			lines.Append( "\n\t\t{");
			lines.Append( "\n\t\t\treturn new LoggerConfiguration()");
            lines.Append( "\n\t\t\t\t.MinimumLevel.Verbose()");
            lines.Append( "\n\t\t\t\t.WriteTo.File($\"{filename}\", rollingInterval: RollingInterval.Day)");
            lines.Append( "\n\t\t\t\t.CreateLogger();");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\t[Fact]");
			lines.Append($"\n\t\tpublic void CreateUpdateDeleteFetch()");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\t// file system");
			lines.Append($"\n\t\t\tvar path = \"{projectPath}\";");
			lines.Append($"\n\t\t\tvar project = \"{domain}\";");
			lines.Append( "\n\t\t\tvar outputPath = $\"{path}/{project}.Infrastructure.Test/TestOutput/\";");
			lines.Append( "\n\t\t\tvar databaseFile = GetDatabase(outputPath, MethodBase.GetCurrentMethod());");
			lines.Append($"\n\t\t\t");

			lines.Append($"\n\t\t\t// logger");
			lines.Append( "\n\t\t\tILogger logger = GetLogger($\"{outputPath}/{MethodBase.GetCurrentMethod().ReflectedType.Name}_{MethodBase.GetCurrentMethod().Name}.log\");");
			lines.Append($"\n\t\t\t");

			lines.Append($"\n\t\t\t// database setup");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - context");
			lines.Append($"\n\t\t\tIDatabaseContext databaseContext = new DatabaseContext(");
			lines.Append( "\n\t\t\t\t$\"Data Source={databaseFile}\" ");
			lines.Append($"\n\t\t\t\t, \"TestDb\"");
			lines.Append($"\n\t\t\t\t, \"TestSchema\"");
			lines.Append( "\n\t\t\t\t, $\"{path}/sql/sqlite/ALL.sql\"");
			lines.Append($"\n\t\t\t);");
			lines.Append($"\n\t\t\tAssert.NotNull(databaseContext);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - manager");
			lines.Append($"\n\t\t\tIManageDatabase databaseManager = new DatabaseManager(databaseContext);");
			lines.Append($"\n\t\t\tAssert.NotNull(databaseManager);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - create tables");
			lines.Append($"\n\t\t\tdatabaseManager.CreateDatabase();");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - repository");
			lines.Append($"\n\t\t\tvar {entityName.ToLowerFirstChar()}Repository = new {entityName}Repository(databaseManager, new {entityName}RepositorySql(), logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entityName.ToLowerFirstChar()}Repository);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// faker");
			lines.Append($"\n\t\t\tBrashActionResult<{entityName}> result = null;");
			lines.Append($"\n\t\t\tvar {entityName.ToLowerFirstChar()}Faker = new {entityName}Faker(databaseManager, logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entityName.ToLowerFirstChar()}Faker);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// create");
			lines.Append($"\n\t\t\tvar {entityName.ToLowerFirstChar()}CreateModel = {entityName.ToLowerFirstChar()}Faker.GetOne();");
			lines.Append($"\n\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Create({entityName.ToLowerFirstChar()}CreateModel);");
			lines.Append($"\n\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS);");
			lines.Append($"\n\t\t\tAssert.False(string.IsNullOrWhiteSpace(result.Model.{entityName}Guid));");
			lines.Append($"\n\t\t\t\tAssert.True(result.Model.{entityName}RecordVersion > 0);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// use model with guid");
			lines.Append($"\n\t\t\t{entityName.ToLowerFirstChar()}CreateModel = result.Model;");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// update");
			lines.Append($"\n\t\t\tvar {entityName.ToLowerFirstChar()}UpdateModel = {entityName.ToLowerFirstChar()}Faker.GetOne();");
			lines.Append($"\n\t\t\t{entityName.ToLowerFirstChar()}UpdateModel.{entityName}Guid = {entityName.ToLowerFirstChar()}CreateModel.{entityName}Guid;");
			lines.Append($"\n\t\t\t{entityName.ToLowerFirstChar()}UpdateModel.{entityName}RecordVersion = {entityName.ToLowerFirstChar()}CreateModel.{entityName}RecordVersion;");
			lines.Append($"\n\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Update({entityName.ToLowerFirstChar()}UpdateModel);");
			lines.Append($"\n\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// delete");
			lines.Append($"\n\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Delete({entityName.ToLowerFirstChar()}CreateModel);");
			lines.Append($"\n\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// fetch"); 
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - make fakes");
			lines.Append($"\n\t\t\tvar fakes = {entityName.ToLowerFirstChar()}Faker.GetMany(10);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - add fakes to database");
			lines.Append($"\n\t\t\tList<string> guids = new List<string>();");
			lines.Append($"\n\t\t\tforeach (var f in fakes)");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Create(f);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS);");
			lines.Append($"\n\t\t\t\tAssert.False(string.IsNullOrWhiteSpace(result.Model.{entityName}Guid));");
			lines.Append($"\n\t\t\t\tAssert.True(result.Model.{entityName}RecordVersion > 0);");
			lines.Append($"\n\t\t\t\tguids.Add(result.Model.{entityName}Guid);");
			lines.Append( "\n\t\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - get fakes from database");
			lines.Append($"\n\t\t\tforeach(var guid in guids)");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\tvar model = new {entityName}()"); 
			lines.Append( "\n\t\t\t\t{");
			lines.Append($"\n\t\t\t\t\t{entityName}Guid = guid");
			lines.Append($"\n\t\t\t\t\t, {entityName}RecordVersion = 1");
			lines.Append( "\n\t\t\t\t};");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t\tresult = {entityName.ToLowerFirstChar()}Repository.Fetch(model);");
			lines.Append($"\n\t\t\t\tAssert.True(result.Status == BrashActionStatus.SUCCESS, result.Message);");
			lines.Append($"\n\t\t\t\tAssert.False(string.IsNullOrWhiteSpace(result.Model.{entityName}Guid));");
			lines.Append($"\n\t\t\t\tAssert.True(result.Model.{entityName}RecordVersion > 0);");
			lines.Append( "\n\t\t\t}");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append( "\n\t}");
			lines.Append( "\n}");

			return lines.ToString();
		}


		public string MakeServiceTestFilePath(Structure entity)
		{
			return System.IO.Path.Combine(_pathServiceTestDirectory, entity.Name + "ServiceTest.cs");
		}

		private void MakeServiceTestFileCs(Structure parent, Structure entity)
		{
			string fileNamePath = MakeServiceTestFilePath(entity);
			StringBuilder lines = new StringBuilder();

			lines.Append( TplCsServiceXtest(
				_domainStructure.Domain
				, parent
				, entity
				, _pathProject
			));

			System.IO.File.WriteAllText( fileNamePath, lines.ToString());
		}

		public string TplCsServiceXtest(
			string domain
			, Structure parent
			, Structure entity
			, string basePath
			)
        {
            string template = null;

			switch(entity.IdPattern ?? Global.IDPATTERN_ASKID)
			{
				case Global.IDPATTERN_ASKGUID:
					template = TplCsServiceXtestAskGuid(domain, parent, entity, basePath);
					break;
				case Global.IDPATTERN_ASKVERSION:
					template = TplCsServiceXtestAskVersion(domain, parent, entity, basePath);
					break;
				case Global.IDPATTERN_ASKID:
				default:
					template = TplCsServiceXtestAskId(domain, parent, entity, basePath);
					break;
			}

            return template;
        }

		public string TplCsServiceXtestAskId(
			string domain
			, Structure parent
			, Structure entity
			, string projectPath
			)
        {
            StringBuilder lines = new StringBuilder();

			lines.Append($"\nusing System.Collections.Generic;");
			lines.Append($"\nusing System.Reflection;");
			lines.Append($"\nusing Xunit;");
			lines.Append($"\nusing Serilog;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing Brash.Infrastructure.Sqlite;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Repository;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.RepositorySql;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Service;");
			lines.Append($"\nusing {domain}.Infrastructure.Test.Sqlite.Faker;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Infrastructure.Test.Sqlite.Service");
			lines.Append( "\n{");
			lines.Append($"\n\tpublic class {entity.Name}ServiceTest");
			lines.Append( "\n\t{");
			lines.Append($"\n\t\tpublic string GetDatabase(string path, MethodBase methodBase)");
			lines.Append( "\n\t\t{");
			lines.Append( "\n\t\t\tstring dbName = $\"{methodBase.ReflectedType.Name}_{methodBase.Name}\";"); 
			lines.Append( "\n\t\t\tstring databaseFile = $\"{path}/{dbName}.sqlite\";");
			lines.Append($"\n\t\t\tSystem.IO.File.Delete(databaseFile);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\treturn databaseFile;");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\tpublic static ILogger GetLogger(string filename)");
			lines.Append( "\n\t\t{");
			lines.Append( "\n\t\t\treturn new LoggerConfiguration()");
            lines.Append( "\n\t\t\t\t.MinimumLevel.Verbose()");
            lines.Append( "\n\t\t\t\t.WriteTo.File($\"{filename}\", rollingInterval: RollingInterval.Day)");
            lines.Append( "\n\t\t\t\t.CreateLogger();");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\t[Fact]");
			lines.Append($"\n\t\tpublic void CreateUpdateDeleteFetch()");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\t// file system");
			lines.Append($"\n\t\t\tvar path = \"{projectPath}\";");
			lines.Append($"\n\t\t\tvar project = \"{domain}\";");
			lines.Append( "\n\t\t\tvar outputPath = $\"{path}/{project}.Infrastructure.Test/TestOutput/\";");
			lines.Append( "\n\t\t\tvar databaseFile = GetDatabase(outputPath, MethodBase.GetCurrentMethod());");
			lines.Append($"\n\t\t\t");

			lines.Append($"\n\t\t\t// logger");
			lines.Append( "\n\t\t\tILogger logger = GetLogger($\"{outputPath}/{MethodBase.GetCurrentMethod()}.log\");");
			lines.Append($"\n\t\t\t");

			lines.Append($"\n\t\t\t// database setup");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - context");
			lines.Append($"\n\t\t\tIDatabaseContext databaseContext = new DatabaseContext(");
			lines.Append( "\n\t\t\t\t$\"Data Source={databaseFile}\" ");
			lines.Append($"\n\t\t\t\t, \"TestDb\"");
			lines.Append($"\n\t\t\t\t, \"TestSchema\"");
			lines.Append( "\n\t\t\t\t, $\"{path}/sql/sqlite/ALL.sql\"");
			lines.Append($"\n\t\t\t);");
			lines.Append($"\n\t\t\tAssert.NotNull(databaseContext);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - manager");
			lines.Append($"\n\t\t\tIManageDatabase databaseManager = new DatabaseManager(databaseContext);");
			lines.Append($"\n\t\t\tAssert.NotNull(databaseManager);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - create tables");
			lines.Append($"\n\t\t\tdatabaseManager.CreateDatabase();");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - repository");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}Repository = new {entity.Name}Repository(databaseManager, new {entity.Name}RepositorySql(), logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entity.Name.ToLowerFirstChar()}Repository);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - service");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}Service = new {entity.Name}Service({entity.Name.ToLowerFirstChar()}Repository, logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entity.Name.ToLowerFirstChar()}Service);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// faker");
			lines.Append($"\n\t\t\tBrashActionResult<{entity.Name}> serviceResult = null;");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}Faker = new {entity.Name}Faker(databaseManager, logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entity.Name.ToLowerFirstChar()}Faker);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// create");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}CreateModel = {entity.Name.ToLowerFirstChar()}Faker.GetOne();");
			lines.Append($"\n\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Create({entity.Name.ToLowerFirstChar()}CreateModel);");
			lines.Append($"\n\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n\t\t\tAssert.True(serviceResult.Model.{entity.Name}Id > 0);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// use model with id");
			lines.Append($"\n\t\t\t{entity.Name.ToLowerFirstChar()}CreateModel = serviceResult.Model;");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// update");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}UpdateModel = {entity.Name.ToLowerFirstChar()}Faker.GetOne();");
			lines.Append($"\n\t\t\t{entity.Name.ToLowerFirstChar()}UpdateModel.{entity.Name}Id = {entity.Name.ToLowerFirstChar()}CreateModel.{entity.Name}Id;");
			lines.Append($"\n\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Update({entity.Name.ToLowerFirstChar()}UpdateModel);");
			lines.Append($"\n\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// delete");
			lines.Append($"\n\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Delete({entity.Name.ToLowerFirstChar()}CreateModel);");
			lines.Append($"\n\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// fetch"); 
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - make fakes");
			lines.Append($"\n\t\t\tvar fakes = {entity.Name.ToLowerFirstChar()}Faker.GetMany(10);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - add fakes to database");
			lines.Append($"\n\t\t\tList<int?> ids = new List<int?>();");
			lines.Append($"\n\t\t\tforeach (var f in fakes)");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Create(f);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n\t\t\t\tAssert.True(serviceResult.Model.{entity.Name}Id >= 0);");
			lines.Append($"\n\t\t\t\tids.Add(serviceResult.Model.{entity.Name}Id);");
			lines.Append( "\n\t\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - get fakes from database");
			lines.Append($"\n\t\t\tforeach(var id in ids)");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\tvar model = new {entity.Name}()"); 
			lines.Append( "\n\t\t\t\t{");
			lines.Append($"\n\t\t\t\t\t{entity.Name}Id = id");
			lines.Append( "\n\t\t\t\t};");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Fetch(model);");
			lines.Append($"\n\t\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n\t\t\t\tAssert.True(serviceResult.Model.{entity.Name}Id >= 0);");
			lines.Append( "\n\t\t\t}");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append( "\n\t}");
			lines.Append( "\n}");

			return lines.ToString();
		}

		public string TplCsServiceXtestAskGuid(
			string domain
			, Structure parent
			, Structure entity
			, string projectPath
			)
        {
            StringBuilder lines = new StringBuilder();

			lines.Append($"\nusing System.Collections.Generic;");
			lines.Append($"\nusing System.Reflection;");
			lines.Append($"\nusing Xunit;");
			lines.Append($"\nusing Serilog;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing Brash.Infrastructure.Sqlite;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Repository;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.RepositorySql;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Service;");
			lines.Append($"\nusing {domain}.Infrastructure.Test.Sqlite.Faker;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Infrastructure.Test.Sqlite.Service");
			lines.Append( "\n{");
			lines.Append($"\n\tpublic class {entity.Name}ServiceTest");
			lines.Append( "\n\t{");
			lines.Append($"\n\t\tpublic string GetDatabase(string path, MethodBase methodBase)");
			lines.Append( "\n\t\t{");
			lines.Append( "\n\t\t\tstring dbName = $\"{methodBase.ReflectedType.Name}_{methodBase.Name}\";"); 
			lines.Append( "\n\t\t\tstring databaseFile = $\"{path}/{dbName}.sqlite\";");
			lines.Append($"\n\t\t\tSystem.IO.File.Delete(databaseFile);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\treturn databaseFile;");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\tpublic static ILogger GetLogger(string filename)");
			lines.Append( "\n\t\t{");
			lines.Append( "\n\t\t\treturn new LoggerConfiguration()");
            lines.Append( "\n\t\t\t\t.MinimumLevel.Verbose()");
            lines.Append( "\n\t\t\t\t.WriteTo.File($\"{filename}\", rollingInterval: RollingInterval.Day)");
            lines.Append( "\n\t\t\t\t.CreateLogger();");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\t[Fact]");
			lines.Append($"\n\t\tpublic void CreateUpdateDeleteFetch()");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\t// file system");
			lines.Append($"\n\t\t\tvar path = \"{projectPath}\";");
			lines.Append($"\n\t\t\tvar project = \"{domain}\";");
			lines.Append( "\n\t\t\tvar outputPath = $\"{path}/{project}.Infrastructure.Test/TestOutput/\";");
			lines.Append( "\n\t\t\tvar databaseFile = GetDatabase(outputPath, MethodBase.GetCurrentMethod());");
			lines.Append($"\n\t\t\t");

			lines.Append($"\n\t\t\t// logger");
			lines.Append( "\n\t\t\tILogger logger = GetLogger($\"{outputPath}/{MethodBase.GetCurrentMethod()}.log\");");
			lines.Append($"\n\t\t\t");

			lines.Append($"\n\t\t\t// database setup");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - context");
			lines.Append($"\n\t\t\tIDatabaseContext databaseContext = new DatabaseContext(");
			lines.Append( "\n\t\t\t\t$\"Data Source={databaseFile}\" ");
			lines.Append($"\n\t\t\t\t, \"TestDb\"");
			lines.Append($"\n\t\t\t\t, \"TestSchema\"");
			lines.Append( "\n\t\t\t\t, $\"{path}/sql/sqlite/ALL.sql\"");
			lines.Append($"\n\t\t\t);");
			lines.Append($"\n\t\t\tAssert.NotNull(databaseContext);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - manager");
			lines.Append($"\n\t\t\tIManageDatabase databaseManager = new DatabaseManager(databaseContext);");
			lines.Append($"\n\t\t\tAssert.NotNull(databaseManager);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - create tables");
			lines.Append($"\n\t\t\tdatabaseManager.CreateDatabase();");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - repository");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}Repository = new {entity.Name}Repository(databaseManager, new {entity.Name}RepositorySql(), logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entity.Name.ToLowerFirstChar()}Repository);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - service");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}Service = new {entity.Name}Service({entity.Name.ToLowerFirstChar()}Repository, logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entity.Name.ToLowerFirstChar()}Service);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// faker");
			lines.Append($"\n\t\t\tBrashActionResult<{entity.Name}> serviceResult = null;");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}Faker = new {entity.Name}Faker(databaseManager, logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entity.Name.ToLowerFirstChar()}Faker);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// create");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}CreateModel = {entity.Name.ToLowerFirstChar()}Faker.GetOne();");
			lines.Append($"\n\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Create({entity.Name.ToLowerFirstChar()}CreateModel);");
			lines.Append($"\n\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n\t\t\tAssert.False(string.IsNullOrWhiteSpace(serviceResult.Model.{entity.Name}Guid));");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// use model with id");
			lines.Append($"\n\t\t\t{entity.Name.ToLowerFirstChar()}CreateModel = serviceResult.Model;");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// update");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}UpdateModel = {entity.Name.ToLowerFirstChar()}Faker.GetOne();");
			lines.Append($"\n\t\t\t{entity.Name.ToLowerFirstChar()}UpdateModel.{entity.Name}Guid = {entity.Name.ToLowerFirstChar()}CreateModel.{entity.Name}Guid;");
			lines.Append($"\n\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Update({entity.Name.ToLowerFirstChar()}UpdateModel);");
			lines.Append($"\n\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// delete");
			lines.Append($"\n\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Delete({entity.Name.ToLowerFirstChar()}CreateModel);");
			lines.Append($"\n\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// fetch"); 
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - make fakes");
			lines.Append($"\n\t\t\tvar fakes = {entity.Name.ToLowerFirstChar()}Faker.GetMany(10);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - add fakes to database");
			lines.Append($"\n\t\t\tList<int?> ids = new List<int?>();");
			lines.Append($"\n\t\t\tforeach (var f in fakes)");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Create(f);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n\t\t\t\tAssert.False(string.IsNullOrWhiteSpace(serviceResult.Model.{entity.Name}Guid));");
			lines.Append($"\n\t\t\t\tids.Add(serviceResult.Model.{entity.Name}Id);");
			lines.Append( "\n\t\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - get fakes from database");
			lines.Append($"\n\t\t\tforeach(var id in ids)");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\tvar model = new {entity.Name}()"); 
			lines.Append( "\n\t\t\t\t{");
			lines.Append($"\n\t\t\t\t\t{entity.Name}Id = id");
			lines.Append( "\n\t\t\t\t};");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Fetch(model);");
			lines.Append($"\n\t\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n\t\t\t\tAssert.False(string.IsNullOrWhiteSpace(serviceResult.Model.{entity.Name}Guid));");
			lines.Append( "\n\t\t\t}");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append( "\n\t}");
			lines.Append( "\n}");

			return lines.ToString();
		}

		public string TplCsServiceXtestAskVersion(
			string domain
			, Structure parent
			, Structure entity
			, string projectPath
			)
        {
            StringBuilder lines = new StringBuilder();

			lines.Append($"\nusing System.Collections.Generic;");
			lines.Append($"\nusing System.Reflection;");
			lines.Append($"\nusing Xunit;");
			lines.Append($"\nusing Serilog;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing Brash.Infrastructure.Sqlite;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Repository;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.RepositorySql;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Service;");
			lines.Append($"\nusing {domain}.Infrastructure.Test.Sqlite.Faker;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Infrastructure.Test.Sqlite.Service");
			lines.Append( "\n{");
			lines.Append($"\n\tpublic class {entity.Name}ServiceTest");
			lines.Append( "\n\t{");
			lines.Append($"\n\t\tpublic string GetDatabase(string path, MethodBase methodBase)");
			lines.Append( "\n\t\t{");
			lines.Append( "\n\t\t\tstring dbName = $\"{methodBase.ReflectedType.Name}_{methodBase.Name}\";"); 
			lines.Append( "\n\t\t\tstring databaseFile = $\"{path}/{dbName}.sqlite\";");
			lines.Append($"\n\t\t\tSystem.IO.File.Delete(databaseFile);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\treturn databaseFile;");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\tpublic static ILogger GetLogger(string filename)");
			lines.Append( "\n\t\t{");
			lines.Append( "\n\t\t\treturn new LoggerConfiguration()");
            lines.Append( "\n\t\t\t\t.MinimumLevel.Verbose()");
            lines.Append( "\n\t\t\t\t.WriteTo.File($\"{filename}\", rollingInterval: RollingInterval.Day)");
            lines.Append( "\n\t\t\t\t.CreateLogger();");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\t[Fact]");
			lines.Append($"\n\t\tpublic void CreateUpdateDeleteFetch()");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\t// file system");
			lines.Append($"\n\t\t\tvar path = \"{projectPath}\";");
			lines.Append($"\n\t\t\tvar project = \"{domain}\";");
			lines.Append( "\n\t\t\tvar outputPath = $\"{path}/{project}.Infrastructure.Test/TestOutput/\";");
			lines.Append( "\n\t\t\tvar databaseFile = GetDatabase(outputPath, MethodBase.GetCurrentMethod());");
			lines.Append($"\n\t\t\t");

			lines.Append($"\n\t\t\t// logger");
			lines.Append( "\n\t\t\tILogger logger = GetLogger($\"{outputPath}/{MethodBase.GetCurrentMethod()}.log\");");
			lines.Append($"\n\t\t\t");

			lines.Append($"\n\t\t\t// database setup");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - context");
			lines.Append($"\n\t\t\tIDatabaseContext databaseContext = new DatabaseContext(");
			lines.Append( "\n\t\t\t\t$\"Data Source={databaseFile}\" ");
			lines.Append($"\n\t\t\t\t, \"TestDb\"");
			lines.Append($"\n\t\t\t\t, \"TestSchema\"");
			lines.Append( "\n\t\t\t\t, $\"{path}/sql/sqlite/ALL.sql\"");
			lines.Append($"\n\t\t\t);");
			lines.Append($"\n\t\t\tAssert.NotNull(databaseContext);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - manager");
			lines.Append($"\n\t\t\tIManageDatabase databaseManager = new DatabaseManager(databaseContext);");
			lines.Append($"\n\t\t\tAssert.NotNull(databaseManager);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - create tables");
			lines.Append($"\n\t\t\tdatabaseManager.CreateDatabase();");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - repository");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}Repository = new {entity.Name}Repository(databaseManager, new {entity.Name}RepositorySql(), logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entity.Name.ToLowerFirstChar()}Repository);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - service");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}Service = new {entity.Name}Service({entity.Name.ToLowerFirstChar()}Repository, logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entity.Name.ToLowerFirstChar()}Service);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// faker");
			lines.Append($"\n\t\t\tBrashActionResult<{entity.Name}> serviceResult = null;");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}Faker = new {entity.Name}Faker(databaseManager, logger);");
			lines.Append($"\n\t\t\tAssert.NotNull({entity.Name.ToLowerFirstChar()}Faker);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// create");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}CreateModel = {entity.Name.ToLowerFirstChar()}Faker.GetOne();");
			lines.Append($"\n\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Create({entity.Name.ToLowerFirstChar()}CreateModel);");
			lines.Append($"\n\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n\t\t\tAssert.False(string.IsNullOrWhiteSpace(serviceResult.Model.{entity.Name}Guid));");
			lines.Append($"\n\t\t\tAssert.True(serviceResult.Model.{entity.Name}RecordVersion > 0);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// use model with id");
			lines.Append($"\n\t\t\t{entity.Name.ToLowerFirstChar()}CreateModel = serviceResult.Model;");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// update");
			lines.Append($"\n\t\t\tvar {entity.Name.ToLowerFirstChar()}UpdateModel = {entity.Name.ToLowerFirstChar()}Faker.GetOne();");
			lines.Append($"\n\t\t\t{entity.Name.ToLowerFirstChar()}UpdateModel.{entity.Name}Guid = {entity.Name.ToLowerFirstChar()}CreateModel.{entity.Name}Guid;");
			lines.Append($"\n\t\t\t{entity.Name.ToLowerFirstChar()}UpdateModel.{entity.Name}RecordVersion = {entity.Name.ToLowerFirstChar()}CreateModel.{entity.Name}RecordVersion;");
			lines.Append($"\n\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Update({entity.Name.ToLowerFirstChar()}UpdateModel);");
			lines.Append($"\n\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n\t\t\tAssert.True(serviceResult.Model.{entity.Name}RecordVersion > 1);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// delete");
			lines.Append($"\n\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Delete({entity.Name.ToLowerFirstChar()}CreateModel);");
			lines.Append($"\n\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// fetch"); 
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - make fakes");
			lines.Append($"\n\t\t\tvar fakes = {entity.Name.ToLowerFirstChar()}Faker.GetMany(10);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - add fakes to database");
			lines.Append($"\n\t\t\tList<int?> ids = new List<int?>();");
			lines.Append($"\n\t\t\tforeach (var f in fakes)");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Create(f);");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n\t\t\t\tAssert.False(string.IsNullOrWhiteSpace(serviceResult.Model.{entity.Name}Guid));");
			lines.Append($"\n\t\t\t\tAssert.True(serviceResult.Model.{entity.Name}RecordVersion > 0);");
			lines.Append($"\n\t\t\t\tids.Add(serviceResult.Model.{entity.Name}Id);");
			lines.Append( "\n\t\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t// - get fakes from database");
			lines.Append($"\n\t\t\tforeach(var id in ids)");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\tvar model = new {entity.Name}()"); 
			lines.Append( "\n\t\t\t\t{");
			lines.Append($"\n\t\t\t\t\t{entity.Name}Id = id");
			lines.Append( "\n\t\t\t\t};");
			lines.Append($"\n");
			lines.Append($"\n\t\t\t\tserviceResult = {entity.Name.ToLowerFirstChar()}Service.Fetch(model);");
			lines.Append($"\n\t\t\t\tAssert.True(serviceResult.Status == BrashActionStatus.SUCCESS, serviceResult.Message);");
			lines.Append($"\n\t\t\t\tAssert.False(string.IsNullOrWhiteSpace(serviceResult.Model.{entity.Name}Guid));");
			lines.Append($"\n\t\t\t\tAssert.True(serviceResult.Model.{entity.Name}RecordVersion > 0);");
			lines.Append( "\n\t\t\t}");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append( "\n\t}");
			lines.Append( "\n}");

			return lines.ToString();
		}



		public string MakeFakerFilePath(Structure entity)
		{
			return System.IO.Path.Combine(_pathFakerDirectory, entity.Name + "Faker.cs");
		}

		private void MakeFakerFileCs(Structure parent, Structure entity)
		{
			string fileNamePath = MakeFakerFilePath(entity);
			StringBuilder lines = new StringBuilder();

			lines.Append( TplCsFaker(
				_domainStructure.Domain
				, entity.Name
			));

			System.IO.File.WriteAllText( fileNamePath, lines.ToString());
		}

		public string TplCsFaker(
			string domain
			, string entityName
			)
        {
            StringBuilder lines = new StringBuilder();
			lines.Append($"using System;");
			lines.Append($"\nusing System.Collections.Generic;");
			lines.Append($"\nusing System.Linq;");
			lines.Append($"\nusing Serilog;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Repository;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.RepositorySql;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Infrastructure.Test.Sqlite.Faker");
			lines.Append( "\n{");
			lines.Append($"\n\tpublic class {entityName}Faker"); 
			lines.Append( "\n\t{");
			lines.Append($"\n\t\tprivate Bogus.Faker<{entityName}> _faker;");
			lines.Append($"\n\t\tpublic {entityName}Faker(IManageDatabase databaseManager, ILogger logger)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tvar random = new Random();");
			lines.Append($"\n\t\t\tint randomNumber = random.Next();");
			lines.Append($"\n\t\t\tBogus.Randomizer.Seed = new Random(randomNumber);");
			if (_addCounter)
			{
				lines.Append($"\n\t\t\tint counter = 1;");
			}
			
			lines.Append($"\n\t\t\t");

			foreach( var fakerStatement in _fakerStatements)
			{
				lines.Append($"\n\t\t\t");
				lines.Append(fakerStatement);
			}

			lines.Append($"\n\t\t\t_faker = new Bogus.Faker<{entityName}>()");
			lines.Append($"\n\t\t\t\t.StrictMode(false)");
			lines.Append($"\n\t\t\t\t.Rules((f, m) =>");
			lines.Append( "\n\t\t\t\t{");

			foreach( var ruleStatement in _ruleStatements)
			{
				lines.Append($"\n\t\t\t\t\t");
				lines.Append(ruleStatement);
			}

			lines.Append( "\n\t\t\t\t});");
			lines.Append( "\n\t\t}");
			lines.Append($"\n");
			lines.Append($"\n\t\tpublic {entityName} GetOne()");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\treturn _faker.Generate(1).First();");
			lines.Append( "\n\t\t}");
			lines.Append($"\n\t\t");
			lines.Append($"\n\t\tpublic List<{entityName}> GetMany(int count)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\treturn _faker.Generate(count);");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t}");
			lines.Append( "\n}");

            return lines.ToString();
        }

		private void SaveTableIdDataType(Structure entry)
		{
			_tablePrimaryKeyDataType.Add(entry.Name, entry.IdPattern ?? Global.IDPATTERN_ASKID);
		}

		private void AnalyzeStructure(Structure parent, Structure entity)
		{
			_addCounter = false;
			_ruleStatements = new List<string>();
			_fakerStatements = new List<string>();
			_repoStatements = new List<string>();

			switch(entity.IdPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					_ruleStatements.Add($"m.{entity.Name}Id = null;");
					_ruleStatements.Add($"m.{entity.Name}Guid = Guid.NewGuid().ToString();");
					break;
				case Global.IDPATTERN_ASKVERSION:
					_ruleStatements.Add($"m.{entity.Name}Id = null;");
					_ruleStatements.Add($"m.{entity.Name}Guid = Guid.NewGuid().ToString();");
					_ruleStatements.Add($"m.{entity.Name}RecordVersion = 1;");
					_ruleStatements.Add($"m.IsCurrent = true;");
					break;
				case Global.IDPATTERN_ASKID:
				default:
					_ruleStatements.Add($"m.{entity.Name}Id = null;");
					break;
			}

			if (parent != null)
			{
				_fakerStatements.Add($"var parentFaker = new {parent.Name}Faker(databaseManager, logger);");
				_fakerStatements.Add($"var parent = parentFaker.GetOne();");
				_fakerStatements.Add($"var parentRepository = new {parent.Name}Repository(databaseManager, new {parent.Name}RepositorySql(), logger);");
				_fakerStatements.Add($"var parentAddResult = parentRepository.Create(parent);");
				_fakerStatements.Add($"parent = parentAddResult.Model;");
				_fakerStatements.Add($"");

				switch(parent.IdPattern)
				{
					case Global.IDPATTERN_ASKGUID:
						_ruleStatements.Add($"m.{parent.Name}Guid = parent.{parent.Name}Guid;");
						break;
					case Global.IDPATTERN_ASKVERSION:
						_ruleStatements.Add($"m.{parent.Name}Guid = parent.{parent.Name}Guid;");
						_ruleStatements.Add($"m.{parent.Name}RecordVersion = parent.{parent.Name}RecordVersion;");
						break;
					case Global.IDPATTERN_ASKID:
					default:
						_ruleStatements.Add($"m.{parent.Name}Id = parent.{parent.Name}Id;");
						break;
				}
			}

			if (entity.AdditionalPatterns != null)
			{
				foreach( string pattern in entity.AdditionalPatterns)
				{
					switch(pattern)
					{
						case Global.ADDITIONALPATTERN_CHOICE:
							_addCounter = true;
							_ruleStatements.Add("m.ChoiceName = f.Lorem.Sentence(3);");
							_ruleStatements.Add($"m.OrderNo = counter++;");
							_ruleStatements.Add($"m.IsDisabled = false;");
							break;
						default:
							string msg = $"Additional Pattern: {pattern} not found.  Entity {entity.Name} has an error.";
							_logger.Warning(msg);
							throw new ArgumentException(msg);
					}
				}
			}

			ProcessFields(entity);
			ProcessReferences(entity);
			ProcessTrackingPattern(entity);

		}

		private void ProcessFields(Structure entity)
		{
			if (entity.Fields != null)
			{
				foreach( var field in entity.Fields)
				{
					AddField( field);
				}
			}
		}

		private void AddField(Field field)
		{
			if (string.IsNullOrWhiteSpace(field.Faker))
			{
				switch(field.Type)
				{
					case "D":
						_ruleStatements.Add($"m.{field.Name} = f.Date.Past();");
						break;
					case "N":
						_ruleStatements.Add($"m.{field.Name} = f.Random.Decimal();");
						break;
					case "B":
						throw new NotImplementedException();
					case "F":
						_ruleStatements.Add($"m.{field.Name} = f.Random.Double();");
						break;
					case "I":
						_ruleStatements.Add($"m.{field.Name} = f.Random.Int();");
						break;
					case "S":
						_ruleStatements.Add($"m.{field.Name} = f.Lorem.Sentence(10);");
						break;
					case "C":
						_ruleStatements.Add($"m.{field.Name} = f.Lorem.Paragraphs();");
						break;
					case "G":
						_ruleStatements.Add($"m.{field.Name} = Guid.NewGuid().ToString();");
						break;
					default:
						_ruleStatements.Add($"m.{field.Name} = f.Lorem.Sentence(3);");
						break;
				}
			}
			else
			{
				if (field.Faker.EndsWith(';'))
				{
					_ruleStatements.Add($"m.{field.Name} = {field.Faker}");
				}
				else
				{
					_ruleStatements.Add($"m.{field.Name} = {field.Faker};");
				}
				
			}
		}

		private void ProcessReferences(Structure entity)
		{
			if (entity.References != null)
			{
				foreach( var reference in entity.References)
				{
					AddReferenceFields( reference);
				}
			}
		}

		private void AddReferenceFields( Reference reference)
		{
			_fakerStatements.Add($"var {reference.ColumnName.ToLowerFirstChar()}Faker = new {reference.TableName}Faker(databaseManager, logger);");
			_fakerStatements.Add($"var {reference.ColumnName.ToLowerFirstChar()}Fake = {reference.ColumnName.ToLowerFirstChar()}Faker.GetOne();");
			_fakerStatements.Add($"var {reference.ColumnName.ToLowerFirstChar()}Repository = new {reference.TableName}Repository(databaseManager, new {reference.TableName}RepositorySql(), logger);");
			_fakerStatements.Add($"var {reference.ColumnName.ToLowerFirstChar()}FakeResult = {reference.ColumnName.ToLowerFirstChar()}Repository.Create({reference.ColumnName.ToLowerFirstChar()}Fake);");
			_fakerStatements.Add($"{reference.ColumnName.ToLowerFirstChar()}Fake = {reference.ColumnName.ToLowerFirstChar()}FakeResult.Model;");
			_fakerStatements.Add($"");

			string idPattern = _tablePrimaryKeyDataType[reference.TableName];
			switch(idPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					_ruleStatements.Add($"m.{reference.ColumnName}GuidRef = {reference.ColumnName.ToLowerFirstChar()}Fake.{reference.TableName}Guid;");
					break;
				case Global.IDPATTERN_ASKVERSION:
					_ruleStatements.Add($"m.{reference.ColumnName}GuidRef = {reference.ColumnName.ToLowerFirstChar()}Fake.{reference.TableName}Guid;");
					_ruleStatements.Add($"m.{reference.ColumnName}RecordVersionRef = {reference.ColumnName.ToLowerFirstChar()}Fake.{reference.TableName}RecordVersion;");
					break;
				case Global.IDPATTERN_ASKID:
				default:
					_ruleStatements.Add($"m.{reference.ColumnName}IdRef = {reference.ColumnName.ToLowerFirstChar()}Fake.{reference.TableName}Id;");
					break;
			}
		}

		private void ProcessTrackingPattern(Structure entity)
		{
			if (entity.TrackingPattern != null && !entity.TrackingPattern.Equals(Global.TRACKINGPATTERN_NONE))
			{
				string pattern = entity.TrackingPattern;
				switch(pattern)
				{
					case Global.TRACKINGPATTERN_AUDIT:
						_ruleStatements.Add($"m.CreatedBy = f.Internet.UserName(f.Name.FirstName(0), f.Name.LastName(0));");
						_ruleStatements.Add($"m.CreatedOn = f.Date.Past();");
						_ruleStatements.Add($"m.UpdatedBy = f.Internet.UserName(f.Name.FirstName(0), f.Name.LastName(0));");
						_ruleStatements.Add($"m.UpdatedOn = f.Date.Recent();");
						break;
					case Global.TRACKINGPATTERN_AUDITPRESERVE:
						_ruleStatements.Add($"m.CreatedBy = f.Internet.UserName(f.Name.FirstName(0), f.Name.LastName(0));");
						_ruleStatements.Add($"m.CreatedOn = f.Date.Past();");
						_ruleStatements.Add($"m.UpdatedBy = f.Internet.UserName(f.Name.FirstName(0), f.Name.LastName(0));");
						_ruleStatements.Add($"m.UpdatedOn = f.Date.Recent();");
						_ruleStatements.Add($"m.IsDeleted = false;");
						break;
					case Global.TRACKINGPATTERN_VERSION:
						_ruleStatements.Add($"RecordState = \"Created\"; // Created, Updated, Deleted, Restored");
						_ruleStatements.Add($"m.PerformedBy = f.Internet.UserName(f.Name.FirstName(0), f.Name.LastName(0));");
						_ruleStatements.Add($"m.PerformedOn = f.Date.Past();");
						_ruleStatements.Add("m.PerformedReason = f.PickRandomParam(new string[] { \"Reason 1\", \"Reason 2\", \"Reason 3\"});");
						break;
					default:
						string msg = $"Tracking Pattern: {pattern} not found.  Entity {entity.Name} has an error.";
						_logger.Warning(msg);
						throw new ArgumentException(msg);
				}
			}
		}



		private string BuildDeleteUpdateFlagStatement(Structure parent, Structure entity)
		{
			StringBuilder statement = new StringBuilder();
			bool addComma = false;

			statement.Append($"UPDATE {entity.Name}");
			statement.Append($"\n\t\t\tSET IsDeleted = 1");
			statement.Append($"\n\t\t\tWHERE");
			
			addComma = false;
			foreach( var column in _repoStatements)
			{
				statement.Append($"\n\t\t\t\t");
				if (addComma)
					statement.Append(", ");

				statement.Append($"{column} = @{column}");
				addComma = true;
			}

			statement.Append($"\n\t\t\t;");

			return statement.ToString();
		}

    }
}