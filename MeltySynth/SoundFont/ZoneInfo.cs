using System;
using System.Collections.Generic;
using System.IO;

namespace MeltySynth.SoundFont
{
    internal sealed class ZoneInfo
    {
        private int generatorIndex;
        private int modulatorIndex;
        private int generatorCount;
        private int modulatorCount;

        private ZoneInfo()
        {
        }

        internal static IReadOnlyList<ZoneInfo> ReadFromChunk(BinaryReader reader, int size)
        {
            if (size % 4 != 0)
            {
                throw new InvalidDataException("The zone list is invalid.");
            }

            var count = size / 4;

            var zones = new List<ZoneInfo>(count);

            for (var i = 0; i < count; i++)
            {
                var zone = new ZoneInfo();
                zone.generatorIndex = reader.ReadUInt16();
                zone.modulatorIndex = reader.ReadUInt16();

                zones.Add(zone);
            }

            for (var i = 0; i < count - 1; i++)
            {
                zones[i].generatorCount = zones[i + 1].generatorIndex - zones[i].generatorIndex;
                zones[i].modulatorCount = zones[i + 1].modulatorIndex - zones[i].modulatorIndex;
            }

            // The last one is the terminator.
            zones.RemoveAt(count - 1);

            return zones;
        }

        public int GeneratorIndex => generatorIndex;
        public int ModulatorIndex => modulatorIndex;
        public int GeneratorCount => generatorCount;
        public int ModulatorCount => modulatorCount;
    }
}
