using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Serilog;
using HandlebarsDotNet;
using brashcli.Option;
using brashcli.Model;

namespace brashcli.Process
{
    public class SqliteGenerationProcess
    {
        private ILogger _logger;
        private SqliteGeneration _options;
		private string _pathProject;
		private string _pathSql;
		private DomainStructure _domainStructure;
		private Dictionary<string,string> _tablePrimaryKeyDataType;
		private List<string> _tableCreationOrder;
        public SqliteGenerationProcess(ILogger logger, SqliteGeneration options)
        {
            _logger = logger;
            _options = options;
			_pathProject = System.IO.Path.GetDirectoryName(_options.FilePath);
			_pathSql = System.IO.Path.Combine(_pathProject, "sql");
			_tablePrimaryKeyDataType = new Dictionary<string, string>();
			_tableCreationOrder = new List<string>();
        }

        public int Execute()
        {
            int returnCode = 0;

            _logger.Debug("SqlGenerationProcess: start");
            do
            {
                try 
                {
					ReadDataJsonFile();
                    MakeSqlDirectory();
					CreateSqlFileCreateTable();
					MakeCombinedSqlFileScript();
                }
                catch(Exception exception)
                {
                    _logger.Error(exception, "SqlGenerationProcess, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while(false);
            _logger.Debug("SqlGenerationProcess: end");

            return returnCode;
        }

        private void MakeSqlDirectory()
        {
			System.IO.Directory.CreateDirectory(_pathSql);
        }

        private void ReadDataJsonFile()
        {
			string json = System.IO.File.ReadAllText(_options.FilePath);
			_domainStructure = JsonConvert.DeserializeObject<DomainStructure>(json);
			_logger.Information($"Domain: {_domainStructure.Domain}, Structures: {_domainStructure.Structure.Count}");
        }

		private void CreateSqlFileCreateTable()
		{
			_logger.Debug("CreateSqlFileCreateTable");
			
			foreach( var entry in _domainStructure.Structure)
			{
				MakeCreateTableSqlFile(null, entry);
			}
		}

		private void MakeCreateTableSqlFile( Structure parent, Structure entry)
		{
			_logger.Debug($"{entry.Name}");
			if (parent != null)
				_logger.Debug($"\t Parent: {parent.Name}");

			MakeCreateTableSql(parent, entry);
			AddAdditionalSql(entry);
			AddChoicesSql(entry);
			AddTableName(entry);
			
			if (entry.Children != null && entry.Children.Count > 0)
			{
				foreach( var child in entry.Children)
				{
					MakeCreateTableSqlFile(entry, child);
				}
			}
			
			if (entry.Extensions != null && entry.Extensions.Count > 0)
			{
				foreach( var extension in entry.Extensions)
				{
					MakeCreateTableSqlFile(entry, extension);
				}
			}
		}

		public string MakeEntryFilePath(Structure entry)
		{
			return System.IO.Path.Combine(_pathSql, entry.Name + ".sql");
		}

		private void MakeCreateTableSql(Structure parent, Structure entry)
		{
			string fileNamePath = MakeEntryFilePath(entry);
			var tableData = new TableData() 
			{
				Domain = _domainStructure.Domain,
				Parent = parent,
				Entry = entry
			};

			SaveTableIdDataType(entry);
			Handlebars.RegisterTemplate("IdPattern", GetIdPattern(entry));
			Handlebars.RegisterTemplate("ParentPattern", GetParentPattern(parent));
			Handlebars.RegisterTemplate("AdditionalPatterns", GetAdditionalPattern(entry));
			Handlebars.RegisterTemplate("Fields", GetFieldsPattern(entry));
			Handlebars.RegisterTemplate("References", GetReferences(entry));
			Handlebars.RegisterTemplate("TrackingPattern", GetTrackingPattern(entry));
			Handlebars.RegisterTemplate("ForeignKeyPattern", GetForeignKeyPattern(parent));
			Handlebars.RegisterTemplate("ForeignKeyReferencePattern", GetForeignKeyReferencePattern(entry));
			var template = Handlebars.Compile( GetTemplateCreateTableSql());

            var result = template( tableData);

			System.IO.File.WriteAllText( fileNamePath, result);
		}

		private void AddAdditionalSql(Structure entry)
		{
			if (entry.AdditionalSqlStatements != null)
			{
				string fileNamePath = MakeEntryFilePath(entry);
				StringBuilder sqlStatements = new StringBuilder();

				sqlStatements.Append("--- Additional Sql");
				foreach( var sql in entry.AdditionalSqlStatements)
				{
					sqlStatements.Append( "\n" + sql);
				}
				sqlStatements.Append("\n\n");

				System.IO.File.AppendAllText( fileNamePath, sqlStatements.ToString());
			}	
		}

		private void AddChoicesSql(Structure entry)
		{
			if (entry.Choices != null)
			{
				string fileNamePath = MakeEntryFilePath(entry);
				StringBuilder sqlStatements = new StringBuilder();

				sqlStatements.Append("--- Choices");
				foreach( var choice in entry.Choices)
				{
					sqlStatements.Append( $"\nINSERT INTO {entry.Name} (ChoiceName, OrderNo) VALUES ('{choice}', (SELECT IFNULL(MAX(OrderNo),0)+1 FROM {entry.Name}));");
				}
				sqlStatements.Append("\n\n");

				System.IO.File.AppendAllText( fileNamePath, sqlStatements.ToString());
			}	
		}

		private void AddTableName(Structure entry)
		{
			_tableCreationOrder.Add(entry.Name);
		}

		private string GetTemplateCreateTableSql()
        {
            return @"---
--- {{Domain}}.{{Entry.Name}}
---
CREATE TABLE {{Entry.Name}} (
	{{>IdPattern}}
	{{>ParentPattern}}
    {{>AdditionalPatterns}}
	{{>Fields}}
	{{>References}}
	{{>TrackingPattern}}
	{{>ForeignKeyPattern}}
	{{>ForeignKeyReferencePattern}}

);
---

";
		}

		private void SaveTableIdDataType(Structure entry)
		{
			_tablePrimaryKeyDataType.Add(entry.Name, entry.IdPattern ?? Global.IDPATTERN_ASKID);
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

		private string GetForeignKeyPattern(Structure entry)
		{
			string template = "";
			if (entry == null)
				return template;

			switch(entry.IdPattern)
			{
				case Global.IDPATTERN_ASKGUID:
					template = GetTemplateForeignKeyPatternAskGuid(entry);
					break;
				case Global.IDPATTERN_ASKVERSION:
					template = GetTemplateForeignKeyPatternAskVersion(entry);
					break;
				case Global.IDPATTERN_ASKID:
				default:
					template = GetTemplateForeignKeyPatternAskId(entry);
					break;
			}

			return template;
		}

		private string GetForeignKeyReferencePattern(Structure entry)
		{
			StringBuilder template = new StringBuilder();

			if (entry.References != null)
			{
				foreach( var reference in entry.References)
				{
					template.Append( GetTemplateForeignKeyReference(reference, _tablePrimaryKeyDataType[entry.Name]));
				}
			}

			return template.ToString();
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

		private void MakeCombinedSqlFileScript()
		{
			var fileNamePath = System.IO.Path.Combine(_pathSql, "combine.sh");
			StringBuilder cmd = new StringBuilder();

			cmd.Append("cat ");
			foreach(var file in _tableCreationOrder)
			{
				cmd.Append($"\\\n{file}.sql ");
			}
			cmd.Append("> ALL.sql");

			System.IO.File.WriteAllText( fileNamePath, cmd.ToString());
		}

    }
}