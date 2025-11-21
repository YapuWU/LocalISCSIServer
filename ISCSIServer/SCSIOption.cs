using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskServer
{
    public sealed class SCSIOption
    {
        [ConfigurationKeyName("Server")]        
        public List<ServerOption> Servers { get; set; } = new List<ServerOption>();
    }
}
