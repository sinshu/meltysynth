using System;
using System.IO;

namespace MeltySynth
{
    public sealed class SampleHeader
    {
        internal static readonly SampleHeader Default = new SampleHeader();

        private readonly string name;
        private readonly int start;
        private readonly int end;
        private readonly int startLoop;
        private readonly int endLoop;
        private readonly int sampleRate;
        private readonly byte originalPitch;
        private readonly sbyte pitchCorrection;
        private readonly ushort link;
        private readonly SampleType type;

        private SampleHeader()
        {
            name = "Default";
        }

        private SampleHeader(BinaryReader reader)
        {
            name = reader.ReadFixedLengthString(20);
            start = reader.ReadInt32();
            end = reader.ReadInt32();
            startLoop = reader.ReadInt32();
            endLoop = reader.ReadInt32();
            sampleRate = reader.ReadInt32();
            originalPitch = reader.ReadByte();
            pitchCorrection = reader.ReadSByte();
            link = reader.ReadUInt16();
            type = (SampleType)reader.ReadUInt16();
        }

        internal static SampleHeader[] ReadFromChunk(BinaryReader reader, int size)
        {
            if (size % 46 != 0)
            {
                throw new InvalidDataException("The sample header list is invalid.");
            }

            var headers = new SampleHeader[size / 46 - 1];

            for (var i = 0; i < headers.Length; i++)
            {
                headers[i] = new SampleHeader(reader);
            }

            // The last one is the terminator.
            new SampleHeader(reader);

            return headers;
        }

        public override string ToString()
        {
            return name;
        }

        public string Name => name;
        public int Start => start;
        public int End => end;
        public int StartLoop => startLoop;
        public int EndLoop => endLoop;
        public int SampleRate => sampleRate;
        public byte OriginalPitch => originalPitch;
        public sbyte PitchCorrection => pitchCorrection;
        public ushort Link => link;
        public SampleType Type => type;
    }
}
