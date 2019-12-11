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
    public class CsDomainGenerationProcess
    {
        private ILogger _logger;
        private CsDomainGeneration _options;
		private string _pathProject;
		private string _pathDomainModel;
		private string _pathDomainService;
		private DomainStructure _domainStructure;
		private Dictionary<string,string> _tablePrimaryKeyDataType = new Dictionary<string, string>();
		StringBuilder _fields = new StringBuilder();
		StringBuilder _interfaceImplementations = new StringBuilder();
		List<string> _interfaces = new List<string>();
		List<string> _serviceInterfaces = new List<string>();
		
        public CsDomainGenerationProcess(ILogger logger, CsDomainGeneration options)
        {
            _logger = logger;
            _options = options;
			_pathProject = System.IO.Path.GetDirectoryName(_options.FilePath);
        }

        public int Execute()
        {
            int returnCode = 0;

            _logger.Debug("CsDomainGenerationProcess: start");
            do
            {
                try 
                {
					ReadDataJsonFile();
                    MakeDomainDirectories();
					CreateDomainFiles();
                }
                catch(Exception exception)
                {
                    _logger.Error(exception, "CsDomainGenerationProcess, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while(false);
            _logger.Debug("CsDomainGenerationProcess: end");

            return returnCode;
        }

        private void MakeDomainDirectories()
        {
			var domainDirectory = _domainStructure.Domain + "." + "Domain";
			_pathDomainModel = System.IO.Path.Combine(_pathProject, domainDirectory, "Model");
			System.IO.Directory.CreateDirectory(_pathDomainModel);

			_pathDomainService = System.IO.Path.Combine(_pathProject, domainDirectory, "Service");
			System.IO.Directory.CreateDirectory(_pathDomainService);
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

		private void CreateDomainFiles()
		{
			_logger.Debug("CreateDomainModelFiles");
			
			foreach( var entity in _domainStructure.Structure)
			{
				MakeDomainFiles(null, entity);
			}
		}

		private void MakeDomainFiles( Structure parent, Structure entity)
		{
			_logger.Debug($"{entity.Name}");
			if (parent != null)
				_logger.Debug($"\t Parent: {parent.Name}");

			MakeDomainFilesCs(parent, entity);
			
			if (entity.Children != null && entity.Children.Count > 0)
			{
				foreach( var child in entity.Children)
				{
					MakeDomainFiles(entity, child);
				}
			}
			
			if (entity.Extensions != null && entity.Extensions.Count > 0)
			{
				foreach( var extension in entity.Extensions)
				{
					MakeDomainFiles(entity, extension);
				}
			}
		}

		public string MakeEntityFilePath(Structure entity)
		{
			return System.IO.Path.Combine(_pathDomainModel, entity.Name + ".cs");
		}

		public string MakeServiceFilePath(Structure entity)
		{
			return System.IO.Path.Combine(_pathDomainService, "I" + entity.Name + "Service.cs");
		}

		private void MakeDomainFilesCs(Structure parent, Structure entity)
		{
			string domainModelFilePath = MakeEntityFilePath(entity);
			StringBuilder domainModelLines = new StringBuilder();

			SaveTableIdDataType(entity);
			AnalyzeStructure(parent, entity);
			MakeDomainModel(parent, entity);
			MakeDomainService(parent, entity);
		}

		private void MakeDomainModel(Structure parent, Structure entity)
		{
			string domainModelFilePath = MakeEntityFilePath(entity);
			StringBuilder domainModelLines = new StringBuilder();

			domainModelLines.Append( TplCsDomainModel(
				_domainStructure.Domain
				, entity.Name
				, GetInterfaces()
				, GetInterfacesImplementations()
				, GetFields()
				, GetConstructor(entity)
			));

			System.IO.File.WriteAllText( domainModelFilePath, domainModelLines.ToString());
		}

		private void MakeDomainService(Structure parent, Structure entity)
		{
			string domainServiceFilePath = MakeServiceFilePath(entity);
			StringBuilder domainServiceLines = new StringBuilder();

			domainServiceLines.Append( TplCsDomainService(
				_domainStructure.Domain
				, parent
				, entity
				, GetServiceInterfaces()
			));

			System.IO.File.WriteAllText( domainServiceFilePath, domainServiceLines.ToString());
		}

		private string TplCsDomainModel(
			string domain
			, string entityName
			, string interfaces
			, string interfacesImplementations
			, string fields
			, string constructor
			)
        {
            StringBuilder lines = new StringBuilder();

			lines.Append($"\nusing System;");
			lines.Append($"\nusing System.Collections;");
			lines.Append($"\nusing System.Collections.Generic;");
			lines.Append($"\nusing System.Linq;");
			lines.Append($"\nusing Brash.Model;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Domain.Model");
			lines.Append( "\n{");
			lines.Append($"\n\tpublic class {entityName} : {interfaces}");
			lines.Append( "\n\t{");
			lines.Append($"\n");
			lines.Append( constructor);
			lines.Append($"\n");
			lines.Append( fields);
			lines.Append($"\n");
			lines.Append( interfacesImplementations);
			lines.Append($"\n");
			lines.Append( "\n\t}");
			lines.Append( "\n}");

            return lines.ToString();
        }

		private string TplCsDomainService(
			string domain
			, Structure parent
			, Structure entity
			, string interfaces
			)
        {
            StringBuilder lines = new StringBuilder();

			lines.Append($"\nusing System;");
			lines.Append($"\nusing System.Collections;");
			lines.Append($"\nusing System.Collections.Generic;");
			lines.Append($"\nusing System.Linq;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing Brash.Model;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Domain.Service");
			lines.Append( "\n{");
			if (string.IsNullOrWhiteSpace(interfaces)) {
				lines.Append($"\n\tpublic interface I{entity.Name}Service ");
			}
			else
			{
				lines.Append($"\n\tpublic interface I{entity.Name}Service : {interfaces}");
			}
			
			lines.Append( "\n\t{");

			lines.Append($"\n\t\tBrashActionResult<{entity.Name}> Create({entity.Name} model);");
        	lines.Append($"\n\t\tBrashActionResult<{entity.Name}> Fetch({entity.Name} model);");
        	lines.Append($"\n\t\tBrashActionResult<{entity.Name}> Update({entity.Name} model);");
        	lines.Append($"\n\t\tBrashActionResult<{entity.Name}> Delete({entity.Name} model);");
        	lines.Append($"\n\t\tBrashQueryResult<{entity.Name}> FindWhere(string where);");

			lines.Append( "\n\t}");
			lines.Append( "\n}");

            return lines.ToString();
        }

		private void SaveTableIdDataType(Structure entity)
		{
			_tablePrimaryKeyDataType.Add(entity.Name, entity.IdPattern ?? Global.IDPATTERN_ASKID);
		}

		private void AnalyzeStructure(Structure parent, Structure entity)
		{
			// clear contents
			_interfaces = new List<string>();
			_serviceInterfaces = new List<string>();
			_interfaceImplementations = new StringBuilder();
			_fields = new StringBuilder();

			StringBuilder template = new StringBuilder();

			_interfaceImplementations.Append("\n\n\t\t// Interface Implementations");

			ProcessIdPattern( entity);
			ProcessParentPattern( parent, entity);
			ProcessAdditionalPatterns( entity);
			ProcessFields( entity);
			ProcessReferences( entity);	
			ProcessTrackingPattern( entity);

		}

		private void ProcessIdPattern(Structure entity)
		{
			_fields.Append("\t\t// IdPattern");

			_fields.Append("\n\t\t");
			_fields.Append($"public int? {entity.Name}Id");
			_fields.Append(" { get; set; }");

			_interfaceImplementations.Append("\n\t\tpublic string GetIdPropertyName()");
			_interfaceImplementations.Append("\n\t\t{");
			_interfaceImplementations.Append($"\n\t\t	return \"{entity.Name}Id\";");
			_interfaceImplementations.Append("\n\t\t}");

			switch(entity.IdPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					_interfaces.Add("IAskGuid");

					_fields.Append("\n\t\t");
					_fields.Append($"public string {entity.Name}Guid");
					_fields.Append(" { get; set; }");

					_interfaceImplementations.Append("\n\t\tpublic string GetGuidPropertyName()");
					_interfaceImplementations.Append("\n\t\t{");
					_interfaceImplementations.Append($"\n\t\t	return \"{entity.Name}Guid\";");
					_interfaceImplementations.Append("\n\t\t}");

					break;
				case Global.IDPATTERN_ASKVERSION:
					_interfaces.Add("IAskVersion");

					_fields.Append("\n\t\t");
					_fields.Append($"public string {entity.Name}Guid");
					_fields.Append(" { get; set; }");

					_fields.Append("\n\t\t");
					_fields.Append($"public decimal? {entity.Name}RecordVersion");
					_fields.Append(" { get; set; }");

					_fields.Append("\n\t\t");
					_fields.Append($"public bool? IsCurrent");
					_fields.Append(" { get; set; }");

					_interfaceImplementations.Append("\n\t\tpublic string GetGuidPropertyName()");
					_interfaceImplementations.Append("\n\t\t{");
					_interfaceImplementations.Append($"\n\t\t	return \"{entity.Name}Guid\";");
					_interfaceImplementations.Append("\n\t\t}");

					_interfaceImplementations.Append("\n\t\tpublic string GetVersionPropertyName()");
					_interfaceImplementations.Append("\n\t\t{");
					_interfaceImplementations.Append($"\n\t\t	return \"{entity.Name}RecordVersion\";");
					_interfaceImplementations.Append("\n\t\t}");
					break;
				case Global.IDPATTERN_ASKID:
				default:
					_interfaces.Add("IAskId");

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
						_fields.Append($"public string {parent.Name}Guid");
						_fields.Append(" { get; set; }");


						_interfaceImplementations.Append("\n\t\t");
						_interfaceImplementations.Append("\n\t\tpublic string GetParentGuidPropertyName()");
						_interfaceImplementations.Append("\n\t\t{");
						_interfaceImplementations.Append($"\n\t\t	return \"{parent.Name}Guid\";");
						_interfaceImplementations.Append("\n\t\t}");
						break;
					case Global.IDPATTERN_ASKVERSION:
						_interfaces.Add("IAskVersionChild");

						_fields.Append("\n\t\t");
						_fields.Append($"public string {parent.Name}Guid");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public decimal? {parent.Name}RecordVersion");
						_fields.Append(" { get; set; }");

						_interfaceImplementations.Append("\n\t\t");
						_interfaceImplementations.Append("\n\t\tpublic string GetParentGuidPropertyName()");
						_interfaceImplementations.Append("\n\t\t{");
						_interfaceImplementations.Append($"\n\t\t	return \"{parent.Name}Guid\";");
						_interfaceImplementations.Append("\n\t\t}");

						_interfaceImplementations.Append("\n\t\t");
						_interfaceImplementations.Append("\n\t\tpublic string GetParentVersionPropertyName()");
						_interfaceImplementations.Append("\n\t\t{");
						_interfaceImplementations.Append($"\n\t\t	return \"{parent.Name}RecordVersion\";");
						_interfaceImplementations.Append("\n\t\t}");
						break;
					case Global.IDPATTERN_ASKID:
					default:
						_interfaces.Add("IAskIdChild");

						_fields.Append("\n\t\t");
						_fields.Append($"public int? {parent.Name}Id");
						_fields.Append(" { get; set; }");

						_interfaceImplementations.Append("\n\t\t");
						_interfaceImplementations.Append("\n\t\tpublic string GetParentIdPropertyName()");
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
					_fields.Append($"public string {reference.ColumnName}GuidRef");
					_fields.Append(" { get; set; }");
					
					break;
				case Global.IDPATTERN_ASKVERSION:
					_fields.Append("\n\t\t");
					_fields.Append($"public string {reference.ColumnName}GuidRef");
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
						_fields.Append($"public string CreatedBy");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public DateTime? CreatedOn");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public string UpdatedBy");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public DateTime? UpdatedOn");
						_fields.Append(" { get; set; }");
						break;
					case Global.TRACKINGPATTERN_AUDITPRESERVE:
						_fields.Append("\n\t\t");
						_fields.Append($"public string CreatedBy");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public DateTime? CreatedOn");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public string UpdatedBy");
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
						_fields.Append($"public string RecordState");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public string PerformedBy");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public DateTime? PerformedOn");
						_fields.Append(" { get; set; }");

						_fields.Append("\n\t\t");
						_fields.Append($"public string PerformedReason");
						_fields.Append(" { get; set; }");
						break;
					default:
						string msg = $"Tracking Pattern: {pattern} not found.  Entity {entity.Name} has an error.";
						_logger.Warning(msg);
						throw new ArgumentException(msg);
				}
			}
		}

		private string GetConstructor(Structure entity)
		{
			StringBuilder template = new StringBuilder();
			template.Append($"\n\t\tpublic {entity.Name}()");
			template.Append( "\n\t\t{");

			switch(entity.IdPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					template.Append($"\n\t\t\t{entity.Name}Guid = Guid.NewGuid().ToString();");
					break;
				case Global.IDPATTERN_ASKVERSION:
					template.Append($"\n\t\t\t{entity.Name}Guid = Guid.NewGuid().ToString();");
					template.Append($"\n\t\t\t{entity.Name}RecordVersion = 1;");
					template.Append($"\n\t\t\tIsCurrent = true;");
					break;
				case Global.IDPATTERN_ASKID:
				default:
					break;
			}

			template.Append( "\n\t\t}");

			return template.ToString();
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

		private string GetServiceInterfaces()
		{
			StringBuilder template = new StringBuilder();

			bool anInterfaceHasBeenAdded = false;
			foreach( var interfaceName in _serviceInterfaces)
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
		
		private string GetIdPattern(Structure entity)
		{
			string template = "";
			switch(entity.IdPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					template = GetTemplateIdPatternAskGuid(entity);
					break;
				case Global.IDPATTERN_ASKVERSION:
					template = GetTemplateIdPatternAskVersion(entity);
					break;
				case Global.IDPATTERN_ASKID:
				default:
					template = GetTemplateIdPatternAskId(entity);
					break;
			}

			return template;
		}

		private string GetParentPattern(Structure entity)
		{
			string template = "";
			if (entity == null)
				return template;

			switch(entity.IdPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					template = GetTemplateParentPatternAskGuid(entity);
					break;
				case Global.IDPATTERN_ASKVERSION:
					template = GetTemplateParentPatternAskVersion(entity);
					break;
				case Global.IDPATTERN_ASKID:
				default:
					template = GetTemplateParentPatternAskId(entity);
					break;
			}

			return template;
		}

	
		private string GetAdditionalPattern(Structure entity)
		{
			StringBuilder template = new StringBuilder();

			if (entity.AdditionalPatterns != null 
				&& entity.AdditionalPatterns.Contains(Global.ADDITIONALPATTERN_CHOICE))
			{
				template.Append(GetTemplateAdditionalPatternChoice(entity));
			}

			return template.ToString();
		}

		private string GetFieldsPattern(Structure entity)
		{
			StringBuilder template = new StringBuilder();

			if (entity.Fields != null)
			{
				foreach( var field in entity.Fields)
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

		private string GetTrackingPattern(Structure entity)
		{
			string template = "";

			if (entity.TrackingPattern != null)
			{
				switch(entity.TrackingPattern)
				{
					case Global.TRACKINGPATTERN_AUDIT:
						template = GetTemplateTrackingPatternAudit(entity);
						break;
					case Global.TRACKINGPATTERN_AUDITPRESERVE:
						template = GetTemplateTrackingPatternAuditPreserve(entity);
						break;
					case Global.TRACKINGPATTERN_VERSION:
						template = GetTemplateTrackingPatternVersion(entity);
						break;
					case Global.TRACKINGPATTERN_NONE:
					default:
						break;
				}
			}

			return template;
		}

		private string GetReferences(Structure entity)
		{
			StringBuilder template = new StringBuilder();

			if (entity.References != null)
			{
				foreach( var reference in entity.References)
				{
					template.Append( GetTemplateReference(reference, _tablePrimaryKeyDataType[entity.Name]));
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
					template = $"\n\t, {reference.ColumnName}RecordVersionRef REAL";
					break;
				case Global.IDPATTERN_ASKID:
				default:
					template = $"\n\t, {reference.ColumnName}IdRef INTEGER";
					break;
			}
			
			return template;
		}
		private string GetTemplateIdPatternAskId( Structure entity)
        {
            return $"\t{entity.Name}Id INTEGER PRIMARY KEY AUTOINCREMENT";
		}

		private string GetTemplateIdPatternAskGuid( Structure entity)
        {
            return $"\t{entity.Name}Guid TEXT PRIMARY KEY";
		}

		private string GetTemplateIdPatternAskVersion( Structure entity)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( $"\t{entity.Name}Id INTEGER PRIMARY KEY AUTOINCREMENT");
			sb.Append( $"\n\t, {entity.Name}Guid TEXT UNIQUE");
			sb.Append( $"\n\t, RecordVersion REAL");
			sb.Append( $"\n\t, IsCurrent INTEGER");
            return sb.ToString();
		}

		private string GetTemplateParentPatternAskId( Structure entity)
        {
            return $"\n\t, {entity.Name}Id INTEGER";
		}

		private string GetTemplateParentPatternAskGuid( Structure entity)
        {
            return $"\n\t, {entity.Name}Guid TEXT";
		}

		private string GetTemplateParentPatternAskVersion( Structure entity)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( $"\n\t, {entity.Name}Guid TEXT");
			sb.Append( $"\n\t, {entity.Name}RecordVersion REAL");
            return sb.ToString();
		}

		private string GetTemplateForeignKeyPatternAskId( Structure entity)
        {
            return $"\n\t, FOREIGN KEY ({entity.Name}Id) REFERENCES {entity.Name}({entity.Name}Id) ON DELETE CASCADE";
		}

		private string GetTemplateForeignKeyPatternAskGuid( Structure entity)
        {
            return $"\n\t, FOREIGN KEY ({entity.Name}Guid) REFERENCES {entity.Name}({entity.Name}Guid) ON DELETE CASCADE";
		}

		private string GetTemplateForeignKeyPatternAskVersion( Structure entity)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( $"\n\t, FOREIGN KEY ({entity.Name}Guid) REFERENCES {entity.Name}({entity.Name}Guid) ON DELETE CASCADE");
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

		private string GetTemplateAdditionalPatternChoice( Structure entity)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( $"\n\t, ChoiceName TEXT");
			sb.Append( $"\n\t, OrderNo INTEGER");
			sb.Append( $"\n\t, IsDisabled INTEGER");
            return sb.ToString();
		}

		private string GetTemplateTrackingPatternAudit( Structure entity)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( $"\n\t, CreatedBy TEXT");
			sb.Append( $"\n\t, CreatedOn NUMERIC");
			sb.Append( $"\n\t, UpdatedBy TEXT");
			sb.Append( $"\n\t, UpdatedOn NUMERIC");

            return sb.ToString();
		}

		private string GetTemplateTrackingPatternAuditPreserve( Structure entity)
        {
			StringBuilder sb = new StringBuilder();
			sb.Append( GetTemplateTrackingPatternAudit(entity));
			sb.Append( $"\n\t, IsDeleted INTEGER");
			
            return sb.ToString();
		}

		private string GetTemplateTrackingPatternVersion( Structure entity)
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