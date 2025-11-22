using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskServer
{
    public sealed class TargetOption
    {
        public required string TargetName { get; set; } = string.Empty;
        public string VendorIdentification { get; set; } = string.Empty;
        [ConfigurationKeyName("Initiator")]
        public required List<string>  Initiator { get; set; } = new List<string>();
        [ConfigurationKeyName("Disk")]
        public required List<DiskOption> Disk { get; set; }
    }
}
