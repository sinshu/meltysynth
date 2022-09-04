﻿using System;
using System.IO;

namespace MeltySynth
{
    internal sealed class Zone
    {
        private ArraySegment<Generator> generators;

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
                zone.generators = new ArraySegment<Generator>(generators, info.GeneratorIndex, info.GeneratorCount);

                zones[i] = zone;
            }

            return zones;
        }

        public ArraySegment<Generator> Generators => generators;
    }
}
