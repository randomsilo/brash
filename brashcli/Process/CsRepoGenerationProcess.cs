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
		StringBuilder _fields = new StringBuilder();
		StringBuilder _interfaceImplementations = new StringBuilder();
		List<string> _interfaces = new List<string>();
		
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
			_pathRepositoryDirectory = System.IO.Path.Combine(_pathProject, directory, "Repository");
			System.IO.Directory.CreateDirectory(_pathRepositoryDirectory);

			_pathRepositorySqlDirectory = System.IO.Path.Combine(_pathProject, directory, "RepositorySql");
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

			lines.Append($"\nusing System;");
			lines.Append($"\nusing System.Collections;");
			lines.Append($"\nusing System.Collections.Generic;");
			lines.Append($"\nusing System.Linq;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing Brash.Infrastructure.Sqlite;");
			lines.Append($"\nusing {domain}.Model;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Infrastructure.Repository");
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

		public string MakeRepoSqlFilePath(Structure entry)
		{
			return System.IO.Path.Combine(_pathRepositorySqlDirectory, entry.Name + "RepositorySql.cs");
		}

		private void MakeRepoSqlFileCs(Structure parent, Structure entry)
		{
			string fileNamePath = MakeRepoSqlFilePath(entry);
			StringBuilder lines = new StringBuilder();

			//SaveTableIdDataType(entry);
			//AnalyzeStructure(parent, entry);

			lines.Append( TplCsRepoSql(
				_domainStructure.Domain
				, entry.Name
				, entry.IdPattern ?? "AskId"
				, ""
				, ""
				, ""
				, ""
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

			lines.Append($"\nusing System;");
			lines.Append($"\nusing System.Collections;");
			lines.Append($"\nusing System.Collections.Generic;");
			lines.Append($"\nusing System.Linq;");
			lines.Append($"\nusing Brash.Model;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing Brash.Infrastructure.Sqlite;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.RepositorySql");
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

		private void AnalyzeStructure(Structure parent, Structure entry)
		{
			// clear contents
			_interfaces = new List<string>();
			_interfaceImplementations = new StringBuilder();
			_fields = new StringBuilder();

			StringBuilder template = new StringBuilder();

			_interfaceImplementations.Append("\n\n\t\t// Interface Implementations");

			ProcessIdPattern( entry);
			ProcessParentPattern( parent, entry);
			ProcessAdditionalPatterns( entry);
			ProcessFields( entry);
			ProcessReferences( entry);	
			ProcessTrackingPattern( entry);

			// TODO Add extension holders
			// TODO Add children holders
		}

		private void ProcessIdPattern(Structure entry)
		{
			_fields.Append("\t\t// IdPattern");
			switch(entry.IdPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					_interfaces.Add("IAskGuid");

					_fields.Append("\n\t\t");
					_fields.Append($"public Guid? {entry.Name}Guid");
					_fields.Append(" { get; set; }");

					_interfaceImplementations.Append("\n\t\tpublic string GetAskGuidPropertyName()");
					_interfaceImplementations.Append("\n\t\t{");
					_interfaceImplementations.Append($"\n\t\t	return \"{entry.Name}Guid\";");
					_interfaceImplementations.Append("\n\t\t}");

					break;
				case Global.IDPATTERN_ASKVERSION:
					_interfaces.Add("IAskVersion");

					_fields.Append("\n\t\t");
					_fields.Append($"public Guid? {entry.Name}Guid");
					_fields.Append(" { get; set; }");

					_fields.Append("\n\t\t");
					_fields.Append($"int? {entry.Name}RecordVersion");
					_fields.Append(" { get; set; }");

					_fields.Append("\n\t\t");
					_fields.Append($"bool? IsCurrent");
					_fields.Append(" { get; set; }");

					_interfaceImplementations.Append("\n\t\tpublic string GetAskVersionPropertyName()");
					_interfaceImplementations.Append("\n\t\t{");
					_interfaceImplementations.Append($"\n\t\t	return \"{entry.Name}Guid\";");
					_interfaceImplementations.Append("\n\t\t}");
					break;
				case Global.IDPATTERN_ASKID:
				default:
					_interfaces.Add("IAskId");

					_fields.Append("\n\t\t");
					_fields.Append($"public int? {entry.Name}Id");
					_fields.Append(" { get; set; }");

					_interfaceImplementations.Append("\n\t\tpublic string GetAskIdPropertyName()");
					_interfaceImplementations.Append("\n\t\t{");
					_interfaceImplementations.Append($"\n\t\t	return \"{entry.Name}Id\";");
					_interfaceImplementations.Append("\n\t\t}");
					break;
			}
		}

		private void ProcessParentPattern(Structure parent, Structure entity)
		{
			if (parent != null)
			{
				_fields.Append("\n\n\t\t// Parent IdPattern");
				switch(parent.IdPattern)
				{
					case Global.IDPATTERN_ASKGUID:
						_interfaces.Add("IAskGuidChild");

						_fields.Append("\n\t\t");
						_fields.Append($"Guid? {parent.Name}Guid");
						_fields.Append(" { get; set; }");


						_interfaceImplementations.Append("\n\t\t");
						_interfaceImplementations.Append("\n\t\tpublic string GetAskGuidParentPropertyName()");
						_interfaceImplementations.Append("\n\t\t{");
						_interfaceImplementations.Append($"\n\t\t	return \"{parent.Name}Guid\";");
						_interfaceImplementations.Append("\n\t\t}");
						break;
					case Global.IDPATTERN_ASKVERSION:
						_interfaces.Add("IAskVersionChild");

						_fields.Append("\n\t\t");
						_fields.Append($"Guid? {parent.Name}Guid");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"int? {parent.Name}RecordVersion");
						_fields.Append(" { get; set; }");

						_interfaceImplementations.Append("\n\t\t");
						_interfaceImplementations.Append("\n\t\tpublic string GetAskVersionParentPropertyName()");
						_interfaceImplementations.Append("\n\t\t{");
						_interfaceImplementations.Append($"\n\t\t	return \"{parent.Name}Guid\";");
						_interfaceImplementations.Append("\n\t\t}");
						break;
					case Global.IDPATTERN_ASKID:
					default:
						_interfaces.Add("IAskIdChild");

						_fields.Append("\n\t\t");
						_fields.Append($"int? {parent.Name}Id");
						_fields.Append(" { get; set; }");

						_interfaceImplementations.Append("\n\t\t");
						_interfaceImplementations.Append("\n\t\tpublic string GetAskIdParentPropertyName()");
						_interfaceImplementations.Append("\n\t\t{");
						_interfaceImplementations.Append($"\n\t\t	return \"{parent.Name}Id\";");
						_interfaceImplementations.Append("\n\t\t}");
						break;
				}
			}
		}

		private void ProcessAdditionalPatterns(Structure entity)
		{
			if (entity.AdditionalPatterns != null)
			{
				_fields.Append("\n\n\t\t// Additional Patterns");
				foreach( string pattern in entity.AdditionalPatterns)
				{
					switch(pattern)
					{
						case Global.ADDITIONALPATTERN_CHOICE:
							_fields.Append("\n\t\t");
							_fields.Append($"public string ChoiceName");
							_fields.Append(" { get; set; }");

							_fields.Append("\n\t\t");
							_fields.Append($"public decimal? OrderNo");
							_fields.Append(" { get; set; }");

							_fields.Append("\n\t\t");
							_fields.Append($"public bool? IsDisabled");
							_fields.Append(" { get; set; }");
							break;
						default:
							string msg = $"Additional Pattern: {pattern} not found.  Entity {entity.Name} has an error.";
							_logger.Warning(msg);
							throw new ArgumentException(msg);
					}
				}
			}
		}

		private void ProcessFields(Structure entity)
		{
			if (entity.Fields != null)
			{
				_fields.Append("\n\n\t\t// Fields");
				foreach( var field in entity.Fields)
				{
					AddField( field);
				}
			}
		}

		private void AddField(Field field)
		{
			_fields.Append("\n\t\tpublic ");
			switch(field.Type)
			{
				case "D":
				case "N":
					_fields.Append("DateTime?");
					break;
				case "F":
					_fields.Append("decimal?");
					break;
				case "I":
					_fields.Append("int?");
					break;
				case "B":
					_fields.Append("byte[]");
					break;
				case "S":
				case "C":
				case "G":
				default:
					_fields.Append("string");
					break;
			}
			_fields.Append($" {field.Name}");
			_fields.Append(" { get; set; }");
		}


		private void ProcessReferences(Structure entity)
		{
			if (entity.References != null)
			{
				_fields.Append("\n\n\t\t// References");
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
					_fields.Append("\n\t\t");
					_fields.Append($"public string? {reference.ColumnName}GuidRef");
					_fields.Append(" { get; set; }");
					
					break;
				case Global.IDPATTERN_ASKVERSION:
					_fields.Append("\n\t\t");
					_fields.Append($"public string? {reference.ColumnName}GuidRef");
					_fields.Append(" { get; set; }");

					_fields.Append("\n\t\t");
					_fields.Append($"public decimal? {reference.ColumnName}RecordVersionRef");
					_fields.Append(" { get; set; }");
					break;
				case Global.IDPATTERN_ASKID:
				default:
					_fields.Append("\n\t\t");
					_fields.Append($"public int? {reference.ColumnName}IdRef");
					_fields.Append(" { get; set; }");
					break;
			}
		}

		private void ProcessTrackingPattern(Structure entity)
		{
			if (entity.TrackingPattern != null && !entity.TrackingPattern.Equals(Global.TRACKINGPATTERN_NONE))
			{
				_fields.Append("\n\n\t\t// Tracking Pattern");
				string pattern = entity.TrackingPattern;
				switch(pattern)
				{
					case Global.TRACKINGPATTERN_AUDIT:
						_fields.Append("\n\t\t");
						_fields.Append($"public string? CreatedBy");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public DateTime? CreatedOn");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public string? UpdatedBy");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public DateTime? UpdatedOn");
						_fields.Append(" { get; set; }");
						break;
					case Global.TRACKINGPATTERN_AUDITPRESERVE:
						_fields.Append("\n\t\t");
						_fields.Append($"public string? CreatedBy");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public DateTime? CreatedOn");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public string? UpdatedBy");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public DateTime? UpdatedOn");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public bool IsDeleted");
						_fields.Append(" { get; set; }");
						break;
					case Global.TRACKINGPATTERN_VERSION:
						_fields.Append("\n\t\t");
						_fields.Append($"public string? RecordState");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public string? PerformedBy");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public DateTime? PerformedOn");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public string? PerformedReason");
						_fields.Append(" { get; set; }");
						break;
					default:
						string msg = $"Tracking Pattern: {pattern} not found.  Entity {entity.Name} has an error.";
						_logger.Warning(msg);
						throw new ArgumentException(msg);
				}
			}
		}

		private string GetInterfaces()
		{
			StringBuilder template = new StringBuilder();

			bool anInterfaceHasBeenAdded = false;
			foreach( var interfaceName in _interfaces)
			{
				if (anInterfaceHasBeenAdded)
					template.Append(", ");

				template.Append(interfaceName);
				anInterfaceHasBeenAdded = true;
			}

			return template.ToString();
		}

		private string GetInterfacesImplementations()
		{
			return _interfaceImplementations.ToString();
		}

		private string GetFields()
		{
			return _fields.ToString();
		}
		
		private string GetIdPattern(Structure entry)
		{
			string template = "";
			switch(entry.IdPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					template = GetTemplateIdPatternAskGuid(entry);
					break;
				case Global.IDPATTERN_ASKVERSION:
					template = GetTemplateIdPatternAskVersion(entry);
					break;
				case Global.IDPATTERN_ASKID:
				default:
					template = GetTemplateIdPatternAskId(entry);
					break;
			}

			return template;
		}

		private string GetParentPattern(Structure entry)
		{
			string template = "";
			if (entry == null)
				return template;

			switch(entry.IdPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					template = GetTemplateParentPatternAskGuid(entry);
					break;
				case Global.IDPATTERN_ASKVERSION:
					template = GetTemplateParentPatternAskVersion(entry);
					break;
				case Global.IDPATTERN_ASKID:
				default:
					template = GetTemplateParentPatternAskId(entry);
					break;
			}

			return template;
		}

	
		private string GetAdditionalPattern(Structure entry)
		{
			StringBuilder template = new StringBuilder();

			if (entry.AdditionalPatterns != null 
				&& entry.AdditionalPatterns.Contains(Global.ADDITIONALPATTERN_CHOICE))
			{
				template.Append(GetTemplateAdditionalPatternChoice(entry));
			}

			return template.ToString();
		}

		private string GetFieldsPattern(Structure entry)
		{
			StringBuilder template = new StringBuilder();

			if (entry.Fields != null)
			{
				foreach( var field in entry.Fields)
				{
					template.Append( GetTemplateField(field));
				}
			}

			return template.ToString();
		}

		private string GetTemplateField(Field field)
		{
			string template = "";

			switch(field.Type)
			{
				case "D":
				case "N":
					template = $"\n\t, {field.Name} NUMERIC";
					break;
				case "F":
					template = $"\n\t, {field.Name} REAL";
					break;
				case "I":
					template = $"\n\t, {field.Name} INTEGER";
					break;
				case "B":
					template = $"\n\t, {field.Name} BLOB";
					break;
				case "S":
				case "C":
				case "G":
				default:
					template = $"\n\t, {field.Name} TEXT";
					break;
			}

			return template;
		}

		private string GetTrackingPattern(Structure entry)
		{
			string template = "";

			if (entry.TrackingPattern != null)
			{
				switch(entry.TrackingPattern)
				{
					case Global.TRACKINGPATTERN_AUDIT:
						template = GetTemplateTrackingPatternAudit(entry);
						break;
					case Global.TRACKINGPATTERN_AUDITPRESERVE:
						template = GetTemplateTrackingPatternAuditPreserve(entry);
						break;
					case Global.TRACKINGPATTERN_VERSION:
						template = GetTemplateTrackingPatternVersion(entry);
						break;
					case Global.TRACKINGPATTERN_NONE:
					default:
						break;
				}
			}

			return template;
		}

		private string GetReferences(Structure entry)
		{
			StringBuilder template = new StringBuilder();

			if (entry.References != null)
			{
				foreach( var reference in entry.References)
				{
					template.Append( GetTemplateReference(reference, _tablePrimaryKeyDataType[entry.Name]));
				}
			}

			return template.ToString();
		}

		private string GetTemplateReference(Reference reference, string idPattern)
		{
			string template = "";
			
			switch(idPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					template = $"\n\t, {reference.ColumnName}GuidRef TEXT";
					break;
				case Global.IDPATTERN_ASKVERSION:
					template = $"\n\t, {reference.ColumnName}GuidRef TEXT";
					template = $"\n\t, {reference.ColumnName}RecordVersionRef INTEGER";
					break;
				case Global.IDPATTERN_ASKID:
				default:
					template = $"\n\t, {reference.ColumnName}IdRef INTEGER";
					break;
			}
			
			return template;
		}
		private string GetTemplateIdPatternAskId( Structure entry)
        {
            return $"\t{entry.Name}Id INTEGER PRIMARY KEY AUTOINCREMENT";
		}

		private string GetTemplateIdPatternAskGuid( Structure entry)
        {
            return $"\t{entry.Name}Guid TEXT PRIMARY KEY";
		}

		private string GetTemplateIdPatternAskVersion( Structure entry)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( $"\t{entry.Name}Id INTEGER PRIMARY KEY AUTOINCREMENT");
			sb.Append( $"\n\t, {entry.Name}Guid TEXT UNIQUE");
			sb.Append( $"\n\t, RecordVersion INTEGER");
			sb.Append( $"\n\t, IsCurrent INTEGER");
            return sb.ToString();
		}

		private string GetTemplateParentPatternAskId( Structure entry)
        {
            return $"\n\t, {entry.Name}Id INTEGER";
		}

		private string GetTemplateParentPatternAskGuid( Structure entry)
        {
            return $"\n\t, {entry.Name}Guid TEXT";
		}

		private string GetTemplateParentPatternAskVersion( Structure entry)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( $"\n\t, {entry.Name}Guid TEXT");
			sb.Append( $"\n\t, {entry.Name}RecordVersion INTEGER");
            return sb.ToString();
		}

		private string GetTemplateForeignKeyPatternAskId( Structure entry)
        {
            return $"\n\t, FOREIGN KEY ({entry.Name}Id) REFERENCES {entry.Name}({entry.Name}Id) ON DELETE CASCADE";
		}

		private string GetTemplateForeignKeyPatternAskGuid( Structure entry)
        {
            return $"\n\t, FOREIGN KEY ({entry.Name}Guid) REFERENCES {entry.Name}({entry.Name}Guid) ON DELETE CASCADE";
		}

		private string GetTemplateForeignKeyPatternAskVersion( Structure entry)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( $"\n\t, FOREIGN KEY ({entry.Name}Guid) REFERENCES {entry.Name}({entry.Name}Guid) ON DELETE CASCADE");
            return sb.ToString();
		}

		private string GetTemplateForeignKeyReference(Reference reference, string idPattern)
		{
			string template = "";
			
			switch(idPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					template = GetTemplateForeignKeyReferencePatternAskGuid(reference);
					break;
				case Global.IDPATTERN_ASKVERSION:
					template = GetTemplateForeignKeyReferencePatternAskVersion(reference);
					break;
				case Global.IDPATTERN_ASKID:
				default:
					template = GetTemplateForeignKeyReferencePatternAskId(reference);
					break;
			}
			
			return template;
		}

		private string GetTemplateForeignKeyReferencePatternAskId( Reference reference)
        {
            return $"\n\t, FOREIGN KEY ({reference.ColumnName}IdRef) REFERENCES {reference.TableName}({reference.TableName}Id) ON DELETE SET NULL";
		}

		private string GetTemplateForeignKeyReferencePatternAskGuid( Reference reference)
        {
            return $"\n\t, FOREIGN KEY ({reference.ColumnName}GuidRef) REFERENCES {reference.TableName}({reference.TableName}Guid) ON DELETE SET NULL";
		}

		private string GetTemplateForeignKeyReferencePatternAskVersion( Reference reference)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( $"\n\t, FOREIGN KEY ({reference.ColumnName}GuidRef) REFERENCES {reference.TableName}({reference.TableName}Guid) ON DELETE SET NULL");
            return sb.ToString();
		}

		private string GetTemplateAdditionalPatternChoice( Structure entry)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( $"\n\t, ChoiceName TEXT");
			sb.Append( $"\n\t, OrderNo INTEGER");
			sb.Append( $"\n\t, IsDisabled INTEGER");
            return sb.ToString();
		}

		private string GetTemplateTrackingPatternAudit( Structure entry)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( $"\n\t, CreatedBy TEXT");
			sb.Append( $"\n\t, CreatedOn NUMERIC");
			sb.Append( $"\n\t, UpdatedBy TEXT");
			sb.Append( $"\n\t, UpdatedOn NUMERIC");

            return sb.ToString();
		}

		private string GetTemplateTrackingPatternAuditPreserve( Structure entry)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( GetTemplateTrackingPatternAudit(entry));
			sb.Append( $"\n\t, IsDeleted INTEGER");
			
            return sb.ToString();
		}

		private string GetTemplateTrackingPatternVersion( Structure entry)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( $"\n\t, RecordState TEXT");
			sb.Append( $"\n\t, PerformedBy TEXT");
			sb.Append( $"\n\t, PerformedOn NUMERIC");
			sb.Append( $"\n\t, PerformedReason TEXT");

            return sb.ToString();
		}

    }
}