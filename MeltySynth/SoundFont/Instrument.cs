using System;
using System.Collections.Generic;
using System.IO;

namespace MeltySynth.SoundFont
{
    public sealed class Instrument
    {
        private string name;
        private Zone[] zones;

        private Instrument(InstrumentInfo info, IReadOnlyList<Zone> zones)
        {
            this.name = info.Name;

            this.zones = new Zone[info.ZoneEndIndex - info.ZoneStartIndex + 1];
            for (var i = 0; i < this.zones.Length; i++)
            {
                this.zones[i] = zones[info.ZoneStartIndex + i];
            }
        }

        internal static IReadOnlyList<Instrument> Create(IReadOnlyList<InstrumentInfo> infos, IReadOnlyList<Zone> zones)
        {
            var instruments = new Instrument[infos.Count];

            for (var i = 0; i < instruments.Length; i++)
            {
                instruments[i] = new Instrument(infos[i], zones);
            }

            return instruments;
        }

        public override string ToString()
        {
            return name;
        }

        public string Name => name;
        public IReadOnlyList<Zone> Zones => zones;
    }
}
