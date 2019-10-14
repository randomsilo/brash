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
		private List<string> _columns = new List<string>();
		private List<string> _selectColumns = new List<string>();
		private List<string> _primaryKeyColumns = new List<string>();
		
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
			lines.Append($"\n\t\t\t// reference other fakers");
			lines.Append($"\n\t\t\t");
			lines.Append($"\n\t\t\t_faker = new Bogus.Faker<{entityName}>()");
			lines.Append($"\n\t\t\t\t.StrictMode(false)");
			lines.Append($"\n\t\t\t\t.Rules((f, m) =>");
			lines.Append( "\n\t\t\t\t{");

			lines.Append($"\n\t\t\t\t");
			lines.Append($"\n\t\t\t\t\t// idPattern");
			lines.Append($"\n\t\t\t\t\t// parentIdPattern");
			lines.Append($"\n\t\t\t\t\t// fields");
			lines.Append($"\n\t\t\t\t\t// choice");
			lines.Append($"\n\t\t\t\t\t// references");
			lines.Append($"\n\t\t\t\t\t// tracking");
			lines.Append($"\n\t\t\t\t");

			lines.Append( "\n\t\t\t\t});");
			lines.Append( "\n\t\t\t}");
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
			_columns = new List<string>();
			_selectColumns = new List<string>();
			_primaryKeyColumns = new List<string>();

			switch(entity.IdPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					_columns.Add($"{entity.Name}Guid");
					_selectColumns.Add($"{entity.Name}Guid");
					_primaryKeyColumns.Add($"{entity.Name}Guid");
					break;
				case Global.IDPATTERN_ASKVERSION:
					_columns.Add($"{entity.Name}Guid");
					_selectColumns.Add($"{entity.Name}Guid");
					_primaryKeyColumns.Add($"{entity.Name}Guid");

					_columns.Add($"{entity.Name}RecordVersion");
					_selectColumns.Add($"{entity.Name}RecordVersion");
					_primaryKeyColumns.Add($"{entity.Name}RecordVersion");

					_columns.Add($"IsCurrent");
					_selectColumns.Add($"IsCurrent");
					break;
				case Global.IDPATTERN_ASKID:
				default:
					_columns.Add($"{entity.Name}Id");
					_selectColumns.Add($"{entity.Name}Id");
					_primaryKeyColumns.Add($"{entity.Name}Id");
					break;
			}

			if (parent != null)
			{
				switch(parent.IdPattern)
				{
					case Global.IDPATTERN_ASKGUID:
						_columns.Add($"{parent.Name}Guid");
						_selectColumns.Add($"{parent.Name}Guid");
						break;
					case Global.IDPATTERN_ASKVERSION:
						_columns.Add($"{parent.Name}Guid");
						_selectColumns.Add($"{parent.Name}Guid");

						_columns.Add($"{parent.Name}RecordVersion");
						_selectColumns.Add($"{parent.Name}RecordVersion");
						break;
					case Global.IDPATTERN_ASKID:
					default:
						_columns.Add($"{parent.Name}Id");
						_selectColumns.Add($"{parent.Name}Id");
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
							_columns.Add($"ChoiceName");
							_selectColumns.Add($"ChoiceName");

							_columns.Add($"OrderNo");
							_selectColumns.Add($"OrderNo");

							_columns.Add($"IsDisabled");
							_selectColumns.Add($"IsDisabled");

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
			switch(field.Type)
			{
				case "D":
				case "N":
					_columns.Add($"{field.Name}");
					_selectColumns.Add($"datetime({field.Name},'unixepoch') AS {field.Name}");
					break;
				case "B":
					throw new NotImplementedException();
				case "F":
				case "I":
				case "S":
				case "C":
				case "G":
				default:
					_columns.Add($"{field.Name}");
					_selectColumns.Add($"{field.Name}");
					break;
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
			string idPattern = _tablePrimaryKeyDataType[reference.TableName];
			switch(idPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					_columns.Add($"{reference.ColumnName}GuidRef");
					_selectColumns.Add($"{reference.ColumnName}GuidRef");
					break;
				case Global.IDPATTERN_ASKVERSION:
					_columns.Add($"{reference.ColumnName}GuidRef");
					_selectColumns.Add($"{reference.ColumnName}GuidRef");

					_columns.Add($"{reference.ColumnName}RecordVersionRef");
					_selectColumns.Add($"{reference.ColumnName}RecordVersionRef");
					break;
				case Global.IDPATTERN_ASKID:
				default:
					_columns.Add($"{reference.ColumnName}IdRef");
					_selectColumns.Add($"{reference.ColumnName}IdRef");
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
						_columns.Add($"CreatedBy");
						_selectColumns.Add($"CreatedBy");

						_columns.Add($"CreatedOn");
						_selectColumns.Add($"datetime(CreatedOn,'unixepoch') AS CreatedOn");

						_columns.Add($"UpdatedBy");
						_selectColumns.Add($"UpdatedBy");

						_columns.Add($"UpdatedOn");
						_selectColumns.Add($"datetime(UpdatedOn,'unixepoch') AS UpdatedOn");
						break;
					case Global.TRACKINGPATTERN_AUDITPRESERVE:
						_columns.Add($"CreatedBy");
						_selectColumns.Add($"CreatedBy");

						_columns.Add($"CreatedOn");
						_selectColumns.Add($"datetime(CreatedOn,'unixepoch') AS CreatedOn");

						_columns.Add($"UpdatedBy");
						_selectColumns.Add($"UpdatedBy");

						_columns.Add($"UpdatedOn");
						_selectColumns.Add($"datetime(UpdatedOn,'unixepoch') AS UpdatedOn");

						_columns.Add($"IsDeleted");
						_selectColumns.Add($"IsDeleted");
						break;
					case Global.TRACKINGPATTERN_VERSION:
						_columns.Add($"RecordState");
						_selectColumns.Add($"RecordState");

						_columns.Add($"PerformedBy");
						_selectColumns.Add($"PerformedBy");

						_columns.Add($"PerformedOn");
						_selectColumns.Add($"datetime(PerformedOn,'unixepoch') AS PerformedOn");

						_columns.Add($"PerformedReason");
						_selectColumns.Add($"PerformedReason");
						break;
					default:
						string msg = $"Tracking Pattern: {pattern} not found.  Entity {entity.Name} has an error.";
						_logger.Warning(msg);
						throw new ArgumentException(msg);
				}
			}
		}


		private string BuildCreateStatement(Structure parent, Structure entity)
		{
			StringBuilder statement = new StringBuilder();
			bool addComma = false;

			statement.Append($"INSERT INTO {entity.Name} (");
			statement.Append($"\n\t\t\t\t--- Columns");

			addComma = false;
			foreach( var column in _columns)
			{
				statement.Append($"\n\t\t\t\t");
				if (addComma)
					statement.Append(", ");

				statement.Append($"{column}");
				addComma = true;
			}

			statement.Append($"\n\t\t\t) VALUES (");
			
			statement.Append($"\n\t\t\t\t--- Values");

			addComma = false;
			foreach( var column in _columns)
			{
				statement.Append($"\n\t\t\t\t");
				if (addComma)
					statement.Append(", ");

				statement.Append($"@{column}");
				addComma = true;
			}

			statement.Append($"\n\t\t\t);");
			statement.Append($"\n\t\t\tSELECT last_insert_rowid();");

			return statement.ToString();
		}

		private string BuildFetchStatement(Structure parent, Structure entity)
		{
			StringBuilder statement = new StringBuilder();
			bool addComma = false;

			statement.Append($"SELECT");
			statement.Append($"\n\t\t\t\t--- Columns");

			addComma = false;
			foreach( var column in _columns)
			{
				statement.Append($"\n\t\t\t\t");
				if (addComma)
					statement.Append(", ");

				statement.Append($"{column}");
				addComma = true;
			}

			statement.Append($"\n\t\t\tFROM");
			statement.Append($"\n\t\t\t\t{entity.Name}");
			statement.Append($"\n\t\t\tWHERE");
			
			addComma = false;
			foreach( var column in _primaryKeyColumns)
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

		private string BuildUpdateStatement(Structure parent, Structure entity)
		{
			StringBuilder statement = new StringBuilder();
			bool addComma = false;

			statement.Append($"UPDATE {entity.Name}");
			statement.Append($"\n\t\t\tSET");

			addComma = false;
			foreach( var column in _columns)
			{
				if (_primaryKeyColumns.Contains(column))
					continue;

				statement.Append($"\n\t\t\t\t");
				if (addComma)
					statement.Append(", ");

				statement.Append($"{column} = @{column}");
				addComma = true;
			}

			statement.Append($"\n\t\t\tWHERE");
			
			addComma = false;
			foreach( var column in _primaryKeyColumns)
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

		private string BuildDeleteStatement(Structure parent, Structure entity)
		{
			if (entity.TrackingPattern != null && entity.TrackingPattern.Equals(Global.TRACKINGPATTERN_AUDITPRESERVE))
			{
				return BuildDeleteUpdateFlagStatement(parent, entity);
			}
			else
			{
				return BuildDeleteStatementActual(parent, entity);
			}
		}

		private string BuildDeleteStatementActual(Structure parent, Structure entity)
		{
			StringBuilder statement = new StringBuilder();
			bool addComma = false;

			statement.Append($"DELETE {entity.Name}");
			statement.Append($"\n\t\t\tWHERE");
			
			addComma = false;
			foreach( var column in _primaryKeyColumns)
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

		private string BuildDeleteUpdateFlagStatement(Structure parent, Structure entity)
		{
			StringBuilder statement = new StringBuilder();
			bool addComma = false;

			statement.Append($"UPDATE {entity.Name}");
			statement.Append($"\n\t\t\tSET IsDeleted = 1");
			statement.Append($"\n\t\t\tWHERE");
			
			addComma = false;
			foreach( var column in _primaryKeyColumns)
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