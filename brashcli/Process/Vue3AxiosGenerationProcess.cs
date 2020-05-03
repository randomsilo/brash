using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Serilog;
using brashcli.Option;
using brashcli.Model;


namespace brashcli.Process
{
    public class Vue3AxiosGenerationProcess
    {
        private ILogger _logger;
        private Vue3AxiosGeneration _options;
        private string _pathProject;
        private string _outputDirectory;
        private DomainStructure _domainStructure;
        private List<string> _entities = new List<string>();

        public Vue3AxiosGenerationProcess(ILogger logger, Vue3AxiosGeneration options)
        {
            _logger = logger;
            _options = options;
            _pathProject = System.IO.Path.GetDirectoryName(_options.FilePath);
        }

        public int Execute()
        {
            int returnCode = 0;

            _logger.Debug("Vue3AxiosGenerationProcess: start");
            do
            {
                try
                {
                    ReadDataJsonFile();
                    MakeDirectories();
                    CreateVueFiles();
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Vue3AxiosGenerationProcess, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while (false);
            _logger.Debug("Vue3AxiosGenerationProcess: end");

            return returnCode;
        }

        private void MakeDirectories()
        {
            var directory = _domainStructure.Domain.ToLower() + "-" + "api";
            _outputDirectory = System.IO.Path.Combine(_options.OutputDirectory, directory);
            System.IO.Directory.CreateDirectory(_outputDirectory);
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

        private void CreateVueFiles()
        {
            _logger.Debug("CreateVueFiles");

            // Make Vue Axios Client
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(_outputDirectory, "index.js")
                , TplVueAxiosClient(_options));  
        }

        private string TplVueAxiosClient(Vue3AxiosGeneration options)
        {
            var CAPS_DOMAIN = _domainStructure.Domain.ToUpper();
            return $@"import Vue from 'vue';
import axios from 'axios';

const { CAPS_DOMAIN }_API_URL = process.env.{ CAPS_DOMAIN }_API_URL || 'http://localhost:{ options.ApiPort }/api/';
const { CAPS_DOMAIN }_API_USER = process.env.{ CAPS_DOMAIN }_API_USER || '{ options.User }';
const { CAPS_DOMAIN }_API_PASS = process.env.{ CAPS_DOMAIN }_API_PASS || '{ options.Pass }';
const { CAPS_DOMAIN }_API_CREDENTIALS = btoa({ CAPS_DOMAIN }_API_USER + ':' + { CAPS_DOMAIN }_API_PASS);

const { _domainStructure.Domain }Api = axios.create({{
  baseURL: { CAPS_DOMAIN }_API_URL
  , headers: {{
    'Content-Type': 'application/json',
    'Authorization': 'Basic ' + { CAPS_DOMAIN }_API_CREDENTIALS
  }}
}});

Vue.prototype.{ _domainStructure.Domain }Api = { _domainStructure.Domain }Api;

export default {{
}};

";
        }



        

    }
}