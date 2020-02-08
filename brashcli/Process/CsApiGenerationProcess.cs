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
		private List<string> _entities = new List<string>();
		
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
			_domainStructure = JsonConvert.DeserializeObject<DomainStructure>(json, new JsonSerializerSettings()
			{
				MissingMemberHandling = MissingMemberHandling.Ignore
			});
			_logger.Information($"Domain: {_domainStructure.Domain}, Structures: {_domainStructure.Structure.Count}");
        }

		private void CreateApiFiles()
		{
			_logger.Debug("CreateApiFiles");

			// Make Program.cs
			System.IO.File.WriteAllText( 
				System.IO.Path.Combine(_pathApiDirectory, "Program.cs")
				, TplCsApiProgram(_domainStructure.Domain));

			// Make Startup.cs
			System.IO.File.WriteAllText( 
				System.IO.Path.Combine(_pathApiDirectory, "Startup.cs")
				, TplCsApiStartup(_domainStructure.Domain));

			// Make launchSettings.json
			System.IO.File.WriteAllText( 
				System.IO.Path.Combine(_pathApiDirectory, "Properties", "launchSettings.json")
				, TplCsApiLaunchSettings(_domainStructure.Domain));			
			
			foreach( var entity in _domainStructure.Structure)
			{
				MakeApiFiles(null, entity);
			}

			// Make BrashConfigure.cs
			System.IO.File.WriteAllText( 
				System.IO.Path.Combine(_pathApiDirectory, "BrashConfigure.cs")
				, TplCsApiBrashConfigure(_domainStructure.Domain, _entities));

			// Make BrashConfigure.cs
			System.IO.File.WriteAllText( 
				System.IO.Path.Combine(_pathApiDirectory, "BrashApiAuth.cs")
				, TplCsApiBrashApiAuth(_domainStructure.Domain));



		}

		private void MakeApiFiles( Structure parent, Structure entity)
		{
			_logger.Debug($"{entity.Name}");

			_entities.Add(entity.Name);

			if (parent != null)
				_logger.Debug($"\t Parent: {parent.Name}");

			MakeControllerFileCs(parent, entity);
			
			if (entity.Children != null && entity.Children.Count > 0)
			{
				foreach( var child in entity.Children)
				{
					MakeApiFiles(entity, child);
				}
			}
			
			if (entity.Extensions != null && entity.Extensions.Count > 0)
			{
				foreach( var extension in entity.Extensions)
				{
					MakeApiFiles(entity, extension);
				}
			}
		}

		public string MakeControllerFilePath(Structure entity)
		{
			return System.IO.Path.Combine(_pathControllerDirectory, entity.Name + "Controller.cs");
		}

		private void MakeControllerFileCs(Structure parent, Structure entity)
		{
			string fileNamePath = MakeControllerFilePath(entity);
			StringBuilder lines = new StringBuilder();

			var idPattern = entity.IdPattern ?? Global.IDPATTERN_ASKID;

			if (idPattern.Equals(Global.IDPATTERN_ASKID))
			{
				lines.Append( TplCsApiAskId(
					_domainStructure.Domain
					, entity
					, parent
				));

				System.IO.File.WriteAllText( fileNamePath, lines.ToString());
			}
			else if (idPattern.Equals(Global.IDPATTERN_ASKGUID))
			{
				lines.Append( TplCsApiAskGuid(
					_domainStructure.Domain
					, entity
					, parent
				));

				System.IO.File.WriteAllText( fileNamePath, lines.ToString());
			}
			else if (idPattern.Equals(Global.IDPATTERN_ASKVERSION))
			{
				lines.Append( TplCsApiAskVersion(
					_domainStructure.Domain
					, entity
					, parent
				));

				System.IO.File.WriteAllText( fileNamePath, lines.ToString());
			}
			else
			{
				_logger.Error($"IdPattern not implemented: {idPattern}");
			}
		}

		public string ToLowerFirstChar(string input)
        {
            string newString = input;
            if (!String.IsNullOrEmpty(newString) && Char.IsUpper(newString[0]))
                newString = Char.ToLower(newString[0]) + newString.Substring(1);

            return newString;
        }

		private string GetFindByParentRoute(
			string domain
			, Structure entity
			, Structure parent
		)
		{
			string idPattern = entity.IdPattern ?? Global.IDPATTERN_ASKID;
			string entityName = entity.Name;
			string entityInstanceName = ToLowerFirstChar(entity.Name);
			StringBuilder lines = new StringBuilder();

			if (parent != null)
			{
				lines.Append($"\n");
				
				switch(parent.IdPattern)
				{
					case Global.IDPATTERN_ASKGUID:
						lines.Append($"\n\t\t// GET /api/{entityName}ByParent/7dd44fed-bf64-42d8-a6ea-04357c73482e");
						lines.Append( "\n\t\t[HttpGet(\"{guid}\")]");
						lines.Append($"\n\t\tpublic ActionResult<IEnumerable<{entityName}>> GetByParent(string guid)");
						lines.Append( "\n\t\t{");
						lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindByParent(guid);");
						lines.Append( "\n\t\t\tif (queryResult.Status == BrashQueryStatus.ERROR)");
						lines.Append( "\n\t\t\t\treturn BadRequest(queryResult.Message);");
						lines.Append( "\n\t\t");
						lines.Append( "\n\t\t\treturn queryResult.Models;");
						lines.Append( "\n\t\t}");
						lines.Append( "\n\t\t");

						break;
					case Global.IDPATTERN_ASKVERSION:
						lines.Append($"\n\t\t// GET /api/{entityName}ByParent/7dd44fed-bf64-42d8-a6ea-04357c73482e/12");
						lines.Append( "\n\t\t[HttpGet(\"{guid}/{version}\")]");
						lines.Append($"\n\t\tpublic ActionResult<IEnumerable<{entityName}>> GetByParent(string guid, decimal version)");
						lines.Append( "\n\t\t{");
						lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindByParent(guid, version);");
						lines.Append( "\n\t\t\tif (queryResult.Status == BrashQueryStatus.ERROR)");
						lines.Append( "\n\t\t\t\treturn BadRequest(queryResult.Message);");
						lines.Append( "\n\t\t");
						lines.Append( "\n\t\t\treturn queryResult.Models;");
						lines.Append( "\n\t\t}");
						lines.Append( "\n\t\t");

						break;
					case Global.IDPATTERN_ASKID:
					default:
						lines.Append($"\n\t\t// GET /api/{entityName}ByParent/4");
						lines.Append( "\n\t\t[HttpGet(\"{id}\")]");
						lines.Append($"\n\t\tpublic ActionResult<IEnumerable<{entityName}>> GetByParent(int id)");
						lines.Append( "\n\t\t{");
						lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindByParent(id);");
						lines.Append( "\n\t\t\tif (queryResult.Status == BrashQueryStatus.ERROR)");
						lines.Append( "\n\t\t\t\treturn BadRequest(queryResult.Message);");
						lines.Append( "\n\t\t");
						lines.Append( "\n\t\t\treturn queryResult.Models;");
						lines.Append( "\n\t\t}");
						lines.Append( "\n\t\t");

						break;
				}
			}

			return lines.ToString();
		}

		public string TplCsApiAskId(
			string domain
			, Structure entity
			, Structure parent
			)
        {
			string idPattern = entity.IdPattern ?? Global.IDPATTERN_ASKID;
			string entityName = entity.Name;
			string entityInstanceName = ToLowerFirstChar(entity.Name);
			StringBuilder lines = new StringBuilder();

			lines.Append(  $"using System.Collections.Generic;");
			lines.Append($"\nusing Microsoft.AspNetCore.Authorization;");
			lines.Append($"\nusing Microsoft.AspNetCore.Mvc;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Service;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Api.Controllers");
			lines.Append( "\n{");
			lines.Append( "\n\t[Authorize]");
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
			
			if (entity.AdditionalPatterns != null && entity.AdditionalPatterns.Contains(Global.ADDITIONALPATTERN_CHOICE))
			{
				lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindWhere(\"WHERE IFNULL(IsDisabled, 0) = 0 ORDER BY OrderNo \");");
			}
			else
			{
				lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindWhere(\"WHERE 1 = 1 ORDER BY 1 \");");
			}
			
			lines.Append( "\n\t\t\tif (queryResult.Status == BrashQueryStatus.ERROR)");
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
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.Message);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.NOT_FOUND)");
			lines.Append( "\n\t\t\t\treturn NotFound(serviceResult.Message);");
			lines.Append( "\n\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.Model;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// POST api/{entityName}");
			lines.Append( "\n\t\t[HttpPost]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Post([FromBody] {entityName} model)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Create(model);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.Message);");
			lines.Append( "\n\t\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.Model;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// PUT api/{entityName}/6");
			lines.Append( "\n\t\t[HttpPut(\"{id}\")]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Put(int id, [FromBody] {entityName} model)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tmodel.{entityName}Id = id;");
			lines.Append( "\n\t\t\t");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Update(model);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.Message);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.NOT_FOUND)");
			lines.Append( "\n\t\t\t\treturn NotFound(serviceResult.Message);");
			lines.Append( "\n\t\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.Model;");
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
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.Message);");
			lines.Append( "\n\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.Model;");
			lines.Append( "\n\t\t}");


			lines.Append( GetFindByParentRoute(domain, entity, parent));


			lines.Append( "\n\t}");
			lines.Append( "\n}");

            return lines.ToString();
        }

		public string TplCsApiAskGuid(
			string domain
			, Structure entity
			, Structure parent
			)
        {
			string idPattern = entity.IdPattern ?? Global.IDPATTERN_ASKID;
			string entityName = entity.Name;
			string entityInstanceName = ToLowerFirstChar(entity.Name);
            StringBuilder lines = new StringBuilder();

			lines.Append(  $"using System.Collections.Generic;");
			lines.Append($"\nusing Microsoft.AspNetCore.Authorization;");
			lines.Append($"\nusing Microsoft.AspNetCore.Mvc;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Service;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Api.Controllers");
			lines.Append( "\n{");
			lines.Append( "\n\t[Authorize]");
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
			
			if (entity.AdditionalPatterns != null && entity.AdditionalPatterns.Contains(Global.ADDITIONALPATTERN_CHOICE))
			{
				lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindWhere(\"WHERE IFNULL(IsDisabled, 0) = 0 ORDER BY OrderNo \");");
			}
			else
			{
				lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindWhere(\"WHERE 1 = 1 ORDER BY 1 \");");
			}
			
			lines.Append( "\n\t\t\tif (queryResult.Status == BrashQueryStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(queryResult.Message);");
			lines.Append( "\n\t\t");
			lines.Append( "\n\t\t\treturn queryResult.Models;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// GET api/{entityName}/7dd44fed-bf64-42d8-a6ea-04357c73482e");
			lines.Append( "\n\t\t[HttpGet(\"{guid}\")]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Get(string guid)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tvar model = new {entityName}()");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\t{entityName}Guid = guid");
			lines.Append( "\n\t\t\t};");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Fetch(model);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.Message);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.NOT_FOUND)");
			lines.Append( "\n\t\t\t\treturn NotFound(serviceResult.Message);");
			lines.Append( "\n\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.Model;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// POST api/{entityName}");
			lines.Append( "\n\t\t[HttpPost]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Post([FromBody] {entityName} model)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Create(model);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.Message);");
			lines.Append( "\n\t\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.Model;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// PUT api/{entityName}/7dd44fed-bf64-42d8-a6ea-04357c73482e");
			lines.Append( "\n\t\t[HttpPut(\"{guid}\")]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Put(string guid, [FromBody] {entityName} model)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tmodel.{entityName}Guid = guid;");
			lines.Append( "\n\t\t\t");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Update(model);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.Message);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.NOT_FOUND)");
			lines.Append( "\n\t\t\t\treturn NotFound(serviceResult.Message);");
			lines.Append( "\n\t\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.Model;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// DELETE api/{entityName}/7dd44fed-bf64-42d8-a6ea-04357c73482e");
			lines.Append( "\n\t\t[HttpDelete(\"{guid}\")]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Delete(string guid)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tvar model = new {entityName}()");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\t{entityName}Guid = guid");
			lines.Append( "\n\t\t\t};");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Delete(model);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.Message);");
			lines.Append( "\n\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.Model;");
			lines.Append( "\n\t\t}");


			lines.Append( GetFindByParentRoute(domain, entity, parent));


			lines.Append( "\n\t}");
			lines.Append( "\n}");

            return lines.ToString();
		}

		public string TplCsApiAskVersion(
			string domain
			, Structure entity
			, Structure parent
			)
        {
			string idPattern = entity.IdPattern ?? Global.IDPATTERN_ASKID;
			string entityName = entity.Name;
			string entityInstanceName = ToLowerFirstChar(entity.Name);
            StringBuilder lines = new StringBuilder();

			lines.Append(  $"using System.Collections.Generic;");
			lines.Append($"\nusing Microsoft.AspNetCore.Authorization;");
			lines.Append($"\nusing Microsoft.AspNetCore.Mvc;");
			lines.Append($"\nusing Brash.Infrastructure;");
			lines.Append($"\nusing {domain}.Domain.Model;");
			lines.Append($"\nusing {domain}.Infrastructure.Sqlite.Service;");
			lines.Append($"\n");
			lines.Append($"\nnamespace {domain}.Api.Controllers");
			lines.Append( "\n{");
			lines.Append( "\n\t[Authorize]");
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

			// IsDeleted
			string IsDeletedClause = "";
			if (entity.TrackingPattern != null && entity.TrackingPattern.Equals(Global.TRACKINGPATTERN_AUDITPRESERVE))
			{
				IsDeletedClause = "AND IsDeleted = 0";
			}
			
			if (idPattern.Equals(Global.IDPATTERN_ASKVERSION))
			{
				lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindWhere(\"WHERE IFNULL(IsCurrent, 0) = 1 {IsDeletedClause} ORDER BY 1 \");");
			}
			else if (entity.AdditionalPatterns != null && entity.AdditionalPatterns.Contains(Global.ADDITIONALPATTERN_CHOICE))
			{
				lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindWhere(\"WHERE IFNULL(IsDisabled, 0) = 0 {IsDeletedClause} ORDER BY OrderNo \");");
			}
			else
			{
				lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindWhere(\"WHERE 1 = 1 {IsDeletedClause} ORDER BY 1 \");");
			}

			
			
			lines.Append( "\n\t\t\tif (queryResult.Status == BrashQueryStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(queryResult.Message);");
			lines.Append( "\n\t\t");
			lines.Append( "\n\t\t\treturn queryResult.Models;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// GET api/{entityName}/7dd44fed-bf64-42d8-a6ea-04357c73482e/1");
			lines.Append( "\n\t\t[HttpGet(\"{guid}/{version}\")]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Get(string guid, decimal version)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tvar model = new {entityName}()");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\t{entityName}Guid = guid");
			lines.Append($"\n\t\t\t\t, {entityName}RecordVersion = version");
			lines.Append( "\n\t\t\t};");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Fetch(model);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.Message);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.NOT_FOUND)");
			lines.Append( "\n\t\t\t\treturn NotFound(serviceResult.Message);");
			lines.Append( "\n\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.Model;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// POST api/{entityName}");
			lines.Append( "\n\t\t[HttpPost]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Post([FromBody] {entityName} model)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Create(model);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.Message);");
			lines.Append( "\n\t\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.Model;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// PUT api/{entityName}/7dd44fed-bf64-42d8-a6ea-04357c73482e/1");
			lines.Append( "\n\t\t[HttpPut(\"{guid}/{version}\")]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Put(string guid, decimal version, [FromBody] {entityName} model)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tmodel.{entityName}Guid = guid;");
			lines.Append($"\n\t\t\tmodel.{entityName}RecordVersion = version;");
			lines.Append( "\n\t\t\t");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Update(model);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.Message);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.NOT_FOUND)");
			lines.Append( "\n\t\t\t\treturn NotFound(serviceResult.Message);");
			lines.Append( "\n\t\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.Model;");
			lines.Append( "\n\t\t}");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t// DELETE api/{entityName}/7dd44fed-bf64-42d8-a6ea-04357c73482e/1");
			lines.Append( "\n\t\t[HttpDelete(\"{guid}/{version}\")]");
			lines.Append($"\n\t\tpublic ActionResult<{entityName}> Delete(string guid, decimal version)");
			lines.Append( "\n\t\t{");
			lines.Append($"\n\t\t\tvar model = new {entityName}()");
			lines.Append( "\n\t\t\t{");
			lines.Append($"\n\t\t\t\t{entityName}Guid = guid");
			lines.Append($"\n\t\t\t\t, {entityName}RecordVersion = version");
			lines.Append( "\n\t\t\t};");
			lines.Append( "\n\t\t");
			lines.Append($"\n\t\t\tvar serviceResult = _{entityInstanceName}Service.Delete(model);");
			lines.Append( "\n\t\t\tif (serviceResult.Status == BrashActionStatus.ERROR)");
			lines.Append( "\n\t\t\t\treturn BadRequest(serviceResult.Message);");
			lines.Append( "\n\t\t");
			lines.Append( "\n\t\t\treturn serviceResult.Model;");
			lines.Append( "\n\t\t}");


			lines.Append( GetFindByParentRoute(domain, entity, parent));


			lines.Append( "\n\t}");
			lines.Append( "\n}");

            return lines.ToString();
		}

		public string TplCsApiProgram(string domain)
		{
			return @"using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace " + domain + @".Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // ASP.NET Core 3.0+:
            // The UseServiceProviderFactory call attaches the
            // Autofac provider to the generic hosting mechanism.
            var host = Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new Autofac.Extensions.DependencyInjection.AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webHostBuilder => {
                webHostBuilder
                    .UseContentRoot(System.IO.Directory.GetCurrentDirectory())
                    .UseStartup<Startup>();
                })
                .Build();

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}";
		}

		public string TplCsApiStartup(string domain)
		{
			return @"using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Autofac;

namespace " + domain + @".Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

			      // configure basic authentication 
            services.AddAuthentication(""BasicAuthentication"")
                .AddScheme<AuthenticationSchemeOptions, BrashBasicAuthenticationHandler>(""BasicAuthentication"", null);
        }

        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            // wire up using autofac specific APIs here
            BrashConfigure.LoadContainer( containerBuilder);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

			      app.UseCors(builder =>
            {
                builder
                    .WithOrigins(""" + _options.DevSite + @""", """ + _options.WebSite + @""")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials(); 
            });

            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

			      app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}";
		}

		public string TplCsApiLaunchSettings(string domain)
		{
			return @"{
  ""$schema"": ""http://json.schemastore.org/launchsettings.json"",
  ""iisSettings"": {
    ""windowsAuthentication"": false,
      ""applicationUrl"": ""http://localhost:32770"",
    ""anonymousAuthentication"": true,
    ""iisExpress"": {
      ""applicationUrl"": ""http://localhost:32770"",
      ""sslPort"": 44335
    }
  },
  ""profiles"": {
    """ + domain + @".Api"": {
      ""commandName"": ""Project"",
      ""launchBrowser"": false,
      ""applicationUrl"": ""https://localhost:" + _options.ApiPort + @""",
      ""environmentVariables"": {
        ""ASPNETCORE_ENVIRONMENT"": ""Development""
      }
    }
  }
}
";
		}

		public string TplCsApiBrashConfigure(string domain, List<string> entities)
		{
			StringBuilder lines = new StringBuilder();

			lines.Append(@"using Autofac;
using Serilog;
using Brash.Infrastructure;
using Brash.Infrastructure.Sqlite;
using " + domain + @".Infrastructure.Sqlite.Repository;
using " + domain + @".Infrastructure.Sqlite.RepositorySql;
using " + domain + @".Infrastructure.Sqlite.Service;

namespace " + domain + @".Api
{
    public class BrashConfigure
    {
        public static void LoadContainer(ContainerBuilder containerBuilder)
        {
            var path = System.IO.Directory.GetCurrentDirectory();");
			lines.Append($"\n\t\t\tvar name = \"{domain}\";");
			lines.Append($"\n\t\t\t");
			lines.Append($"\n\t\t\t// Logger");
			lines.Append($"\n\t\t\tvar logfilename = $"); lines.Append("\"{path}/{name}.log\";");
			lines.Append(@"
            var logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.WriteTo.File(logfilename, rollingInterval: RollingInterval.Day)
				.CreateLogger();
			");
			
            lines.Append("\n\t\t\t// Database Configuration");
            lines.Append("\n\t\t\tvar databaseFile = $\"../sql/sqlite/{name}.webapi.sqlite\";");
            lines.Append("\n\t\t\tbool databaseExists = System.IO.File.Exists(databaseFile);");
            lines.Append("\n\t\t\tvar databaseContext = new DatabaseContext(");
            lines.Append("\n\t\t\t        $\"Data Source={databaseFile}\" ");
            lines.Append("\n\t\t\t        , $\"{name}Db\"");
            lines.Append("\n\t\t\t        , $\"{name}Schema\"");
            lines.Append("\n\t\t\t        , $\"../sql/sqlite/ALL.sql\");");

			lines.Append(@"

            // Database Manager
            var databaseManager = new DatabaseManager(databaseContext);
            if (!databaseExists)
            {
                databaseManager.CreateDatabase();
            }
			
            // Container Registar: Database 
            containerBuilder.RegisterInstance(logger).As<Serilog.ILogger>();
            containerBuilder.RegisterInstance(databaseContext).As<IDatabaseContext>();
            containerBuilder.RegisterInstance(databaseManager).As<IManageDatabase>();
			");

			foreach( var entityName in entities)
			{
				lines.Append( $"\n\t\t\t// Container Registar: {entityName}");
				lines.Append( "\n\t\t\tcontainerBuilder.Register<" + entityName + "RepositorySql>((c) => { return new " + entityName + "RepositorySql(); });");
        lines.Append( "\n\t\t\tcontainerBuilder.Register<" + entityName + "Repository>((c) => { return new " + entityName + "Repository( c.Resolve<IManageDatabase>(), c.Resolve<" + entityName + "RepositorySql>(), c.Resolve<Serilog.ILogger>()); });");
        lines.Append( "\n\t\t\tcontainerBuilder.Register<" + entityName + "Service>((c) => { return new " + entityName + "Service( c.Resolve<" + entityName + "Repository>(), c.Resolve<Serilog.ILogger>()); });");
        lines.Append( $"\n\t\t\t");
			}

			lines.Append( $"\n\t\t\t// BasicAuth");
			lines.Append( $"\n\t\t\tcontainerBuilder.Register<IBrashApiAuthService>((c) => {{");
			lines.Append( $"\n\t\t\t\treturn new BrashApiAuthService().AddAuthAccount(");
			lines.Append( $"\n\t\t\t\t\tnew BrashApiAuthModel(){{");
			lines.Append( $"\n\t\t\t\t\t\tApiAuthId = 1");
			lines.Append( $"\n\t\t\t\t\t\t, ApiAuthName = \"{_options.User}\"");
			lines.Append( $"\n\t\t\t\t\t\t, ApiAuthPass = \"{_options.Pass}\"");
			lines.Append( $"\n\t\t\t\t\t}});");
			lines.Append( $"\n\t\t\t\t}});");

			lines.Append(  "\n\t\t}");
			lines.Append(  "\n\t}");
			lines.Append(  "\n}");

			return lines.ToString();
		}

		public string TplCsApiBrashApiAuth(string domain)
		{
			StringBuilder lines = new StringBuilder();

			lines.Append(
@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace " + domain + @".Api
{
    public class BrashApiAuthModel
    {
        public int ApiAuthId { get; set; }
        public string ApiAuthName { get; set; }
        public string ApiAuthPass { get; set; }
    }

    public interface IBrashApiAuthService
    {
        Task<BrashApiAuthModel> Authenticate(string apiAuthName, string apiAuthPass);
    }

    public class BrashApiAuthService : IBrashApiAuthService
    {
        private List<BrashApiAuthModel> _accounts = new List<BrashApiAuthModel>();

        public BrashApiAuthService AddAuthAccount(BrashApiAuthModel account)
        {
            _accounts.Add(account);
            return this;
        }

        public async Task<BrashApiAuthModel> Authenticate(string apiAuthName, string apiAuthPass)
        {
            var auth = await Task.Run(() => _accounts.SingleOrDefault(x => x.ApiAuthName == apiAuthName && x.ApiAuthPass == apiAuthPass));

            if (auth == null)
                return null;

            return auth;
        }
    }

    public class BrashBasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IBrashApiAuthService _apiAuthService;

        public BrashBasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IBrashApiAuthService apiAuthService)
            : base(options, logger, encoder, clock)
        {
            _apiAuthService = apiAuthService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(""Authorization""))
                return AuthenticateResult.Fail(""Missing Authorization Header"");

            BrashApiAuthModel user = null;
            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers[""Authorization""]);
                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
                var apiAuthName = credentials[0];
                var apiAuthPass = credentials[1];

                user = await _apiAuthService.Authenticate(apiAuthName, apiAuthPass);
            }
            catch
            {
                return AuthenticateResult.Fail(""Invalid Authorization Header"");
            }

            if (user == null)
                return AuthenticateResult.Fail(""Invalid AuthName or Password"");

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, user.ApiAuthId.ToString()),
                new Claim(ClaimTypes.Name, user.ApiAuthName),
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
");

			return lines.ToString();
		}
    }
}