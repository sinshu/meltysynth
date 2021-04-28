using System;
using System.Collections.Immutable;
using System.IO;

namespace MeltySynth
{
    /// <summary>
    /// Represents a preset in the SoundFont.
    /// </summary>
    public sealed class Preset
    {
        internal static readonly Preset Default = new Preset();

        private readonly string name;
        private readonly int patchNumber;
        private readonly int bankNumber;
        private readonly int library;
        private readonly int genre;
        private readonly int morphology;
        private readonly ImmutableArray<PresetRegion> regions;

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

            var zoneSpan = zones.AsSpan(info.ZoneStartIndex, zoneCount);

            regions = ImmutableArray.Create(PresetRegion.Create(this, zoneSpan, instruments));
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

        /// <summary>
        /// Gets the name of the preset.
        /// </summary>
        /// <returns>
        /// The name of the preset.
        /// </returns>
        public override string ToString()
        {
            return name;
        }

        /// <summary>
        /// The name of the preset.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// The patch number of the preset.
        /// </summary>
        public int PatchNumber => patchNumber;

        /// <summary>
        /// The bank number of the preset.
        /// </summary>
        public int BankNumber => bankNumber;

        public int Library => library;
        public int Genre => genre;
        public int Morphology => morphology;

        /// <summary>
        /// The regions of the preset.
        /// </summary>
        public ImmutableArray<PresetRegion> Regions => regions;
    }
}
