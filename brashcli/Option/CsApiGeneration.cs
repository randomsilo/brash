
using CommandLine;

namespace brashcli.Option
{
    [Verb("cs-api-sqlite", HelpText = "Create c# restful api to sqlite repositories from json structure")]
    public class CsApiGeneration 
    {
        [Option('f', "file", Required = true, HelpText = "file name, pull path")]
        public string FilePath { get; set; }

    }
}