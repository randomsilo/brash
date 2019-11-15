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
    public class CsApiGenerationProcess
    {
        private ILogger _logger;
        private CsApiGeneration _options;
		private string _pathProject;
		private string _pathApiDirectory;
		private string _pathControllerDirectory;
		private DomainStructure _domainStructure;
		private Dictionary<string,string> _tablePrimaryKeyDataType = new Dictionary<string, string>();
		private List<string> _columns = new List<string>();
		private List<string> _selectColumns = new List<string>();
		private List<string> _primaryKeyColumns = new List<string>();
		
        public CsApiGenerationProcess(ILogger logger, CsApiGeneration options)
        {
            _logger = logger;
            _options = options;
			_pathProject = System.IO.Path.GetDirectoryName(_options.FilePath);
        }

        public int Execute()
        {
            int returnCode = 0;

            _logger.Debug("CsApiGenerationProcess: start");
            do
            {
                try 
                {
					ReadDataJsonFile();
                    MakeDirectories();
					CreateApiFiles();
                }
                catch(Exception exception)
                {
                    _logger.Error(exception, "CsApiGenerationProcess, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while(false);
            _logger.Debug("CsApiGenerationProcess: end");

            return returnCode;
        }

        private void MakeDirectories()
        {
			var directory = _domainStructure.Domain + "." + "Api";
			_pathApiDirectory = System.IO.Path.Combine(_pathProject, directory);
			System.IO.Directory.CreateDirectory(_pathApiDirectory);

			_pathControllerDirectory = System.IO.Path.Combine(_pathProject, directory, "Controllers");
			System.IO.Directory.CreateDirectory(_pathControllerDirectory);
        }

        private void ReadDataJsonFile()
        {
			string json = System.IO.File.ReadAllText(_options.FilePath);
			_domainStructure = JsonConvert.DeserializeObject<DomainStructure>(json);
			_logger.Information($"Domain: {_domainStructure.Domain}, Structures: {_domainStructure.Structure.Count}");
        }

		private void CreateApiFiles()
		{
			_logger.Debug("CreateApiFiles");

			// Make Program.cs
			// Make Startup.cs
			// Make BrashConfigure.cs
			
			foreach( var entry in _domainStructure.Structure)
			{
				MakeApiFiles(null, entry);
			}
		}

		private void MakeApiFiles( Structure parent, Structure entry)
		{
			_logger.Debug($"{entry.Name}");
			if (parent != null)
				_logger.Debug($"\t Parent: {parent.Name}");

			MakeControllerFileCs(parent, entry);
			
			if (entry.Children != null && entry.Children.Count > 0)
			{
				foreach( var child in entry.Children)
				{
					MakeApiFiles(entry, child);
				}
			}
			
			if (entry.Extensions != null && entry.Extensions.Count > 0)
			{
				foreach( var extension in entry.Extensions)
				{
					MakeApiFiles(entry, extension);
				}
			}
		}

		public string MakeControllerFilePath(Structure entry)
		{
			return System.IO.Path.Combine(_pathControllerDirectory, entry.Name + "Controller.cs");
		}

		private void MakeControllerFileCs(Structure parent, Structure entry)
		{
			string fileNamePath = MakeControllerFilePath(entry);
			StringBuilder lines = new StringBuilder();

			var idPattern = entry.IdPattern ?? Global.IDPATTERN_ASKID;

			if (idPattern.Equals(Global.IDPATTERN_ASKID))
			{
				lines.Append( TplCsApiAskId(
					_domainStructure.Domain
					, entry
					, idPattern
				));
			}
			else
			{
				throw new NotImplementedException($"{idPattern} has not been implemented.");
			}

			System.IO.File.WriteAllText( fileNamePath, lines.ToString());
		}

		public string ToLowerFirstChar(string input)
        {
            string newString = input;
            if (!String.IsNullOrEmpty(newString) && Char.IsUpper(newString[0]))
                newString = Char.ToLower(newString[0]) + newString.Substring(1);

            return newString;
        }

		public string TplCsApiAskId(
			string domain
			, Structure entity
			, string idPattern
			)
        {
			string entityName = entity.Name;
			string entityInstanceName = ToLowerFirstChar(entity.Name);
            StringBuilder lines = new StringBuilder();

			lines.Append(  $"using System.Collections.Generic;");
			lines.Append($"\nusing Microsoft.AspNetCore.Mvc;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Service;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Api.Controllers");
			lines.Append( "\n{");
			lines.Append( "\n\t[Route(\"api/[controller]\")]");
			lines.Append($"\n\t[ApiController]");
			lines.Append($"\n\tpublic class {entityName}Controller : ControllerBase");
			lines.Append( "\n\t{");
			lines.Append($"\n\t\tprivate {entityName}Service _{entityInstanceName}Service"); lines.Append(" { get; set; }");
			lines.Append( "\n\t\tprivate Serilog.ILogger _logger { get; set; }");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\tpublic {entityName}Controller({entityName}Service {entityInstanceName}Service, Serilog.ILogger logger) : base()");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\t_{entityInstanceName}Service = {entityInstanceName}Service;");
			lines.Append( "\n\t\t\t_logger = logger;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// GET /api/{entityName}/");
			lines.Append( "\n\t\t[HttpGet]");
			lines.Append($"\n\t\tpublic ActionResult<IEnumerable<{entityName}>> Get()");
			lines.Append( "\n\t\t{");
			
			if (entity.AdditionalPatterns.Contains(Global.ADDITIONALPATTERN_CHOICE))
			{
				lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindWhere(\"WHERE IsDisabled = 0 ORDER BY OrderNo \");");
			}
			else
			{
				lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindWhere(\"WHERE 1 = 1\");");
			}
			
			lines.Append( "\n\t\t\tif (queryResult.Status == Brash.Infrastructure.QueryStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(queryResult.Message);");
			lines.Append( "\n\t\t");
			lines.Append( "\n\t\t\treturn queryResult.Models;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// GET api/{entityName}/5");
			lines.Append( "\n\t\t[HttpGet(\"{id}\")]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Get(int id)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tvar model = new {entityName}()");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\t{entityName}Id = id");
			lines.Append( "\n\t\t\t};");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Fetch(model);");
			lines.Append( "\n\t\t\tif (serviceResult.HasError())");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.GetErrorMessage());");
			lines.Append( "\n\t\t\tif (serviceResult.WorkResult.Status == Brash.Infrastructure.ActionStatus.NOT_FOUND)");
			lines.Append( "\n\t\t\t\treturn NotFound(serviceResult.WorkResult.Message);");
			lines.Append( "\n\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.WorkResult.Model;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// POST api/{entityName}");
			lines.Append( "\n\t\t[HttpPost]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Post([FromBody] {entityName} model)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Create(model);");
			lines.Append( "\n\t\t\tif (serviceResult.HasError())");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.GetErrorMessage());");
			lines.Append( "\n\t\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.WorkResult.Model;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// PUT api/{entityName}/6");
			lines.Append( "\n\t\t[HttpPut(\"{id}\")]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Put(int id, [FromBody] {entityName} model)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tmodel.{entityName}Id = id;");
			lines.Append( "\n\t\t\t");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Update(model);");
			lines.Append( "\n\t\t\tif (serviceResult.HasError())");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.GetErrorMessage());");
			lines.Append( "\n\t\t\tif (serviceResult.WorkResult.Status == Brash.Infrastructure.ActionStatus.NOT_FOUND)");
			lines.Append( "\n\t\t\t\treturn NotFound(serviceResult.WorkResult.Message);");
			lines.Append( "\n\t\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.WorkResult.Model;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// DELETE api/{entityName}/6");
			lines.Append( "\n\t\t[HttpDelete(\"{id}\")]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Delete(int id)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tvar model = new {entityName}()");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\t{entityName}Id = id");
			lines.Append( "\n\t\t\t};");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Delete(model);");
			lines.Append( "\n\t\t\tif (serviceResult.HasError())");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.GetErrorMessage());");
			lines.Append( "\n\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.WorkResult.Model;");
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
					_columns.Add($"{field.Name}");
					_selectColumns.Add($"datetime({field.Name},'unixepoch') AS {field.Name}");
					break;
				case "B":
					throw new NotImplementedException();
				case "N":
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
			foreach( var column in _selectColumns)
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

		private string BuildFindStatement(Structure parent, Structure entity)
		{
			StringBuilder statement = new StringBuilder();
			bool addComma = false;

			statement.Append($"SELECT");
			statement.Append($"\n\t\t\t\t--- Columns");

			addComma = false;
			foreach( var column in _selectColumns)
			{
				statement.Append($"\n\t\t\t\t");
				if (addComma)
					statement.Append(", ");

				statement.Append($"{column}");
				addComma = true;
			}

			statement.Append($"\n\t\t\tFROM");
			statement.Append($"\n\t\t\t\t{entity.Name}");
			statement.Append($"\n\t\t\t");

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