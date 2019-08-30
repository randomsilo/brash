
using CommandLine;

namespace brashcli.Option
{
    [Verb("sql-gen", HelpText = "Create sql from json structure")]
    public class SqlGeneration 
    {
        [Option('f', "file", Required = true, HelpText = "file name, pull path")]
        public string FilePath { get; set; }

    }
}