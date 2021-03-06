using System;
using Serilog;
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
                catch (Exception exception)
                {
                    _logger.Error(exception, "DataInitializationProcess, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while (false);
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
            System.IO.File.WriteAllText($"{_options.DirectoryName}/structure.json", TplDataJsonFile());
        }

        private string TplDataJsonFile()
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
				{ ""Name"": ""Abbreviation"", ""Type"": ""S"", ""Faker"": ""f.Address.StateAbbr()"" }
			]
			, ""AdditionalSqlStatements"": [
				""INSERT INTO UsState (ChoiceName, Abbreviation, OrderNo) VALUES ('Nebraska', 'NE', (SELECT IFNULL(MAX(OrderNo),0)+1 FROM UsState));""
				, ""INSERT INTO UsState (ChoiceName, Abbreviation, OrderNo) VALUES ('Iowa', 'IA', (SELECT IFNULL(MAX(OrderNo),0)+1 FROM UsState));""
				, ""INSERT INTO UsState (ChoiceName, Abbreviation, OrderNo) VALUES ('North Dakota', 'ND', (SELECT IFNULL(MAX(OrderNo),0)+1 FROM UsState));""
			]
		}
		, {
			""Name"": ""Person""
			, ""Fields"": [
				  { ""Name"": ""LastName"", ""Type"": ""S"", ""Faker"": ""f.Name.LastName(0)"" }
				, { ""Name"": ""FirstName"", ""Type"": ""S"", ""Faker"": ""f.Name.FirstName(0)"" }
				, { ""Name"": ""MiddleName"", ""Type"": ""S"", ""Faker"": ""f.Name.FirstName(0)"" }
				, { ""Name"": ""UserName"", ""Type"": ""S"", ""Faker"": ""f.Internet.UserName(m.FirstName, m.LastName)"" }
				, { ""Name"": ""Email"", ""Type"": ""S"", ""Faker"": ""f.Internet.Email(m.FirstName, m.LastName)"" }
				, { ""Name"": ""DateOfBirth"", ""Type"": ""D"" }
			]
			, ""References"": [
				{
					""ColumnName"": ""Gender""
					, ""TableName"": ""BiologoicalSex""
				}
			]
			, ""Extensions"": [
				{
					""Name"": ""Identication""
					, ""Fields"": [
						{ ""Name"": ""PinCode"", ""Type"": ""I"" }
						, { ""Name"": ""SSN"", ""Type"": ""S"" }
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
							""ColumnName"": ""RecordPhoneType""
							, ""TableName"": ""PhoneType""
						}
					]
				}
				, {
					""Name"": ""Address""
					, ""Fields"": [
						  { ""Name"": ""Attention"", ""Type"": ""S"" }
						  , { ""Name"": ""AddressLine1"", ""Type"": ""S"", ""Faker"": ""f.Address.StreetAddress()"" }
						  , { ""Name"": ""AddressLine2"", ""Type"": ""S"" }
						  , { ""Name"": ""City"", ""Type"": ""S"", ""Faker"": ""f.Address.City()"" }
						  , { ""Name"": ""PostalCode"", ""Type"": ""S"", ""Faker"": ""f.Address.ZipCode()"" }
					]
					, ""References"": [
						{
							""ColumnName"": ""RecordUsState""
							, ""TableName"": ""UsState""
						}
						, {
							""ColumnName"": ""RecordAddressType""
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