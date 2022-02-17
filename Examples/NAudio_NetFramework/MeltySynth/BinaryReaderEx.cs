using System;
using System.IO;
using System.Text;

namespace MeltySynth
{
    internal static class BinaryReaderEx
    {
        public static string ReadFourCC(this BinaryReader reader)
        {
            var data = reader.ReadBytes(4);

            for (var i = 0; i < data.Length; i++)
            {
                var value = data[i];
                if (!(32 <= value && value <= 126))
                {
                    data[i] = (byte)'?';
                }
            }

            return Encoding.ASCII.GetString(data, 0, data.Length);
        }

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

        public static short ReadInt16BigEndian(this BinaryReader reader)
        {
            var value = reader.ReadInt16();
            var b1 = 0xFF & (value >> 0);
            var b2 = 0xFF & (value >> 8);
            return (short)((b1 << 8) | b2);
        }

        public static int ReadInt32BigEndian(this BinaryReader reader)
        {
            var value = reader.ReadInt32();
            var b1 = 0xFF & (value >> 0);
            var b2 = 0xFF & (value >> 8);
            var b3 = 0xFF & (value >> 16);
            var b4 = 0xFF & (value >> 24);
            return (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;
        }

        public static int ReadIntVariableLength(this BinaryReader reader)
        {
            var acc = 0;
            var count = 0;
            while (true)
            {
                var value = reader.ReadByte();
                acc = (acc << 7) | (value & 127);
                if ((value & 128) == 0)
                {
                    break;
                }
                count++;
                if (count == 4)
                {
                    throw new InvalidDataException("The length of the value must be equal to or less than 4.");
                }
            }
            return acc;
        }
    }
}
