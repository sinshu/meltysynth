using System;
using System.Collections.Generic;
using System.IO;

namespace MeltySynth
{
    public sealed class Instrument
    {
        private string name;
        private InstrumentRegion[] regions;

        private Instrument(InstrumentInfo info, Zone[] allZones, SampleHeader[] samples)
        {
            this.name = info.Name;

            var zoneCount = info.ZoneEndIndex - info.ZoneStartIndex + 1;
            if (zoneCount <= 0)
            {
                throw new InvalidDataException($"The instrument '{info.Name}' has no zone.");
            }

            var zones = new Zone[zoneCount];
            for (var i = 0; i < zones.Length; i++)
            {
                zones[i] = allZones[info.ZoneStartIndex + i];
            }

            regions = InstrumentRegion.Create(this, zones, samples);
        }

        internal static Instrument[] Create(InstrumentInfo[] infos, Zone[] allZones, SampleHeader[] samples)
        {
            if (infos.Length <= 1)
            {
                throw new InvalidDataException("No valid instrument was found.");
            }

            // The last one is the terminator.
            var instruments = new Instrument[infos.Length - 1];

            for (var i = 0; i < instruments.Length; i++)
            {
                instruments[i] = new Instrument(infos[i], allZones, samples);
            }

            return instruments;
        }

        public override string ToString()
        {
            return name;
        }

        public string Name => name;
        public IReadOnlyList<InstrumentRegion> Regions => regions;
    }
}
