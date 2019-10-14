
using CommandLine;

namespace brashcli.Option
{
    [Verb("cs-xtest-sqlite", HelpText = "Create c# xunit tests for sqlite repositories from json structure")]
    public class CsXtestGeneration 
    {
        [Option('f', "file", Required = true, HelpText = "file name, pull path")]
        public string FilePath { get; set; }

    }
}