using System;
using System.IO;
using System.Text;

namespace MeltySynth
{
    internal static class BinaryReaderEx
    {
        public static string ReadFixedLengthString(this BinaryReader reader, int length)
        {
            var data = reader.ReadBytes(length);

            int actualLength;
            for (actualLength = 0; actualLength < data.Length; actualLength++)
            {
                if (data[actualLength] == 0)
                {
                    break;
                }
            }

            return Encoding.ASCII.GetString(data, 0, actualLength);
        }
    }
}
