using CommandLine;

namespace MongoRunner
{
    public class CmdOptions
    {
        [Option('v', "verbose", Default = false, Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
        
        [Option('p', "port", Default = 27017, Required = false, HelpText = "Port to run mongo db")]
        public int Port { get; set; }
        
        [Option('d', "dir", Default = "", Required = false, HelpText = "Path to mongo data")]
        public string DataDirectory { get; set; }
    }
}