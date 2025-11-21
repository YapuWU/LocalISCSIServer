using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskServer
{
    public sealed class ServerOption
    {
        public string IPAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 3260;
        [ConfigurationKeyName("Target")]
        public List<TargetOption> Target { get; set; } = new List<TargetOption>();
    }
}
