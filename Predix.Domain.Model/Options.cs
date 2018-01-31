using CommandLine;
using CommandLine.Text;

namespace Predix.Domain.Model
{
    public class Options
    {
        [Option('l', "loc", Required = true,
            HelpText = "RefreshLocations.")]
        public bool RefreshLocations { get; set; }

        [Option('i', "irc", Required = true,
            HelpText = "Ignore Regulation Check.")]
        public bool IgnoreRegulationCheck { get; set; }

        [Option('e', "se", Required = true,
            HelpText = "Save Events.")]
        public bool SaveEvents { get; set; }

        //[Option('v', "verbose", DefaultValue = true,
        //     HelpText = "Prints all messages to standard output.")]
        //public bool Verbose { get; set; }
    }
}