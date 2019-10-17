using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Serilog;
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
		private DomainStructure _domainStructure;
		private Dictionary<string,string> _tablePrimaryKeyDataType = new Dictionary<string, string>();
		private List<string> _ruleStatements = new List<string>();
		private List<string> _fakerStatements = new List<string>();
		private List<string> _repoStatements = new List<string>();
		
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
        }

        private void ReadDataJsonFile()
        {
			string json = System.IO.File.ReadAllText(_options.FilePath);
			_domainStructure = JsonConvert.DeserializeObject<DomainStructure>(json);
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

		private void MakeFiles( Structure parent, Structure entry)
		{
			_logger.Debug($"{entry.Name}");
			if (parent != null)
				_logger.Debug($"\t Parent: {parent.Name}");

			MakeFakerFileCs(parent, entry);
			//MakeRepoFileCs(parent, entry);
			
			if (entry.Children != null && entry.Children.Count > 0)
			{
				foreach( var child in entry.Children)
				{
					MakeFiles(entry, child);
				}
			}
			
			if (entry.Extensions != null && entry.Extensions.Count > 0)
			{
				foreach( var extension in entry.Extensions)
				{
					MakeFiles(entry, extension);
				}
			}
		}

		public string MakeRepoFilePath(Structure entry)
		{
			return System.IO.Path.Combine(_pathFakerDirectory, entry.Name + "Repository.cs");
		}

		private void MakeRepoFileCs(Structure parent, Structure entry)
		{
			string fileNamePath = MakeRepoFilePath(entry);
			StringBuilder lines = new StringBuilder();

			lines.Append( TplCsXtest(
				_domainStructure.Domain
				, entry.Name
				, entry.IdPattern ?? "AskId"
			));

			System.IO.File.WriteAllText( fileNamePath, lines.ToString());
		}

		public string TplCsXtest(
			string domain
			, string entityName
			, string idPattern
			)
        {
            StringBuilder lines = new StringBuilder();

			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing Brash.Infrastructure.Sqlite;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Infrastructure.Sqlite.Repository");
			lines.Append( "\n{");
			lines.Append($"\n\tpublic class {entityName}Repository : {idPattern}Repository<{entityName}>");
			lines.Append( "\n\t{");
			lines.Append($"\n\t\tpublic {entityName}Repository(IManageDatabase databaseManager) : base(databaseManager)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\t");
			lines.Append( "\n\t\t}");
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

			SaveTableIdDataType(entity);
			AnalyzeStructure(parent, entity);

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
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Infrastructure.Test.Sqlite.Faker");
			lines.Append( "\n{");
			lines.Append($"\n\tpublic class {entityName}Faker"); 
			lines.Append( "\n\t{");
			lines.Append($"\n\t\tprivate Bogus.Faker<{entityName}> _faker;");
			lines.Append($"\n\t\tpublic {entityName}Faker()");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tvar random = new Random();");
			lines.Append($"\n\t\t\tint randomNumber = random.Next();");
			lines.Append($"\n\t\t\tBogus.Randomizer.Seed = new Random(randomNumber);");
			lines.Append($"\n\t\t\tint counter = 1;");
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
			_ruleStatements = new List<string>();
			_fakerStatements = new List<string>();
			_repoStatements = new List<string>();

			switch(entity.IdPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					_ruleStatements.Add($"m.{entity.Name}Guid = new Guid();");
					break;
				case Global.IDPATTERN_ASKVERSION:
					_ruleStatements.Add($"m.{entity.Name}Guid = new Guid();");
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
				_fakerStatements.Add($"var parentFaker = new {parent.Name}Faker();");
				_fakerStatements.Add($"var parent = parentFaker.GetOne();");
				_fakerStatements.Add($"// add repo");
				_fakerStatements.Add($"// add parent");
				_fakerStatements.Add($"// fetch parent for id");
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
						_ruleStatements.Add($"m.{field.Name} = new Guid();");
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
			_fakerStatements.Add($"var {reference.ColumnName}Faker = new {reference.TableName}Faker();");
			_fakerStatements.Add($"var {reference.ColumnName}Fake = {reference.ColumnName}Faker.GetOne();");
			_fakerStatements.Add($"// add repo");
			_fakerStatements.Add($"// add model");
			_fakerStatements.Add($"// fetch model for id");
			_fakerStatements.Add($"");

			string idPattern = _tablePrimaryKeyDataType[reference.TableName];
			switch(idPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					_ruleStatements.Add($"m.{reference.ColumnName}GuidRef = {reference.ColumnName}Fake.{reference.TableName}Guid;");
					break;
				case Global.IDPATTERN_ASKVERSION:
					_ruleStatements.Add($"m.{reference.ColumnName}GuidRef = {reference.ColumnName}Fake.{reference.TableName}Guid;");
					_ruleStatements.Add($"m.{reference.ColumnName}RecordVersionRef = {reference.ColumnName}Fake.{reference.TableName}RecordVersion;");
					break;
				case Global.IDPATTERN_ASKID:
				default:
					_ruleStatements.Add($"m.{reference.ColumnName}IdRef = {reference.ColumnName}Fake.{reference.TableName}Id;");
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