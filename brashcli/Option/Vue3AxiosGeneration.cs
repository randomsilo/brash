
using CommandLine;

namespace brashcli.Option
{
    [Verb("vue3-axios", HelpText = "Create Vue3 Axios client")]
    public class Vue3AxiosGeneration 
    {
        [Option('f', "file", Required = true, HelpText = "file name, pull path")]
        public string FilePath { get; set; }

        [Option('u', "user", Required = true, HelpText = "basic auth user name")]
        public string User { get; set; }

        [Option('p', "pass", Required = true, HelpText = "basic auth user password")]
        public string Pass { get; set; }

        [Option('t', "port", Required = true, HelpText = "api port")]
        public int ApiPort { get; set; }

        [Option('o', "output-dir", Required = true, HelpText = "Output directory for Vue3 code")]
        public string OutputDirectory { get; set; }

    }
}