using System;
using System.IO;

namespace MeltySynth
{
    internal struct Zone
    {
        private ArraySegment<Generator> generators;

        private Zone(ArraySegment<Generator> generators)
        {
            this.generators = generators;
        }

        private Zone(ZoneInfo info, Generator[] generators)
        {
            this.generators = new ArraySegment<Generator>(generators, info.GeneratorIndex, info.GeneratorCount);
        }

        internal static Zone[] Create(ZoneInfo[] infos, Generator[] generators)
        {
            if (infos.Length <= 1)
            {
                throw new InvalidDataException("No valid zone was found.");
            }

            // The last one is the terminator.
            var zones = new Zone[infos.Length - 1];

            for (var i = 0; i < zones.Length; i++)
            {
                if (infos[i].GeneratorIndex < 0 || infos[i].GeneratorCount < 0 ||
                    infos[i].GeneratorIndex > generators.Length - infos[i].GeneratorCount ||
                    infos[i].ModulatorCount < 0)
                    throw new InvalidDataException("SoundFont zone indices are invalid.");
                zones[i] = new Zone(infos[i], generators);
            }

            return zones;
        }

        public static Zone Empty => new Zone(ArraySegment<Generator>.Empty);

        public ArraySegment<Generator> Generators => generators;
    }
}
