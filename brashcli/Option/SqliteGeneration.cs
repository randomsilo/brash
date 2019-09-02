
using CommandLine;

namespace brashcli.Option
{
    [Verb("sqlite-gen", HelpText = "Create sqlite sql from json structure")]
    public class SqliteGeneration 
    {
        [Option('f', "file", Required = true, HelpText = "file name, pull path")]
        public string FilePath { get; set; }

    }
}