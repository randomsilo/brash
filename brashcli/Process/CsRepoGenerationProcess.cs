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
    public class CsRepoGenerationProcess
    {
        private ILogger _logger;
        private CsRepoGeneration _options;
		private string _pathProject;
		private string _pathRepositoryDirectory;
		private string _pathRepositorySqlDirectory;
		private DomainStructure _domainStructure;
		private Dictionary<string,string> _tablePrimaryKeyDataType = new Dictionary<string, string>();
		private List<string> _columns = new List<string>();
		private List<string> _selectColumns = new List<string>();
		private List<string> _primaryKeyColumns = new List<string>();
		
        public CsRepoGenerationProcess(ILogger logger, CsRepoGeneration options)
        {
            _logger = logger;
            _options = options;
			_pathProject = System.IO.Path.GetDirectoryName(_options.FilePath);
        }

        public int Execute()
        {
            int returnCode = 0;

            _logger.Debug("CsRepoGenerationProcess: start");
            do
            {
                try 
                {
					ReadDataJsonFile();
                    MakeInfrastructureDirectories();
					CreateRepoFiles();
                }
                catch(Exception exception)
                {
                    _logger.Error(exception, "CsRepoGenerationProcess, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while(false);
            _logger.Debug("CsRepoGenerationProcess: end");

            return returnCode;
        }

        private void MakeInfrastructureDirectories()
        {
			var directory = _domainStructure.Domain + "." + "Infrastructure";
			_pathRepositoryDirectory = System.IO.Path.Combine(_pathProject, directory, "Sqlite/Repository");
			System.IO.Directory.CreateDirectory(_pathRepositoryDirectory);

			_pathRepositorySqlDirectory = System.IO.Path.Combine(_pathProject, directory, "Sqlite/RepositorySql");
			System.IO.Directory.CreateDirectory(_pathRepositorySqlDirectory);
        }

        private void ReadDataJsonFile()
        {
			string json = System.IO.File.ReadAllText(_options.FilePath);
			_domainStructure = JsonConvert.DeserializeObject<DomainStructure>(json);
			_logger.Information($"Domain: {_domainStructure.Domain}, Structures: {_domainStructure.Structure.Count}");
        }

		private void CreateRepoFiles()
		{
			_logger.Debug("CreateRepoFiles");
			
			foreach( var entry in _domainStructure.Structure)
			{
				MakeRepoFileFile(null, entry);
			}
		}

		private void MakeRepoFileFile( Structure parent, Structure entry)
		{
			_logger.Debug($"{entry.Name}");
			if (parent != null)
				_logger.Debug($"\t Parent: {parent.Name}");

			MakeRepoFileCs(parent, entry);
			MakeRepoSqlFileCs(parent, entry);
			
			if (entry.Children != null && entry.Children.Count > 0)
			{
				foreach( var child in entry.Children)
				{
					MakeRepoFileFile(entry, child);
				}
			}
			
			if (entry.Extensions != null && entry.Extensions.Count > 0)
			{
				foreach( var extension in entry.Extensions)
				{
					MakeRepoFileFile(entry, extension);
				}
			}
		}

		public string MakeRepoFilePath(Structure entry)
		{
			return System.IO.Path.Combine(_pathRepositoryDirectory, entry.Name + "Repository.cs");
		}

		private void MakeRepoFileCs(Structure parent, Structure entry)
		{
			string fileNamePath = MakeRepoFilePath(entry);
			StringBuilder lines = new StringBuilder();

			lines.Append( TplCsRepo(
				_domainStructure.Domain
				, entry.Name
				, entry.IdPattern ?? "AskId"
			));

			System.IO.File.WriteAllText( fileNamePath, lines.ToString());
		}

		public string TplCsRepo(
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
			lines.Append($"\n\t\tpublic {entityName}Repository(IManageDatabase databaseManager, AAskIdRepositorySql repositorySql) : base(databaseManager, repositorySql)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\t");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t}");
			lines.Append( "\n}");

            return lines.ToString();
        }

		public string MakeRepoSqlFilePath(Structure entity)
		{
			return System.IO.Path.Combine(_pathRepositorySqlDirectory, entity.Name + "RepositorySql.cs");
		}

		private void MakeRepoSqlFileCs(Structure parent, Structure entity)
		{
			string fileNamePath = MakeRepoSqlFilePath(entity);
			StringBuilder lines = new StringBuilder();

			SaveTableIdDataType(entity);
			AnalyzeStructure(parent, entity);

			lines.Append( TplCsRepoSql(
				_domainStructure.Domain
				, entity.Name
				, entity.IdPattern ?? Global.IDPATTERN_ASKID
				, BuildCreateStatement(parent, entity)
				, BuildFetchStatement(parent, entity)
				, BuildUpdateStatement(parent, entity)
				, BuildDeleteStatement(parent, entity)
			));

			System.IO.File.WriteAllText( fileNamePath, lines.ToString());
		}

		public string TplCsRepoSql(
			string domain
			, string entityName
			, string idPattern
			, string createStatement
			, string fetchStatement
			, string updateStatement
			, string deleteStatement
			)
        {
            StringBuilder lines = new StringBuilder();

			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Infrastructure.Sqlite.RepositorySql");
			lines.Append( "\n{");
			lines.Append($"\n\tpublic class {entityName}RepositorySql : A{idPattern}RepositorySql");
			lines.Append( "\n\t{");
			lines.Append($"\n\t\tpublic {entityName}RepositorySql() : base()");
			lines.Append( "\n\t\t{");

			lines.Append($"\n\t\t\t_sql[{idPattern}RepositorySqlTypes.CREATE] = @\"");
			lines.Append($"\n\t\t\t{createStatement}");
			lines.Append($"\n\t\t\t\";");
			lines.Append($"\n");

			lines.Append($"\n\t\t\t_sql[{idPattern}RepositorySqlTypes.FETCH] = @\"");
			lines.Append($"\n\t\t\t{fetchStatement}");
			lines.Append($"\n\t\t\t\";");
			lines.Append($"\n");

			lines.Append($"\n\t\t\t_sql[{idPattern}RepositorySqlTypes.UPDATE] = @\"");
			lines.Append($"\n\t\t\t{updateStatement}");
			lines.Append($"\n\t\t\t\";");
			lines.Append($"\n");

			lines.Append($"\n\t\t\t_sql[{idPattern}RepositorySqlTypes.DELETE] = @\"");
			lines.Append($"\n\t\t\t{deleteStatement}");
			lines.Append($"\n\t\t\t\";");
			lines.Append($"\n");
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

			statement.Append($"DELETE FROM {entity.Name}");
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