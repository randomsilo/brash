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
            return @"{ 
    ""Domain"": """ + _options.ProjectName + @"""
	, ""Structure"": [
		{
			""Name"": ""BiologoicalSex""
			, ""AdditionalPatterns"": [
				""Choice""
			]
			, ""Choices"": [
				""Male""
				, ""Female""
			]
		}
		, {
			""Name"": ""PhoneType""
			, ""AdditionalPatterns"": [
				""Choice""
			]
			, ""Choices"": [
				""Cell""
				, ""Home""
				, ""Work""
			]
		}
		, {
			""Name"": ""AddressType""
			, ""AdditionalPatterns"": [
				""Choice""
			]
			, ""Choices"": [
				""Residence""
				, ""Mailing""
				, ""Billing""
			]
		}
		, {
			""Name"": ""UsState""
			, ""AdditionalPatterns"": [
				""Choice""
			]
			, ""Fields"": [
				{ ""Name"": ""Abbreviation"", ""Type"": ""S"" }
			]
			, ""AdditionalSqlStatements"": [
				""INSERT INTO UsState (ChoiceName, Abbreviation) VALUES ('Nebraska', 'NE');""
				, ""INSERT INTO UsState (ChoiceName, Abbreviation) VALUES ('Iowa', 'IA');""
				, ""INSERT INTO UsState (ChoiceName, Abbreviation) VALUES ('North Dakota', 'ND');""
			]
		}
		, {
			""Name"": ""Person""
			, ""Fields"": [
				  { ""Name"": ""LastName"", ""Type"": ""S"" }
				, { ""Name"": ""FirstName"", ""Type"": ""S"" }
				, { ""Name"": ""MiddleName"", ""Type"": ""S"" }
				, { ""Name"": ""DateOfBirth"", ""Type"": ""D"" }
			]
			, ""References"": [
				{
					""ColumnName"": ""BiologoicalSexRef""
					, ""TableName"": ""BiologoicalSex""
				}
			]
			, ""Extensions"": [
				{
					""Name"": ""Identication""
					, ""Fields"": [
						{ ""Name"": ""UserName"", ""Type"": ""S"" }
						, { ""Name"": ""Email"", ""Type"": ""S"" }
						, { ""Name"": ""PinCode"", ""Type"": ""I"" }
					]
				}
			]
			, ""Children"": [
				{
					""Name"": ""Phone""
					, ""Fields"": [
						  { ""Name"": ""PhoneNumber"", ""Type"": ""S"" }
						  , { ""Name"": ""Notes"", ""Type"": ""S"" }
					]
					, ""References"": [
						{
							""ColumnName"": ""PhoneTypeRef""
							, ""TableName"": ""PhoneType""
						}
					]
				}
				, {
					""Name"": ""Address""
					, ""Fields"": [
						  { ""Name"": ""Attention"", ""Type"": ""S"" }
						  , { ""Name"": ""AddressLine1"", ""Type"": ""S"" }
						  , { ""Name"": ""AddressLine2"", ""Type"": ""S"" }
						  , { ""Name"": ""City"", ""Type"": ""S"" }
						  , { ""Name"": ""PostalCode"", ""Type"": ""S"" }
					]
					, ""References"": [
						{
							""ColumnName"": ""UsStateRef""
							, ""TableName"": ""UsState""
						}
						, {
							""ColumnName"": ""AddressTypeRef""
							, ""TableName"": ""AddressType""
						}
					]
				}
			]
		}
	]
}
";
        }


    }
}