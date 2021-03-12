using System;
using System.IO;
using System.Text;

namespace MeltySynth
{
    public static class BinaryReaderEx
    {
        public static string ReadAsciiString(this BinaryReader reader)
        {
            var sb = new StringBuilder();
            for (var c = (char)reader.ReadByte(); c != '\0'; c = (char)reader.ReadByte())
            {
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static string ReadAsciiString(this BinaryReader reader, int count)
        {
            var data = reader.ReadBytes(count);

            int length;
            for (length = 0; length < data.Length; length++)
            {
                if (data[length] == 0)
                {
                    break;
                }
            }

            return Encoding.ASCII.GetString(data, 0, length);
        }
    }
}
