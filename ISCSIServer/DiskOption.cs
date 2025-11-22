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
            long scale = 1024 * 1024 * 1024;
            long sizeValue = 0;
            long sizeInBytes = 0;
            if (Size.EndsWith("GB", StringComparison.OrdinalIgnoreCase))
                scale = 1024*1024*1024;
            else if (Size.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
                scale = 1024 * 1024;

            string sizeString = Size.Substring(0, Size.Length - 2);
            if(!long.TryParse(sizeString, out sizeValue))
            {
                throw new FormatException("Invalid size format.");
            }
            sizeInBytes = sizeValue * scale;

            return sizeInBytes;
        }
        public int Name { get; set; }
        public required string Path { get; set; } = string.Empty;
        public required string Size { get; set; }
        public required bool AutoCreate { get; set; } = false;
        public required bool Dynamic { get; set; } = false;
        public required bool ReadOnly { get; set; } = false;
        public required string SerialNumber { get; set; } = string.Empty;
    }
}
