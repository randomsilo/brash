
using CommandLine;

namespace brashcli.Option
{
    [Verb("vue3-bs4-gm-icons", HelpText = "Create Vue3, Bootstrap4 components")]
    public class Vue3Bs4ComponentGeneration 
    {
        [Option('f', "file", Required = true, HelpText = "file name, pull path")]
        public string FilePath { get; set; }

        [Option('o', "output-dir", Required = true, HelpText = "Output directory for Vue3 code")]
        public string OutputDirectory { get; set; }

    }
}