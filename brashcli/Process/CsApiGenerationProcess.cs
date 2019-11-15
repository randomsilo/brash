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
			_domainStructure = JsonConvert.DeserializeObject<DomainStructure>(json);
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
			
			foreach( var entity in _domainStructure.Structure)
			{
				MakeApiFiles(null, entity);
			}

			// Make BrashConfigure.cs
			System.IO.File.WriteAllText( 
				System.IO.Path.Combine(_pathApiDirectory, "BrashConfigure.cs")
				, TplCsApiBrashConfigure(_domainStructure.Domain, _entities));

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
			
			if (entity.AdditionalPatterns != null && entity.AdditionalPatterns.Contains(Global.ADDITIONALPATTERN_CHOICE))
			{
				lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindWhere(\"WHERE IsDisabled = 0 ORDER BY OrderNo \");");
			}
			else
			{
				lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindWhere(\"WHERE 1 = 1 ORDER BY 1 \");");
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

		public string TplCsApiAskGuid(
			string domain
			, Structure entity
			, string idPattern
			)
        {
			string entityName = entity.Name;
			string entityInstanceName = ToLowerFirstChar(entity.Name);
            StringBuilder lines = new StringBuilder();

			// TODO

			return lines.ToString();
		}

		public string TplCsApiAskVersion(
			string domain
			, Structure entity
			, string idPattern
			)
        {
			string entityName = entity.Name;
			string entityInstanceName = ToLowerFirstChar(entity.Name);
            StringBuilder lines = new StringBuilder();

			// TODO

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

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}";
		}

		public string TplCsApiBrashConfigure(string domain, List<string> entities)
		{
			StringBuilder lines = new StringBuilder();

			lines.Append(@"using Autofac;
using Serilog;
using Brash.Infrastructure;
using Brash.Infrastructure.Sqlite;
using MyProject.Infrastructure.Sqlite.Repository;
using MyProject.Infrastructure.Sqlite.RepositorySql;
using MyProject.Infrastructure.Sqlite.Service;

namespace MyProject.Api
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

			return lines.ToString();
		}
    }
}