using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace AVRecordManager
{
    public static class MemoryStreamExtensions
    {
        public static byte[] TrimEnd(this byte[] array)
        {
            int lastIndex = Array.FindLastIndex(array, b => b != 0);

            Array.Resize(ref array, lastIndex + 1);

            return array;
        }

        public static void Clear(this MemoryStream source)
        {
            byte[] buffer = source.GetBuffer();
            Array.Clear(buffer, 0, buffer.Length);
            source.Position = 0;
            source.SetLength(0);
        }
    }
}