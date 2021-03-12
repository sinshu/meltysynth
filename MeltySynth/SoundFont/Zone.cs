using System;
using System.Collections.Generic;
using System.IO;

namespace MeltySynth.SoundFont
{
    public sealed class Zone
    {
        private ModulatorParameter[] modulators;
        private GeneratorParameter[] generators;

        private Zone()
        {
        }

        internal static IReadOnlyList<Zone> Create(IReadOnlyList<ZoneInfo> infos, IReadOnlyList<ModulatorParameter> modulators, IReadOnlyList<GeneratorParameter> generators)
        {
            var zones = new Zone[infos.Count];

            for (var i = 0; i < zones.Length; i++)
            {
                var info = infos[i];

                var zone = new Zone();
                zone.generators = new GeneratorParameter[info.GeneratorCount];
                for (var j = 0; j < zone.generators.Length; j++)
                {
                    zone.generators[j] = generators[info.GeneratorIndex + j];
                }
                zone.modulators = new ModulatorParameter[info.ModulatorCount];
                for (var j = 0; j < zone.modulators.Length; j++)
                {
                    zone.modulators[j] = modulators[info.ModulatorIndex + j];
                }

                zones[i] = zone;
            }

            return zones;
        }

        public override string ToString()
        {
            return modulators.Length + " modulators, " + generators.Length + "generators";
        }

        public IReadOnlyList<ModulatorParameter> Modulators => modulators;
        public IReadOnlyList<GeneratorParameter> Generators => generators;
    }
}
