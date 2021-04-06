using System;
using System.IO;

namespace MeltySynth
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

        internal static ZoneInfo[] ReadFromChunk(BinaryReader reader, int size)
        {
            if (size % 4 != 0)
            {
                throw new InvalidDataException("The zone list is invalid.");
            }

            var count = size / 4;

            var zones = new ZoneInfo[count];

            for (var i = 0; i < count; i++)
            {
                var zone = new ZoneInfo();
                zone.generatorIndex = reader.ReadUInt16();
                zone.modulatorIndex = reader.ReadUInt16();

                zones[i] = zone;
            }

            for (var i = 0; i < count - 1; i++)
            {
                zones[i].generatorCount = zones[i + 1].generatorIndex - zones[i].generatorIndex;
                zones[i].modulatorCount = zones[i + 1].modulatorIndex - zones[i].modulatorIndex;
            }

            return zones;
        }

        public int GeneratorIndex => generatorIndex;
        public int ModulatorIndex => modulatorIndex;
        public int GeneratorCount => generatorCount;
        public int ModulatorCount => modulatorCount;
    }
}
