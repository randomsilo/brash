
using CommandLine;

namespace brashcli.Option
{
    [Verb("cs-repo-sqlite", HelpText = "Create c# sqlite repositories from json structure")]
    public class CsRepoGeneration 
    {
        [Option('f', "file", Required = true, HelpText = "file name, pull path")]
        public string FilePath { get; set; }

    }
}