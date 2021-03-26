using System;
using System.Collections.Immutable;
using System.IO;

namespace MeltySynth
{
    public sealed class Instrument
    {
        internal static readonly Instrument Default = new Instrument();

        private string name;
        private ImmutableArray<InstrumentRegion> regions;

        private Instrument()
        {
            name = "Default";
            regions = ImmutableArray.Create<InstrumentRegion>();
        }

        private Instrument(InstrumentInfo info, Zone[] zones, SampleHeader[] samples)
        {
            this.name = info.Name;

            var zoneCount = info.ZoneEndIndex - info.ZoneStartIndex + 1;
            if (zoneCount <= 0)
            {
                throw new InvalidDataException($"The instrument '{info.Name}' has no zone.");
            }

            regions = ImmutableArray.Create(InstrumentRegion.Create(this, zones.AsSpan().Slice(info.ZoneStartIndex, zoneCount), samples));
        }

        internal static Instrument[] Create(InstrumentInfo[] infos, Zone[] zones, SampleHeader[] samples)
        {
            if (infos.Length <= 1)
            {
                throw new InvalidDataException("No valid instrument was found.");
            }

            // The last one is the terminator.
            var instruments = new Instrument[infos.Length - 1];

            for (var i = 0; i < instruments.Length; i++)
            {
                instruments[i] = new Instrument(infos[i], zones, samples);
            }

            return instruments;
        }

        public override string ToString()
        {
            return name;
        }

        public string Name => name;
        public ImmutableArray<InstrumentRegion> Regions => regions;
    }
}
