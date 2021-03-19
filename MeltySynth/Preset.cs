using System;
using System.Collections.Generic;
using System.IO;

namespace MeltySynth
{
    public sealed class Preset
    {
        private string name;
        private int patchNumber;
        private int bankNumber;
        private int library;
        private int genre;
        private int morphology;
        private PresetRegion[] regions;

        private Preset(PresetInfo info, Zone[] allZones, Instrument[] instruments)
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

            var zones = new Zone[zoneCount];
            for (var i = 0; i < zones.Length; i++)
            {
                zones[i] = allZones[info.ZoneStartIndex + i];
            }

            regions = PresetRegion.Create(this, zones, instruments);
        }

        internal static Preset[] Create(PresetInfo[] infos, Zone[] allZones, Instrument[] instruments)
        {
            if (infos.Length <= 1)
            {
                throw new InvalidDataException("No valid preset was found.");
            }

            // The last one is the terminator.
            var presets = new Preset[infos.Length - 1];

            for (var i = 0; i < presets.Length; i++)
            {
                presets[i] = new Preset(infos[i], allZones, instruments);
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
        public IReadOnlyList<PresetRegion> Regions => regions;
    }
}
