using System;
using System.IO;

namespace MeltySynth
{
    internal sealed class InstrumentInfo
    {
        private string name;
        private int zoneStartIndex;
        private int zoneEndIndex;

        private InstrumentInfo(BinaryReader reader)
        {
            name = reader.ReadFixedLengthString(20);
            zoneStartIndex = reader.ReadUInt16();
        }

        internal static InstrumentInfo[] ReadFromChunk(BinaryReader reader, int size)
        {
            if (size % 22 != 0)
            {
                throw new InvalidDataException("The instrument list is invalid.");
            }

            var count = size / 22;

            var instruments = new InstrumentInfo[count];

            for (var i = 0; i < count; i++)
            {
                instruments[i] = new InstrumentInfo(reader);
            }

            for (var i = 0; i < count - 1; i++)
            {
                instruments[i].zoneEndIndex = instruments[i + 1].zoneStartIndex - 1;
            }

            return instruments;
        }

        public string Name => name;
        public int ZoneStartIndex => zoneStartIndex;
        public int ZoneEndIndex => zoneEndIndex;
    }
}
