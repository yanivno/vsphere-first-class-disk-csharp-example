using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace fcdSampleCode
{
    class Options
    {
        [Option('s', "server", Required = true, HelpText = "vCenter SDK Url in https://<vcenter>/sdk")]
        public string Server { get; set; }

        [Option('u', "username", Required = true, HelpText = "Username for vCenter Connection")]
        public string Username { get; set; }

        [Option('p', "password", Required = true, HelpText = "password for vCenter Connection")]
        public string Password { set; get; }

        [Option('d', "datastore", Required = true, HelpText = "Datastore name")]
        public string Datastore { get; set; }

        [Option('f', "fcd", Required = true, HelpText = "Existing First Class Disk Name")]
        public string FcdName { get; set; }

        [Option('v', "vm", Required = true, HelpText = "Target VM Name")]
        public string VMName { get; set; }

        [Option('n', "snapshot", Required = true, HelpText = "Snapshot Name Prefix, i.e. snap-")]
        public string SnapshotNamePrefix { get; set; }
    }
}
