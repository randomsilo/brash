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
    public class SqlGenerationProcess
    {
        private ILogger _logger;
        private SqlGeneration _options;
		private string _pathProject;
		private string _pathSql;
		private DomainStructure _domainStructure;
        public SqlGenerationProcess(ILogger logger, SqlGeneration options)
        {
            _logger = logger;
            _options = options;
			_pathProject = System.IO.Path.GetDirectoryName(_options.FilePath);
			_pathSql = System.IO.Path.Combine(_pathProject, "sql");
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
					CreateSqlFileForeignKeys();
					CreateSqlFileChoiceData();
					CreateSqlFileAdditionalSql();
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

			MakeCreateTableSql(parent, entry);
			
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

		private void MakeCreateTableSql(Structure parent, Structure entry)
		{
			string fileNamePath = System.IO.Path.Combine(_pathSql, entry.Name + ".sql");
			var tableData = new TableData() 
			{
				Domain = _domainStructure.Domain,
				Parent = parent,
				Entry = entry
			};

			Handlebars.RegisterTemplate("IdPattern", GetIdPattern(entry));
			Handlebars.RegisterTemplate("ParentPattern", GetParentPattern(parent));
			Handlebars.RegisterTemplate("AdditionalPatterns", GetAdditionalPattern(entry));
			Handlebars.RegisterTemplate("Fields", GetFieldsPattern(entry));
			//Handlebars.RegisterTemplate("References", partialSource);
			Handlebars.RegisterTemplate("TrackingPattern", GetTrackingPattern(entry));
			var template = Handlebars.Compile( GetTemplateCreateTableSql());

            var result = template( tableData);

			System.IO.File.WriteAllText( fileNamePath, result);
		}

		private void CreateSqlFileForeignKeys()
		{
			_logger.Debug("CreateSqlFileForeignKeys");
		}

		private void CreateSqlFileChoiceData()
		{
			_logger.Debug("CreateSqlFileChoiceData");
		}

		private void CreateSqlFileAdditionalSql()
		{
			_logger.Debug("CreateSqlFileAdditionalSql");
		}

		private string GetTemplateCreateTableSql()
        {
            return @"
CREATE TABLE {{Domain}}.{{Entry.Name}} (
	{{>IdPattern}}
	{{>ParentPattern}}
    {{>AdditionalPatterns}}
	{{>Fields}}
	{{>TrackingPattern}}
	
);
";
		}

		/*
		{{>References}}
		{{>TrackingPattern}}
		 */
		
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
					template = GetTemplateParentPatternAskId(entry);
					break;
				default:
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