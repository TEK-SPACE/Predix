using CommandLine;
using CommandLine.Text;

namespace Predix.Domain.Model
{
    public class Options
    {
        [Option('l', "loc", //Required = true,
            HelpText = "RefreshLocations.")]
        public bool RefreshLocations { get; set; }

        [Option('i', "irc", //Required = true,
            HelpText = "Ignore Regulation Check.")]
        public bool IgnoreRegulationCheck { get; set; }

        [Option('e', "se", //Required = true,
            HelpText = "Save Events.")]
        public bool SaveEvents { get; set; }

        [Option('m', "img", //Required = true,
            HelpText = "Save Images.")]
        public bool SaveImages { get; set; }
        [Option('a', "all", //Required = true,
            HelpText = "Mark All as Violations")]
        public bool MarkAllAsViolations { get; set; }

        [Option('d', "dg", //Required = true,
            HelpText = "Enable Debugging")]
        public bool Debug { get; set; }

        //[Option('v', "verbose", DefaultValue = true,
        //     HelpText = "Prints all messages to standard output.")]
        //public bool Verbose { get; set; }
    }
}