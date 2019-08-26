
using CommandLine;

namespace brashcli.Option
{
    [Verb("data-init", HelpText = "Create project data json file")]
    public class DataInitialization 
    {
        [Option('n', "name", Required = true, HelpText = "Project name, camal case, no spaces or special characters")]
        public string ProjectName { get; set; }

        [Option('d', "directory", Required = true, HelpText = "Directory path, linux, like: ./tmp or ../tmp")]
        public string DirectoryName { get; set; }
    }
}