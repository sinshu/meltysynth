using System;
using System.Collections.Generic;
using System.IO;

namespace MeltySynth.SoundFont
{
    public sealed class Preset
    {
        private string name;
        private int patchNumber;
        private int bankNumber;
        private int library;
        private int genre;
        private int morphology;
        private Zone[] zones;

        private Preset(PresetInfo info, IReadOnlyList<Zone> zones)
        {
            this.name = info.Name;
            this.patchNumber = info.PatchNumber;
            this.bankNumber = info.BankNumber;
            this.library = info.Library;
            this.genre = info.Genre;
            this.morphology = info.Morphology;

            this.zones = new Zone[info.ZoneEndIndex - info.ZoneStartIndex + 1];
            for (var i = 0; i < this.zones.Length; i++)
            {
                this.zones[i] = zones[info.ZoneStartIndex + i];
            }
        }

        internal static IReadOnlyList<Preset> Create(IReadOnlyList<PresetInfo> infos, IReadOnlyList<Zone> zones)
        {
            var presets = new Preset[infos.Count];

            for (var i = 0; i < presets.Length; i++)
            {
                presets[i] = new Preset(infos[i], zones);
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
        public IReadOnlyList<Zone> Zones => zones;
    }
}
