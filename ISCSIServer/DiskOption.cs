using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskServer
{
    public sealed class DiskOption
    {
        public long GetDiskSizeInBytes()
        {
            return Size * 1024 * 1024*1024;
        }
        public required string Path { get; set; } = string.Empty;
        public required long Size { get; set; }
        public required bool AutoCreate { get; set; } = false;
        public required bool Dynamic { get; set; } = false;
        public required bool ReadOnly { get; set; } = false;
    }
}
