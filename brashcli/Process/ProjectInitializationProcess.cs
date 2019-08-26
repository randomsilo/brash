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

namespace brashcli.Process
{
    public class ProjectInitializationProcess
    {
        private ILogger _logger;
        private ProjectInitialization _options;
        public ProjectInitializationProcess(ILogger logger, ProjectInitialization options)
        {
            _logger = logger;
            _options = options;

            if (!_options.DirectoryName.EndsWith("/"))
                _options.DirectoryName = _options.DirectoryName + "/";

        }

        public int Execute()
        {
            int returnCode = 0;

            _logger.Debug("CreateProjectInitializeScript: start");
            do
            {
                try 
                {
                    MakeProjectDirectory();
                    MakeProjectScript();
                }
                catch(Exception exception)
                {
                    _logger.Error(exception, "CreateProjectInitializeScript, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while(false);
            _logger.Debug("CreateProjectInitializeScript: end");

            return returnCode;
        }

        private void MakeProjectDirectory()
        {
            if (System.IO.Directory.Exists(_options.DirectoryName)) 
            { 
                System.IO.Directory.Delete(_options.DirectoryName, true);
                System.Threading.Thread.Sleep(2000);
            }
            System.IO.Directory.CreateDirectory(_options.DirectoryName);
        }

        private void MakeProjectScript()
        {
            var template = Handlebars.Compile( GetTemplateProjectScript());
            var result = template( _options);

            System.IO.File.WriteAllText( $"{_options.DirectoryName}/init.sh", result);
        }

        private string GetTemplateProjectScript()
        {
            return @"
#/bin/bash

# brashcli - generated project initialization script

## File System
mkdir -p {{ProjectName}}.Api
mkdir -p {{ProjectName}}.Domain
mkdir -p {{ProjectName}}.Infrastructure
mkdir -p {{ProjectName}}.Infrastructure.Test

## Solution
dotnet new sln

## Project
cd {{ProjectName}}.Api
dotnet new webapi
cd ..

cd {{ProjectName}}.Domain
dotnet new classlib
cd ..

cd {{ProjectName}}.Infrastructure
dotnet new classlib
cd ..

cd {{ProjectName}}.Infrastructure.Test
dotnet new xunit
cd ..

## Projects to Solution
dotnet sln {{ProjectName}}.sln add {{ProjectName}}.Api/{{ProjectName}}.Api.csproj
dotnet sln {{ProjectName}}.sln add {{ProjectName}}.Domain/{{ProjectName}}.Domain.csproj
dotnet sln {{ProjectName}}.sln add {{ProjectName}}.Infrastructure/{{ProjectName}}.Infrastructure.csproj
dotnet sln {{ProjectName}}.sln add {{ProjectName}}.Infrastructure.Test/{{ProjectName}}.Infrastructure.Test.csproj

## References

### API
dotnet add {{ProjectName}}.Api/{{ProjectName}}.Api.csproj reference {{ProjectName}}.Domain/{{ProjectName}}.Domain.csproj
dotnet add {{ProjectName}}.Api/{{ProjectName}}.Api.csproj reference {{ProjectName}}.Infrastructure/{{ProjectName}}.Infrastructure.csproj

### Domain

### Infrastructure
dotnet add {{ProjectName}}.Infrastructure/{{ProjectName}}.Infrastructure.csproj reference {{ProjectName}}.Domain/{{ProjectName}}.Domain.csproj

### Infrastructure Test
dotnet add {{ProjectName}}.Infrastructure.Test/{{ProjectName}}.Infrastructure.Test.csproj reference {{ProjectName}}.Domain/{{ProjectName}}.Domain.csproj
dotnet add {{ProjectName}}.Infrastructure.Test/{{ProjectName}}.Infrastructure.Test.csproj reference {{ProjectName}}.Infrastructure/{{ProjectName}}.Infrastructure.csproj

## Packages

cd {{ProjectName}}.Domain
dotnet add package Dapper
dotnet add package Serilog
#dotnet add package Brash.Domain
cd ..

cd {{ProjectName}}.Infrastructure
dotnet add package System.Data.SQLite
dotnet add package Dapper
dotnet add package Serilog
#dotnet add package Brash.Infrastructure
cd ..

cd {{ProjectName}}.Infrastructure.Test
dotnet add package Bogus
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
cd ..

cd {{ProjectName}}.Api
dotnet add package Dapper
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
cd ..

";
        }


    }
}