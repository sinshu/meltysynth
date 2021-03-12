using System;
using System.Collections.Generic;
using System.IO;

namespace MeltySynth.SoundFont
{
    public sealed class SampleHeader
    {
        private string name;
        private uint start;
        private uint end;
        private uint loopStart;
        private uint loopEnd;
        private uint sampleRate;
        private byte originalPitch;
        private sbyte pitchCorrection;
        private ushort link;
        private SampleType type;

        private SampleHeader(BinaryReader reader)
        {
            name = reader.ReadAsciiString(20);
            start = reader.ReadUInt32();
            end = reader.ReadUInt32();
            loopStart = reader.ReadUInt32();
            loopEnd = reader.ReadUInt32();
            sampleRate = reader.ReadUInt32();
            originalPitch = reader.ReadByte();
            pitchCorrection = reader.ReadSByte();
            link = reader.ReadUInt16();
            type = (SampleType)reader.ReadUInt16();
        }

        internal static IReadOnlyList<SampleHeader> ReadFromChunk(BinaryReader reader, int size)
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
        public uint Start => start;
        public uint End => end;
        public uint LoopStart => loopStart;
        public uint LoopEnd => loopEnd;
        public uint SampleRate => sampleRate;
        public byte OriginalPitch => originalPitch;
        public sbyte PitchCorrection => pitchCorrection;
        public ushort Link => link;
        public SampleType Type => type;
    }
}
