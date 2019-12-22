
using CommandLine;

namespace brashcli.Option
{
    [Verb("cs-api-sqlite", HelpText = "Create c# restful api to sqlite repositories from json structure")]
    public class CsApiGeneration 
    {
        [Option('f', "file", Required = true, HelpText = "file name, pull path")]
        public string FilePath { get; set; }

        [Option('u', "user", Required = true, HelpText = "basic auth user name")]
        public string User { get; set; }

        [Option('p', "pass", Required = true, HelpText = "basic auth user password")]
        public string Pass { get; set; }

        [Option('t', "port", Required = true, HelpText = "api port")]
        public int ApiPort { get; set; }

        [Option('d', "dev-site", Required = true, HelpText = "development web site, https://localhost:5001")]
        public string DevSite { get; set; }

        [Option('w', "web-site", Required = true, HelpText = "public web site, https://myapp.mydomain.com")]
        public string WebSite { get; set; }

    }
}