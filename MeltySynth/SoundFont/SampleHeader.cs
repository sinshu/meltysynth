using System;
using System.Collections.Generic;
using System.IO;

namespace MeltySynth.SoundFont
{
    public sealed class SampleHeader
    {
        private string name;
        private int start;
        private int end;
        private int loopStart;
        private int loopEnd;
        private int sampleRate;
        private byte originalPitch;
        private sbyte pitchCorrection;
        private ushort link;
        private SampleType type;

        private SampleHeader(BinaryReader reader)
        {
            name = reader.ReadFixedLengthString(20);
            start = reader.ReadInt32();
            end = reader.ReadInt32();
            loopStart = reader.ReadInt32();
            loopEnd = reader.ReadInt32();
            sampleRate = reader.ReadInt32();
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
        public int Start => start;
        public int End => end;
        public int LoopStart => loopStart;
        public int LoopEnd => loopEnd;
        public int SampleRate => sampleRate;
        public byte OriginalPitch => originalPitch;
        public sbyte PitchCorrection => pitchCorrection;
        public ushort Link => link;
        public SampleType Type => type;
    }
}
