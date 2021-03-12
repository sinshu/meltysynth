using System;
using System.Collections.Generic;
using System.IO;

namespace MeltySynth.SoundFont
{
    internal sealed class InstrumentInfo
    {
        private string name;
        private int zoneStartIndex;
        private int zoneEndIndex;

        private InstrumentInfo()
        {
        }

        internal static IReadOnlyList<InstrumentInfo> ReadFromChunk(BinaryReader reader, int size)
        {
            if (size % 22 != 0)
            {
                throw new InvalidDataException("The instrument list is invalid.");
            }

            var count = size / 22;

            var instruments = new List<InstrumentInfo>(count);

            for (var i = 0; i < count; i++)
            {
                var instrument = new InstrumentInfo();
                instrument.name = reader.ReadAsciiString(20);
                instrument.zoneStartIndex = reader.ReadUInt16();

                instruments.Add(instrument);
            }

            for (var i = 0; i < count - 1; i++)
            {
                instruments[i].zoneEndIndex = instruments[i + 1].zoneStartIndex - 1;
            }

            // The last one is the terminator.
            instruments.RemoveAt(count - 1);

            return instruments;
        }

        public string Name => name;
        public int ZoneStartIndex => zoneStartIndex;
        public int ZoneEndIndex => zoneEndIndex;
    }
}
