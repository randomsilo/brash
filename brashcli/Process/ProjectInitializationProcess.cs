using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Serilog;
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
        }


    }
}