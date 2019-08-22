using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Serilog;

namespace hcli
{

    [Verb("init", HelpText = "Create project setup script")]
    public class InitOptions 
    {
        [Option('n', "name", Required = true, HelpText = "Project name, camal case, no spaces or special characters")]
        public string ProjectName { get; set; }

        [Option('d', "directory", Required = true, HelpText = "Directory path, linux, like: ./tmp or ../tmp")]
        public string DirectoryName { get; set; }
    }
    
    public class Program
    {
        private static ILogger GetLogger()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File($"hcli_.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        static int Main(string[] args) 
        {
            return CommandLine.Parser.Default.ParseArguments<InitOptions>(args)
	            .MapResult(
	                (InitOptions opts) => CreateProjectInitializeScript(opts)
                    , errs => 1);
        }

        static int CreateProjectInitializeScript(InitOptions opts)
		{
            int returnCode = 0;
            var logger = GetLogger();

            logger.Information($"Project  : {opts.ProjectName}"); 
            logger.Information($"Directory: {opts.DirectoryName}"); 

			do 
            {
                try 
                {
                    // put work here
                }
                catch(Exception global)
                {
                    logger.Error(global, "CreateProjectInitializeScript, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while(false);

            return returnCode;
		}
    }
}
