using System;
using System.Collections.Immutable;
using System.IO;

namespace MeltySynth
{
    public sealed class Preset
    {
        internal static readonly Preset Default = new Preset();

        private string name;
        private int patchNumber;
        private int bankNumber;
        private int library;
        private int genre;
        private int morphology;
        private ImmutableArray<PresetRegion> regions;

        private Preset()
        {
            name = "Default";
            regions = ImmutableArray.Create<PresetRegion>();
        }

        private Preset(PresetInfo info, Zone[] zones, Instrument[] instruments)
        {
            this.name = info.Name;
            this.patchNumber = info.PatchNumber;
            this.bankNumber = info.BankNumber;
            this.library = info.Library;
            this.genre = info.Genre;
            this.morphology = info.Morphology;

            var zoneCount = info.ZoneEndIndex - info.ZoneStartIndex + 1;
            if (zoneCount <= 0)
            {
                throw new InvalidDataException($"The preset '{info.Name}' has no zone.");
            }

            regions = ImmutableArray.Create(PresetRegion.Create(this, zones.AsSpan().Slice(info.ZoneStartIndex, zoneCount), instruments));
        }

        internal static Preset[] Create(PresetInfo[] infos, Zone[] zones, Instrument[] instruments)
        {
            if (infos.Length <= 1)
            {
                throw new InvalidDataException("No valid preset was found.");
            }

            // The last one is the terminator.
            var presets = new Preset[infos.Length - 1];

            for (var i = 0; i < presets.Length; i++)
            {
                presets[i] = new Preset(infos[i], zones, instruments);
            }

            return presets;
        }

        public override string ToString()
        {
            return name;
        }

        public string Name => name;
        public int PatchNumber => patchNumber;
        public int BankNumber => bankNumber;
        public int Library => library;
        public int Genre => genre;
        public int Morphology => morphology;
        public ImmutableArray<PresetRegion> Regions => regions;
    }
}
