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
    public class DataInitializationProcess
    {
        private ILogger _logger;
        private DataInitialization _options;
        public DataInitializationProcess(ILogger logger, DataInitialization options)
        {
            _logger = logger;
            _options = options;

            if (!_options.DirectoryName.EndsWith("/"))
                _options.DirectoryName = _options.DirectoryName + "/";

        }

        public int Execute()
        {
            int returnCode = 0;

            _logger.Debug("DataInitializationProcess: start");
            do
            {
                try 
                {
                    CheckProjectDirectory();
                    MakeDataJsonFile();
                }
                catch(Exception exception)
                {
                    _logger.Error(exception, "DataInitializationProcess, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while(false);
            _logger.Debug("DataInitializationProcess: end");

            return returnCode;
        }

        private void CheckProjectDirectory()
        {
            if (!System.IO.Directory.Exists(_options.DirectoryName)) 
            { 
                throw new Exception($"{_options.DirectoryName} is missing. Try: brashcli project-init -n {_options.ProjectName} -d {_options.DirectoryName}");
            }
        }

        private void MakeDataJsonFile()
        {
            var template = Handlebars.Compile( GetTemplateDataJsonFile());
            var result = template( _options);

            System.IO.File.WriteAllText( $"{_options.DirectoryName}/structure.json", result);
        }

        private string GetTemplateDataJsonFile()
        {
            return @"
{
	""entities"": [
		{
			""name"": ""State""
			, ""fields"": [
				  { ""name"": ""Name"", ""type"": ""S"" }
				, { ""name"": ""Abbrv"", ""type"": ""S"" }
			]
		}
		, {
			""name"": ""Employee""
			, ""base"": ""Audit""
			, ""extensions"": [
				{
					""name"": ""MailingAddress""
					, ""fields"": [
						  { ""name"": ""AddressLine1"", ""type"": ""S"" }
						, { ""name"": ""AddressLine2"", ""type"": ""S"" }
						, { ""name"": ""City"", ""type"": ""S"" }
						, { ""name"": ""StateRef"", ""type"": ""R"", ""entity"": ""State""}
						, { ""name"": ""ZipCode"", ""type"": ""S"" }
						/*
							S = String
							G = Guid
							R = Reference
							D = DateTime
							N = Decimal
							I = Integer
							B = Blob
							C = Clob
						*/
					]
				}
			]
			, ""children"": [
			
			]
		}
	]
}
";
        }


    }
}