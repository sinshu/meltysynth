using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MeltySynth.SoundFont
{
    public sealed class SoundFontSampleData
    {
        private int bitsPerSample;
        private short[] samples;

        internal SoundFontSampleData(BinaryReader reader)
        {
            var chunkId = reader.ReadFixedLengthString(4);
            if (chunkId != "LIST")
            {
                throw new InvalidDataException("The LIST chunk was not found.");
            }

            var end = reader.BaseStream.Position + reader.ReadInt32();

            var listType = reader.ReadFixedLengthString(4);
            if (listType != "sdta")
            {
                throw new InvalidDataException($"The type of the LIST chunk must be 'sdta', but was '{listType}'.");
            }

            while (reader.BaseStream.Position < end)
            {
                var id = reader.ReadFixedLengthString(4);
                var size = reader.ReadInt32();

                switch (id)
                {
                    case "smpl":
                        bitsPerSample = 16;
                        samples = new short[size / 2];
                        reader.Read(MemoryMarshal.Cast<short, byte>(samples));
                        break;
                    case "sm24":
                        // 24 bit audio is not supported.
                        reader.BaseStream.Position += size;
                        break;
                    default:
                        throw new InvalidDataException($"The INFO list contains an unknown ID '{id}'.");
                }
            }

            if (samples == null)
            {
                throw new InvalidDataException("No valid sample data was found.");
            }
        }

        public override string ToString()
        {
            return $"{bitsPerSample} bit, {samples.Length} samples";
        }

        public int BitsPerSample => bitsPerSample;
        public short[] Samples => samples;
    }
}
