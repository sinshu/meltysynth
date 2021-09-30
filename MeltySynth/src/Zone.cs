using System;
using System.IO;

namespace MeltySynth
{
    internal sealed class Zone
    {
        private Generator[] generators;

        private Zone()
        {
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
                var info = infos[i];

                var zone = new Zone();
                zone.generators = new Generator[info.GeneratorCount];
                Array.Copy(generators, info.GeneratorIndex, zone.generators, 0, info.GeneratorCount);

                zones[i] = zone;
            }

            return zones;
        }

        public Generator[] Generators => generators;
    }
}
