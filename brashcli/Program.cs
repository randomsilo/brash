using System;
using CommandLine;
using Serilog;
using brashcli.Option;
using brashcli.Process;

namespace brashcli
{
    public class Program
    {
        private static ILogger GetLogger()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File($"./tmp/brashcli_.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        static int Main(string[] args) 
        {
            System.IO.Directory.CreateDirectory("./tmp");

            return CommandLine.Parser.Default.ParseArguments<ProjectInitialization,DataInitialization, SqliteGeneration, CsDomainGeneration>(args)
	            .MapResult(
	                (ProjectInitialization opts) => CreateProjectInitializeScript(opts)
                    , (DataInitialization opts) => CreateDataJsonFile(opts)
                    , (SqliteGeneration opts) => CreateSqlFiles(opts)
                    , (CsDomainGeneration opts) => CreateCsDomainFiles(opts)
                    , errs => 1);
        }

        static int CreateProjectInitializeScript(ProjectInitialization opts)
		{
            int returnCode = 0;
            var logger = GetLogger();

            logger.Information($"Project  : {opts.ProjectName}"); 
            logger.Information($"Directory: {opts.DirectoryName}"); 

			do 
            {
                try 
                {
                    ProjectInitializationProcess process = new ProjectInitializationProcess(logger, opts);
                    returnCode = process.Execute();
                    if (returnCode != 0)
                        break;

                }
                catch(Exception exception)
                {
                    logger.Error(exception, "CreateProjectInitializeScript, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while(false);

            return returnCode;
		}

        static int CreateDataJsonFile(DataInitialization opts)
		{
            int returnCode = 0;
            var logger = GetLogger();

            logger.Information($"Project  : {opts.ProjectName}"); 
            logger.Information($"Directory: {opts.DirectoryName}"); 

			do 
            {
                try 
                {
                    DataInitializationProcess process = new DataInitializationProcess(logger, opts);
                    returnCode = process.Execute();
                    if (returnCode != 0)
                        break;

                }
                catch(Exception exception)
                {
                    logger.Error(exception, "CreateDataJsonFile, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while(false);

            return returnCode;
		}

        static int CreateSqlFiles(SqliteGeneration opts)
		{
            int returnCode = 0;
            var logger = GetLogger();

            logger.Information($"File  : {opts.FilePath}"); 

			do 
            {
                try 
                {
                    SqliteGenerationProcess process = new SqliteGenerationProcess(logger, opts);
                    returnCode = process.Execute();
                    if (returnCode != 0)
                        break;

                }
                catch(Exception exception)
                {
                    logger.Error(exception, "CreateSqlFiles, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while(false);

            return returnCode;
		}

        static int CreateCsDomainFiles(CsDomainGeneration opts)
		{
            int returnCode = 0;
            var logger = GetLogger();

            logger.Information($"File  : {opts.FilePath}"); 

			do 
            {
                try 
                {
                    CsDomainGenerationProcess process = new CsDomainGenerationProcess(logger, opts);
                    returnCode = process.Execute();
                    if (returnCode != 0)
                        break;

                }
                catch(Exception exception)
                {
                    logger.Error(exception, "CreateCsDomainFiles, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while(false);

            return returnCode;
		}
    }
}
