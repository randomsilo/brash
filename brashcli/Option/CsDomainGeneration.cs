
using CommandLine;

namespace brashcli.Option
{
    [Verb("cs-domain", HelpText = "Create c# models from json structure")]
    public class CsDomainGeneration 
    {
        [Option('f', "file", Required = true, HelpText = "file name, pull path")]
        public string FilePath { get; set; }

    }
}